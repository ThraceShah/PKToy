using static SourceSerializer.Serializer;

namespace Viewer.Geometry
{
    public partial class PartGeometry : SourceSerializer.ISourceSerilize
    {
        public void Serialize(Stream stream)
        {
            stream.WriteArray(VertexArray); // TODO: Serialize array VertexArray of unmanaged type
            stream.WriteT(VertexArrayLength); // TODO: Serialize unmanaged type VertexArrayLength
            stream.WriteArray(IndexArray); // TODO: Serialize array IndexArray of unmanaged type
            stream.WriteT(FaceStartIndex); // TODO: Serialize unmanaged type FaceStartIndex
            stream.WriteT(FaceIndexLength); // TODO: Serialize unmanaged type FaceIndexLength
            stream.WriteT(EdgeStartIndex); // TODO: Serialize unmanaged type EdgeStartIndex
            stream.WriteT(EdgeIndexLength); // TODO: Serialize unmanaged type EdgeIndexLength
            stream.WriteArray(FaceStartIndexArray); // TODO: Serialize array FaceStartIndexArray of unmanaged type
            stream.WriteArray(ProtoFaceIdArray); // TODO: Serialize array ProtoFaceIdArray of unmanaged type
            stream.WriteArray(EdgeStartIndexArray); // TODO: Serialize array EdgeStartIndexArray of unmanaged type
            stream.WriteArray(ProtoEdgeIdArray); // TODO: Serialize array ProtoEdgeIdArray of unmanaged type
            stream.WriteArray(box); // TODO: Serialize array box of unmanaged type
        }

        public void DeSerialize(Stream stream)
        {
            {
                this.VertexArray = stream.ReadArray<System.Numerics.Vector4>();
            }

            {
                this.VertexArrayLength = stream.ReadT<int>(); // TODO: Serialize unmanaged type VertexArrayLength
            }

            {
                this.IndexArray = stream.ReadArray<int>();
            }

            {
                this.FaceStartIndex = stream.ReadT<int>(); // TODO: Serialize unmanaged type FaceStartIndex
            }

            {
                this.FaceIndexLength = stream.ReadT<int>(); // TODO: Serialize unmanaged type FaceIndexLength
            }

            {
                this.EdgeStartIndex = stream.ReadT<int>(); // TODO: Serialize unmanaged type EdgeStartIndex
            }

            {
                this.EdgeIndexLength = stream.ReadT<int>(); // TODO: Serialize unmanaged type EdgeIndexLength
            }

            {
                this.FaceStartIndexArray = stream.ReadArray<int>();
            }

            {
                this.ProtoFaceIdArray = stream.ReadArray<int>();
            }

            {
                this.EdgeStartIndexArray = stream.ReadArray<int>();
            }

            {
                this.ProtoEdgeIdArray = stream.ReadArray<int>();
            }

            {
                this.box = stream.ReadArray<System.Numerics.Vector3>();
            }
        }
    }
}