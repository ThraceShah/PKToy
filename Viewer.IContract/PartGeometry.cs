using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Viewer.IContract
{
    public static class Constants
    {
        public const int STRIPBREAK = -1;
    }

    public interface IGeometryData
    {
        bool GetCellGeometryRange(int cellIndex, out int startCell, out int length);

        Box Box { get; }

        int CellCount { get; }

        int IndicesCount { get; }
    }

    public class StripFaceGeometry : IGeometryData
    {
        private readonly List<Vector4> points = [];
        private readonly List<Vector3> normals = [];
        private readonly List<uint> colors = [];
        private readonly List<int> indices = [];
        private readonly List<int> cells = [];
        private Box? box;

        public List<Vector4> Points => points;
        public List<Vector3> Normals => normals;
        public List<uint> Colors => colors;
        public List<int> Indices => indices;
        public List<int> Cells => cells;
        public Box Box
        {
            get
            {
                box ??= CalBox();
                return box.Value;
            }
        }

        private Box CalBox()
        {
            Vector4 min = new(float.MaxValue, float.MaxValue, float.MaxValue, float.MinValue);
            Vector4 max = new(float.MinValue, float.MinValue, float.MinValue, float.MaxValue);
            foreach (var point in points)
            {
                min = Vector4.Min(min, point);
                max = Vector4.Max(max, point);
            }
            return new Box
            {
                Min = new Vector3(min.X, min.Y, min.Z),
                Max = new Vector3(max.X, max.Y, max.Z),
            };
        }

        public ReadOnlySpan<Vector4> PointsSpan => points.ToArray();
        public ReadOnlySpan<Vector3> NormalsSpan => normals.ToArray();
        public ReadOnlySpan<uint> ColorsSpan => colors.ToArray();
        public ReadOnlySpan<int> IndicesSpan => indices.ToArray();
        public ReadOnlySpan<int> CellsSpan => cells.ToArray();
        public int CellCount => cells.Count;
        public int IndicesCount => indices.Count;


        public bool GetCellGeometryRange(int cell, out int startIndex, out int length)
        {
            if (cell < 0 || cell >= cells.Count)
            {
                startIndex = 0;
                length = 0;
                return false;
            }
            if (cell == 0)
            {
                startIndex = 0;
                length = cells[0];
                return true;
            }
            startIndex = cells[cell - 1];
            length = cells[cell] - startIndex;
            return true;
        }


        public void InsertNextCell()
        {
            cells.Add(indices.Count);
        }

        public void InsertNextStrip()
        {
            indices.Add(Constants.STRIPBREAK);
        }

        public unsafe void InsertNextPoint(Vector3 point, Vector3 normal, uint color)
        {
            int id = cells.Count;
            points.Add(new Vector4(point, *(float*)&id));
            normals.Add(normal);
            colors.Add(color);
            indices.Add(points.Count - 1);
        }

        public unsafe void InsertNextPoint(double* point, double* normal, uint color)
        {
            int id = cells.Count;
            points.Add(new Vector4((float)point[0], (float)point[1], (float)point[2], *(float*)&id));
            normals.Add(new Vector3((float)normal[0], (float)normal[1], (float)normal[2]));
            colors.Add(color);
            indices.Add(points.Count - 1);
        }

    }

    public class StripEdgeGeometry : IGeometryData
    {
        private readonly List<Vector4> points = [];
        private readonly List<int> indices = [];
        private readonly List<int> cells = [];
        private Box? box;

        public List<Vector4> Points => points;
        public List<int> Indices => indices;
        public List<int> Cells => cells;

        public Box Box
        {
            get
            {
                box ??= CalBox();
                return box.Value;
            }
        }
        private Box CalBox()
        {
            Vector4 min = new(float.MaxValue, float.MaxValue, float.MaxValue, float.MinValue);
            Vector4 max = new(float.MinValue, float.MinValue, float.MinValue, float.MaxValue);
            foreach (var point in points)
            {
                min = Vector4.Min(min, point);
                max = Vector4.Max(max, point);
            }
            return new Box
            {
                Min = new Vector3(min.X, min.Y, min.Z),
                Max = new Vector3(max.X, max.Y, max.Z),
            };
        }



        public ReadOnlySpan<Vector4> PointsSpan => points.ToArray();
        public ReadOnlySpan<int> IndicesSpan => indices.ToArray();
        public ReadOnlySpan<int> CellsSpan => cells.ToArray();
        public int CellCount => cells.Count;
        public int IndicesCount => indices.Count;

        public bool GetCellGeometryRange(int cell, out int startIndex, out int length)
        {
            if (cell < 0 || cell >= cells.Count)
            {
                startIndex = 0;
                length = 0;
                return false;
            }
            startIndex = cells[cell];
            if (cell + 1 >= cells.Count)
            {
                length = indices.Count - startIndex;
                return true;
            }
            length = cells[cell + 1] - startIndex;
            return true;
        }

        public void InsertNextCell()
        {
            indices.Add(Constants.STRIPBREAK);
            cells.Add(indices.Count);
        }

        public unsafe void InsertNextPoint(Vector3 point)
        {
            int id = cells.Count;
            points.Add(new Vector4(point, *(float*)&id));
            indices.Add(points.Count - 1);
        }

        public unsafe void InsertNextPoint(double* point)
        {
            int id = cells.Count;
            points.Add(new Vector4((float)point[0], (float)point[1], (float)point[2], *(float*)&id));
            indices.Add(points.Count - 1);
        }


    }

    public class EdgeGeometry(List<Vector4> points, List<int> indices, List<int> cells) : IGeometryData
    {
        private Box? box;

        public List<Vector4> Points => points;
        public List<int> Indices => indices;
        public List<int> Cells => cells;

        public Box Box
        {
            get
            {
                box ??= CalBox();
                return box.Value;
            }
        }
        private Box CalBox()
        {
            Vector4 min = new(float.MaxValue, float.MaxValue, float.MaxValue, float.MinValue);
            Vector4 max = new(float.MinValue, float.MinValue, float.MinValue, float.MaxValue);
            foreach (var point in points)
            {
                min = Vector4.Min(min, point);
                max = Vector4.Max(max, point);
            }
            return new Box
            {
                Min = new Vector3(min.X, min.Y, min.Z),
                Max = new Vector3(max.X, max.Y, max.Z),
            };
        }



        public ReadOnlySpan<Vector4> PointsSpan => points.ToArray();
        public ReadOnlySpan<int> IndicesSpan => indices.ToArray();
        public ReadOnlySpan<int> CellsSpan => cells.ToArray();
        public int CellCount => cells.Count;
        public int IndicesCount => indices.Count;

        public bool GetCellGeometryRange(int cell, out int startIndex, out int length)
        {
            if (cell < 0 || cell >= cells.Count)
            {
                startIndex = 0;
                length = 0;
                return false;
            }
            if (cell == 0)
            {
                startIndex = 0;
                length = cells[0];
                return true;
            }
            startIndex = cells[cell - 1];
            length = cells[cell] - startIndex;
            return true;
        }

        public void InsertNextCell()
        {
            indices.Add(Constants.STRIPBREAK);
            cells.Add(indices.Count);
        }

        public unsafe void InsertNextPoint(Vector3 point)
        {
            int id = cells.Count;
            points.Add(new Vector4(point, *(float*)&id));
            indices.Add(points.Count - 1);
        }

        public unsafe void InsertNextPoint(double* point)
        {
            int id = cells.Count;
            points.Add(new Vector4((float)point[0], (float)point[1], (float)point[2], *(float*)&id));
            indices.Add(points.Count - 1);
        }


    }

    public class EdgeGeometryBuilder
    {
        private readonly Dictionary<Vector4, int> pointIndexMap = [];
        private readonly List<Vector4> points = [];
        private readonly List<int> indices = [];
        private readonly List<int> cells = [];

        public EdgeGeometry Build()
        {
            return new EdgeGeometry(points, indices, cells);
        }

        public void InsertNextEdge()
        {
            cells.Add(indices.Count);
        }

        public unsafe void InsertNextPoint(Vector3 point)
        {
            int id = cells.Count;
            var point4 = new Vector4(point, *(float*)&id);
            if (pointIndexMap.TryGetValue(point4, out var index))
            {
                indices.Add(index);
            }
            else
            {
                points.Add(point4);
                index = points.Count - 1;
                indices.Add(index);
                pointIndexMap.Add(point4, index);
            }
        }

        public unsafe void InsertNextPoint(double* point)
        {
            int id = cells.Count;
            var point4 = new Vector4((float)point[0], (float)point[1], (float)point[2], *(float*)&id);
            if (pointIndexMap.TryGetValue(point4, out var index))
            {
                indices.Add(index);
            }
            else
            {
                points.Add(point4);
                index = points.Count - 1;
                indices.Add(index);
                pointIndexMap.Add(point4, index);
            }
        }


    }


    public class VertexGeometry : IGeometryData
    {
        private readonly List<Vector4> points = [];
        private Box? box;
        public List<Vector4> Points => points;
        public Box Box
        {
            get
            {
                box ??= CalBox();
                return box.Value;
            }
        }

        private Box CalBox()
        {
            Vector4 min = new(float.MaxValue, float.MaxValue, float.MaxValue, float.MinValue);
            Vector4 max = new(float.MinValue, float.MinValue, float.MinValue, float.MaxValue);
            foreach (var point in points)
            {
                min = Vector4.Min(min, point);
                max = Vector4.Max(max, point);
            }
            return new Box
            {
                Min = new Vector3(min.X, min.Y, min.Z),
                Max = new Vector3(max.X, max.Y, max.Z),
            };
        }

        public ReadOnlySpan<Vector4> PointsSpan => points.ToArray();
        public int CellCount => points.Count;
        public int IndicesCount => points.Count;

        public bool GetCellGeometryRange(int cell, out int startIndex, out int length)
        {
            if (cell < 0 || cell >= points.Count)
            {
                startIndex = 0;
                length = 0;
                return false;
            }
            startIndex = cell;
            length = 1;
            return true;
        }

        public unsafe void InsertNextPoint(Vector3 point)
        {
            int id = points.Count;
            points.Add(new Vector4(point, *(float*)&id));
        }

        public unsafe void InsertNextPoint(double* point)
        {
            int id = points.Count;
            points.Add(new Vector4((float)point[0], (float)point[1], (float)point[1], *(float*)&id));
        }

    }

    public class PartGeometry
    {

        public Vector4[] VertexArray;
        public Vector3[] NormalArray;
        public Vector4[] ColorArray;
        public int[] IndexArray;

        public int[] FaceStartIndexArray;

        public int[] EdgeStartIndexArray;

        public int FaceStartIndex;
        public int FaceIndexLength;

        public int EdgeStartIndex;
        public int EdgeIndexLength;
        private unsafe Box box;


        public PartGeometry(Vector4[] vertexArray,
            Vector3[] normalArray,
            Vector4[] colorArray,
            int[] indexArray,
            int lineStartIndex,
            int[] faceStartIndexArray,
            int[] edgeStartIndexArray)
        {
            this.VertexArray = vertexArray;
            this.NormalArray = normalArray;
            this.ColorArray = colorArray;
            this.IndexArray = indexArray;
            this.FaceStartIndexArray = faceStartIndexArray;
            this.EdgeStartIndexArray = edgeStartIndexArray;
            this.FaceStartIndex = 0;
            this.FaceIndexLength = lineStartIndex;
            this.EdgeStartIndex = lineStartIndex;
            this.EdgeIndexLength = (int)(indexArray.Length - (int)lineStartIndex);
            CalBox(VertexArray.Length, IndexArray, VertexArray, out box);
        }

        public Box Box
        {
            get
            {
                if (box.Equals(default))
                {
                    CalBox(VertexArray.Length, IndexArray, VertexArray, out box);
                }
                return box;
            }
        }

        private static void CalBox(int pointNum,
        in int[] indexArray,
        in Vector4[] vertexArray, out Box box)
        {
            float[] xSpan = new float[pointNum];
            float[] ySpan = new float[pointNum];
            float[] zSpan = new float[pointNum];
            for (int i = 0; i < pointNum; i++)
            {
                var index = indexArray[i];
                if (index == Constants.STRIPBREAK)
                {
                    continue;
                }
                var p = vertexArray[index];
                xSpan[i] = p.X;
                ySpan[i] = p.Y;
                zSpan[i] = p.Z;
            }
            box = new Box
            {
                Min = new Vector3(xSpan.Min(), ySpan.Min(), zSpan.Min()),
                Max = new Vector3(xSpan.Max(), ySpan.Max(), zSpan.Max()),
            };
        }

        private static readonly int[] int32Array = [0, 1, 2, 3];


        public bool GetFaceStartIndexAndLengthByIndexArrayIndex(
            int indexArrayIndex, out int faceStartIndex,
            out int length)
        {
            faceStartIndex = 0;
            length = 0;
            var outIndex = FaceStartIndex + this.FaceIndexLength;
            if (indexArrayIndex < FaceStartIndex ||
            indexArrayIndex >= outIndex)
            {
                return false;
            }
            for (int i = 0; i < FaceStartIndexArray.Length; i++)
            {
                if (FaceStartIndexArray[i] > indexArrayIndex)
                {
                    faceStartIndex = FaceStartIndexArray[i - 1];
                    length = FaceStartIndexArray[i] - faceStartIndex;
                    return true;
                }
            }
            return false;
        }


        public bool GetEdgeStartIndexAndLengthByIndexArrayIndex(
            int indexArrayIndex, out int edgeStartIndex,
            out int length)
        {
            edgeStartIndex = 0;
            length = 0;
            var outIndex = this.EdgeIndexLength;
            if (indexArrayIndex < 0 || indexArrayIndex >= outIndex)
            {
                return false;
            }
            for (int i = 0; i < EdgeStartIndexArray.Length; i++)
            {
                if (EdgeStartIndexArray[i] > indexArrayIndex)
                {
                    edgeStartIndex = EdgeStartIndexArray[i - 1];
                    length = EdgeStartIndexArray[i] - edgeStartIndex;
                    return true;
                }
            }
            return false;
        }
        public static PartGeometry GetDefault()
        {
            int[] zero = [0];
            var result = new PartGeometry
            ([Vector4.Zero, Vector4.Zero, Vector4.Zero, Vector4.Zero],
            [Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero],
            [Vector4.Zero, Vector4.Zero, Vector4.Zero, Vector4.Zero],
            int32Array, 0, zero, zero);
            return result;
        }
    }

}
