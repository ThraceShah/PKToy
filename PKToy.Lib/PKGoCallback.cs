
using System.Numerics;
using System.Text;
using Viewer.IContract;
using static PK.UNCLASSED;

namespace PKToy.Lib;

public class BodyGeometry
{
    public List<Vector4> FaceVertices=[];
    public List<Vector3> Normals=[];
    public List<uint> FaceIndices=[];
    public List<uint> EdgeIndices = [];

}

public unsafe class PKGoCallback:IDisposable
{
    const int CONTIN = (int)graphics_ifails_t.CONTIN;
    const int ABORT = (int)graphics_ifails_t.ABORT;
    private bool disposedValue;
    private Dictionary<int, PartGeometry> bodyParts = [];
    private BodyGeometry currentBodyPart=new();
    private int currentBody=0;
    private int currentFace=0;
    private uint faceVerticesCount=0;
    public PKGoCallback()
    {
        Frustrum.RegGoCallback(this);
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

    public void GOSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        // PrintSegmentParams(segtyp, ntags, tags, ngeom, geom, nlntp, lntp);
        switch ((go_segment_types_t)(*segtyp))
        {
            case go_segment_types_t.SGTPFT:
                break;
            case go_segment_types_t.SGTPTS:
            {
                uint offset=faceVerticesCount;
                float face = *(float*)tags;
                int vCount=*ngeom/2;
                double* normal=geom+vCount*3;
                for (int i = 0; i < vCount; i++)
                {
                    currentBodyPart.FaceIndices.Add(faceVerticesCount);
                    faceVerticesCount++;
                    currentBodyPart.FaceVertices.Add(new((float)geom[i * 3], (float)geom[i * 3 + 1], (float)geom[i * 3 + 2], face));
                    currentBodyPart.Normals.Add(new((float)normal[i * 3], (float)normal[i * 3 + 1], (float)normal[i * 3 + 2]));
                }
                //插入strip重启索引
                currentBodyPart.FaceIndices.Add(0xFFFFFFFF);
                for(uint i=1;i<*ntags;i++)
                {
                    float edge = *(float*)(tags+i);
                    if(edge!=0)
                    {
                        uint v1= (i - 1) / 2 + offset;
                        uint v2=i/2+1+offset;
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
                // currentBodyPart.FaceIndices.Add(0xFFFFFFFF);
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
                uint edgeStartIndex = (uint)currentBodyPart.FaceIndices.Count;
                currentBodyPart.FaceIndices.AddRange(currentBodyPart.EdgeIndices);
                bodyParts[currentBody] = new PartGeometry([.. currentBodyPart.FaceVertices],
                [.. currentBodyPart.Normals], 
                [..currentBodyPart.FaceIndices],
                edgeStartIndex,[],[]);

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