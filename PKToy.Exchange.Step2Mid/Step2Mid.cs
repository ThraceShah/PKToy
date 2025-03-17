using PKToy.Exchange.Midlayer;
using StepCodeDotNet.Base;
using StepCodeDotNet.Gen.ap203;

namespace PKToy.Exchange.Step2Mid;

public class Step2Mid
{
    public static MidMgr ResolveStep2Mid(string stepFile)
    {
        var stepCreator = new StepObjCreator();
        var stepParser = new StepParser(stepCreator);
        var stepObjs = stepParser.Resolve(stepFile);
        var stepPoints = new List<ICartesian_point>();
        var stepLines = new List<ICurve>();
        var stepPlanes = new List<ISurface>();
        var stepVertices = new List<IVertex_point>();
        var stepEdges = new List<IEdge_curve>();
        var stepOrientedEdges = new List<IOriented_edge>();
        var stepLoops = new List<ILoop>();
        var stepFaces = new List<IFace_surface>();
        var stepFaceShells = new List<IConnected_face_set>();
        var stepManifoldSolids = new List<IManifold_solid_brep>();
        foreach (var stepObj in stepObjs)
        {
            switch (stepObj)
            {
                case StepComplexImp stepComplex:
                    break;
                case ICartesian_point stepPoint:
                    stepPoints.Add(stepPoint);
                    break;
                case ICurve stepLine:
                    stepLines.Add(stepLine);
                    break;
                case ISurface stepPlane:
                    stepPlanes.Add(stepPlane);
                    break;
                case IVertex_point stepVertex:
                    stepVertices.Add(stepVertex);
                    break;
                case IEdge_curve stepEdge:
                    stepEdges.Add(stepEdge);
                    break;
                case IOriented_edge stepOrientedEdge:
                    stepOrientedEdges.Add(stepOrientedEdge);
                    break;
                case ILoop stepLoop:
                    stepLoops.Add(stepLoop);
                    break;
                case IFace_surface stepFace:
                    stepFaces.Add(stepFace);
                    break;
                case IConnected_face_set stepFaceShell:
                    stepFaceShells.Add(stepFaceShell);
                    break;
                case IManifold_solid_brep stepManifoldSolid:
                    stepManifoldSolids.Add(stepManifoldSolid);
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
        return midMgr;
    }

    private static void ResolvePoints(List<ICartesian_point> stepPoints, MidMgr midMgr)
    {
        foreach (var stepPoint in stepPoints)
        {
            var midPoint = midMgr.GetOrCreateMidObj<PointObj>(stepPoint.line_id);
            midPoint.Position = new Vector3(stepPoint.coordinates[0], stepPoint.coordinates[1], stepPoint.coordinates[2]);
        }
    }

    private static void ResolveCurves(List<ICurve> stepCurves, MidMgr midMgr)
    {
        foreach (var stepCurve in stepCurves)
        {
            switch (stepCurve)
            {
                case ILine stepLine:
                    var midLine = midMgr.GetOrCreateMidObj<LineObj>(stepLine.line_id);
                    midLine.Location = new Vector3(stepLine.pnt.coordinates[0], stepLine.pnt.coordinates[1], stepLine.pnt.coordinates[2]);
                    midLine.Axis = new Vector3(stepLine.dir.orientation.direction_ratios[0], stepLine.dir.orientation.direction_ratios[1], stepLine.dir.orientation.direction_ratios[2]);
                    break;
                default:
                    break;
            }
        }
    }

    private static void ResolveSurfs(List<ISurface> stepSurfs, MidMgr midMgr)
    {
        foreach (var stepSurf in stepSurfs)
        {
            switch (stepSurf)
            {
                case plane_imp stepPlane:
                    var midPlane = midMgr.GetOrCreateMidObj<PlaneObj>(stepPlane.line_id);
                    var basisSet = new Axis3D();
                    var planePos = stepPlane.position;
                    basisSet.Location = new Vector3(planePos.location.coordinates[0], planePos.location.coordinates[1], planePos.location.coordinates[2]);
                    basisSet.Axis = new Vector3(planePos.axis.direction_ratios[0], planePos.axis.direction_ratios[1], planePos.axis.direction_ratios[2]);
                    basisSet.RefDir = new Vector3(planePos.ref_direction.direction_ratios[0], planePos.ref_direction.direction_ratios[1], planePos.ref_direction.direction_ratios[2]);
                    midPlane.BasisSet = basisSet;
                    break;
                default:
                    break;
            }
        }
    }

    private static void ResolveVertices(List<IVertex_point> stepVertices, MidMgr midMgr)
    {
        foreach (var stepVertex in stepVertices)
        {
            var midVertex = midMgr.GetOrCreateMidObj<VertexObj>(stepVertex.line_id);
            var midPoint = midMgr.GetOrCreateMidObj<PointObj>(stepVertex.vertex_geometry.line_id);
            midVertex.Point = midPoint;
        }
    }

    private static void ResolveEdges(List<IEdge_curve> stepEdges, MidMgr midMgr)
    {
        foreach (var stepEdge in stepEdges)
        {
            var midEdge = midMgr.GetOrCreateMidObj<EdgeObj>(stepEdge.line_id);
            midEdge.Start = midMgr.GetMidObj<VertexObj>(stepEdge.edge_start.line_id);
            midEdge.End = midMgr.GetMidObj<VertexObj>(stepEdge.edge_end.line_id);
            midEdge.Curve = midMgr.GetMidObj<ICurveObj>(stepEdge.edge_geometry.line_id);
        }
    }

    private static void ResolveOrientedEdges(List<IOriented_edge> stepOrientedEdges, MidMgr midMgr)
    {
        foreach (var stepOrientedEdge in stepOrientedEdges)
        {
            var midOrientedEdge = midMgr.GetOrCreateMidObj<FinObj>(stepOrientedEdge.line_id);
            midOrientedEdge.Edge = midMgr.GetMidObj<EdgeObj>(stepOrientedEdge.edge_element.line_id);
            midOrientedEdge.Orientation = stepOrientedEdge.orientation;
        }
    }

    private static void ResolveLoops(List<ILoop> stepLoops, MidMgr midMgr)
    {
        foreach (var stepLoop in stepLoops)
        {
            switch (stepLoop)
            {
                case IEdge_loop stepEdgeLoop:
                    var midLoop = midMgr.GetOrCreateMidObj<LoopObj>(stepEdgeLoop.line_id);
                    var fins = stepEdgeLoop.edge_list.Select(edge => midMgr.GetMidObj<FinObj>(edge.line_id)).ToArray();
                    midLoop.Fins = fins;
                    break;
                default:
                    break;
            }
        }
    }

    private static void ResolveFaces(List<IFace_surface> stepFaces, MidMgr midMgr)
    {
        foreach (var stepFace in stepFaces)
        {
            var midFace = midMgr.GetOrCreateMidObj<FaceObj>(stepFace.line_id);
            midFace.Loops = stepFace.bounds.Select(bound => midMgr.GetMidObj<LoopObj>(bound.bound.line_id)).ToArray();
            midFace.Surf = midMgr.GetMidObj<ISurfaceObj>(stepFace.face_geometry.line_id);
        }
    }

    private static void ResolveFaceShells(List<IConnected_face_set> stepFaceShells, MidMgr midMgr)
    {
        foreach (var stepFaceShell in stepFaceShells)
        {
            var midFaceShell = midMgr.GetOrCreateMidObj<FaceShellObj>(stepFaceShell.line_id);
            midFaceShell.Faces = stepFaceShell.cfs_faces.Select(face => midMgr.GetMidObj<FaceObj>(face.line_id)).ToArray();
        }
    }

    private static void ResolveManifoldSolids(List<IManifold_solid_brep> stepManifoldSolids, MidMgr midMgr)
    {

    }



}
