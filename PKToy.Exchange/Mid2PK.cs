using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Exchange.Midlayer;

namespace PKToy.Exchange;

public unsafe class Mid2PK
{
    public static PK.PARTITION_t ResolveMid2PK(MidMgr midMgr)
    {
        var midBodies = midMgr.GetMidObjs<IBodyObj>().ToArray();
        if (midBodies.Length == 0)
        {
            return PK.PARTITION_t.@null;
        }
        PK.PARTITION_t curPartition;
        PK.SESSION.ask_curr_partition(&curPartition);
        PK.PARTITION_t partition;
        PK.PARTITION.create_empty(&partition);
        PK.PARTITION.set_current(partition);
        foreach (var midObj in midBodies)
        {
            var pkBody = MidBody2PK(midObj);
            Console.WriteLine($"MidBody #{midObj.ImpId} -> PKBody #{pkBody.Value}");
        }
        PK.PARTITION.set_current(curPartition);
        return partition;
    }

    private static PK.BODY_t MidBody2PK(IBodyObj midBody)
    {
        Dictionary<ITopoObj, IGeoObj> topoGeoMap = [];
        Dictionary<ITopoObj, int> topoIdMap = new() { { midBody, 0 } };
        var pkTopoClassList = new List<PK.CLASS_t>() { PK.CLASS_t.body };
        var pkParentList = new List<int>();
        var pkChildList = new List<int>();
        var pkSenceList = new List<PK.TOPOL.sense_t>();
        Queue<ITopoObj> topoQueue = new();
        topoQueue.Enqueue(midBody);
        while (topoQueue.Count > 0)
        {
            var parentObj = topoQueue.Dequeue();
            var mid2pkData = parentObj.GetChildren();
            if (mid2pkData is null)
            {
                continue;
            }
            var pkParentId = topoIdMap[parentObj];
            for (var i = 0; i < mid2pkData.TopoChildren.Length; i++)
            {
                var childObj = mid2pkData.TopoChildren[i];
                if (topoIdMap.TryGetValue(childObj, out var pkChildId) is false)
                {
                    pkChildId = pkTopoClassList.Count;
                    topoIdMap[childObj] = pkChildId;
                    topoQueue.Enqueue(childObj);
                    var pkChildClass = GetTopoPKClass(childObj);
                    pkTopoClassList.Add(pkChildClass);
                }
                pkParentList.Add(pkParentId);
                pkChildList.Add(pkChildId);
                pkSenceList.Add(mid2pkData.Sense[i]);
            }
            if (mid2pkData.GeomChildren is not null)
            {
                topoGeoMap[parentObj] = mid2pkData.GeomChildren;
            }
        }
        // PrintTopoTable(pkTopoClassList, pkParentList, pkChildList, pkSenceList);
        var pkTopoClasses = PrivateAccessor.GetListInternalArray(pkTopoClassList).AsSpan();
        var pkParents = PrivateAccessor.GetListInternalArray(pkParentList).AsSpan();
        var pkChildren = PrivateAccessor.GetListInternalArray(pkChildList).AsSpan();
        var pkSences = PrivateAccessor.GetListInternalArray(pkSenceList).AsSpan();
        var nTopols = pkTopoClassList.Count;
        var nRelations = pkParentList.Count;
        PK.BODY.create_topology_2_r_t r;
        PK.BODY.create_topology_2_o_t op = new(GetBodyType(midBody));
        PK.BODY.create_topology_2(nTopols, ref pkTopoClasses[0], nRelations, ref pkParents[0], ref pkChildren[0], ref pkSences[0], &op, &r);
        if (r.body == PK.BODY_t.@null)
        {
            PK.BODY.create_topology_2_r_f(&r);
            return PK.BODY_t.@null;
        }
        var pkBody = r.body;
        var pkTopolGeomMap = new Dictionary<ITopoObj, (PK.TOPOL_t, IGeoObj)>(topoGeoMap.Count);
        foreach (var (topoObj, geoObj) in topoGeoMap)
        {
            var pkTopl = r.topols[topoIdMap[topoObj]];
            pkTopolGeomMap[topoObj] = (pkTopl, geoObj);
        }
        ProcessTopoGeom(pkTopolGeomMap);
        PK.BODY.create_topology_2_r_f(&r);
        PK.BODY.type_t bodyType;
        PK.BODY.ask_type(pkBody, &bodyType);
        return pkBody;
    }

