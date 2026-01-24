using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Exchange.Midlayer;
using NativeCorLib;

namespace PKToy.Exchange;

public unsafe class Mid2PK
{
    public static PK_PARTITION_t ResolveMid2PK(MidMgr midMgr)
    {
        var midBodies = midMgr.GetMidObjs<IBodyObj>().ToArray();
        if (midBodies.Length == 0)
        {
            return NULTAG;
        }
        PK_PARTITION_t curPartition;
        PK_SESSION_ask_curr_partition(&curPartition);
        PK_PARTITION_t partition;
        PK_PARTITION_create_empty(&partition);
        PK_PARTITION_set_current(partition);
        var builder = new KernelBodyBuilder(midMgr);
        foreach (var midObj in midBodies)
        {
            var pkBody = builder.Build(midObj);
            Console.WriteLine($"MidBody #{midObj.ImpId} -> PKBody #{pkBody}");
        }
        PK_PARTITION_set_current(curPartition);
        return partition;
    }

    internal class KernelBodyBuilder(MidMgr midMgr)
    {
        bool cleared = true;
        readonly Dictionary<ITopolObj, int> topolIdMap = [];
        readonly Dictionary<VertexObj, IPointObj> vertexPointMap = [];
        readonly Dictionary<EdgeObj, ICurveObj> edgeCurveMap = [];
        readonly Dictionary<FaceObj, ISurfaceObj> faceSurfMap = [];
        readonly MidGeom2PKTool geom2PKTool = new();
        private void Clear()
        {
            topolIdMap.Clear();
            vertexPointMap.Clear();
            edgeCurveMap.Clear();
            faceSurfMap.Clear();
            cleared = true;
        }
        public PK_BODY_t Build(IBodyObj midBody)
        {
            if (cleared is false)
            {
                Clear();
            }
            geom2PKTool.Unit = midBody.Unit;
            var body = BuildTopol(midBody);
            if (body == NULTAG)
            {
                return body;
            }
            ProcessTopolGeom(body);
            cleared = false;
            return body;
        }

        private void ProcessTopolGeom<TTopol, TGeom>(Dictionary<TTopol, TGeom> topoGeoMap,
        Action<UMList<PK_TOPOL_t>, UMList<PK_GEOM_t>, UMList<PK_LOGICAL_t>> attachFunc, Func<TTopol, bool> getSenseFunc) where TTopol : ITopolObj where TGeom : IGeoObj
        {
            using var pkTopolList = new UMList<PK_TOPOL_t>(topoGeoMap.Count);
            using var pkGeomList = new UMList<PK_GEOM_t>(topoGeoMap.Count);
            using var pkSenseList = new UMList<PK_LOGICAL_t>(topoGeoMap.Count);
            foreach (var (topoObj, geoObj) in topoGeoMap)
            {
                var pkTopol = topoObj.ExpId;
                var pkGeom = geom2PKTool.MidGeom2PK(geoObj);
                if (pkGeom == NULTAG)
                {
                    Console.WriteLine($"Convert MidGeom #{geoObj.ImpId} to PK failed");
                    continue;
                }
                midMgr.SetObjExpId(topoObj, pkTopol);
                pkTopolList.Add(pkTopol.Id);
                pkGeomList.Add(pkGeom);
                pkSenseList.Add(getSenseFunc(topoObj) ? PK_LOGICAL_true : PK_LOGICAL_false);
            }
            attachFunc(pkTopolList, pkGeomList, pkSenseList);
        }

        private static void VertexAttachPoints(UMList<PK_TOPOL_t> pkTopolList, UMList<PK_GEOM_t> pkGeomList, UMList<PK_LOGICAL_t> pkSenseList)
        {
            PK_VERTEX_attach_points(pkTopolList.Count, (PK_VERTEX_t*)pkTopolList.Data, (PK_POINT_t*)pkGeomList.Data);
        }

        private static void EdgeAttachCurves(UMList<PK_TOPOL_t> pkTopolList, UMList<PK_GEOM_t> pkGeomList, UMList<PK_LOGICAL_t> pkSenseList)
        {
            PK_EDGE_attach_curves_o_t op = new()
            {
                have_senses = PK_LOGICAL_true,
                senses = pkSenseList.Data
            };
            using var rt = new PKScopeData<PK_ENTITY_track_r_t>(&PK_ENTITY_track_r_f);
            PK_EDGE_attach_curves_2(pkTopolList.Count, (PK_EDGE_t*)pkTopolList.Data, (PK_CURVE_t*)pkGeomList.Data, &op, &rt.data);
            if (rt.data.n_track_records > 0)
            {
                Console.WriteLine($"Edge attach curves has {rt.data.n_track_records} records");
            }
        }

