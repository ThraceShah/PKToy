using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Viewer.IContract;
using static Viewer.Math.MathEx;
using Matrix = System.Numerics.Matrix4x4;


namespace Viewer.Graphic.Opengl
{
    enum HighlightType
    {
        None,
        Face,
        Edge,

    }
    public partial class GlRender
    {
        public bool EnableHover { get; set; } = true;
        private int highlightComp = -1;

        private int highlightCell = -1;
        bool computeEnd = true;

        private int lastHoverX = 0;
        private int lastHoverY = 0;

        public void Hover(int x, int y)
        {
            if (geometry == null || geometry.Components.Count == 0)
            {
                return;
            }
            if (EnableHover && keyCode == KeyCode.None)
            {
                if (x != lastHoverX || y != lastHoverY)
                {
                    lastHoverX = x;
                    lastHoverY = y;
                    HighlightPrimitiveByMousePostion(x, y, true);
                }

            }
        }

        private int lastHoverId = int.MinValue;

        /// <summary>
        /// 离屏渲染拾取图元
        /// </summary>
        /// <param name="nx"></param>
        /// <param name="ny"></param>
        public void HighlightPrimitiveByMousePostion(int nx, int ny, bool hover = false)
        {
            if (computeEnd is false)
            {
                return;
            }
            computeEnd = false;

            var watch = new Stopwatch();
            watch.Start();
            var id = this.GetPickObjectId(nx, ny);
            if (hover && id == lastHoverId)
            {
                computeEnd = true;
                return;
            }
            lastHoverId = id;
            highlightComp = -1;
            highlightCell = -1;
            if (id < 0)
            {
                watch.Stop();
                computeEnd = true;
                return;
            }
            for (int i = 0; i < geometry.Components.Count; i++)
            {
                var comp = geometry.Components[i];
                var part = geometry.Parts[comp.PartIndex];
                highlightComp = i;
                if (id < part.CellCount)
                {
                    highlightCell = id;
                    break;
                }
                id -= part.CellCount;
            }
            ;
            watch.Stop();
            Console.WriteLine($"gpu:highlightComp={highlightComp},highlightCell={highlightCell},time={watch.ElapsedMilliseconds}ms");
            computeEnd = true;
        }


        public void RenderToFramebuffer()
        {
            // 绑定 Framebuffer
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, offSrcFramebuffer);

            // 设置视口
            gl.Viewport(0, 0, width, height);

            // 清除颜色和深度缓冲
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // 在这里执行渲染操作
            // ...

            // 解绑 Framebuffer
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }


        /// <summary>
        /// 根据鼠标位置获取图元的id
        /// </summary>
        /// <param name="x">鼠标的x位置</param>
        /// <param name="y">鼠标的y位置</param>
        /// <returns>拾取到的图元的id,如果id>0,证明拾取成功</returns>
        private unsafe int GetPickObjectId(int x, int y)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, offSrcFramebuffer);
            // 设置视口
            gl.Viewport(0, 0, width, height);

            gl.Disable(GLEnum.LineSmooth);  // 启用线条平滑
            gl.Disable(GLEnum.Blend);

            // 渲染代码
            gl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha,
            GLEnum.One, GLEnum.One);
            gl.Enable(GLEnum.DepthTest);

            gl.ClearColor(0.3725f, 0.6196f, 0.6275f, 1.0f);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // also clear the depth buffer now!
                                                                                       // Prepare matrices
            var modelMatrix = Matrix.CreateTranslation(bBoxCenter);
            modelMatrix = Matrix.CreateFromQuaternion(rotation) * modelMatrix;
            modelMatrix = Matrix.CreateTranslation(-bBoxCenter) * modelMatrix;
            m_VSConstantBuffer.world = modelMatrix;
            Matrix.Invert(modelMatrix, out var result);
            var nm = Matrix.Transpose(result);
            ReadOnlySpan<float> normalModel = [nm.M11, nm.M12, nm.M13, nm.M21, nm.M22, nm.M23, nm.M31, nm.M32, nm.M33];

            m_VSConstantBuffer.world = modelMatrix;
            pickShader.Use();

            gl.Enable(GLEnum.PolygonOffsetFill);//开启深度偏移
            // //设置深度偏移,offset = m * factor + r * units,其中，m是多边形的最大深度斜率，r是能产生显著深度变化的最小值
            gl.PolygonOffset(2, 3f);
            pickShader.SetUniform("g_World", m_VSConstantBuffer.world);
            pickShader.SetUniform("g_View", m_VSConstantBuffer.view);
            pickShader.SetUniform("g_Proj", m_VSConstantBuffer.projection);
            pickShader.SetUniform("g_Translation", m_VSConstantBuffer.translation);
            for (int i = 0; i < geometry.Components.Count; i++)
            {
                var comp = geometry.Components[i];
                var part = geometry.Parts[comp.PartIndex];
                if (part is StripFacePart)
                {
                    int compId = geometry.GetCompFirstIdByIndex(i);
                    Vector4 baseId = new(*(float*)(&compId), 0, 0, 0);
                    pickShader.SetUniform("baseId", baseId);
                    pickShader.SetUniform("g_Origin", comp.CompMatrix);
                    partBuffers.GetPartBuffer(comp.PartIndex, out var vao, out var ebo);
                    gl.BindVertexArray(vao);
                    gl.DrawElements(GLEnum.TriangleStrip, (uint)part.IndicesCount, GLEnum.UnsignedInt, null);
                }
            }
            gl.Disable(GLEnum.PolygonOffsetFill);
            gl.LineWidth(4.0f);
            for (int i = 0; i < geometry.Components.Count; i++)
            {
                var comp = geometry.Components[i];
                var part = geometry.Parts[comp.PartIndex];
                if (part is EdgePart)
                {
                    int compId = geometry.GetCompFirstIdByIndex(i);
                    Vector4 baseId = new(*(float*)(&compId), 0, 0, 0);
                    pickShader.SetUniform("baseId", baseId);
                    pickShader.SetUniform("g_Origin", comp.CompMatrix);
                    partBuffers.GetPartBuffer(comp.PartIndex, out var vao, out var ebo);
                    gl.BindVertexArray(vao);
                    gl.DrawElements(GLEnum.Lines, (uint)part.IndicesCount,
                    GLEnum.UnsignedInt, null);
                }
            }
            gl.LineWidth(2.0f);
            //注意opengl的Texture2D坐标原点在左下角(d3d则在左上角),与winform控件坐标原点(在左上角)不同,需要进行转换
            var rgba = stackalloc byte[4];
            gl.ReadPixels(x, (int)height - y, 1, 1, GLEnum.Rgba, GLEnum.UnsignedByte, rgba);
            int id = *(int*)rgba;
            gl.BindFramebuffer(GLEnum.Framebuffer, 0);
            gl.Enable(GLEnum.LineSmooth);  // 启用线条平滑
            gl.Enable(GLEnum.Blend);

            return id;
        }

    }
}
