
namespace Exchange.Midlayer;

public record struct ImpId(int Id = 0)
{
    public static implicit operator ImpId(int id) => new(id);
    public static implicit operator int(ImpId impId) => impId.Id;
}

public record struct ExpId(int Id = 0)
{
    public static implicit operator ExpId(int id) => new(id);
    public static implicit operator int(ExpId expId) => expId.Id;
}

public struct Vector3D(double x, double y, double z)
{
    public double X = x;
    public double Y = y;
    public double Z = z;
}

public struct Axis2D
{
    public Vector3D Location;

    public Vector3D Axis;
}

public struct Axis3D
{
    public Vector3D Location;

    public Vector3D Axis;

    public Vector3D RefDir;
}

public interface IMidObj
{
    ImpId ImpId { get; internal set; }
    ExpId ExpId { get; internal set; }
}

public interface IGeoObj : IMidObj;

public interface IPointObj : IGeoObj;

public interface ICurveObj : IGeoObj;
public interface ISurfaceObj : IGeoObj;

public class PointObj : IPointObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public Vector3D Position { get; set; }
}

public class LineObj : ICurveObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public Axis2D BasisSet { get; set; }
}

public class CircleObj : ICurveObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public Axis3D BasisSet { get; set; }
    public double Radius { get; set; }
}

public class PlaneObj : ISurfaceObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public Axis3D BasisSet { get; set; }
}

public class ConeSurfObj : ISurfaceObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public Axis3D BasisSet { get; set; }
    public double Radius { get; set; }
    public double SemiAngle { get; set; }

}

public interface ITopolObj : IMidObj;

public class VertexObj : ITopolObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public PointObj? Point { get; set; }
}

public class EdgeObj : ITopolObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public VertexObj? Start { get; set; }
    public VertexObj? End { get; set; }
    public ICurveObj? Curve { get; set; }
    public bool Sense { get; set; } = true;
}

public class FinObj : ITopolObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public EdgeObj? Edge { get; set; }
    public bool Orientation { get; set; }
}

public interface ILoopObj : ITopolObj;

public class EdgeLoopObj : ILoopObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public FinObj[]? Fins { get; set; }
}

public class VertexLoopObj : ILoopObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public VertexObj? Vertex { get; set; }
}

public class FaceObj : ITopolObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public ILoopObj[]? Loops { get; set; }
    public ISurfaceObj? Surf { get; set; }
    public bool Sence { get; set; } = true;
}

public interface IShellObj : ITopolObj;

public class FaceShellObj : IShellObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public FaceObj[]? Faces { get; set; }
    public bool Closed { get; set; } = false;
    public bool Oriented { get; set; } = true;
}

public class WireShellObj : IShellObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public EdgeObj[]? Edges { get; set; }
}

public class VertexShellObj : IShellObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public VertexObj? Vertex { get; set; }
}


public interface IRegionObj : ITopolObj
{
    public IShellObj[]? Shells { get; }
}

public class SolidRegionObj : IRegionObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public FaceShellObj[]? Shells { get; set; }
    IShellObj[]? IRegionObj.Shells => Shells;
}

public class VoidRegionObj : IRegionObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public IShellObj[]? Shells { get; set; }
}

public class BoundVoidRegionObj : IRegionObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public IShellObj[]? Shells { get; set; }
}

public interface IBodyObj : ITopolObj
{
    public IRegionObj[]? Regions { get; set; }
}

public class SolidBodyObj : IBodyObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public IRegionObj[]? Regions { get; set; }
}

public class SheetBodyObj : IBodyObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public IRegionObj[]? Regions { get; set; }
}

public class WireBodyObj : IBodyObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public IRegionObj[]? Regions { get; set; }
}

public class AcornBodyObj : IBodyObj
{
    ImpId impId;
    ExpId expId;
    public ImpId ImpId => impId;
    public ExpId ExpId => expId;
    ImpId IMidObj.ImpId { get => impId; set => impId = value; }
    ExpId IMidObj.ExpId { get => expId; set => expId = value; }
    public IRegionObj[]? Regions { get; set; }
}

