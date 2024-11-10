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
                if(index== Constants.STRIPBREAK)
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
