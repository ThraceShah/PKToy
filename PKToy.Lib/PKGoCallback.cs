
using System.Collections.Concurrent;
using System.Numerics;
using System.Text;
using Viewer.IContract;
using static PK.UNCLASSED;

namespace PKToy.Lib;


public unsafe class PKGoCallback : IDisposable
{
    const int CONTIN = (int)graphics_ifails_t.CONTIN;
    const int ABORT = (int)graphics_ifails_t.ABORT;

    class BodyGeometry
    {
        public readonly StripFacePart Shaded = new();
        public readonly EdgePart Wire = new();
    }

    class Params
    {
        public int currentBody = 0;
        public BodyGeometry curBody = new();
    }

    private bool disposedValue;

    private readonly HashSet<IGeometryData> gottenGeometries = [];
    private readonly ConcurrentDictionary<int, Params> threadIds = new();

    private readonly ConcurrentDictionary<int, StripFacePart> shadedParts = [];
    private readonly ConcurrentDictionary<int, EdgePart> wireframeParts = [];
    private readonly int colorTag;
    public PKGoCallback()
    {
        Frustrum.RegGoCallback(this);
        PK.ATTDEF_t colorAttdef;
        PK.ATTDEF.find("SDL/TYSA_COLOUR", &colorAttdef);
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
        string? sgetypName = Enum.GetName((go_segment_types_t)(*segtyp));

        string tagsStr = FormatTags(new Span<int>(tags, *ntags));
        string lntpStr = FormatLineType(new Span<int>(lntp, *nlntp));
        Console.WriteLine($"MethodName:{methodName},<segtyp:{sgetypName}>,<tags:{tagsStr}>,<ngeom:{*ngeom}>,<lntp:{lntpStr}>");
    }




    public void GOOpenSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        threadIds.TryAdd(Environment.CurrentManagedThreadId, new Params());
        var curParams = threadIds[Environment.CurrentManagedThreadId];

        // PrintSegmentParams(segtyp, ntags, tags, ngeom, geom, nlntp, lntp);
        switch ((go_segment_types_t)(*segtyp))
        {
            case go_segment_types_t.SGTPBY:
                curParams.currentBody = *tags;
                curParams.curBody = new BodyGeometry();
                break;
            case go_segment_types_t.SGTPFA:
                break;
            default:
                break;
        }
        *ifail = CONTIN;
    }

    public void GOCloseSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {

        var curParams = threadIds[Environment.CurrentManagedThreadId];

        // // PrintSegmentParams(segtyp, ntags, tags, ngeom, geom, nlntp, lntp);
        switch ((go_segment_types_t)(*segtyp))
        {
            case go_segment_types_t.SGTPBY:
                shadedParts.TryAdd(curParams.currentBody, curParams.curBody.Shaded);
                wireframeParts.TryAdd(curParams.currentBody, curParams.curBody.Wire);
                break;
            case go_segment_types_t.SGTPFA:
                break;
            default:
                break;
        }
        *ifail = CONTIN;
    }

    public void GOSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        // PrintSegmentParams(segtyp, ntags, tags, ngeom, geom, nlntp, lntp);
        var curParams = threadIds[Environment.CurrentManagedThreadId];

        switch ((go_segment_types_t)(*segtyp))
        {
            case go_segment_types_t.SGTPFT:
                break;
            case go_segment_types_t.SGTPTS:
                {
                    int faceTag = tags[0];
                    using var colorAttribs = new PKScopeArray<PK.ATTRIB_t>();
                    PK.ENTITY.ask_attribs(faceTag, colorTag, &colorAttribs.size, &colorAttribs.data);
                    Span<byte> colorRef = [150, 150, 150, 255];
                    Span<uint> color = MemoryMarshal.Cast<byte, uint>(colorRef);
                    if (colorAttribs.size > 0)
                    {
                        if (colorAttribs[0] != PK.ENTITY_t.@null)
                        {
                            using var colorValue = new PKScopeArray<double>();
                            PK.ATTRIB.ask_doubles(colorAttribs[0], 0, &colorValue.size, &colorValue.data);
                            var colorSpan = colorValue.Span;
                            colorRef[0] = (byte)(colorValue[0] * 255);
                            colorRef[1] = (byte)(colorValue[1] * 255);
                            colorRef[2] = (byte)(colorValue[2] * 255);
                        }
                    }
                    int vCount = *ngeom / 2;
                    double* normal = geom + vCount * 3;

                    if (curParams.curBody.Shaded.TagFaces.ContainsKey(faceTag) == false)
                    {
                        curParams.curBody.Shaded.TagFaces.Add(faceTag, new StripFace());
                    }
                    var tagStripFace = curParams.curBody.Shaded.TagFaces[faceTag];
                    for (int i = 0; i < vCount; i++)
                    {
                        tagStripFace.InsertNextPoint(geom + i * 3, normal + i * 3, color[0]);
                    }
                    tagStripFace.InsertNextStrip();
                    var tagEdgeBuilderMap = new Dictionary<int, EdgeBuilder>();
                    for (int i = 1; i < *ntags; i++)
                    {
                        int edgeTag = *(int*)(tags + i);
                        if (edgeTag == 0)
                        {
                            continue;
                        }
                        if (curParams.curBody.Wire.TagEdges.ContainsKey(edgeTag))
                        {
                            continue;
                        }
                        if (tagEdgeBuilderMap.ContainsKey(edgeTag) == false)
                        {
                            tagEdgeBuilderMap.Add(edgeTag, new EdgeBuilder());
                        }
                        var tagEdgeBuilder = tagEdgeBuilderMap[edgeTag];
                        int v1 = (i - 1) / 2;
                        int v2 = i / 2 + 1;
                        tagEdgeBuilder.InsertNextPoint(geom + v1 * 3);
                        tagEdgeBuilder.InsertNextPoint(geom + v2 * 3);
                    }
                    foreach (var (edgeTag, edgeBuilder) in tagEdgeBuilderMap)
                    {
                        curParams.curBody.Wire.TagEdges.Add(edgeTag, edgeBuilder.Build());
                    }
                }
                break;
            default:
                break;
        }
        *ifail = CONTIN;
    }


    public IGeometryData GetShadedGeometry(int partId)
    {
        var part = shadedParts[partId];
        if (gottenGeometries.Add(part))
        {
            part.Modified();
        }
        return part;
    }

    public IGeometryData GetWireframeGeometry(int partId)
    {
        var part = wireframeParts[partId];
        if (gottenGeometries.Add(part))
        {
            part.Modified();
        }
        return part;
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