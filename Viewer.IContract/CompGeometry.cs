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
    public class CompGeometry
    {

        public uint PartIndex;

        public Matrix4x4 CompMatrix;

        public static CompGeometry GetDefault()
        {
            return new CompGeometry
            {
                PartIndex = 0,
                CompMatrix = Matrix4x4.Identity,
            };
        }
    }


}
