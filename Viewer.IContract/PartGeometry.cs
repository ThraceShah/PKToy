using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Viewer.IContract;
public static class Constants
{
    public const int STRIPBREAK = -1;
}

public static class PrivateAccessor
{
    // [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_items")]
    // extern static ref T[] GetListInternalArray<T>(List<T> list);

    // [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_items")]
    // extern static ref Vector4[] GetListInternalArrayVec4(List<Vector4> list);
    // [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_items")]
    // extern static ref Vector3[] GetListInternalArray(List<Vector3> list);
    // [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_items")]
    // extern static ref uint[] GetListInternalArray(List<uint> list);

    // [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_items")]
    // extern static ref int[] GetListInternalArray(List<int> list);


    public static void Init()
    { }

    public static T[] GetListInternalArray<T>(List<T> a)
    {
        return ((Func<List<T>, T[]>)cache[typeof(T)])(a);
    }

    static void Compile<T>()
    {
        var parameter = Expression.Parameter(typeof(List<T>), "x");
        var field = Expression.Field(parameter, typeof(List<T>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance));
        var lambda = Expression.Lambda<Func<List<T>, T[]>>(field, parameter);
        var func = lambda.Compile();
        cache[typeof(T)] = func;
    }

    static PrivateAccessor()
    {
        Compile<Vector4>();
        Compile<Vector3>();
        Compile<uint>();
        Compile<int>();
    }

    static readonly Dictionary<Type, Delegate> cache = [];

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

public class StripFace
{
    private readonly List<Vector4> points = [];
    private readonly List<Vector3> normals = [];
    private readonly List<uint> colors = [];
    private readonly List<int> indices = [];
    public List<Vector4> Points => points;
    public List<Vector3> Normals => normals;
    public List<uint> Colors => colors;
    public List<int> Indices => indices;
    public int IndicesCount => indices.Count;

    public unsafe void InsertNextPoint(Vector3 point, Vector3 normal, uint color)
    {
        points.Add(new Vector4(point, 0));
        normals.Add(normal);
        colors.Add(color);
        indices.Add(points.Count - 1);
    }

    public unsafe void InsertNextPoint(double* point, double* normal, uint color)
    {
        points.Add(new Vector4((float)point[0], (float)point[1], (float)point[2], 0));
        normals.Add(new Vector3((float)normal[0], (float)normal[1], (float)normal[2]));
        colors.Add(color);
        indices.Add(points.Count - 1);
    }

    public void InsertNextStrip()
    {
        indices.Add(Constants.STRIPBREAK);
    }

}
public unsafe class StripFacePart : IGeometryData
{
    private readonly Dictionary<int, StripFace> tagFaces = [];
    public Dictionary<int, StripFace> TagFaces => tagFaces;

    private readonly Dictionary<int, int> tagIndexMap = [];
    public Dictionary<int, int> TagIndexMap => tagIndexMap;
    private readonly Dictionary<int, int> indexTagMap = [];
    public Dictionary<int, int> IndexTagMap => indexTagMap;

    public bool IsModified { get; private set; }


    public Box Box
    {
        get
        {
            Vector4 min = new(float.MaxValue, float.MaxValue, float.MaxValue, float.MinValue);
            Vector4 max = new(float.MinValue, float.MinValue, float.MinValue, float.MaxValue);
            foreach (var face in tagFaces.Values)
            {
                foreach (var point in face.Points)
                {
                    min = Vector4.Min(min, point);
                    max = Vector4.Max(max, point);
                }
            }
            return new Box
            {
                Min = new Vector3(min.X, min.Y, min.Z),
                Max = new Vector3(max.X, max.Y, max.Z),
            };
        }
    }

    public int CellCount => tagFaces.Count;

    private int indicesCount = 0;
    public int IndicesCount => indicesCount;

    private int verticesCount = 0;