        private static void FaceAttachSurfs(UMList<PK_TOPOL_t> pkTopolList, UMList<PK_GEOM_t> pkGeomList, UMList<PK_LOGICAL_t> pkSenseList)
        {
            PK_FACE_attach_surfs(pkTopolList.Count, (PK_FACE_t*)pkTopolList.Data, (PK_SURF_t*)pkGeomList.Data, pkSenseList.Data);
        }


        private void ProcessTopolGeom(PK_BODY_t pkBody)
        {
            ProcessTopolGeom(vertexPointMap, VertexAttachPoints, topoObj => true);
            ProcessTopolGeom(edgeCurveMap, EdgeAttachCurves, topoObj => topoObj.Sense);
            ProcessTopolGeom(faceSurfMap, FaceAttachSurfs, topoObj => topoObj.Sence);
            PK_BODY_check_o_t bodyCheckOp = new();
            using PKScopeArray<PK_check_fault_t> faults = new();
            PK_BODY_check(pkBody, &bodyCheckOp, &faults.size, &faults.data);
            if (faults.size > 0)
            {
                Console.WriteLine($"Check pk body #{pkBody} has {faults.size} faults");
                for (var i = 0; i < faults.size; i++)
                {
                    PrintCheckFault(faults[i]);
                    ProcessBodyCheckResult(faults[i]);
                }
            }
            else
            {
                Console.WriteLine($"Check pk body #{pkBody} successfully");
            }
        }

        private static void ProcessBadEdge(PK_check_fault_t fault)
        {
            PK_EDGE_repair_o_t op = new();
            op.max_tolerance = 2e-5;
            using var rt = new PKScopeData<PK_TOPOL_track_r_t>(&PK_TOPOL_track_r_f);
            PK_EDGE_repair(1, (PK_EDGE_t*)&fault.entity_2, &op, &rt.data);
            if (rt.data.n_track_records > 0)
            {
                Console.WriteLine($"Repair edge #{fault.entity_2} has {rt.data.n_track_records} records");
            }
        }

        private static void ProcessBodyCheckResult(PK_check_fault_t fault)
        {
            switch (fault.state)
            {
                case PK_FACE_state_bad_edge_c:
                {
                    ProcessBadEdge(fault);
                    break;
                }
                default:
                    {
                        Console.WriteLine($"Unimplemented check fault repair: {fault.state}");
                        break;
                    }
            }
        }

        private PK_BODY_t BuildTopol(IBodyObj midBody)
        {
            topolIdMap[midBody] = 0;
            var capacity = 80;
            using var pkTopoClassList = new UMList<PK_CLASS_t>(capacity);
            pkTopoClassList.Add(PK_CLASS_body);
            using var pkParentList = new UMList<int>(capacity);
            using var pkChildList = new UMList<int>(capacity);
            using var pkSenceList = new UMList<PK_TOPOL_sense_t>(capacity);
            Queue<ITopolObj> topoQueue = new();
            topoQueue.Enqueue(midBody);
            while (topoQueue.Count > 0)
            {
                var parentObj = topoQueue.Dequeue();
                var mid2pkData = parentObj.GetChildren();
                if (mid2pkData is null)
                {
                    continue;
                }
                var pkParentId = topolIdMap[parentObj];
                for (var i = 0; i < mid2pkData.TopoChildren.Length; i++)
                {
                    var childObj = mid2pkData.TopoChildren[i];
                    if (topolIdMap.TryGetValue(childObj, out var pkChildId) is false)
                    {
                        pkChildId = pkTopoClassList.Count;
                        topolIdMap[childObj] = pkChildId;
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
                    MatchTopolGeom(parentObj, mid2pkData.GeomChildren);
                }
            }
            PrintTopoTable(pkTopoClassList, pkParentList, pkChildList, pkSenceList);
            var nTopols = pkTopoClassList.Count;
            var nRelations = pkParentList.Count;
            using var r = new PKScopeData<PK_BODY_create_topology_2_r_t>(&PK_BODY_create_topology_2_r_f);
            PK_BODY_create_topology_2_o_t op = new(GetBodyType(midBody));
            PK_BODY_create_topology_2(nTopols, pkTopoClassList.Data, nRelations, pkParentList.Data, pkChildList.Data, pkSenceList.Data, &op, &r.data);
            SetTopolTags(r.data.topols, r.data.n_topols);
            PrintCreateTopolFaults(midBody, r.data);
            return r.data.body;
        }

        private void MatchTopolGeom(ITopolObj topol, IGeoObj geom)
        {
            switch (topol)
            {
                case FaceObj midFace:
                    {
                        if (geom is ISurfaceObj midSurf)
                        {
                            faceSurfMap[midFace] = midSurf;
                        }
                        break;
                    }
                case EdgeObj midEdge:
                    {
                        if (geom is ICurveObj midCurve)
                        {
                            edgeCurveMap[midEdge] = midCurve;
                        }
                        break;
                    }
                case VertexObj midVertex:
                    {
                        if (geom is IPointObj midPoint)
                        {
                            vertexPointMap[midVertex] = midPoint;
                        }
                        break;
                    }
                default:
                    throw new NotImplementedException($"Unknown topo class: {topol.GetType()}");
            }
        }
        private void SetTopolTags(PK_TOPOL_t* topols, int nTopols)
        {
            if (nTopols == 0)
            {
                return;
            }
            foreach (var (midTopol, id) in this.topolIdMap)
            {
                midMgr.SetObjExpId(midTopol, topols[id]);
            }
        }


    }


