using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SourceSerializer
{
    public class Adapter
    {
        public static void GetGeometryByPath(string fileName,out Viewer.IContract.AsmGeometry result)
        {
            using var stream = new FileStream(fileName, FileMode.Open);
            var geometry = AsmGeometry.DeSerialize(stream);
            geometry.ToIContract(out result);
        }

    }
}
