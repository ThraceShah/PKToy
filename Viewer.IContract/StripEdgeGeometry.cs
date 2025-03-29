using System;
using System.Collections.Generic;
using System.Numerics;


namespace Viewer.IContract;

// public class StripEdgeGeometry : IGeometryData
// {
//     private readonly List<Vector4> points = [];
//     private readonly List<int> indices = [];
//     private readonly List<int> cells = [];
//     private Box? box;

//     public List<Vector4> Points => points;
//     public List<int> Indices => indices;
//     public List<int> Cells => cells;

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
//     public ReadOnlySpan<int> IndicesSpan => indices.ToArray();
//     public ReadOnlySpan<int> CellsSpan => cells.ToArray();
//     public int CellCount => cells.Count;
//     public int IndicesCount => indices.Count;

//     public bool GetCellGeometryRange(int cell, out int startIndex, out int length)
//     {
//         if (cell < 0 || cell >= cells.Count)
//         {
//             startIndex = 0;
//             length = 0;
//             return false;
//         }
//         startIndex = cells[cell];
//         if (cell + 1 >= cells.Count)
//         {
//             length = indices.Count - startIndex;
//             return true;
//         }
//         length = cells[cell + 1] - startIndex;
//         return true;
//     }

//     public void InsertNextCell()
//     {
//         indices.Add(Constants.STRIPBREAK);
//         cells.Add(indices.Count);
//     }

//     public unsafe void InsertNextPoint(Vector3 point)
//     {
//         int id = cells.Count;
//         points.Add(new Vector4(point, *(float*)&id));
//         indices.Add(points.Count - 1);
//     }

//     public unsafe void InsertNextPoint(double* point)
//     {
//         int id = cells.Count;
//         points.Add(new Vector4((float)point[0], (float)point[1], (float)point[2], *(float*)&id));
//         indices.Add(points.Count - 1);
//     }


// }
