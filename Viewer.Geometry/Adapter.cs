using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Viewer.Geometry
{
    public class Adapter
    {
        public static void GetGeometryByPath(string fileName, out Viewer.IContract.AsmGeometry result)
        {
            using var stream = new FileStream(fileName, FileMode.Open,FileAccess.Read);
            var geometry = new AsmGeometry();
            geometry.DeSerialize(stream);
            geometry.ToIContract(out result);
        }

    }
}
