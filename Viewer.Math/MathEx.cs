using System;

namespace Viewer.Math;

public class MathEx
{
    public static float Radians(float degrees)
    {
        const float a= (float)System.Math.PI / 180f;
        return a * degrees;
    }
}