    public bool GetCellGeometryRange(int cellIndex, out int startCell, out int length)
    {
        startCell = 0;
        length = 0;
        if (indexTagMap.ContainsKey(cellIndex) == false)
        {
            return false;
        }
        var tag = indexTagMap[cellIndex];
        var target = tagFaces[tag];
        foreach (var face in tagFaces.Values)
        {
            if (face == target)
            {
                break;
            }
            startCell += face.IndicesCount;
        }
        length = target.IndicesCount;
        return true;
    }

    public void Modified()
    {
        IsModified = true;
        indicesCount = tagFaces.Values.Sum(f => f.IndicesCount);
        verticesCount = tagFaces.Values.Sum(f => f.Points.Count);
    }

    public void UpdateCells()
    {
        tagIndexMap.Clear();
        indexTagMap.Clear();
        int index = 0;
        foreach (var tag in tagFaces.Keys)
        {
            tagIndexMap[tag] = index;
            indexTagMap[index] = tag;
            index++;
        }
    }

    public UnSafeArray<Vector4> GetPoints()
    {
        IsModified = false;
        var array = new UnSafeArray<Vector4>(verticesCount);
        var arraySpan = array.Span;
        int start = 0;
        int index = 0;
        tagIndexMap.Clear();
        indexTagMap.Clear();
        foreach (var pair in tagFaces)
        {
            var face = pair.Value;
            var points = PrivateAccessor.GetListInternalArray(face.Points).AsSpan();
            var facePoints = arraySpan[start..];
            points[..face.Points.Count].CopyTo(facePoints);
            start += face.Points.Count;
            foreach (ref var point in facePoints)
            {
                point.W = *(float*)&index;
            }
            var tag = pair.Key;
            tagIndexMap[tag] = index;
            indexTagMap[index] = tag;
            index++;
        }
        return array;
    }

    public UnSafeArray<Vector3> GetNormals()
    {
        var array = new UnSafeArray<Vector3>(verticesCount);
        var arraySpan = array.Span;
        int start = 0;
        foreach (var face in tagFaces.Values)
        {
            var normals = PrivateAccessor.GetListInternalArray(face.Normals).AsSpan();
            normals[..face.Normals.Count].CopyTo(arraySpan[start..]);
            start += face.Normals.Count;
        }
        return array;
    }

    public UnSafeArray<uint> GetColors()
    {
        var array = new UnSafeArray<uint>(verticesCount);
        var arraySpan = array.Span;
        int start = 0;
        foreach (var face in tagFaces.Values)
        {
            var colors = PrivateAccessor.GetListInternalArray(face.Colors).AsSpan();
            colors[..face.Colors.Count].CopyTo(arraySpan[start..]);
            start += face.Colors.Count;
        }
        return array;
    }

    public UnSafeArray<int> GetIndices()
    {
        var array = new UnSafeArray<int>(IndicesCount);
        int i = 0;
        int pointStart = 0;
        foreach (var face in tagFaces.Values)
        {
            foreach (var index in face.Indices)
            {
                if (index == Constants.STRIPBREAK)
                {
                    array[i] = Constants.STRIPBREAK;
                }
                else
                {
                    array[i] = index + pointStart;
                }
                i++;
            }
            pointStart += face.Points.Count;
        }
        return array;
    }

}

public class Edge(List<Vector4> points, List<int> indices)
{
    public List<Vector4> Points => points;
    public List<int> Indices => indices;
    public int IndicesCount => indices.Count;

}

public class EdgeBuilder
{
    private readonly Dictionary<Vector4, int> pointIndexMap = [];
    private readonly List<Vector4> points = [];
    private readonly List<int> indices = [];

    public Edge Build()
    {
        return new Edge(points, indices);
    }

