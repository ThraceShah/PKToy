using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;

namespace SourceSerializer
{
    public partial class CompGeometry
    {

        public int PartIndex;

        public Matrix4x4 CompMatrix;

        public static CompGeometry GetDefault()
        {
            return new CompGeometry
            {
                PartIndex = 0,
                CompMatrix = Matrix4x4.Identity,
            };
        }


        public void Serialize(Stream stream)
        {
            stream.WriteT(PartIndex);
            stream.WriteT(CompMatrix);
        }


        public static CompGeometry DeSerialize(Stream stream)
        {
            var result = new CompGeometry
            {
                PartIndex = stream.ReadT<int>(),
                CompMatrix = stream.ReadT<Matrix4x4>()
            };
            return result;
        }


        public void ToIContract(out Viewer.IContract.CompGeometry result)
        {
            result = new Viewer.IContract.CompGeometry
            {
                PartIndex = this.PartIndex,
                CompMatrix = this.CompMatrix,
            };
        }


    }

}