using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SourceSerializer
{
    public partial class PartGeometry
    {
        /// <summary>
        /// 顶点数组,xyz是顶点坐标,w是图元(面或者线)的id(类型是float,实际使用按位转成int)
        /// </summary>
        public Vector4[] VertexArray;

        /// <summary>
        /// 顶点数组的长度
        /// </summary>
        public int VertexArrayLength;

        /// <summary>
        /// 索引数组
        /// </summary>
        public int[] IndexArray;

        /// <summary>
        /// 面的起始索引,在indexarray中的位置,目前永远都是0
        /// </summary>
        public int FaceStartIndex;
        /// <summary>
        /// 所有面的索引长度,面的索引区域 [FaceStartIndex,FaceStartIndex+FaceIndexLength)
        /// </summary>
        public int FaceIndexLength;
        /// <summary>
        /// 线条的起始索引,在indexarray中的位置
        /// </summary>
        public int EdgeStartIndex;
        /// <summary>
        /// 所有线的索引长度,面的索引区域 [EdgeStartIndex,EdgeStartIndex+EdgeIndexLength)
        /// </summary>/
        public int EdgeIndexLength;

        /// <summary>
        /// 这个数组是保存的Face的起始索引,因为同一个face在indexarray是连续存放的,
        /// 所以当面face的所有顶点索引,等于查找面的下一个面的起始索引和当前面的起始索引中间的所有索引
        /// </summary>
        public int[] FaceStartIndexArray;

        /// <summary>
        /// 面的protobuf里面的面id,和FaceStartIndexArray的长度是一致的,且面是一一对应的
        /// </summary>
        public int[] ProtoFaceIdArray;

        /// <summary>
        /// 这个数组是保存的Edge的起始索引,因为同一个edge在indexarray是连续存放的,
        /// 所以当edge的所有顶点索引,等于查找线的下一个线的起始索引和当前线的起始索引中间的所有索引
        /// </summary>
        public int[] EdgeStartIndexArray;

        /// <summary>
        /// 线的protobuf里面的面id,和EdgeStartIndexArray的长度是一致的,且面是一一对应的
        /// </summary>
        public int[] ProtoEdgeIdArray;

        public PartGeometry()
        {

        }

        public void ToIContract(out Viewer.IContract.PartGeometry result)
        {
            result = new Viewer.IContract.PartGeometry(
                this.VertexArray,
                this.IndexArray,
                this.FaceIndexLength,
                FaceStartIndexArray,
                ProtoFaceIdArray,
                EdgeStartIndexArray,
                ProtoEdgeIdArray
            );
        }



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
            this.EdgeStartIndex = lineStartIndex;
            this.FaceStartIndexArray = faceStartIndexArray;
            this.ProtoFaceIdArray = protoFaceIdArray;
            this.EdgeStartIndexArray = edgeStartIndexArray;
            this.ProtoEdgeIdArray = protoEdgeIdArray;
            this.FaceStartIndex = 0;
            this.FaceIndexLength = lineStartIndex;
            //末尾有两个指向原点的点,是多余的
            this.EdgeIndexLength = indexArray.Length - lineStartIndex - 2;
            VertexArrayLength = vertexArray.Length;
            box = MemoryMarshal.Cast<float, Vector3>(this.CalBox()).ToArray();
        }

        private Vector3[] box;

        public Vector3[] Box
        {
            get
            {
                box ??= MemoryMarshal.Cast<float, Vector3>(this.CalBox()).ToArray();
                return box;
            }
        }


        private float[] CalBox()
        {
            var pointNum = VertexArray.Length;
            float[] xSpan = new float[pointNum];
            float[] ySpan = new float[pointNum];
            float[] zSpan = new float[pointNum];
            for (int i = 0; i < pointNum; i++)
            {
                var index = IndexArray[i];
                var p = VertexArray[index];
                xSpan[i] = p.X;
                ySpan[i] = p.Y;
                zSpan[i] = p.Z;
            }
            return new float[]{
                xSpan.Min(),ySpan.Min(),zSpan.Min(),xSpan.Max(),ySpan.Max(),zSpan.Max()
            };
        }

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

        public bool GetFaceStartIndexAndLengthByProtoFaceId(int protoFaceId,
            out int faceStartIndex, out int length)
        {
            for (int i = 0; i < ProtoFaceIdArray.Length - 1; i++)
            {
                if (ProtoFaceIdArray[i] == protoFaceId)
                {
                    length = FaceStartIndexArray[i + 1] - FaceStartIndexArray[i];
                    faceStartIndex = FaceStartIndexArray[i];
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

        public bool GetEdgeStartIndexAndLengthByProtoEdgeId(int protoEdgeId,
            out int edgeStartIndex, out int length)
        {
            for (int i = 0; i < ProtoEdgeIdArray.Length - 1; i++)
            {
                if (ProtoEdgeIdArray[i] == protoEdgeId)
                {
                    length = EdgeStartIndexArray[i + 1] - EdgeStartIndexArray[i];
                    edgeStartIndex = EdgeStartIndexArray[i];
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
            new[] { 0, 1, 2, 3 }, 0, zero, zero, zero, zero);
            return result;
        }

        public void Serialize(Stream stream)
        {
            stream.WriteArray(VertexArray);
            stream.WriteArray(IndexArray);
            stream.WriteT(EdgeStartIndex);
            stream.WriteArray(FaceStartIndexArray);
            stream.WriteArray(ProtoFaceIdArray);
            stream.WriteArray(EdgeStartIndexArray);
            stream.WriteArray(ProtoEdgeIdArray);
        }


        public static PartGeometry DeSerialize(Stream stream)
        {
            var v = stream.ReadArray<Vector4>();
            var i = stream.ReadArray<int>();
            var esi = stream.ReadT<int>();
            var fsia = stream.ReadArray<int>();
            var pfia = stream.ReadArray<int>();
            var esia = stream.ReadArray<int>();
            var peia = stream.ReadArray<int>();
            var result = new PartGeometry(v, i, esi, fsia, pfia, esia, peia);
            return result;
        }



    }

}