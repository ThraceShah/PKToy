using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NativeCorLib;


namespace Viewer.IContract;
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

    Int64 OutPutSize { get; }

}
public class StripFace
{
    private uint color = 0xFFFFFFFF;
    private readonly UMList<Vector3> points = new();
    private readonly UMList<Vector3> normals = new();
    private readonly UMList<int> indices = new();
    public UMList<Vector3> Points => points;
    public UMList<Vector3> Normals => normals;
    public UMList<int> Indices => indices;
    public int IndicesCount => indices.Count;
    public uint Color => color;

    public void SetColor(uint color)
    {
        this.color = color;
    }

    public void SetColor(byte r, byte g, byte b, byte a)
    {
        color = (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }

    public void SetColor(Vector4 color)
    {
        SetColor((byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255), (byte)(color.W * 255));
    }

    public void SetColor(float r, float g, float b, float a)
    {
        SetColor((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), (byte)(a * 255));
    }

    public void SetColor(double r, double g, double b, double a)
    {
        SetColor((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), (byte)(a * 255));
    }

    public unsafe void InsertNextPoint(Vector3 point, Vector3 normal)
    {
        points.Add(point);
        normals.Add(normal);
        indices.Add(points.Count - 1);
    }

    public unsafe void InsertNextPoint(double* point, double* normal)
    {
        points.Add(new Vector3((float)point[0], (float)point[1], (float)point[2]));
        normals.Add(new Vector3((float)normal[0], (float)normal[1], (float)normal[2]));
        indices.Add(points.Count - 1);
    }

    public void InsertNextStrip()
    {
        indices.Add(Constants.STRIPBREAK);
    }

    public void Fit()
    {
        points.Fit();
        normals.Fit();
        indices.Fit();
    }

}


public readonly ref struct StripFacePartOutput(UnSafeArray<Vector3> points, UnSafeArray<Vector3> normals, UnSafeArray<uint> colors, UnSafeArray<int> indices, UnSafeArray<int> indicesCells)
{
    public UnSafeArray<Vector3> Points => points;
    public UnSafeArray<Vector3> Normals => normals;
    public UnSafeArray<uint> Colors => colors;
    public UnSafeArray<int> Indices => indices;
    public UnSafeArray<int> IndicesCells => indicesCells;
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
            Vector3 min = new(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new(float.MinValue, float.MinValue, float.MinValue);
            foreach (var face in tagFaces.Values)
            {
                foreach (var point in face.Points)
                {
                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                }
            }
            return new Box
            {
                Min = min,
                Max = max,
            };
        }
    }

    public int CellCount => tagFaces.Count;

    private int indicesCount = 0;
    public int IndicesCount => indicesCount;

    private int verticesCount = 0;

    private Int64 outPutSize = 0;
    public Int64 OutPutSize => outPutSize;

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

        indicesCount = 0;
        verticesCount = 0;
        outPutSize = 0;
        foreach (var face in tagFaces.Values)
        {
            indicesCount += face.IndicesCount;
            verticesCount += face.Points.Count;
            face.Fit();
        }
        outPutSize = (Int64)indicesCount * sizeof(int) + (Int64)verticesCount * (sizeof(Vector3) * 2 + sizeof(uint) + sizeof(int));
    }

    public StripFacePartOutput Update(nint memoryBlock)
    {
        IsModified = false;
        tagIndexMap.Clear();
        indexTagMap.Clear();

        // var points = memoryPool.Rent<Vector3>(verticesCount);
        // var normals = memoryPool.Rent<Vector3>(verticesCount);
        // var colors = memoryPool.Rent<uint>(verticesCount);
        // var indices = memoryPool.Rent<int>(indicesCount);
        // var indicesCells = memoryPool.Rent<int>(verticesCount);

        var points = new UnSafeArray<Vector3>((Vector3*)memoryBlock, verticesCount);
        int offset = sizeof(Vector3) * verticesCount;
        var normals = new UnSafeArray<Vector3>((Vector3*)(memoryBlock + offset), verticesCount);
        offset += sizeof(Vector3) * verticesCount;
        var colors = new UnSafeArray<uint>((uint*)(memoryBlock + offset), verticesCount);
        offset += sizeof(uint) * verticesCount;
        var indices = new UnSafeArray<int>((int*)(memoryBlock + offset), indicesCount);
        offset += sizeof(int) * indicesCount;
        var indicesCells = new UnSafeArray<int>((int*)(memoryBlock + offset), verticesCount);
        var pointsSpan = points.Span;
        var normalsSpan = normals.Span;


        int i = 0;
        int pointsStart = 0;
        int cell = 0;
        foreach (var pair in tagFaces)
        {
            var face = pair.Value;
            var srcFacePoints = face.Points.AsSpan();
            srcFacePoints[..face.Points.Count].CopyTo(pointsSpan[pointsStart..]);

            var srcNormals = face.Normals.AsSpan();
            srcNormals[..face.Normals.Count].CopyTo(normalsSpan[pointsStart..]);

            foreach (var index in face.Indices)
            {
                if (index == Constants.STRIPBREAK)
                {
                    indices[i] = Constants.STRIPBREAK;
                }
                else
                {
                    indices[i] = index + pointsStart;
                }
                i++;
            }
            indicesCells.Slice(pointsStart, face.Points.Count).Fill(cell);
            colors.Slice(pointsStart, face.Points.Count).Fill(face.Color);

            pointsStart += face.Points.Count;

            var tag = pair.Key;
            tagIndexMap[tag] = cell;
            indexTagMap[cell] = tag;
            cell++;
        }

        return new StripFacePartOutput(points, normals, colors, indices, indicesCells);
    }

}