    public unsafe void InsertNextPoint(Vector3 point)
    {
        var point4 = new Vector4(point, 0);
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
        var point4 = new Vector4((float)point[0], (float)point[1], (float)point[2], 0);
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

public unsafe class EdgePart : IGeometryData
{
    private readonly Dictionary<int, Edge> tagEdges = [];
    public Dictionary<int, Edge> TagEdges => tagEdges;

    private readonly Dictionary<int, int> tagIndexMap = [];
    public Dictionary<int, int> TagIndexMap => tagIndexMap;
    private readonly Dictionary<int, int> indexTagMap = [];
    public Dictionary<int, int> IndexTagMap => indexTagMap;

    public bool IsModified { get; private set; }

    public Box Box
    {
        get
        {
            Vector4 min = new(float.MaxValue, float.MaxValue, float.MaxValue, float.MinValue);
            Vector4 max = new(float.MinValue, float.MinValue, float.MinValue, float.MaxValue);
            foreach (var edge in tagEdges.Values)
            {
                foreach (var point in edge.Points)
                {
                    min = Vector4.Min(min, point);
                    max = Vector4.Max(max, point);
                }
            }
            return new Box
            {
                Min = new Vector3(min.X, min.Y, min.Z),
                Max = new Vector3(max.X, max.Y, max.Z),
            };
        }
    }

    public int CellCount => tagEdges.Count;

    private int indicesCount = 0;
    public int IndicesCount => indicesCount;

    private int verticesCount = 0;

    public bool GetCellGeometryRange(int cellIndex, out int startCell, out int length)
    {
        startCell = 0;
        length = 0;
        if (indexTagMap.ContainsKey(cellIndex) == false)
        {
            return false;
        }
        var tag = indexTagMap[cellIndex];
        var target = tagEdges[tag];
        foreach (var edge in tagEdges.Values)
        {
            if (edge == target)
            {
                break;
            }
            startCell += edge.IndicesCount;
        }
        length = target.IndicesCount;
        return true;
    }

    public void Modified()
    {
        IsModified = true;
        var edges = tagEdges.Values;
        indicesCount = edges.Sum(f => f.IndicesCount);
        verticesCount = edges.Sum(f => f.Points.Count);
    }

    public void UpdateCells()
    {
        tagIndexMap.Clear();
        indexTagMap.Clear();
        int index = 0;
        foreach (var tag in tagEdges.Keys)
        {
            tagIndexMap[tag] = index;
            indexTagMap[index] = tag;
            index++;
        }
    }

    public UnSafeArray<Vector4> GetPoints()
    {
        IsModified = false;
        var array = new UnSafeArray<Vector4>(verticesCount);
        var arraySpan = array.Span;
        int start = 0;
        int index = 0;
        tagIndexMap.Clear();
        indexTagMap.Clear();
        foreach (var pair in tagEdges)
        {
            var edge = pair.Value;
            var points = PrivateAccessor.GetListInternalArray(edge.Points).AsSpan();
            var edgePoints = arraySpan[start..];
            points[..edge.Points.Count].CopyTo(edgePoints);
            start += edge.Points.Count;
            foreach (ref var point in edgePoints)
            {
                point.W = *(float*)&index;
            }
            var tag = pair.Key;
            tagIndexMap[tag] = index;
            indexTagMap[index] = tag;
            index++;
        }
        return array;
    }

    public UnSafeArray<int> GetIndices()
    {
        var array = new UnSafeArray<int>(IndicesCount);
        int i = 0;
        int pointStart = 0;
        foreach (var edge in tagEdges.Values)
        {
            foreach (var index in edge.Indices)
            {
                array[i] = index + pointStart;
                i++;
            }
            pointStart += edge.Points.Count;
        }
        return array;
    }

}

public class EdgePartBuilder
{
    private readonly Dictionary<int, EdgeBuilder> tagEdgeBuilders = [];
    public Dictionary<int, EdgeBuilder> TagEdgeBuilders => tagEdgeBuilders;

    public EdgePart Build()
    {
        var part = new EdgePart();
        foreach (var pair in tagEdgeBuilders)
        {
            part.TagEdges[pair.Key] = pair.Value.Build();
        }
        return part;
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