    private static PK_BODY_type_t GetBodyType(IBodyObj midBody) => midBody switch
    {
        SolidBodyObj => PK_BODY_type_solid_c,
        SheetBodyObj => PK_BODY_type_sheet_c,
        WireBodyObj => PK_BODY_type_wire_c,
        AcornBodyObj => PK_BODY_type_acorn_c,
        _ => throw new NotImplementedException($"Unknown body type: {midBody.GetType()}")
    };

    private static PK_CLASS_t GetTopoPKClass(ITopolObj topoObj) => topoObj switch
    {
        IBodyObj => PK_CLASS_body,
        IRegionObj => PK_CLASS_region,
        IShellObj => PK_CLASS_shell,
        FaceObj => PK_CLASS_face,
        ILoopObj => PK_CLASS_loop,
        FinObj => PK_CLASS_fin,
        EdgeObj => PK_CLASS_edge,
        VertexObj => PK_CLASS_vertex,
        _ => throw new NotImplementedException($"Unknown topo class: {topoObj.GetType()}")
    };

    private static void PrintTopoTable(Span<PK_CLASS_t> pkTopoClassList, Span<int> pkParentList, Span<int> pkChildList, Span<PK_TOPOL_sense_t> pkSenceList)
    {
        Console.WriteLine($"topols count: {pkTopoClassList.Length}");
        for (var i = 0; i < pkTopoClassList.Length; i++)
        {
            Console.Write($"[{i}] {pkTopoClassList[i]}: ");
        }
        Console.WriteLine();
        Console.WriteLine($"relations count: {pkParentList.Length}");
        PK_CLASS_t lastType = NULTAG;
        for (var i = 0; i < pkParentList.Length; i++)
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

    private static void PrintCreateTopolFaults(IBodyObj midBody, PK_BODY_create_topology_2_r_t crt)
    {
        if (crt.n_create_faults == 0)
        {
            Console.WriteLine($"Create midbody #{midBody.ImpId} topology successfully, body tag: #{crt.body}");
            return;
        }
        Console.WriteLine($"Create midbody #{midBody.ImpId} topology ,faults count: {crt.n_create_faults},body tag: {crt.body}");
        for (var i = 0; i < crt.n_create_faults; i++)
        {
            var fault = crt.create_faults[i];
            PrintCreateFault(fault);
        }
    }

    private static void PrintCreateFault(PK_create_fault_t fault)
    {
        Console.Write($"state: {fault.state}, indices:[");
        for (var i = 0; i < fault.n_indices; i++)
        {
            var index = fault.indices[i];
            Console.Write($" {index},");
        }
        Console.WriteLine("]");
    }

    private static void PrintCheckFault(PK_check_fault_t fault)
    {
        var posStr = $"[{fault.position.coord[0]},{fault.position.coord[1]},{fault.position.coord[2]}]";
        Console.WriteLine($"Check fault: {fault.state}, entity1:{fault.entity_1},entity2: {fault.entity_2}, postion{posStr}");
    }


}