public class Edge(UMList<Vector3> points, UMList<int> indices)
{
    public UMList<Vector3> Points => points;
    public UMList<int> Indices => indices;
    public int IndicesCount => indices.Count;

    public void Fit()
    {
        points.Fit();
        indices.Fit();
    }

}

public class EdgeBuilder
{
    private readonly Dictionary<Vector3, int> pointIndexMap = [];
    private readonly UMList<Vector3> points = new();
    private readonly UMList<int> indices = new();

    public Edge Build()
    {
        points.Fit();
        indices.Fit();
        return new Edge(points, indices);
    }

    public unsafe void InsertNextPoint(Vector3 point)
    {
        if (pointIndexMap.TryGetValue(point, out var index))
        {
            indices.Add(index);
        }
        else
        {
            points.Add(point);
            index = points.Count - 1;
            indices.Add(index);
            pointIndexMap.Add(point, index);
        }
    }

    public unsafe void InsertNextPoint(double* point)
    {
        var point4 = new Vector3((float)point[0], (float)point[1], (float)point[2]);
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

public readonly ref struct EdgePartOutput(UnSafeArray<Vector3> points, UnSafeArray<int> indices, UnSafeArray<int> indicesCells)
{
    public UnSafeArray<Vector3> Points => points;
    public UnSafeArray<int> Indices => indices;
    public UnSafeArray<int> IndicesCells => indicesCells;
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
            Vector3 min = new(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new(float.MinValue, float.MinValue, float.MinValue);
            foreach (var edge in tagEdges.Values)
            {
                foreach (var point in edge.Points)
                {
                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                }
            }
            return new Box
            {
                Min = min,
                Max = max,
            };
        }
    }

    public int CellCount => tagEdges.Count;

    private int indicesCount = 0;
    public int IndicesCount => indicesCount;

    private int verticesCount = 0;

    private Int64 outPutSize = 0;
    public Int64 OutPutSize => outPutSize;

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
        indicesCount = 0;
        verticesCount = 0;
        outPutSize = 0;
        foreach (var edge in tagEdges.Values)
        {
            indicesCount += edge.IndicesCount;
            verticesCount += edge.Points.Count;
            edge.Fit();
        }
        outPutSize = (Int64)indicesCount * sizeof(int) + (Int64)verticesCount * (sizeof(Vector3) + sizeof(int));
    }

    public EdgePartOutput Update(nint memoryBlock)
    {
        IsModified = false;
        tagIndexMap.Clear();
        indexTagMap.Clear();
        // var points = memoryPool.Rent<Vector3>(verticesCount);
        // var indices = memoryPool.Rent<int>(indicesCount);
        // var indicesCells = memoryPool.Rent<int>(verticesCount);
        var points = new UnSafeArray<Vector3>((Vector3*)memoryBlock, verticesCount);
        int offset = sizeof(Vector3) * verticesCount;
        var indices = new UnSafeArray<int>((int*)(memoryBlock + offset), indicesCount);
        offset += sizeof(int) * indicesCount;
        var indicesCells = new UnSafeArray<int>((int*)(memoryBlock + offset), verticesCount);

        var pointsSpan = points.Span;
        int pointsStart = 0;
        int i = 0;
        int cell = 0;
        foreach (var pair in tagEdges)
        {
            var edge = pair.Value;
            var edgeSrcPoints = edge.Points.AsSpan();
            var edgePoints = pointsSpan[pointsStart..];
            edgeSrcPoints[..edge.Points.Count].CopyTo(edgePoints);

            foreach (var index in edge.Indices)
            {
                indices[i] = index + pointsStart;
                i++;
            }
            indicesCells.Slice(pointsStart, edge.Points.Count).Fill(cell);

            pointsStart += edge.Points.Count;
            var tag = pair.Key;
            tagIndexMap[tag] = cell;
            indexTagMap[cell] = tag;
            cell++;
        }

        return new EdgePartOutput(points, indices, indicesCells);

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
