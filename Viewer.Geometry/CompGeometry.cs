using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using SourceSerializer;

namespace Viewer.Geometry
{
    [SourceSerializeAttribute]
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