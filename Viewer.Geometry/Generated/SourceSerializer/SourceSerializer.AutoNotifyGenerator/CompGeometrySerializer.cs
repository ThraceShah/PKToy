using static SourceSerializer.Serializer;

namespace Viewer.Geometry
{
    public partial class CompGeometry : SourceSerializer.ISourceSerilize
    {
        public void Serialize(Stream stream)
        {
            stream.WriteT(PartIndex); // TODO: Serialize unmanaged type PartIndex
            stream.WriteT(CompMatrix); // TODO: Serialize unmanaged type CompMatrix
        }

        public void DeSerialize(Stream stream)
        {
            {
                this.PartIndex = stream.ReadT<int>(); // TODO: Serialize unmanaged type PartIndex
            }

            {
                this.CompMatrix = stream.ReadT<System.Numerics.Matrix4x4>(); // TODO: Serialize unmanaged type CompMatrix
            }
        }
    }
}