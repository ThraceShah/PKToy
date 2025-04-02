using Exchange.Midlayer;
using StepCodeDotNet.Base;
using StepCodeDotNet.Gen.ap203;

namespace Exchange.Step2Mid;

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
            midPoint.Position = new Vector3D(stepPoint.coordinates[0], stepPoint.coordinates[1], stepPoint.coordinates[2]);
        }
    }

    private static void ResolveCurves(List<ICurve> stepCurves, MidMgr midMgr)
    {
        foreach (var stepCurve in stepCurves)
        {
            switch (stepCurve)
            {
                case ILine stepLine:
                    {
                        var midLine = midMgr.GetOrCreateMidObj<LineObj>(stepLine.line_id);
                        var location = new Vector3D(stepLine.pnt.coordinates[0], stepLine.pnt.coordinates[1], stepLine.pnt.coordinates[2]);
                        var axis = new Vector3D(stepLine.dir.orientation.direction_ratios[0], stepLine.dir.orientation.direction_ratios[1], stepLine.dir.orientation.direction_ratios[2]);
                        midLine.BasisSet = new Axis2D { Location = location, Axis = axis };
                        break;
                    }
                case ICircle stepCircle:
                    {
                        var midCircle = midMgr.GetOrCreateMidObj<CircleObj>(stepCircle.line_id);
                        if (stepCircle.position is IAxis2_placement_3d position)
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

    private static void ResolveSurfs(List<ISurface> stepSurfs, MidMgr midMgr)
    {
        foreach (var stepSurf in stepSurfs)
        {
            switch (stepSurf)
            {
                case IPlane stepPlane:
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
                case IConical_surface stepConicalSurface:
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
                    var midLoop = midMgr.GetOrCreateMidObj<EdgeLoopObj>(stepEdgeLoop.line_id);
                    var fins = stepEdgeLoop.edge_list.Select(edge => midMgr.GetMidObj<FinObj>(edge.line_id)).Reverse().ToArray();
                    midLoop.Fins = fins;
                    break;
                case IVertex_loop stepVertexLoop:
                    var midVertexLoop = midMgr.GetOrCreateMidObj<VertexLoopObj>(stepVertexLoop.line_id);
                    midVertexLoop.Vertex = midMgr.GetMidObj<VertexObj>(stepVertexLoop.loop_vertex.line_id);
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
            midFace.Loops = [.. stepFace.bounds.Select(bound => midMgr.GetMidObj<ILoopObj>(bound.bound.line_id)).Reverse()];
            midFace.Surf = midMgr.GetMidObj<ISurfaceObj>(stepFace.face_geometry.line_id);
            midFace.Sence = stepFace.same_sense;
        }
    }

    private static void ResolveFaceShells(List<IConnected_face_set> stepFaceShells, MidMgr midMgr)
    {
        foreach (var stepFaceShell in stepFaceShells)
        {
            var midFaceShell = midMgr.GetOrCreateMidObj<FaceShellObj>(stepFaceShell.line_id);
            switch (stepFaceShell)
            {
                case IOriented_open_shell stepOrientedOpenShell:
                    midFaceShell.Closed = false;
                    midFaceShell.Oriented = stepOrientedOpenShell.orientation;
                    midFaceShell.Faces = [.. stepOrientedOpenShell.open_shell_element.cfs_faces.Select(face => midMgr.GetMidObj<FaceObj>(face.line_id)).Reverse()];
                    break;
                case IOriented_closed_shell stepOrientedClosedShell:
                    midFaceShell.Closed = true;
                    midFaceShell.Oriented = stepOrientedClosedShell.orientation;
                    midFaceShell.Faces = [.. stepOrientedClosedShell.closed_shell_element.cfs_faces.Select(face => midMgr.GetMidObj<FaceObj>(face.line_id)).Reverse()];
                    break;
                case IOpen_shell stepOpenShell:
                    midFaceShell.Closed = false;
                    midFaceShell.Oriented = true;
                    midFaceShell.Faces = [.. stepFaceShell.cfs_faces.Select(face => midMgr.GetMidObj<FaceObj>(face.line_id)).Reverse()];
                    break;
                case IClosed_shell stepClosedShell:
                    midFaceShell.Closed = true;
                    midFaceShell.Oriented = true;
                    midFaceShell.Faces = [.. stepFaceShell.cfs_faces.Select(face => midMgr.GetMidObj<FaceObj>(face.line_id)).Reverse()];
                    break;

            }
        }
    }

    private static void ResolveManifoldSolids(List<IManifold_solid_brep> stepManifoldSolids, MidMgr midMgr)
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
            if (stepManifoldSolid is IBrep_with_voids stepBrepWithVoids)
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
