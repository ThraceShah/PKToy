#if AP_VERSION_203
namespace Exchange.Step2Mid.Ap203;
#endif
#if AP_VERSION_203E2
namespace Exchange.Step2Mid.Ap203e2;
#endif
#if AP_VERSION_214E3
namespace Exchange.Step2Mid.Ap214e3;
#endif
public class Step2Mid
{
    public static MidMgr ResolveStep2Mid(string stepFile)
    {
        var stepCreator = new StepObjCreator();
        var stepParser = new StepParser(stepCreator);
        stepParser.Resolve(stepFile);
        var stepObjs = stepParser.GetStepObjs();
        var stepIdObjMap = new Dictionary<int, IStepObj>(stepObjs.Length);
        var stepPoints = new List<cartesian_point>();
        var stepLines = new List<curve>();
        var stepPlanes = new List<surface>();
        var stepVertices = new List<vertex_point>();
        var stepEdges = new List<edge_curve>();
        var stepOrientedEdges = new List<oriented_edge>();
        var stepLoops = new List<loop>();
        var stepFaces = new List<face_surface>();
        var stepFaceShells = new List<connected_face_set>();
        var stepManifoldSolids = new List<manifold_solid_brep>();
        var shapeRes = new List<shape_representation>();
        foreach (var stepObj in stepObjs)
        {
            if (stepObj is null)
            {
                continue;
            }
            stepIdObjMap.Add(stepObj.line_id, stepObj);
            switch (stepObj)
            {
                case cartesian_point stepPoint:
                    stepPoints.Add(stepPoint);
                    break;
                case curve stepLine:
                    stepLines.Add(stepLine);
                    break;
                case surface stepPlane:
                    stepPlanes.Add(stepPlane);
                    break;
                case vertex_point stepVertex:
                    stepVertices.Add(stepVertex);
                    break;
                case edge_curve stepEdge:
                    stepEdges.Add(stepEdge);
                    break;
                case oriented_edge stepOrientedEdge:
                    stepOrientedEdges.Add(stepOrientedEdge);
                    break;
                case loop stepLoop:
                    stepLoops.Add(stepLoop);
                    break;
                case face_surface stepFace:
                    stepFaces.Add(stepFace);
                    break;
                case connected_face_set stepFaceShell:
                    stepFaceShells.Add(stepFaceShell);
                    break;
                case manifold_solid_brep stepManifoldSolid:
                    stepManifoldSolids.Add(stepManifoldSolid);
                    break;
                case shape_representation stepShapeRes:
                    shapeRes.Add(stepShapeRes);
                    break;
                default:
                    break;
            }
        }
        var midMgr = new MidMgr();
        ResolvePoints(stepPoints, midMgr);
        ResolveCurves(stepLines, midMgr);
        ResolveSurfs(stepPlanes, midMgr);
        ResolveVertices(stepVertices, midMgr);
        ResolveEdges(stepEdges, midMgr);
        ResolveOrientedEdges(stepOrientedEdges, midMgr);
        ResolveLoops(stepLoops, midMgr);
        ResolveFaces(stepFaces, midMgr);
        ResolveFaceShells(stepFaceShells, midMgr);
        ResolveManifoldSolids(stepManifoldSolids, midMgr);
        ResolveShapeRes(stepIdObjMap, shapeRes, midMgr);
        return midMgr;
    }

    private static void ResolveShapeRes(Dictionary<int, IStepObj> stepIdObjMap, List<shape_representation> stepShapeRes, MidMgr midMgr)
    {
        var unit2Mid = new StepUnit2Mid(stepIdObjMap);
        foreach (var stepShape in stepShapeRes)
        {
            unit2Mid.ResolveUnit(stepShape, midMgr);
        }
    }

    private static void ResolvePoints(List<cartesian_point> stepPoints, MidMgr midMgr)
    {
        foreach (var stepPoint in stepPoints)
        {
            var midPoint = midMgr.GetOrCreateMidObj<PointObj>(stepPoint.line_id);
            midPoint.Position = new Vector3D(stepPoint.coordinates[0], stepPoint.coordinates[1], stepPoint.coordinates[2]);
        }
    }

    private static void ResolveCurves(List<curve> stepCurves, MidMgr midMgr)
    {
        foreach (var stepCurve in stepCurves)
        {
            switch (stepCurve)
            {
                case line stepLine:
                    {
                        var midLine = midMgr.GetOrCreateMidObj<LineObj>(stepLine.line_id);
                        var location = new Vector3D(stepLine.pnt.coordinates[0], stepLine.pnt.coordinates[1], stepLine.pnt.coordinates[2]);
                        var axis = new Vector3D(stepLine.dir.orientation.direction_ratios[0], stepLine.dir.orientation.direction_ratios[1], stepLine.dir.orientation.direction_ratios[2]);
                        midLine.BasisSet = new Axis2D { Location = location, Axis = axis };
                        break;
                    }
                case circle stepCircle:
                    {
                        var midCircle = midMgr.GetOrCreateMidObj<CircleObj>(stepCircle.line_id);
                        if (stepCircle.position is axis2_placement_3d position)
                        {
                            var location = new Vector3D(position.location.coordinates[0], position.location.coordinates[1], position.location.coordinates[2]);
                            var axis = new Vector3D(position.axis.direction_ratios[0], position.axis.direction_ratios[1], position.axis.direction_ratios[2]);
                            var refDir = new Vector3D(position.ref_direction.direction_ratios[0], position.ref_direction.direction_ratios[1], position.ref_direction.direction_ratios[2]);
                            midCircle.BasisSet = new Axis3D { Location = location, Axis = axis, RefDir = refDir };
                            midCircle.Radius = stepCircle.radius;
                        }
                        break;
                    }
                default:
                    break;
            }
        }
    }

