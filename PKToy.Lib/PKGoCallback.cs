
using System.Numerics;
using System.Text;
using Viewer.IContract;
using static PK.UNCLASSED;

namespace PKToy.Lib;

public class BodyGeometry
{
    public List<Vector4> FaceVertices=[];
    public List<Vector3> Normals=[];
    public List<Vector4> Colors=[];
    public List<int> FaceIndices=[];
    public List<int> EdgeIndices = [];
    public List<int> FaceStartIndexArray=[];
    public List<int> EdgeStartIndexArray=[];
}

public unsafe class PKGoCallback:IDisposable
{
    const int CONTIN = (int)graphics_ifails_t.CONTIN;
    const int ABORT = (int)graphics_ifails_t.ABORT;
    private bool disposedValue;
    private readonly Dictionary<int, PartGeometry> bodyParts = [];
    private BodyGeometry currentBodyPart=new();
    private int currentBody=0;
    private int currentFace=0;
    private int faceVerticesCount=0;
    private readonly int colorTag;
    public PKGoCallback()
    {
        Frustrum.RegGoCallback(this);
        PK.ATTDEF_t colorAttdef;
        PKErrorCheck err = PK.ATTDEF.find("SDL/TYSA_COLOUR", &colorAttdef);
        colorTag = colorAttdef;
    }

    static string FormatLineType(Span<int> lntp)
    {
        if (lntp.Length == 1)
        {
            return $"[lntp0:{lntp[0]}]";
        }
        else
        {
            return $"[lntp0:{lntp[0]},lntp1:{Enum.GetName((go_line_types_t)lntp[1])}]";
        }
    }

    static string FormatTags(Span<int> tags)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        PK.CLASS_t cl;
        foreach (var tag in tags)
        {
            sb.Append(tag);
            PK.ENTITY.ask_class(tag, &cl);
            sb.Append($":{Enum.GetName(cl)},");
        }
        sb.Append(']');
        return sb.ToString();
    }

    static void PrintSegmentParams(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, [CallerMemberName] string methodName = "")
    {
        string? sgetypName=Enum.GetName((go_segment_types_t)(*segtyp));
        string tagsStr = FormatTags(new Span<int>(tags, *ntags));
        string lntpStr = FormatLineType(new Span<int>(lntp, *nlntp));
        Console.WriteLine($"MethodName:{methodName},<segtyp:{sgetypName}>,<tags:{tagsStr}>,<ngeom:{*ngeom}>,<lntp:{lntpStr}>");
    }

    int lastFaceTag=0;
    int lastFaceStartIndex=0;
    float lastFrameTag=0;


    public void GOSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        // PrintSegmentParams(segtyp, ntags, tags, ngeom, geom, nlntp, lntp);
        PKErrorCheck err;
        switch ((go_segment_types_t)(*segtyp))
        {
            case go_segment_types_t.SGTPFT:
                break;
            case go_segment_types_t.SGTPTS:
            {
                int offset=faceVerticesCount;
                float face = *(float*)tags;
                using var colorAttribs = new PKScopeArray<PK.ATTRIB_t>();
                err=PK.ENTITY.ask_attribs(tags[0],colorTag, &colorAttribs.size, &colorAttribs.data);
                var color=new Vector4(0.5882353f, 0.5882353f, 0.5882353f, 1f);
                if (colorAttribs.size > 0)
                {
                    if(colorAttribs[0] !=PK.ENTITY_t.@null)
                    {
                        using var colorValue = new PKScopeArray<double>();
                        err=PK.ATTRIB.ask_doubles(colorAttribs[0],0, &colorValue.size, &colorValue.data);
                        color = new Vector4((float)colorValue[0], (float)colorValue[1], (float)colorValue[2], 1f);
                    }
                }
                int vCount=*ngeom/2;
                double* normal=geom+vCount*3;

                if(lastFaceTag!=tags[0])
                {
                    lastFaceTag=tags[0];
                    int faceStartIndex = currentBodyPart.FaceIndices.Count;
                    var count = currentBodyPart.FaceStartIndexArray.Count;
                    float frameTag = *(float*)(&count);
                    lastFaceStartIndex=faceStartIndex;
                    lastFrameTag=frameTag;
                    currentBodyPart.FaceStartIndexArray.Add(faceStartIndex);
                }

                for (int i = 0; i < vCount; i++)
                {
                    currentBodyPart.FaceIndices.Add(faceVerticesCount);
                    faceVerticesCount++;
                    currentBodyPart.FaceVertices.Add(new((float)geom[i * 3], (float)geom[i * 3 + 1], (float)geom[i * 3 + 2], lastFrameTag));
                    currentBodyPart.Normals.Add(new((float)normal[i * 3], (float)normal[i * 3 + 1], (float)normal[i * 3 + 2]));
                    currentBodyPart.Colors.Add(color);
                }
                //插入strip重启索引
                currentBodyPart.FaceIndices.Add(Constants.STRIPBREAK);
                for(int i=1;i<*ntags;i++)
                {
                    float edge = *(float*)(tags+i);
                    if(edge!=0)
                    {
                        int v1= (i - 1) / 2 + offset;
                        int v2=i/2+1+offset;
                        currentBodyPart.EdgeIndices.Add(v1);
                        currentBodyPart.EdgeIndices.Add(v2);
                    }
                }
            }
                break;
            default:
                break;
        }
        *ifail = CONTIN;
    }

    public void GOOpenSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        // PrintSegmentParams(segtyp, ntags, tags, ngeom, geom, nlntp, lntp);
        switch ((go_segment_types_t)(*segtyp))
        {
            case go_segment_types_t.SGTPBY:
                currentBody = *tags;
                currentBodyPart = new BodyGeometry();
                faceVerticesCount = 0;
                break;
            case go_segment_types_t.SGTPFA:
                currentFace = *tags;
                // //插入strip重启索引
                // currentBodyPart.FaceIndices.Add(Constants.STRIPBREAK);
                break;
            default:
                break;
        }
        *ifail = CONTIN;
    }

    public void GOCloseSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        // PrintSegmentParams(segtyp, ntags, tags, ngeom, geom, nlntp, lntp);
        switch ((go_segment_types_t)(*segtyp))
        {
            case go_segment_types_t.SGTPBY:
                currentBodyPart.FaceStartIndexArray.Add(currentBodyPart.FaceIndices.Count);
                int edgeStartIndex = currentBodyPart.FaceIndices.Count;
                currentBodyPart.FaceIndices.AddRange(currentBodyPart.EdgeIndices);
                bodyParts[currentBody] = new PartGeometry([.. currentBodyPart.FaceVertices],
                [.. currentBodyPart.Normals],
                [.. currentBodyPart.Colors], 
                [..currentBodyPart.FaceIndices],
                edgeStartIndex,[..currentBodyPart.FaceStartIndexArray],[]);

                break;
            case go_segment_types_t.SGTPFA:
                break;
            default:
                break;
        }
        *ifail = CONTIN;
    }

    public PartGeometry GetPartGeometry(int partId)
    {
        return bodyParts[partId];
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            Frustrum.UnRegGoCallback();
            disposedValue = true;
        }
    }

    // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~PKGoCallback()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}