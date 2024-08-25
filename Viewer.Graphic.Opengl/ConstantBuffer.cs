using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using Matrix = System.Numerics.Matrix4x4;

namespace Viewer.Graphic.Opengl;

public struct VSConstantBuffer
{
    public Matrix world;
    public Matrix view;
    public Matrix projection;
    public Matrix translation;
    public Matrix origin;

    public static VSConstantBuffer GetDefault(float width = 800f, float height = 600f)
    {
        var eye = new Vector3(0f, 0f, -16f);
        var target = Vector3.Zero;
        var up = new Vector3(0f, 1f, 0f);
        var view = Matrix.CreateLookAt(eye, target, up);
        //Matrix4x4 proj = Matrix.CreatePerspectiveFieldOfView(1.570796327f, width / height, 0.1F, 100F);
        Matrix4x4 proj = Matrix.CreateOrthographicOffCenter(-width/height, width/height, -1, 1, 0.1f, 100f);
        return new VSConstantBuffer
        {
            world = Matrix.Identity,
            view = view,
            projection = proj,
            translation = Matrix.Identity,
            origin = Matrix.Identity,
        };
    }

}

public struct PSConstantBuffer()
{
    public Vector4 objColor= new(1f, 0.5f, 0.31f, 1f);
}