    private static void ResolveSurfs(List<surface> stepSurfs, MidMgr midMgr)
    {
        foreach (var stepSurf in stepSurfs)
        {
            switch (stepSurf)
            {
                case plane stepPlane:
                    {
                        var midPlane = midMgr.GetOrCreateMidObj<PlaneObj>(stepPlane.line_id);
                        var basisSet = new Axis3D();
                        var planePos = stepPlane.position;
                        basisSet.Location = new Vector3D(planePos.location.coordinates[0], planePos.location.coordinates[1], planePos.location.coordinates[2]);
                        basisSet.Axis = new Vector3D(planePos.axis.direction_ratios[0], planePos.axis.direction_ratios[1], planePos.axis.direction_ratios[2]);
                        basisSet.RefDir = new Vector3D(planePos.ref_direction.direction_ratios[0], planePos.ref_direction.direction_ratios[1], planePos.ref_direction.direction_ratios[2]);
                        midPlane.BasisSet = basisSet;
                        break;
                    }
                case conical_surface stepConicalSurface:
                    {
                        var midConicalSurface = midMgr.GetOrCreateMidObj<ConeSurfObj>(stepConicalSurface.line_id);
                        var basisSet = new Axis3D();
                        var conicalPos = stepConicalSurface.position;
                        basisSet.Location = new Vector3D(conicalPos.location.coordinates[0], conicalPos.location.coordinates[1], conicalPos.location.coordinates[2]);
                        basisSet.Axis = new Vector3D(conicalPos.axis.direction_ratios[0], conicalPos.axis.direction_ratios[1], conicalPos.axis.direction_ratios[2]);
                        basisSet.RefDir = new Vector3D(conicalPos.ref_direction.direction_ratios[0], conicalPos.ref_direction.direction_ratios[1], conicalPos.ref_direction.direction_ratios[2]);
                        midConicalSurface.BasisSet = basisSet;
                        midConicalSurface.Radius = stepConicalSurface.radius;
                        midConicalSurface.SemiAngle = stepConicalSurface.semi_angle;
                        break;
                    }
                default:
                    break;
            }
        }
    }

    private static void ResolveVertices(List<vertex_point> stepVertices, MidMgr midMgr)
    {
        foreach (var stepVertex in stepVertices)
        {
            var midVertex = midMgr.GetOrCreateMidObj<VertexObj>(stepVertex.line_id);
            var midPoint = midMgr.GetOrCreateMidObj<PointObj>(stepVertex.vertex_geometry.line_id);
            midVertex.Point = midPoint;
        }
    }

    private static void ResolveEdges(List<edge_curve> stepEdges, MidMgr midMgr)
    {
        foreach (var stepEdge in stepEdges)
        {
            var midEdge = midMgr.GetOrCreateMidObj<EdgeObj>(stepEdge.line_id);
            midEdge.Start = midMgr.GetMidObjByImp<VertexObj>(stepEdge.edge_start.line_id);
            midEdge.End = midMgr.GetMidObjByImp<VertexObj>(stepEdge.edge_end.line_id);
            midEdge.Curve = midMgr.GetMidObjByImp<ICurveObj>(stepEdge.edge_geometry.line_id);
            midEdge.Sense = stepEdge.same_sense;
        }
    }

    private static void ResolveOrientedEdges(List<oriented_edge> stepOrientedEdges, MidMgr midMgr)
    {
        foreach (var stepOrientedEdge in stepOrientedEdges)
        {
            var midOrientedEdge = midMgr.GetOrCreateMidObj<FinObj>(stepOrientedEdge.line_id);
            midOrientedEdge.Edge = midMgr.GetMidObjByImp<EdgeObj>(stepOrientedEdge.edge_element.line_id);
            midOrientedEdge.Orientation = stepOrientedEdge.orientation;
        }
    }

