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
    public struct PartGeometry:IDisposable
    {
        public UnSafeArray<Vector4> VertexArray;
        public UnSafeArray<Vector3> NormalArray;
        public UnSafeArray<uint> IndexArray;

        public UnSafeArray<uint> FaceStartIndexArray;

        public UnSafeArray<uint> EdgeStartIndexArray;

        public uint FaceStartIndex;
        public uint FaceIndexLength;

        public uint EdgeStartIndex;
        public uint EdgeIndexLength;
        private unsafe Box box;


        public PartGeometry(Vector4[] vertexArray,
            Vector3[] normalArray,
            uint[] indexArray,
            uint lineStartIndex,
            uint[] faceStartIndexArray,
            uint[] edgeStartIndexArray)
        {
            this.VertexArray = vertexArray;
            this.NormalArray = normalArray;
            this.IndexArray = indexArray;
            this.FaceStartIndexArray = faceStartIndexArray;
            this.EdgeStartIndexArray = edgeStartIndexArray;
            this.FaceStartIndex = 0;
            this.FaceIndexLength = lineStartIndex;
            this.EdgeStartIndex = lineStartIndex;
            this.EdgeIndexLength = (uint)(indexArray.Length - (int)lineStartIndex);
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

        private static void CalBox(uint pointNum,
        in UnSafeArray<uint> indexArray,
        in UnSafeArray<Vector4> vertexArray, out Box box)
        {
            float[] xSpan = new float[pointNum];
            float[] ySpan = new float[pointNum];
            float[] zSpan = new float[pointNum];
            for (uint i = 0; i < pointNum; i++)
            {
                var index = indexArray[i];
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

        private static readonly uint[] int32Array = new[] { 0u, 1u, 2u, 3u };


        public bool GetFaceStartIndexAndLengthByIndexArrayIndex(
            uint indexArrayIndex, out uint faceStartIndex,
            out uint length)
        {
            faceStartIndex = 0;
            length = 0;
            var outIndex = FaceStartIndex + this.FaceIndexLength;
            if (indexArrayIndex < FaceStartIndex ||
            indexArrayIndex >= outIndex)
            {
                return false;
            }
            for (uint i = 0; i < FaceStartIndexArray.Length; i++)
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
            uint indexArrayIndex, out uint edgeStartIndex,
            out uint length)
        {
            edgeStartIndex = 0;
            length = 0;
            var outIndex = this.EdgeIndexLength;
            if (indexArrayIndex < 0 || indexArrayIndex >= outIndex)
            {
                return false;
            }
            for (uint i = 0; i < EdgeStartIndexArray.Length; i++)
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
            uint[] zero = new uint[] { 0 };
            var result = new PartGeometry
            ([Vector4.Zero, Vector4.Zero, Vector4.Zero, Vector4.Zero],
            [Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero],
            int32Array, 0, zero, zero);
            return result;
        }

        public readonly void Dispose()
        {
            VertexArray.Dispose();
            IndexArray.Dispose();
            FaceStartIndexArray.Dispose();
            EdgeStartIndexArray.Dispose();
        }
    
    }

}
