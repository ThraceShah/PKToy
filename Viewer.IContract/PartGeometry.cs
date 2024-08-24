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
        public UnSafeArray<Vector4>? VertexArray;

        public UnSafeArray<int> IndexArray;

        public UnSafeArray<int> FaceStartIndexArray;

        public UnSafeArray<int> ProtoFaceIdArray;

        public UnSafeArray<int> EdgeStartIndexArray;

        public UnSafeArray<int> ProtoEdgeIdArray;

        public int FaceStartIndex;
        public int FaceIndexLength;

        public int EdgeStartIndex;
        public int EdgeIndexLength;


        public PartGeometry(Vector4[] vertexArray,
            int[] indexArray,
            int lineStartIndex,
            int[] faceStartIndexArray,
            int[] protoFaceIdArray,
            int[] edgeStartIndexArray,
            int[] protoEdgeIdArray)
        {
            this.VertexArray = vertexArray;
            this.IndexArray = indexArray;
            this.FaceStartIndexArray = faceStartIndexArray;
            this.ProtoFaceIdArray = protoFaceIdArray;
            this.EdgeStartIndexArray = edgeStartIndexArray;
            this.ProtoEdgeIdArray = protoEdgeIdArray;
            this.FaceStartIndex = 0;
            this.FaceIndexLength = lineStartIndex;
            this.EdgeStartIndex = lineStartIndex;
            this.EdgeIndexLength = indexArray.Length - lineStartIndex;
            CalBox(VertexArray.Value.Length, IndexArray.Span, VertexArray.Value.Span, out box);
        }

        private Box? box;

        public Box Box
        {
            get
            {
                if (box is null)
                {
                    CalBox(VertexArray.Value.Length, IndexArray.Span, VertexArray.Value.Span, out box);
                }
                return box.Value;
            }
        }

        private static void CalBox(int pointNum,
        in ReadOnlySpan<int> indexArray,
        in ReadOnlySpan<Vector4> vertexArray, out Box? box)
        {
            float[] xSpan = new float[pointNum];
            float[] ySpan = new float[pointNum];
            float[] zSpan = new float[pointNum];
            for (int i = 0; i < pointNum; i++)
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

        private static readonly int[] int32Array = new[] { 0, 1, 2, 3 };


        public bool GetFaceStartIndexAndLengthByIndexArrayIndex(
            int indexArrayIndex, out int faceStartIndex,
            out int length)
        {
            faceStartIndex = -1;
            length = -1;
            var outIndex = FaceStartIndex + this.FaceIndexLength;
            if (indexArrayIndex < FaceStartIndex ||
            indexArrayIndex >= outIndex)
            {
                return false;
            }
            var span = FaceStartIndexArray.Span;
            for (int i = 0; i < FaceStartIndexArray.Length; i++)
            {
                if (span[i] > indexArrayIndex)
                {
                    faceStartIndex = span[i - 1];
                    length = span[i] - faceStartIndex;
                    return true;
                }
            }
            return false;
        }

        public bool GetFaceStartIndexAndLengthByProtoFaceId(int protoFaceId,
            out int faceStartIndex, out int length)
        {
            var span = ProtoFaceIdArray.Span;
            for (int i = 0; i < ProtoFaceIdArray.Length - 1; i++)
            {
                if (span[i] == protoFaceId)
                {
                    length = span[i + 1] - span[i];
                    faceStartIndex = span[i];
                    return true;
                }
            }
            faceStartIndex = -1;
            length = -1;
            return false;
        }

        public bool GetEdgeStartIndexAndLengthByIndexArrayIndex(
            int indexArrayIndex, out int edgeStartIndex,
            out int length)
        {
            edgeStartIndex = -1;
            length = -1;
            var outIndex = this.EdgeIndexLength;
            if (indexArrayIndex < 0 || indexArrayIndex >= outIndex)
            {
                return false;
            }
            var span = EdgeStartIndexArray.Span;
            for (int i = 0; i < EdgeStartIndexArray.Length; i++)
            {
                if (span[i] > indexArrayIndex)
                {
                    edgeStartIndex = span[i - 1];
                    length = span[i] - edgeStartIndex;
                    return true;
                }
            }
            return false;
        }

        public bool GetEdgeStartIndexAndLengthByProtoEdgeId(int protoEdgeId,
            out int edgeStartIndex, out int length)
        {
            var span = ProtoEdgeIdArray.Span;
            for (int i = 0; i < ProtoEdgeIdArray.Length - 1; i++)
            {
                if (span[i] == protoEdgeId)
                {
                    length = span[i + 1] - span[i];
                    edgeStartIndex = span[i];
                    return true;
                }
            }
            edgeStartIndex = -1;
            length = -1;
            return false;
        }


        public static PartGeometry GetDefault()
        {
            int[] zero = new int[] { 0 };
            var result = new PartGeometry
            (new Vector4[] { Vector4.Zero, Vector4.Zero, Vector4.Zero, Vector4.Zero },
            int32Array, 0, zero, zero, zero, zero);
            return result;
        }

        public readonly void Dispose()
        {
            VertexArray?.Dispose();
            IndexArray.Dispose();
            FaceStartIndexArray.Dispose();
            ProtoFaceIdArray.Dispose();
            EdgeStartIndexArray.Dispose();
            ProtoEdgeIdArray.Dispose();
        }
    
    }

}
