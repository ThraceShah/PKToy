using System.Diagnostics;
using System.Numerics;
using Silk.NET.Core.Contexts;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Viewer.IContract;
using static Viewer.Math.MathEx;
using Matrix = System.Numerics.Matrix4x4;

namespace Viewer.Graphic.Opengl;


public partial class GlRender(GL gl) : IDisposable
{

    Camera camera;

    Matrix world;

    VSConstantBuffer m_VSConstantBuffer;            // 用于修改用于VS的GPU常量缓冲区的变量
    PSConstantBuffer m_PSConstantBuffer;            // 用于修改用于PS的GPU常量缓冲区的变量

    public GL gl = gl;

    private Shader faceShader;

    private Shader lineShader;

    private Shader pickShader;

    public unsafe void GLControlLoad()
    {
        camera = new Camera(new Vector3(0.0f, 0.0f, -20.0f));
        m_VSConstantBuffer = VSConstantBuffer.GetDefault();
        m_PSConstantBuffer = PSConstantBuffer.GetDefault();
        gl.Enable(GLEnum.CullFace);
        gl.CullFace(GLEnum.Back);
        gl.Enable(GLEnum.DepthTest);
        gl.Enable(GLEnum.LineSmooth);  // 启用线条平滑
        gl.Enable(GLEnum.Blend);
        gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        gl.Enable(GLEnum.PolygonOffsetFill);//开启深度偏移
        //设置深度偏移,offset = m * factor + r * units,其中，m是多边形的最大深度斜率，r是能产生显著深度变化的最小值
        gl.PolygonOffset(1, 0.5f);
        // fbo=gl.GenFramebuffer();
        // texture= gl.GenTexture();

        faceShader = new Shader(gl,"GLSL/faceShader.vert",
        "GLSL/faceShader.frag","GLSL/faceShader.geom");
        lineShader = new Shader(gl, "GLSL/lineShader.vert",
        "GLSL/lineShader.frag");
        pickShader = new Shader(gl, "GLSL/pickShader.vert",
        "GLSL/pickShader.frag");
    }

    private PartBuffers partBuffers;

    private AsmGeometry geometry;

    public void UpdateGeometry(ref AsmGeometry asmGeometry)
    {
        //重置摄像机
        camera = new Camera(new Vector3(0.0f, 0.0f, -20.0f));
        m_VSConstantBuffer = VSConstantBuffer.GetDefault();
        UpdateProjMatrix();
        asmGeometry.CreateAsmWorldRH(16, 12, out world);
        partBuffers?.Dispose();
        partBuffers = PartBuffers.GenPartBuffers(gl, asmGeometry);
        geometry.Dispose();
        geometry = asmGeometry;

        watch.Reset();
        watch.Start();
    }

    private uint width;

    private uint height;

    public void GLControlResize(uint width,uint height)
    {
        this.width = width;
        this.height = height;
        gl.Viewport(0, 0,width,height);
        this.UpdateProjMatrix();
        watch.Reset();
        watch.Start();
    }
    /// <summary>
    /// 定时器,当用户超过一定时间没有操作,我们就不绘制
    /// </summary>
    readonly Stopwatch watch = new();

