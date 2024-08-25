
using System.Numerics;
using System.Text;
using Viewer.IContract;
using static PK.UNCLASSED;

namespace PKToy.Lib;

public class BodyGeometry
{
    public List<Vector4> FaceVertices=[];
    public List<int> FaceIndices=[];
    public List<Vector4> EdgeVertices = [];
    public List<int> EdgeIndices = [];

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
        PrintSegmentParams(segtyp, ntags, tags, ngeom, geom, nlntp, lntp);
        switch ((go_segment_types_t)(*segtyp))
        {
            case go_segment_types_t.SGTPFT:
                float face=*(float*)tags;
                for (int i = 0; i < 3; i++)
                {
                    currentBodyPart.FaceIndices.Add(currentBodyPart.FaceVertices.Count);
                    currentBodyPart.FaceVertices.Add(new ((float)geom[i * 3], (float)geom[i * 3 + 1], (float)geom[i * 3 + 2], face));
                }
                float edge1=*(float*)(tags+1);
                float edge2=*(float*)(tags+2);
                float edge3=*(float*)(tags+3);
                if(edge1!=0)
                {
                    currentBodyPart.EdgeIndices.Add(currentBodyPart.EdgeVertices.Count);
                    currentBodyPart.EdgeVertices.Add(new ((float)geom[6],(float)geom[7],(float)geom[8],edge1));
                    currentBodyPart.EdgeIndices.Add(currentBodyPart.EdgeVertices.Count);
                    currentBodyPart.EdgeVertices.Add(new ((float)geom[0],(float)geom[1],(float)geom[2],edge1));
                }
                if(edge2!=0)
                {
                    currentBodyPart.EdgeIndices.Add(currentBodyPart.EdgeVertices.Count);
                    currentBodyPart.EdgeVertices.Add(new((float)geom[0], (float)geom[1], (float)geom[2], edge2));
                    currentBodyPart.EdgeIndices.Add(currentBodyPart.EdgeVertices.Count);
                    currentBodyPart.EdgeVertices.Add(new ((float)geom[3],(float)geom[4],(float)geom[5],edge2));
                }
                if(edge3!=0)
                {
                    currentBodyPart.EdgeIndices.Add(currentBodyPart.EdgeVertices.Count);
                    currentBodyPart.EdgeVertices.Add(new ((float)geom[3],(float)geom[4],(float)geom[5], edge3));
                    currentBodyPart.EdgeIndices.Add(currentBodyPart.EdgeVertices.Count);
                    currentBodyPart.EdgeVertices.Add(new ((float)geom[6],(float)geom[7],(float)geom[8],edge3));
                }
                break;
            case go_segment_types_t.SGTPTS:
                break;
            default:
                break;
        }
        *ifail = CONTIN;
    }

    public void GOOpenSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        PrintSegmentParams(segtyp, ntags, tags, ngeom, geom, nlntp, lntp);
        switch ((go_segment_types_t)(*segtyp))
        {
            case go_segment_types_t.SGTPBY:
                currentBody = *tags;
                currentBodyPart = new BodyGeometry();
                break;
            case go_segment_types_t.SGTPFA:
                currentFace = *tags;
                break;
            default:
                break;
        }
        *ifail = CONTIN;
    }

    public void GOCloseSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        PrintSegmentParams(segtyp, ntags, tags, ngeom, geom, nlntp, lntp);
        switch ((go_segment_types_t)(*segtyp))
        {
            case go_segment_types_t.SGTPBY:
                Console.WriteLine($"edgeVerticesCount:{currentBodyPart.EdgeVertices.Count}");
                Console.WriteLine($"edgeIndicesCount:{currentBodyPart.EdgeIndices.Count}");
                currentBodyPart.EdgeIndices.Add(currentBodyPart.EdgeVertices.Count);
                currentBodyPart.EdgeIndices.Add(currentBodyPart.EdgeVertices.Count);
                currentBodyPart.EdgeVertices.Add(Vector4.Zero);
                int edgeStartIndex =currentBodyPart.FaceIndices.Count;
                currentBodyPart.FaceVertices.AddRange(currentBodyPart.EdgeVertices);
                var indices=new int[currentBodyPart.FaceIndices.Count+currentBodyPart.EdgeIndices.Count];
                currentBodyPart.FaceIndices.CopyTo(indices);
                for (int i = 0; i < currentBodyPart.EdgeIndices.Count; i++)
                {
                    indices[edgeStartIndex+i]=currentBodyPart.EdgeIndices[i]+edgeStartIndex;
                }
                bodyParts[currentBody] = new PartGeometry([.. currentBodyPart.FaceVertices], indices,
                edgeStartIndex,[],[],[],[]);
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