    private static void ProcessTopoGeom(Dictionary<ITopoObj, (PK.TOPOL_t, IGeoObj)> pkTopolGeomMap)
    {
        foreach (var (topoObj, (pkTopo, midGeom)) in pkTopolGeomMap)
        {
            var pkGeom = MidGeom2PKTool.MidGeom2PK(midGeom);
            if (pkGeom == PK.GEOM_t.@null)
            {
                Console.WriteLine($"Convert MidGeom #{midGeom.ImpId} to PK failed");
                continue;
            }
            ProcessTopoGeom(topoObj, pkTopo, pkGeom);
        }
    }

    private static void ProcessTopoGeom(ITopoObj topoObj, int pkTopol, int pkGeom)
    {
        switch (topoObj)
        {
            case FaceObj midFace:
                {
                    PK.LOGICAL_t sense = midFace.Sence;
                    PK.FACE.attach_surfs(1, (PK.FACE_t*)&pkTopol, (PK.SURF_t*)&pkGeom, &sense);
                    break;
                }
            case EdgeObj midEdge:
                {
                    PK.EDGE.attach_curves(1, (PK.EDGE_t*)&pkTopol, (PK.CURVE_t*)&pkGeom);
                    break;
                }
            case VertexObj midVertex:
                {
                    PK.VERTEX.attach_points(1, (PK.VERTEX_t*)&pkTopol, (PK.POINT_t*)&pkGeom);
                    break;
                }
            default:
                throw new NotImplementedException($"Unknown topo class: {topoObj.GetType()}");
        }
    }

    private static PK.BODY.type_t GetBodyType(IBodyObj midBody) => midBody switch
    {
        SolidBodyObj => PK.BODY.type_t.solid_c,
        SheetBodyObj => PK.BODY.type_t.sheet_c,
        WireBodyObj => PK.BODY.type_t.wire_c,
        AcornBodyObj => PK.BODY.type_t.acorn_c,
        _ => throw new NotImplementedException($"Unknown body type: {midBody.GetType()}")
    };

    private static PK.CLASS_t GetTopoPKClass(ITopoObj topoObj) => topoObj switch
    {
        IBodyObj => PK.CLASS_t.body,
        IRegionObj => PK.CLASS_t.region,
        IShellObj => PK.CLASS_t.shell,
        FaceObj => PK.CLASS_t.face,
        LoopObj => PK.CLASS_t.loop,
        FinObj => PK.CLASS_t.fin,
        EdgeObj => PK.CLASS_t.edge,
        VertexObj => PK.CLASS_t.vertex,
        _ => throw new NotImplementedException($"Unknown topo class: {topoObj.GetType()}")
    };

    private static void PrintTopoTable(List<PK.CLASS_t> pkTopoClassList, List<int> pkParentList, List<int> pkChildList, List<PK.TOPOL.sense_t> pkSenceList)
    {
        Console.WriteLine($"topols count: {pkTopoClassList.Count}");
        for (var i = 0; i < pkTopoClassList.Count; i++)
        {
            Console.Write($"[{i}] {pkTopoClassList[i]}: ");
        }
        Console.WriteLine();
        Console.WriteLine($"relations count: {pkParentList.Count}");
        PK.CLASS_t lastType = PK.CLASS_t.@null;
        for (var i = 0; i < pkParentList.Count; i++)
        {
            var pkParent = pkParentList[i];
            var pkChild = pkChildList[i];
            var pClass = pkTopoClassList[pkParent];
            var cClass = pkTopoClassList[pkChild];
            if (lastType != pClass)
            {
                Console.WriteLine();
                lastType = pClass;
            }
            Console.WriteLine($"[{i}] {pkParent}:{pClass} - {pkChild}:{cClass}-> {pkSenceList[i]}");
        }
    }
}

public static class PrivateAccessor
{
    public static void Init()
    { }

    public static T[] GetListInternalArray<T>(List<T> a)
    {
        return ((Func<List<T>, T[]>)cache[typeof(T)])(a);
    }

    static void Compile<T>()
    {
        var parameter = Expression.Parameter(typeof(List<T>), "x");
        var fieldInfo = typeof(List<T>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new Exception($"Field '_items' not found in List<{typeof(T)}>");
        var field = Expression.Field(parameter, fieldInfo);
        var lambda = Expression.Lambda<Func<List<T>, T[]>>(field, parameter);
        var func = lambda.Compile();
        cache[typeof(T)] = func;
    }

    static PrivateAccessor()
    {
        Compile<int>();
        Compile<PK.CLASS_t>();
        Compile<PK.TOPOL.sense_t>();
    }

    static readonly Dictionary<Type, Delegate> cache = [];
}
