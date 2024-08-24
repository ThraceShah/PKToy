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
    public Matrix proj;
    public Matrix translation;
    public Matrix origin;

    /// <summary>
    /// 实际上这个baseid是个int,但是ConstantBuffer的总大小必须为16字节的倍数
    /// 所以放一个vector4,用的时候只用前4个字节
    /// </summary>
    public Vector4 baseId;

    public static VSConstantBuffer GetDefault(float width = 800f, float height = 600f)
    {
        var eye = new Vector3(0f, 0f, -16f);
        var target = Vector3.Zero;
        var up = new Vector3(0f, 1f, 0f);
        var view = Matrix.CreateLookAt(eye, target, up);
        Matrix4x4 proj = Matrix.CreatePerspectiveFieldOfView(1.570796327f, width / height, 0.1F, 100F);
        return new VSConstantBuffer
        {
            world = Matrix.Identity,
            view = view,
            proj = proj,
            translation = Matrix.Identity,
            origin = Matrix.Identity,
        };
    }

}

public struct PSConstantBuffer
{
    public DirectionalLight dirLight;
    public PointLight pointLight;

    public Material material;
    public Vector4 objColor;
    public Vector4 eyePos;

    public static PSConstantBuffer GetDefault()
    {
        return new PSConstantBuffer
        {
            dirLight = new DirectionalLight
            {
                ambient = new Vector4(0.2f, 0.2f, 0.2f, 1.0f),
                diffuse = new Vector4(0.8f, 0.8f, 0.8f, 1f),
                specular = new Vector4(0.5f, 0.5f, 0.5f, 1f),
                direction = new Vector3(0.577f, -0.577f, 0.577f),
            },
            pointLight = new PointLight
            {
                position = new Vector3(0f, 0f, -10f),
                ambient = new Vector4(0.3f, 0.3f, 0.3f, 1f),
                diffuse = new Vector4(0.7f, 0.7f, 0.7f, 1f),
                specular = new Vector4(0.5f, 0.5f, 0.5f, 1f),
                att = new Vector3(0f, 0.1f, 0f),
                range = 25f,
            },
            objColor = new Vector4(1f, 0.5f, 0.31f, 1f),
            material = new Material
            {
                ambient = new Vector4(0.5f, 0.5f, 0.5f, 1f),
                diffuse = new Vector4(1f, 1f, 1f, 1f),
                specular = new Vector4(0.1f, 0.1f, 0.1f, 5f),
            },
            eyePos = new Vector4(0f, 0f, -5f, 0f),
        };
    }
}


public struct GSConstantBuffer
{
    public Matrix worldInvTranspose;

}

public struct FaceCalCSConstantBuffer
{
    public Vector4 p;
}

public struct FaceCSConstantBuffer
{
    public Matrix matrix;
}

public struct DirectionalLight
{
    public Vector4 ambient;
    public Vector4 diffuse;
    public Vector4 specular;
    public Vector3 direction;
    public float pad;
}

public struct PointLight
{
    public Vector4 ambient;
    public Vector4 diffuse;
    public Vector4 specular;
    public Vector3 position;
    public float range;
    public Vector3 att;
    public float pad;
}

/// <summary>
/// 聚光灯
/// </summary>
public struct SpotLight
{
    public Vector4 ambient;
    public Vector4 diffuse;
    public Vector4 specular;
    public Vector3 position;
    public float range;
    public Vector3 direction;
    public float spot;
    public Vector3 att;
    public float pad;
}

public struct Material
{
    public Vector4 ambient;
    public Vector4 diffuse;
    public Vector4 specular;// w = 镜面反射强度
    public Vector4 reflect;
}
