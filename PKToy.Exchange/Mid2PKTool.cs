using Exchange.Midlayer;
using Exchange.Step2Mid;

namespace PKToy.Exchange;

record Mid2PKData(ITopoObj[] TopoChildren, IGeoObj? GeomChildren, PK.TOPOL.sense_t[] Sense);

static class Mid2PKTool
{
    public static Mid2PKData? GetChildren(this ITopoObj topoObj) => topoObj switch
    {
        IBodyObj solidBodyObj => GetMid2PKData(solidBodyObj),
        IRegionObj regionObj => GetMid2PKData(regionObj),
        FaceShellObj shellObj => GetMid2PKData(shellObj),
        WireShellObj shellObj => GetMid2PKData(shellObj),
        VertexShellObj shellObj => GetMid2PKData(shellObj),
        FaceObj faceObj => GetMid2PKData(faceObj),
        LoopObj loopObj => GetMid2PKData(loopObj),
        FinObj finObj => GetMid2PKData(finObj),
        EdgeObj edgeObj => GetMid2PKData(edgeObj),
        VertexObj vertexObj => GetMid2PKData(vertexObj),
        _ => null,
    };

    private static T[] CreateArray<T>(int length, T initValue)
    {
        var array = new T[length];
        for (var i = 0; i < length; i++)
        {
            array[i] = initValue;
        }
        return array;
    }

    private static Mid2PKData? GetMid2PKData(IBodyObj solidBodyObj)
    {
        var regions = solidBodyObj.Regions;
        if (regions is null || regions.Length == 0)
        {
            return null;
        }
        var sences = regions.Select(region => region switch
        {
            SolidRegionObj solidRegion => PK.TOPOL.sense_t.positive_c,
            VoidRegionObj voidRegion => PK.TOPOL.sense_t.negative_c,
            BoundRegionObj boundRegion => PK.TOPOL.sense_t.negative_c,
            _ => PK.TOPOL.sense_t.none_c,
        }).ToArray();
        return new(regions, null, sences);
    }

    private static Mid2PKData? GetMid2PKData(IRegionObj regionObj)
    {
        var shells = regionObj.Shells;
        if (shells is null || shells.Length == 0)
        {
            return null;
        }
        return new(shells, null, CreateArray(shells.Length, PK.TOPOL.sense_t.none_c));
    }

    private static Mid2PKData? GetMid2PKData(FaceShellObj shellObj)
    {
        var faces = shellObj.Faces;
        if (faces is null || faces.Length == 0)
        {
            return null;
        }
        var sences = new PK.TOPOL.sense_t[faces.Length];
        if (shellObj.Closed)
        {
            if (shellObj.Oriented)
            {
                for (var i = 0; i < faces.Length; i++)
                {
                    sences[i] = PK.TOPOL.sense_t.positive_c;
                }
            }
            else
            {
                for (var i = 0; i < faces.Length; i++)
                {
                    sences[i] = PK.TOPOL.sense_t.negative_c;
                }
            }
        }
        else
        {
            for (var i = 0; i < faces.Length; i++)
            {
                sences[i] = PK.TOPOL.sense_t.none_c;
            }
        }
        return new(faces, null, sences);
    }

    private static Mid2PKData? GetMid2PKData(WireShellObj shellObj)
    {
        var edges = shellObj.Edges;
        if (edges is null || edges.Length == 0)
        {
            return null;
        }
        return new(edges, null, CreateArray(edges.Length, PK.TOPOL.sense_t.none_c));
    }

    private static Mid2PKData? GetMid2PKData(VertexShellObj shellObj)
    {
        var vertex = shellObj.Vertex;
        if (vertex is null)
        {
            return null;
        }
        return new([vertex], null, [PK.TOPOL.sense_t.none_c]);
    }

    private static Mid2PKData? GetMid2PKData(FaceObj faceObj)
    {
        var loops = faceObj.Loops;
        if (loops is null || loops.Length == 0)
        {
            return null;
        }
        return new(loops, faceObj.Surf, CreateArray(loops.Length, PK.TOPOL.sense_t.none_c));
    }

    private static Mid2PKData? GetMid2PKData(LoopObj loopObj)
    {
        var fins = loopObj.Fins;
        if (fins is null || fins.Length == 0)
        {
            return null;
        }
        return new(fins, null, CreateArray(fins.Length, PK.TOPOL.sense_t.none_c));
    }

    private static Mid2PKData? GetMid2PKData(FinObj finObj)
    {
        var edge = finObj.Edge;
        if (edge is null)
        {
            return null;
        }
        var sence = finObj.Orientation ? PK.TOPOL.sense_t.positive_c : PK.TOPOL.sense_t.negative_c;
        return new([edge], null, [sence]);
    }

    private static Mid2PKData? GetMid2PKData(EdgeObj edgeObj)
    {
        var topoChildren = new List<ITopoObj>();
        if (edgeObj.Start is not null)
        {
            topoChildren.Add(edgeObj.Start);
        }
        if (edgeObj.End is not null)
        {
            topoChildren.Add(edgeObj.End);
        }
        var sences = CreateArray(topoChildren.Count, PK.TOPOL.sense_t.none_c);
        return new([.. topoChildren], edgeObj.Curve, sences);
    }

    private static Mid2PKData? GetMid2PKData(VertexObj vertexObj)
    {
        return new([], vertexObj.Point, []);
    }

}