    private bool first = true;
    public unsafe void Render()
    {
        if (watch.ElapsedMilliseconds > 1000)
        {
            return;
        }
        if(geometry.Parts.Length==0&&first)
        {
            first = false;
            return;
        }


        gl.ClearColor(0.3725f, 0.6196f, 0.6275f, 1.0f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // also clear the depth buffer now!


        // Prepare matrices
        var xRadian = Radians(camera.MouseXOffset);
        var yRadian = Radians(camera.MouseYOffset);
        Matrix W =
        world *
        Matrix.CreateRotationX(yRadian) *
        Matrix.CreateRotationY(xRadian);
        m_VSConstantBuffer.world = W;
        m_PSConstantBuffer.objColor = new Vector4(0.5882353f, 0.5882353f, 0.5882353f, 1f);

        gl.Enable(GLEnum.PolygonOffsetFill);//开启深度偏移
        faceShader.Use();
        if (Matrix.Invert(W, out var result))
        {
            var nm = Matrix.Transpose(result);
            ReadOnlySpan<float> normalModel = [nm.M11, nm.M12, nm.M13, nm.M21, nm.M22, nm.M23, nm.M31, nm.M32, nm.M33];
            faceShader.UniformMatrix3("g_WIT", normalModel);
        }
        faceShader.SetUniform("g_World", m_VSConstantBuffer.world);
        faceShader.SetUniform("g_View", m_VSConstantBuffer.view);
        faceShader.SetUniform("g_Proj", m_VSConstantBuffer.proj);
        faceShader.SetUniform("g_Translation", m_VSConstantBuffer.translation);
        faceShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);


        for (int i = 0; i < geometry.Components.Length;i++)
        {
            var comp = geometry.Components.Span[i];
            var part = geometry.Parts.Span[comp.PartIndex];
            faceShader.SetUniform("g_Origin", comp.CompMatrix);
            partBuffers.GetPartBuffer(comp.PartIndex, out var vao, out var ebo);
            gl.BindVertexArray(vao);
            if (hightlightFaceComp == i)
            {
                if (part.GetFaceStartIndexAndLengthByIndexArrayIndex(hightlightFaceIndex,
                out int start, out var length))
                {
                    //去除高亮面的深度值加值,使得有多个面重叠的情况下,高亮面总是显示在最上面
                    gl.Disable(GLEnum.PolygonOffsetFill);
                    m_PSConstantBuffer.objColor = new Vector4(1f, 0.501f, 0f, 1f);
                    faceShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
                    gl.DrawElements(GLEnum.Triangles, (uint)length,
                    GLEnum.UnsignedInt, (void*)(start*sizeof(uint)));
                    gl.Enable(GLEnum.PolygonOffsetFill);//开启深度偏移
                    m_PSConstantBuffer.objColor = new Vector4(0.5882353f, 0.5882353f, 0.5882353f, 1f);
                    faceShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
                }
            }
            gl.DrawElements(GLEnum.Triangles, (uint)part.FaceIndexLength,
            GLEnum.UnsignedInt, (void*)0);
        }

        gl.Disable(GLEnum.PolygonOffsetFill);
        lineShader.Use();
        m_PSConstantBuffer.objColor = new Vector4(0f, 0f, 0f, 1f);
        lineShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
        lineShader.SetUniform("g_World", m_VSConstantBuffer.world);
        lineShader.SetUniform("g_View", m_VSConstantBuffer.view);
        lineShader.SetUniform("g_Proj", m_VSConstantBuffer.proj);
        lineShader.SetUniform("g_Translation", m_VSConstantBuffer.translation);
        for (int i = 0; i < geometry.Components.Length; i++)
        {
            var comp = geometry.Components.Span[i];
            var part = geometry.Parts.Span[comp.PartIndex];
            lineShader.SetUniform("g_Origin", comp.CompMatrix);
            partBuffers.GetPartBuffer(comp.PartIndex, out var vao, out var ebo);
            gl.BindVertexArray(vao);
            if (hightlightEdgeComp == i)
            {
                if (part.GetEdgeStartIndexAndLengthByIndexArrayIndex(hightlightEdgeIndex,
                out int start, out var length))
                {
                    gl.LineWidth(2.0f);
                    m_PSConstantBuffer.objColor = new Vector4(1f, 0f, 0f, 1f);
                    lineShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
                    gl.DrawElements(GLEnum.Lines, (uint)length,
                    GLEnum.UnsignedInt, (void*)((start + part.EdgeStartIndex) * sizeof(uint)));
                    m_PSConstantBuffer.objColor = new Vector4(0f, 0f, 0f, 1f);
                    lineShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
                    gl.LineWidth(1.0f);
                }
            }
            gl.DrawElements(GLEnum.Lines, (uint)part.EdgeIndexLength,
            GLEnum.UnsignedInt, (void*)(part.EdgeStartIndex * sizeof(uint)));
        }

    }

    float lastX = 0;
    float lastY = 0;


    public void MouseDown(KeyCode keyCode, int x, int y)
    {
        lastX = x;
        lastY = y;
        camera.KeyCode |= keyCode;
    }

    public void MouseUp(KeyCode keyCode, int x, int y)
    {
        if (keyCode ==KeyCode.Left &&camera.KeyCode == KeyCode.Left)
        {
            this.HighlightPrimitiveByMousePostion(x, y);
            watch.Reset();
            watch.Start();

        }
        camera.KeyCode &= ~keyCode;
    }


    public void MouseMove(int x, int y)
    {
        if (camera.KeyCode != KeyCode.Middle &&
        camera.KeyCode != KeyCode.ControlLeft)
        {
            return;
        }
        var xPosIn = x;
        var yPosIn = y;

        float xPos = (float)xPosIn;
        float yPos = (float)yPosIn;

        float xOffset = xPos - lastX;
        float yOffset = lastY - yPos; // reversed since y-coordinates go from bottom to top

        lastX = xPos;
        lastY = yPos;
        switch (camera.KeyCode)
        {
            case KeyCode.Middle:
                camera.ProcessMouseMovement(xOffset, yOffset);
                watch.Reset();
                watch.Start();
                break;
            case KeyCode.ControlLeft:
                m_VSConstantBuffer.translation.M14 += xOffset * 0.002f;
                m_VSConstantBuffer.translation.M24 += yOffset * 0.002f;
                watch.Reset();
                watch.Start();
                break;
        }
    }

    public void MouseWheel(int delta)
    {
        camera.ProcessMouseScroll(delta * 0.01f);
        UpdateProjMatrix();
        watch.Reset();
        watch.Start();
    }


    public void KeyDown(KeyCode keyCode)
    {
        camera.KeyCode |= keyCode;
        watch.Reset();
        watch.Start();
    }

    public void KeyUp(KeyCode keyCode)
    {
        camera.KeyCode &= ~keyCode;
        watch.Reset();
        watch.Start();
    }



    public void Dispose()
    {
        geometry.Dispose();
        partBuffers?.Dispose();
        faceShader.Dispose();
        lineShader.Dispose();
        pickShader.Dispose();
        gl.Dispose();
        // gl.DeleteFramebuffer(fbo);
        // gl.DeleteTexture(texture);

    }

    #region  private
    private void UpdateProjMatrix()
    {
        var radians = Radians(camera.Zoom);
        var aspectRatio = float.Max(width, 1) / float.Max(height, 1);
        m_VSConstantBuffer.proj = Matrix.CreatePerspectiveFieldOfView(
            radians, aspectRatio, 0.1F, 100F);
        watch.Reset();
        watch.Start();
    }

    #endregion

}
