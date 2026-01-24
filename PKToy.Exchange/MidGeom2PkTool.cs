using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Exchange.Midlayer;
using Exchange.Step2Mid;

namespace PKToy.Exchange;

unsafe class MidGeom2PKTool
{
    public Unit Unit { get; set; }
    internal PK_GEOM_t MidGeom2PK(IGeoObj midGeom) =>
    midGeom switch
    {
        PointObj midPoint => MidPoint2PK(midPoint),
        LineObj midLine => MidLine2PK(midLine),
        PlaneObj midPlane => MidPlane2PK(midPlane),
        CircleObj midPlane => MidCircle2PK(midPlane),
        ConeSurfObj midPlane => MidCone2PK(midPlane),
        _ => throw new NotSupportedException($"Unsupported MidGeom type: {midGeom.GetType().Name}")
    };

    private PK_GEOM_t MidPoint2PK(PointObj midPoint)
    {
        var point = CastWithUnit<Vector3D, PK_POINT_sf_t>(midPoint.Position);
        PK_POINT_t tag;
        PK_POINT_create(&point, &tag);
        // Console.WriteLine($"MidPoint2PK->#{midPoint.ImpId}: ({point.position.coord[0]},{point.position.coord[1]},{point.position.coord[2]})");
        return tag;
    }

    private PK_GEOM_t MidLine2PK(LineObj midLine)
    {
        PK_LINE_sf_t line;
        line.basis_set = CastWithUnit(midLine.BasisSet);
        // Console.Write($"MidLine2PK->#{midLine.ImpId}: ({line.basis_set.location.coord[0]},{line.basis_set.location.coord[1]},{line.basis_set.location.coord[2]})");
        // Console.WriteLine($"-({line.basis_set.axis.coord[0]},{line.basis_set.axis.coord[1]},{line.basis_set.axis.coord[2]})");
        PK_LINE_t tag;
        PK_LINE_create(&line, &tag);
        return tag;
    }
    private PK_GEOM_t MidCircle2PK(CircleObj midGeom)
    {
        PK_CIRCLE_sf_t circle;
        circle.basis_set = CastWithUnit(midGeom.BasisSet);
        circle.radius = midGeom.Radius * Unit.LengthFactor;
        PK_CIRCLE_t tag;
        PK_CIRCLE_create(&circle, &tag);
        return tag;
    }


    private PK_GEOM_t MidPlane2PK(PlaneObj midGeom)
    {
        PK_PLANE_sf_t plane;
        plane.basis_set = CastWithUnit(midGeom.BasisSet);
        // Console.Write($"MidPlane2PK->#{midPlane.ImpId}: ({plane.basis_set.location.coord[0]},{plane.basis_set.location.coord[1]},{plane.basis_set.location.coord[2]})");
        // Console.Write($"-({plane.basis_set.axis.coord[0]},{plane.basis_set.axis.coord[1]},{plane.basis_set.axis.coord[2]})");
        // Console.WriteLine($"-({plane.basis_set.ref_direction.coord[0]},{plane.basis_set.ref_direction.coord[1]},{plane.basis_set.ref_direction.coord[2]})");
        PK_PLANE_t tag;
        PK_PLANE_create(&plane, &tag);
        return tag;
    }

    private PK_GEOM_t MidCone2PK(ConeSurfObj midGeom)
    {
        PK_CONE_sf_t cone;
        cone.basis_set = CastWithUnit(midGeom.BasisSet);
        cone.radius = midGeom.Radius * Unit.LengthFactor;
        cone.semi_angle = midGeom.SemiAngle * Unit.RadianFactor;
        PK_CONE_t tag;
        PK_CONE_create(&cone, &tag);
        return tag;
    }

    private static bool IsMakeOfDouble(Type type)
    {
        var fields = type.GetFields();
        foreach (var field in fields)
        {
            var fieldType = field.FieldType;
            if (fieldType != typeof(double))
            {
                if (IsMakeOfDouble(fieldType) == false)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static bool IsMakeOfDouble<T>() where T : unmanaged
    {
        var type = typeof(T);
        return IsMakeOfDouble(type);
    }

    private PK_AXIS2_sf_t CastWithUnit(Axis3D axis)
    {
        PK_AXIS2_sf_t axis2D;
        axis2D.location = CastWithUnit<Vector3D, PK_VECTOR_t>(axis.Location);
        axis2D.axis = UnsafeCast<Vector3D, PK_VECTOR1_t>(axis.Axis);
        axis2D.ref_direction = UnsafeCast<Vector3D, PK_VECTOR1_t>(axis.RefDir);
        return axis2D;
    }

    private PK_AXIS1_sf_t CastWithUnit(Axis2D axis)
    {
        PK_AXIS1_sf_t axis1D;
        axis1D.location = CastWithUnit<Vector3D, PK_VECTOR_t>(axis.Location);
        axis1D.axis = UnsafeCast<Vector3D, PK_VECTOR1_t>(axis.Axis);
        return axis1D;
    }

    private static T2 UnsafeCast<T1, T2>(T1 obj) where T1 : unmanaged where T2 : unmanaged
    {
        Debug.Assert(sizeof(T1) == sizeof(T2), $"Size mismatch: {sizeof(T1)} != {sizeof(T2)}");
        return *(T2*)&obj;
    }


    private T2 CastWithUnit<T1, T2>(T1 obj) where T1 : unmanaged where T2 : unmanaged
    {
        Debug.Assert(sizeof(T1) == sizeof(T2), $"Size mismatch: {sizeof(T1)} != {sizeof(T2)}");
        Debug.Assert((sizeof(T1) % sizeof(double)) == 0, $"Size mismatch: {sizeof(T1)}%{sizeof(double)} != 0");
        Debug.Assert(IsMakeOfDouble<T1>(), $"Type {typeof(T1).Name} is not made of double.");
        Debug.Assert(IsMakeOfDouble<T2>(), $"Type {typeof(T2).Name} is not made of double.");
        var n = sizeof(T1) / sizeof(double);
        var ptr1 = (double*)&obj;
        T2 r;
        var ptr2 = (double*)&r;
        for (int i = 0; i < n; i++)
        {
            ptr2[i] = ptr1[i] * Unit.LengthFactor;
        }
        return r;
    }
}