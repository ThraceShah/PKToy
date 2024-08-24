using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;

namespace Viewer.Math
{
    public static class NumericsEx
    {
        public static unsafe Vector3 ToVector3(this Vector4 vec)
        {
            return *(Vector3*)&vec;
        }

        public static unsafe void ToVector3(this Vector4 vec,out Vector3 result)
        {
            result= *(Vector3*)&vec;
        }

    }
}
