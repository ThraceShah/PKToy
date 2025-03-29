using System;
using System.Collections.Generic;
using System.Numerics;


namespace Viewer.IContract;

// public class VertexGeometry : IGeometryData
// {
//     private readonly List<Vector4> points = [];
//     private Box? box;
//     public List<Vector4> Points => points;
//     public Box Box
//     {
//         get
//         {
//             box ??= CalBox();
//             return box.Value;
//         }
//     }

//     private Box CalBox()
//     {
//         Vector4 min = new(float.MaxValue, float.MaxValue, float.MaxValue, float.MinValue);
//         Vector4 max = new(float.MinValue, float.MinValue, float.MinValue, float.MaxValue);
//         foreach (var point in points)
//         {
//             min = Vector4.Min(min, point);
//             max = Vector4.Max(max, point);
//         }
//         return new Box
//         {
//             Min = new Vector3(min.X, min.Y, min.Z),
//             Max = new Vector3(max.X, max.Y, max.Z),
//         };
//     }

//     public ReadOnlySpan<Vector4> PointsSpan => points.ToArray();
//     public int CellCount => points.Count;
//     public int IndicesCount => points.Count;

//     public bool GetCellGeometryRange(int cell, out int startIndex, out int length)
//     {
//         if (cell < 0 || cell >= points.Count)
//         {
//             startIndex = 0;
//             length = 0;
//             return false;
//         }
//         startIndex = cell;
//         length = 1;
//         return true;
//     }

//     public unsafe void InsertNextPoint(Vector3 point)
//     {
//         int id = points.Count;
//         points.Add(new Vector4(point, *(float*)&id));
//     }

//     public unsafe void InsertNextPoint(double* point)
//     {
//         int id = points.Count;
//         points.Add(new Vector4((float)point[0], (float)point[1], (float)point[1], *(float*)&id));
//     }

// }
