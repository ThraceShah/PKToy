using System.Numerics;
using Viewer.IContract;

namespace PKToy.Test.Viewer;

public class AsmGeometryTest
{
    [Fact]
    public void GetBoundingBoxTransformsAllEightCorners()
    {
        var part = new BoxGeometry(new Box
        {
            Min = new Vector3(-1, -2, -3),
            Max = new Vector3(1, 2, 3)
        });
        var assembly = new AsmGeometry();
        var transform = Matrix4x4.CreateRotationZ(MathF.PI / 4) * Matrix4x4.CreateTranslation(10, 20, 30);
        assembly.AddComponent(part, transform);

        var box = assembly.GetBoundingBox();

        var extent = 3 / MathF.Sqrt(2);
        Assert.Equal(10 - extent, box.Min.X, 5);
        Assert.Equal(20 - extent, box.Min.Y, 5);
        Assert.Equal(27, box.Min.Z, 5);
        Assert.Equal(10 + extent, box.Max.X, 5);
        Assert.Equal(20 + extent, box.Max.Y, 5);
        Assert.Equal(33, box.Max.Z, 5);
    }

    private sealed class BoxGeometry(Box box) : IGeometryData
    {
        public bool GetCellGeometryRange(int cellIndex, out int startCell, out int length)
        {
            startCell = 0;
            length = 0;
            return false;
        }

        public Box Box { get; } = box;
        public int CellCount => 0;
        public int IndicesCount => 0;
        public long OutPutSize => 0;
    }
}
