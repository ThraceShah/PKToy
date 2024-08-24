using static SourceSerializer.Serializer;

namespace Viewer.Geometry
{
    public partial class AsmGeometry : SourceSerializer.ISourceSerilize
    {
        public void Serialize(Stream stream)
        {
            stream.WriteT(Parts.Length);
            foreach (var item in Parts)
            {
                item.Serialize(stream);
            }

            stream.WriteT(Components.Length);
            foreach (var item in Components)
            {
                item.Serialize(stream);
            }
        }

        public void DeSerialize(Stream stream)
        {
            {
                var l = stream.ReadT<int>();
                var parts = new Viewer.Geometry.PartGeometry[l];
                for (int i = 0; i < l; i++)
                {
                    var ele = new Viewer.Geometry.PartGeometry();
                    ele.DeSerialize(stream);
                    parts[i] = ele;
                }

                this.Parts = parts;
            }

            {
                var l = stream.ReadT<int>();
                var parts = new Viewer.Geometry.CompGeometry[l];
                for (int i = 0; i < l; i++)
                {
                    var ele = new Viewer.Geometry.CompGeometry();
                    ele.DeSerialize(stream);
                    parts[i] = ele;
                }

                this.Components = parts;
            }
        }
    }
}