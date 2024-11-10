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

    public class AsmGeometry(PartGeometry[] parts, CompGeometry[] comps)
    {

        public PartGeometry[] Parts = parts;

        public CompGeometry[] Components = comps;

        public static AsmGeometry GetDefault()
        {
            var parts = new[] { PartGeometry.GetDefault() };
            var comps = new[] { CompGeometry.GetDefault() };
            return new AsmGeometry(parts,comps);
        }

        public int GetCompFirstIdByIndex(int compIndex)
        {
            int id = 0;
            for (int i = 0; i < compIndex;i++)
            {
                //FaceStartIndexArray和EdgeStartIndexArray里面最后一个元素并不代表一个面或者一条线
                var comp = this.Components[i];
                var part = this.Parts[comp.PartIndex];
                if(part.EdgeStartIndexArray.Length>0)
                {
                    id += part.FaceStartIndexArray.Length + part.EdgeStartIndexArray.Length - 2;
                }
                else
                {
                    id += part.FaceStartIndexArray.Length + part.EdgeStartIndexArray.Length - 1;
                }
            }
            return id;
        }

        public Vector3 GetBBoxCenter()
        {
            //先计算每个组件原始模型的box,然后将这些box偏移,最后获得整个组件的box
            float[] xSpan = new float[Components.Length * 2];
            float[] ySpan = new float[Components.Length * 2];
            float[] zSpan = new float[Components.Length * 2];
            for (int i = 0; i < Components.Length; i++)
            {
                var comp = this.Components[i];
                var part = this.Parts[comp.PartIndex];
                var partBox = part.Box;
                var newBox = new Box
                {
                    Min = Vector3.Transform(partBox.Min, comp.CompMatrix),
                    Max = Vector3.Transform(partBox.Max, comp.CompMatrix)
                };
                xSpan[i * 2] = newBox.Min.X;
                xSpan[i * 2 + 1] = newBox.Max.X;
                ySpan[i * 2] = newBox.Min.Y;
                ySpan[i * 2 + 1] = newBox.Max.Y;
                zSpan[i * 2] = newBox.Min.Z;
                zSpan[i * 2 + 1] = newBox.Max.Z;
            }
            var maxX = xSpan.Max();
            var minX = xSpan.Min();
            var maxY = ySpan.Max();
            var minY = ySpan.Min();
            var maxZ = zSpan.Max();
            var minZ = zSpan.Min();

            //计算中心点
            var x = (maxX + minX) / 2;
            var y = (maxY + minY) / 2;
            var z = (maxZ + minZ) / 2;
            var postion = new Vector3(x, y, z);
            return postion;
        }

        public void CreateAsmWorldRH(float xSize, float ySize, out Matrix4x4 world)
        {
            //先计算每个组件原始模型的box,然后将这些box偏移,最后获得整个组件的box
            float[] xSpan = new float[Components.Length * 2];
            float[] ySpan = new float[Components.Length * 2];
            float[] zSpan = new float[Components.Length * 2];
            for (int i = 0; i < Components.Length; i++)
            {
                var comp = this.Components[i];
                var part = this.Parts[comp.PartIndex];
                var partBox = part.Box;
                var newBox = new Box
                {
                    Min = Vector3.Transform(partBox.Min, comp.CompMatrix),
                    Max = Vector3.Transform(partBox.Max, comp.CompMatrix)
                };
                xSpan[i * 2] = newBox.Min.X;
                xSpan[i * 2 + 1] = newBox.Max.X;
                ySpan[i * 2] = newBox.Min.Y;
                ySpan[i * 2 + 1] = newBox.Max.Y;
                zSpan[i * 2] = newBox.Min.Z;
                zSpan[i * 2 + 1] = newBox.Max.Z;
            }
            var maxX = xSpan.Max();
            var minX = xSpan.Min();
            var maxY = ySpan.Max();
            var minY = ySpan.Min();
            var maxZ = zSpan.Max();
            var minZ = zSpan.Min();

            //计算中心点
            var x = (maxX + minX) / 2;
            var y = (maxY + minY) / 2;
            var z = (maxZ + minZ) / 2;
            var postion = new Vector3(x, y, z);

            //计算厚度方向,并根据厚度方向修改世界坐标系,使得厚度方向始终和z轴保持一致
            var tx = maxX - minX;
            var ty = maxY - minY;
            var tz = maxZ - minZ;
            var t = Math.Min(tz, Math.Min(tx, ty));

            if (t == tz)
            {
                world = CreateWorldRH(postion, Vector3.UnitZ, Vector3.UnitY);
            }
            else if (t == ty)
            {

                world = CreateWorldRH(postion, Vector3.UnitY, Vector3.UnitX);
            }
            else
            {
                world = CreateWorldRH(postion, Vector3.UnitX, Vector3.UnitZ);
            }


            var min = Vector3.Transform(new Vector3(minX, minY, minZ), world);
            var max = Vector3.Transform(new Vector3(maxX, maxY, maxZ), world);

            var xLength = Math.Abs(max.X - min.X);
            var yLength = Math.Abs(max.Y - min.Y);
            var xScale = xSize / xLength;
            var yScale = ySize / yLength;
            var scale = Math.Min(xScale, yScale);
            world *= Matrix4x4.CreateScale(scale);
        }

        public static Matrix4x4 CreateWorldRH(Vector3 position, Vector3 forward, Vector3 up)
        {
            Vector3 zaxis = forward;
            zaxis = Vector3.Normalize(zaxis);
            Vector3 xaxis = Vector3.Cross(up, zaxis);
            xaxis = Vector3.Normalize(xaxis);
            Vector3 yaxis = Vector3.Cross(zaxis, xaxis);

            Matrix4x4 result = new(
                xaxis.X, yaxis.X, zaxis.X, 0,
                xaxis.Y, yaxis.Y, zaxis.Y, 0,
                xaxis.Z, yaxis.Z, zaxis.Z, 0,
                -Vector3.Dot(xaxis, position), -Vector3.Dot(yaxis, position), -Vector3.Dot(zaxis, position), 1
            );
            return result;
        }

    }


}
