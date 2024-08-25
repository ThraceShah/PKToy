using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using static Viewer.Math.MathEx;
using Matrix = System.Numerics.Matrix4x4;


namespace Viewer.Graphic.Opengl
{
    public partial class GlRender
    {

        /// <summary>
        /// 高亮线条的索引
        /// </summary>
        int hightlightEdgeIndex = -1;
        /// <summary>
        /// 高亮面的索引
        /// </summary>
        int hightlightFaceIndex = -1;
        /// <summary>
        /// 高亮面的component的索引
        /// </summary>
        int hightlightFaceComp = -1;
        /// <summary>
        /// 高亮线条的component索引
        /// </summary>
        int hightlightEdgeComp = -1;

        bool computeEnd = true;

        /// <summary>
        /// 离屏渲染拾取图元
        /// </summary>
        /// <param name="nx"></param>
        /// <param name="ny"></param>
        private void HighlightPrimitiveByMousePostion(int nx, int ny)
        {
            if (computeEnd is false)
            {
                return;
            }
            computeEnd = false;

            var watch = new Stopwatch();
            watch.Start();

            var id = this.GetPickObjectId(nx, ny);
            this.ConvertIdToCompIndex(id, out hightlightFaceComp,
            out hightlightFaceIndex, out hightlightEdgeIndex);
            if (hightlightEdgeIndex >= 0)
            {
                hightlightEdgeComp = hightlightFaceComp;
                hightlightFaceComp = -1;
            }
            else
            {
                hightlightEdgeComp = -1;
            }
            watch.Stop();
            Console.WriteLine($"gpu:fComp={hightlightFaceComp},lComp={hightlightEdgeComp},faceI={hightlightFaceIndex},lineI={hightlightEdgeIndex},time={watch.ElapsedMilliseconds}ms");
            computeEnd = true;
        }


        /// <summary>
        /// 根据鼠标位置获取图元的id
        /// </summary>
        /// <param name="x">鼠标的x位置</param>
        /// <param name="y">鼠标的y位置</param>
        /// <returns>拾取到的图元的id,如果id>0,证明拾取成功</returns>
        private unsafe int GetPickObjectId(int x, int y)
        {
            gl.Disable(GLEnum.LineSmooth);  // 启用线条平滑
            gl.Disable(GLEnum.Blend);


            // gl.BindTexture(GLEnum.Texture2D, texture);
            // gl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba8,
            // width, height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, (void*)0);
            // gl.BindFramebuffer(GLEnum.Framebuffer, fbo);
            // gl.FramebufferTexture2D(GLEnum.Framebuffer,
            // GLEnum.ColorAttachment0, GLEnum.Texture2D, texture, 0);


            // 渲染代码
            gl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha, 
            GLEnum.One, GLEnum.One);
            gl.Enable(GLEnum.DepthTest);

            gl.ClearColor(0.3725f, 0.6196f, 0.6275f, 1.0f);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // also clear the depth buffer now!
            // Prepare matrices
            var xRadian = Radians(mouseXOffset);
            var yRadian = Radians(mouseYOffset);
            Matrix W =
            world *
            Matrix.CreateRotationX(yRadian) *
            Matrix.CreateRotationY(xRadian);
            m_VSConstantBuffer.world = W;
            pickShader.Use();
            gl.Enable(GLEnum.PolygonOffsetFill);//开启深度偏移
            // //设置深度偏移,offset = m * factor + r * units,其中，m是多边形的最大深度斜率，r是能产生显著深度变化的最小值
            gl.PolygonOffset(2, 1f);
            pickShader.SetUniform("g_World", m_VSConstantBuffer.world);
            pickShader.SetUniform("g_View", m_VSConstantBuffer.view);
            pickShader.SetUniform("g_Proj", m_VSConstantBuffer.projection);
            pickShader.SetUniform("g_Translation", m_VSConstantBuffer.translation);
            for (int i = 0; i < geometry.Components.Length;i++)
            {
                var comp = geometry.Components[i];
                var part = geometry.Parts.Span[comp.PartIndex];
                partBuffers.GetPartBuffer(comp.PartIndex, out var vao, out var ebo);
                var baseId= geometry.GetCompFirstIdByIndex(i);
                float t = *(float*)&baseId;
                pickShader.SetUniform("baseId",new Vector4(t,0,0,0));
                pickShader.SetUniform("g_Origin", comp.CompMatrix);
                gl.BindVertexArray(vao);
                unsafe
                {
                    gl.DrawElements(GLEnum.Triangles, (uint)part.FaceIndexLength,
                    GLEnum.UnsignedInt, (void*)0);
                }
            }
            gl.Disable(GLEnum.PolygonOffsetFill);
            gl.LineWidth(4.0f);
            for (int i = 0; i < geometry.Components.Length; i++)
            {
                var comp = geometry.Components[i];
                pickShader.SetUniform("g_Origin", comp.CompMatrix);
                var baseId = geometry.GetCompFirstIdByIndex(i);
                float t = *(float*)&baseId;
                pickShader.SetUniform("baseId", new Vector4(t, 0, 0, 0));
                var part = geometry.Parts.Span[comp.PartIndex];
                partBuffers.GetPartBuffer(comp.PartIndex, out var vao, out var ebo);
                gl.BindVertexArray(vao);
                unsafe
                {
                    gl.DrawElements(GLEnum.Lines, (uint)part.EdgeIndexLength,
                    GLEnum.UnsignedInt, (void*)(part.EdgeStartIndex * sizeof(uint)));
                }
            }
            gl.LineWidth(1.0f);
            //注意opengl的Texture2D坐标原点在左下角(d3d则在左上角),与winform控件坐标原点(在左上角)不同,需要进行转换
            var rgba = stackalloc byte[4];
            gl.ReadPixels(x, (int)height-y, 1, 1, GLEnum.Rgba, GLEnum.UnsignedByte, rgba);
            int id = *(int*)rgba;
            gl.BindFramebuffer(GLEnum.Framebuffer, 0);
            gl.Enable(GLEnum.LineSmooth);  // 启用线条平滑
            gl.Enable(GLEnum.Blend);

            return id;
        }

        private void ConvertIdToCompIndex(int id, out int compIndex,
        out int faceIndex, out int lineIndex)
        {
            compIndex = -1;
            faceIndex = -1;
            lineIndex = -1;
            if (id < 0)
            {
                return;
            }
            for (int i = 0; i < geometry.Components.Length; i++)
            {
                var comp = geometry.Components[i];
                var part = geometry.Parts[comp.PartIndex];
                var baseId = geometry.GetCompFirstIdByIndex(i);
                var faceMax = baseId + part.FaceStartIndexArray.Length - 1;
                var edgeMax = faceMax + part.EdgeStartIndexArray.Length - 1;
                if (id >= edgeMax)
                {
                    continue;
                }
                compIndex = i;
                if (id < faceMax)
                {
                    var j = id - baseId;
                    faceIndex = part.FaceStartIndexArray[j];
                    return;

                }
                else
                {
                    var edgeBaseId = baseId + part.FaceStartIndexArray.Length - 1;
                    var j = id - edgeBaseId;
                    lineIndex = part.EdgeStartIndexArray[j];
                    return;
                }
            };
        }

    }
}
