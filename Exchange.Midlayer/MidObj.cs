
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

public struct Axis3D
{
    public Vector3D Location;

    public Vector3D Axis;

    public Vector3D RefDir;
}

public interface IMidObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
}

public interface IGeoObj : IMidObj;

public interface IPointObj : IGeoObj;

public interface ICurveObj : IGeoObj;
public interface ISurfaceObj : IGeoObj;

public class PointObj : IPointObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public Vector3D Position { get; set; }
}

public class LineObj : ICurveObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public Vector3D Location { get; set; }
    public Vector3D Axis { get; set; }
}

public class PlaneObj : ISurfaceObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public Axis3D BasisSet { get; set; }
}

public interface ITopoObj : IMidObj;

public class VertexObj : ITopoObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public PointObj? Point { get; set; }
}

public class EdgeObj : ITopoObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public VertexObj? Start { get; set; }
    public VertexObj? End { get; set; }
    public ICurveObj? Curve { get; set; }
}

public class FinObj : ITopoObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public EdgeObj? Edge { get; set; }
    public bool Orientation { get; set; }
}

public class LoopObj : ITopoObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public FinObj[]? Fins { get; set; }
}

public class FaceObj : ITopoObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public LoopObj[]? Loops { get; set; }
    public ISurfaceObj? Surf { get; set; }
    public bool Sence { get; set; } = true;
}

public interface IShellObj : ITopoObj;

public class FaceShellObj : IShellObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public FaceObj[]? Faces { get; set; }
    public bool Closed { get; set; } = false;
    public bool Oriented { get; set; } = true;
}

public class WireShellObj : IShellObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public EdgeObj[]? Edges { get; set; }
}

public class VertexShellObj : IShellObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public VertexObj? Vertex { get; set; }
}


public interface IRegionObj : ITopoObj
{
    public IShellObj[]? Shells { get; }
}

public class SolidRegionObj : IRegionObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public FaceShellObj[]? Shells { get; set; }
    IShellObj[]? IRegionObj.Shells => Shells;
}

public class VoidRegionObj : IRegionObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public IShellObj[]? Shells { get; set; }
}

public class BoundVoidRegionObj : IRegionObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public IShellObj[]? Shells { get; set; }
}

public interface IBodyObj : ITopoObj
{
    public IRegionObj[]? Regions { get; set; }
}

public class SolidBodyObj : IBodyObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public IRegionObj[]? Regions { get; set; }
}

public class SheetBodyObj : IBodyObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public IRegionObj[]? Regions { get; set; }
}

public class WireBodyObj : IBodyObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public IRegionObj[]? Regions { get; set; }
}

public class AcornBodyObj : IBodyObj
{
    public ImpId ImpId { get; set; }
    public ExpId ExpId { get; set; }
    public IRegionObj[]? Regions { get; set; }
}