    private static void ResolveLoops(List<loop> stepLoops, MidMgr midMgr)
    {
        foreach (var stepLoop in stepLoops)
        {
            switch (stepLoop)
            {
                case edge_loop stepEdgeLoop:
                    var midLoop = midMgr.GetOrCreateMidObj<EdgeLoopObj>(stepEdgeLoop.line_id);
                    var fins = stepEdgeLoop.edge_list.Select(edge => midMgr.GetMidObjByImp<FinObj>(edge.line_id)).ToArray();
                    midLoop.Fins = fins;
                    break;
                case vertex_loop stepVertexLoop:
                    var midVertexLoop = midMgr.GetOrCreateMidObj<VertexLoopObj>(stepVertexLoop.line_id);
                    midVertexLoop.Vertex = midMgr.GetMidObjByImp<VertexObj>(stepVertexLoop.loop_vertex.line_id);
                    break;
                default:
                    break;
            }
        }
    }

    private static void ResolveFaces(List<face_surface> stepFaces, MidMgr midMgr)
    {
        foreach (var stepFace in stepFaces)
        {
            var midFace = midMgr.GetOrCreateMidObj<FaceObj>(stepFace.line_id);
            midFace.Loops = [.. stepFace.bounds.Select(bound => midMgr.GetMidObjByImp<ILoopObj>(bound.bound.line_id)).Reverse()];
            midFace.Surf = midMgr.GetMidObjByImp<ISurfaceObj>(stepFace.face_geometry.line_id);
            midFace.Sence = stepFace.same_sense;
        }
    }

    private static void ResolveFaceShells(List<connected_face_set> stepFaceShells, MidMgr midMgr)
    {
        foreach (var stepFaceShell in stepFaceShells)
        {
            var midFaceShell = midMgr.GetOrCreateMidObj<FaceShellObj>(stepFaceShell.line_id);
            switch (stepFaceShell)
            {
                case oriented_open_shell stepOrientedOpenShell:
                    midFaceShell.Closed = false;
                    midFaceShell.Oriented = stepOrientedOpenShell.orientation;
                    midFaceShell.Faces = [.. stepOrientedOpenShell.open_shell_element.cfs_faces.Select(face => midMgr.GetMidObjByImp<FaceObj>(face.line_id)).Reverse()];
                    break;
                case oriented_closed_shell stepOrientedClosedShell:
                    midFaceShell.Closed = true;
                    midFaceShell.Oriented = stepOrientedClosedShell.orientation;
                    midFaceShell.Faces = [.. stepOrientedClosedShell.closed_shell_element.cfs_faces.Select(face => midMgr.GetMidObjByImp<FaceObj>(face.line_id)).Reverse()];
                    break;
                case open_shell stepOpenShell:
                    midFaceShell.Closed = false;
                    midFaceShell.Oriented = true;
                    midFaceShell.Faces = [.. stepFaceShell.cfs_faces.Select(face => midMgr.GetMidObjByImp<FaceObj>(face.line_id)).Reverse()];
                    break;
                case closed_shell stepClosedShell:
                    midFaceShell.Closed = true;
                    midFaceShell.Oriented = true;
                    midFaceShell.Faces = [.. stepFaceShell.cfs_faces.Select(face => midMgr.GetMidObjByImp<FaceObj>(face.line_id)).Reverse()];
                    break;

            }
        }
    }

    private static void ResolveManifoldSolids(List<manifold_solid_brep> stepManifoldSolids, MidMgr midMgr)
    {
        foreach (var stepManifoldSolid in stepManifoldSolids)
        {
            var solidBody = midMgr.GetOrCreateMidObj<SolidBodyObj>(stepManifoldSolid.line_id);
            var regions = new List<IRegionObj>();
            var solidRegion = midMgr.CreateMidObj<SolidRegionObj>();
            var solidShell = midMgr.GetOrCreateMidObj<FaceShellObj>(stepManifoldSolid.outer.line_id);
            var solidShells = new List<FaceShellObj>() { solidShell };
            var voidRegion = midMgr.CreateMidObj<VoidRegionObj>();
            var voidShell = midMgr.CreateMidObj<FaceShellObj>();
            voidShell.Closed = solidShell.Closed;
            voidShell.Oriented = !solidShell.Oriented;
            voidShell.Faces = solidShell.Faces;
            voidRegion.Shells = [voidShell];
            regions.Add(voidRegion);
            regions.Add(solidRegion);
            if (stepManifoldSolid is brep_with_voids stepBrepWithVoids)
            {
                foreach (var stepShell in stepBrepWithVoids.voids)
                {
                    var boundRegion = midMgr.CreateMidObj<BoundVoidRegionObj>();
                    var boundShell = midMgr.GetOrCreateMidObj<FaceShellObj>(stepShell.line_id);
                    boundRegion.Shells = [boundShell];
                    regions.Add(boundRegion);
                    var solidBoundShell = midMgr.CreateMidObj<FaceShellObj>();
                    solidBoundShell.Closed = boundShell.Closed;
                    solidBoundShell.Oriented = !boundShell.Oriented;
                    solidBoundShell.Faces = boundShell.Faces;
                    solidShells.Add(solidBoundShell);
                }
            }
            solidRegion.Shells = [.. solidShells];
            solidBody.Regions = [.. regions];
        }
    }



}
