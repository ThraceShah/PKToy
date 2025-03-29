using System.Diagnostics;
using Exchange.Midlayer;
using Exchange.Step2Mid;

namespace PKToy.Exchange;

unsafe class MidGeom2PKTool
{
    internal static PK.GEOM_t MidGeom2PK(IGeoObj midGeom) =>
    midGeom switch
    {
        PointObj midPoint => MidPoint2PK(midPoint),
        LineObj midLine => MidLine2PK(midLine),
        PlaneObj midPlane => MidPlane2PK(midPlane),
        CircleObj midPlane => MidCircle2PK(midPlane),
        ConeSurfObj midPlane => MidCone2PK(midPlane),
        _ => throw new NotSupportedException($"Unsupported MidGeom type: {midGeom.GetType().Name}")
    };

    private static PK.GEOM_t MidPoint2PK(PointObj midPoint)
    {
        var point = UnsafeCast<Vector3D, PK.POINT_sf_t>(midPoint.Position);
        PK.POINT_t tag;
        PK.POINT.create(&point, &tag);
        // Console.WriteLine($"MidPoint2PK->#{midPoint.ImpId}: ({point.position.coord[0]},{point.position.coord[1]},{point.position.coord[2]})");
        return tag;
    }

    private static PK.GEOM_t MidLine2PK(LineObj midLine)
    {
        PK.LINE_sf_t line = UnsafeCast<Axis2D, PK.LINE_sf_t>(midLine.BasisSet);
        // line.basis_set.location = UnsafeCast<Vector3D, PK.VECTOR_t>(midLine.Location);
        // line.basis_set.axis = UnsafeCast<Vector3D, PK.VECTOR1_t>(midLine.Axis);
        // Console.Write($"MidLine2PK->#{midLine.ImpId}: ({line.basis_set.location.coord[0]},{line.basis_set.location.coord[1]},{line.basis_set.location.coord[2]})");
        // Console.WriteLine($"-({line.basis_set.axis.coord[0]},{line.basis_set.axis.coord[1]},{line.basis_set.axis.coord[2]})");
        PK.LINE_t tag;
        PK.LINE.create(&line, &tag);
        return tag;
    }
    private static PK.GEOM_t MidCircle2PK(CircleObj midPlane)
    {
        PK.CIRCLE_sf_t circle;
        circle.basis_set = UnsafeCast<Axis3D, PK.AXIS2_sf_t>(midPlane.BasisSet);
        circle.radius = midPlane.Radius;
        PK.CIRCLE_t tag;
        PK.CIRCLE.create(&circle, &tag);
        return tag;
    }


    private static PK.GEOM_t MidPlane2PK(PlaneObj midPlane)
    {
        PK.PLANE_sf_t plane;
        plane.basis_set = UnsafeCast<Axis3D, PK.AXIS2_sf_t>(midPlane.BasisSet);
        // Console.Write($"MidPlane2PK->#{midPlane.ImpId}: ({plane.basis_set.location.coord[0]},{plane.basis_set.location.coord[1]},{plane.basis_set.location.coord[2]})");
        // Console.Write($"-({plane.basis_set.axis.coord[0]},{plane.basis_set.axis.coord[1]},{plane.basis_set.axis.coord[2]})");
        // Console.WriteLine($"-({plane.basis_set.ref_direction.coord[0]},{plane.basis_set.ref_direction.coord[1]},{plane.basis_set.ref_direction.coord[2]})");
        PK.PLANE_t tag;
        PK.PLANE.create(&plane, &tag);
        return tag;
    }

    private static PK.GEOM_t MidCone2PK(ConeSurfObj midPlane)
    {
        PK.CONE_sf_t cone;
        cone.basis_set = UnsafeCast<Axis3D, PK.AXIS2_sf_t>(midPlane.BasisSet);
        cone.radius = midPlane.Radius;
        cone.semi_angle = midPlane.SemiAngle;
        PK.CONE_t tag;
        PK.CONE.create(&cone, &tag);
        return tag;
    }




    private static T2 UnsafeCast<T1, T2>(T1 obj) where T1 : unmanaged where T2 : unmanaged
    {
        Debug.Assert(sizeof(T1) == sizeof(T2), $"Size mismatch: {sizeof(T1)} != {sizeof(T2)}");
        return *(T2*)&obj;
    }
}