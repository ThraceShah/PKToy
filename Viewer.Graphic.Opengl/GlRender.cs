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


    Matrix world;

    VSConstantBuffer m_VSConstantBuffer;
    PSConstantBuffer m_PSConstantBuffer;

    public GL gl = gl;

    private Shader faceShader;

    private Shader lineShader;

    private Shader pickShader;

    private Shader highlightFaceShader;

    private KeyCode keyCode = KeyCode.None;
    private float orthoScale = 1;
    private Quaternion rotation = Quaternion.Identity;
    private Vector3 bBoxCenter = Vector3.Zero;

    private uint width;

    private uint height;

    private float aspectRatio;
    private bool first = true;

    private PartBuffers partBuffers;

    private AsmGeometry geometry;

    float lastX = 0;
    float lastY = 0;

    private uint offSrcFramebuffer;
    private uint offSrcTexture;
    private uint offSrcRenderbuffer;
    private unsafe void InitOffscreenFrame()
    {
        // 如果已经存在 Framebuffer，先删除它
        if (offSrcFramebuffer != 0)
        {
            gl.DeleteFramebuffer(offSrcFramebuffer);
            gl.DeleteTexture(offSrcTexture);
            gl.DeleteRenderbuffer(offSrcRenderbuffer);
            offSrcFramebuffer = 0;
            offSrcTexture = 0;
            offSrcRenderbuffer = 0;
        }

        // 创建 Framebuffer 对象
        offSrcFramebuffer = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, offSrcFramebuffer);

        // 创建纹理对象
        offSrcTexture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, offSrcTexture);
        gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, offSrcTexture, 0);

        // 创建 Renderbuffer 对象用于深度和模板测试
        offSrcRenderbuffer = gl.GenRenderbuffer();
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, offSrcRenderbuffer);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, GLEnum.Depth24Stencil8, width, height);
        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, offSrcRenderbuffer);

        // 检查 Framebuffer 是否完整
        if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            throw new Exception("Framebuffer is not complete.");
        }
        // 解绑 Framebuffer
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }


    private void ProcessMouseMovement(float xoffset, float yoffset)
    {
        var x=xoffset* 800/height;
        var y=yoffset* 800/height;
        var yRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, Radians(x));
        var xRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, Radians(y));
        rotation = yRotation * xRotation * rotation;
    }
    private void ProcessMouseScroll(float yoffset)
    {
        orthoScale -= yoffset * 0.1f;
        if (orthoScale <= 0.1f)
        {
            orthoScale = 0.1f;
        }
    }


    public unsafe void GLControlLoad()
    {
        m_VSConstantBuffer = VSConstantBuffer.GetDefault();
        m_PSConstantBuffer = new();
        gl.Enable(GLEnum.CullFace);
        gl.CullFace(GLEnum.Back);
        gl.Enable(GLEnum.DepthTest);
        gl.Enable(GLEnum.LineSmooth);  // 启用线条平滑
        gl.Enable(GLEnum.Blend);
        gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        gl.Enable(GLEnum.PolygonOffsetFill);//开启深度偏移
        //设置深度偏移,offset = m * factor + r * units,其中，m是多边形的最大深度斜率，r是能产生显著深度变化的最小值
        gl.PolygonOffset(1, 0.5f);


        // 启用重启索引功能
        gl.Enable(GLEnum.PrimitiveRestart);

        faceShader = new Shader(gl, "GLSL/faceShader.vert",
        "GLSL/faceShader.frag");
        lineShader = new Shader(gl, "GLSL/lineShader.vert",
        "GLSL/lineShader.frag");
        pickShader = new Shader(gl, "GLSL/pickShader.vert",
        "GLSL/pickShader.frag");
        highlightFaceShader = new Shader(gl, "GLSL/highlightFaceShader.vert",
        "GLSL/highlightFaceShader.frag");
    }


    public void UpdateGeometry(ref AsmGeometry asmGeometry)
    {
        this.keyCode = KeyCode.None;
        orthoScale = 1.0f;

        m_VSConstantBuffer = VSConstantBuffer.GetDefault();
        UpdateProjMatrix();
        bBoxCenter = asmGeometry.GetBBoxCenter();
        asmGeometry.CreateAsmWorldRH(1, 1, out world);
        partBuffers?.Dispose();
        partBuffers = PartBuffers.GenPartBuffers(gl, asmGeometry);
        geometry = asmGeometry;

    }

    public void GLControlResize(uint width, uint height)
    {
        this.width = uint.Max(width, 1);
        this.height = uint.Max(height, 1);
        aspectRatio =  this.width/(float)this.height;
        gl.Viewport(0, 0, this.width, this.height);
        InitOffscreenFrame();
        this.UpdateProjMatrix();
    }

    public void MouseDown(KeyCode keyCode, int x, int y)
    {
        lastX = x;
        lastY = y;
        this.keyCode |= keyCode;
    }

    public void MouseUp(KeyCode keyCode, int x, int y)
    {
        if (keyCode == KeyCode.Left && keyCode == KeyCode.Left)
        {
            this.HighlightPrimitiveByMousePostion(x, y);
        }
        this.keyCode &= ~keyCode;
    }


    public void MouseMove(int x, int y)
    {
        if (this.keyCode != KeyCode.Middle &&
        this.keyCode != KeyCode.ControlLeft)
        {
            return;
        }

        float xOffset = x - lastX;
        float yOffset = lastY - y; // reversed since y-coordinates go from bottom to top

        lastX = x;
        lastY = y;
        switch (this.keyCode)
        {
            case KeyCode.Middle:
                ProcessMouseMovement(xOffset, yOffset);
                break;
            case KeyCode.ControlLeft:
                m_VSConstantBuffer.translation.M14 += xOffset * 0.002f;
                m_VSConstantBuffer.translation.M24 += yOffset * 0.002f;
                break;
        }
    }

    public void MouseWheel(int delta)
    {
        ProcessMouseScroll(delta * 0.01f);
        UpdateProjMatrix();

    }


    public void KeyDown(KeyCode keyCode)
    {
        this.keyCode |= keyCode;

    }

    public void KeyUp(KeyCode keyCode)
    {
        this.keyCode &= ~keyCode;

    }


    public unsafe void Render()
    {
        gl.ClearColor(0.3725f, 0.6196f, 0.6275f, 1.0f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // also clear the depth buffer now!
        if(geometry==null)
        {
            return;
        }
        if (geometry.Parts.Length == 0 && first)
        {
            first = false;
            return;
        }
        var modelMatrix = Matrix.CreateTranslation(bBoxCenter);
        modelMatrix = Matrix.CreateFromQuaternion(rotation) * modelMatrix;
        modelMatrix = Matrix.CreateTranslation(-bBoxCenter) * modelMatrix;
        m_VSConstantBuffer.world = modelMatrix;
        Matrix.Invert(modelMatrix, out var result);
        var nm = Matrix.Transpose(result);
        ReadOnlySpan<float> normalModel = [nm.M11, nm.M12, nm.M13, nm.M21, nm.M22, nm.M23, nm.M31, nm.M32, nm.M33];

        if (highlightType == HighlightType.Face)
        {
            for (uint i = 0; i < geometry.Components.Length; i++)
            {
                if(highlightFaceComp == i)
                {
                    var comp = geometry.Components[i];
                    var part = geometry.Parts[comp.PartIndex];
                    if (part.GetFaceStartIndexAndLengthByIndexArrayIndex(highlightFaceIndex,
                    out var start, out var length))
                    {
                        highlightFaceShader.Use();
                        highlightFaceShader.UniformMatrix3("g_WIT", normalModel);
                        highlightFaceShader.SetUniform("g_World", m_VSConstantBuffer.world);
                        highlightFaceShader.SetUniform("g_View", m_VSConstantBuffer.view);
                        highlightFaceShader.SetUniform("g_Proj", m_VSConstantBuffer.projection);
                        highlightFaceShader.SetUniform("g_Translation", m_VSConstantBuffer.translation);
                        highlightFaceShader.SetUniform("g_Origin", comp.CompMatrix);
                        //去除高亮面的深度值加值,使得有多个面重叠的情况下,高亮面总是显示在最上面
                        gl.Disable(GLEnum.PolygonOffsetFill);
                        m_PSConstantBuffer.objColor = new Vector4(1f, 0.501f, 0f, 1f);
                        highlightFaceShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
                        partBuffers.GetPartBuffer(comp.PartIndex, out var vao, out var ebo);
                        gl.BindVertexArray(vao);
                        gl.DrawElements(GLEnum.TriangleStrip, (uint)length,
                        GLEnum.UnsignedInt, (void*)(start * sizeof(uint)));
                    }
                }
            }
        }

        gl.Enable(GLEnum.PolygonOffsetFill);//开启深度偏移
        faceShader.Use();
        faceShader.UniformMatrix3("g_WIT", normalModel);
        faceShader.SetUniform("g_World", m_VSConstantBuffer.world);
        faceShader.SetUniform("g_View", m_VSConstantBuffer.view);
        faceShader.SetUniform("g_Proj", m_VSConstantBuffer.projection);
        faceShader.SetUniform("g_Translation", m_VSConstantBuffer.translation);

        for (uint i = 0; i < geometry.Components.Length; i++)
        {
            var comp = geometry.Components[i];
            var part = geometry.Parts[comp.PartIndex];
            faceShader.SetUniform("g_Origin", comp.CompMatrix);
            partBuffers.GetPartBuffer(comp.PartIndex, out var vao, out var ebo);
            gl.BindVertexArray(vao);
            gl.DrawElements(GLEnum.TriangleStrip, (uint)part.FaceIndexLength, GLEnum.UnsignedInt, (void*)0);

        }

        gl.Disable(GLEnum.PolygonOffsetFill);
        lineShader.Use();
        m_PSConstantBuffer.objColor = new Vector4(0f, 0f, 0f, 1f);
        lineShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
        lineShader.SetUniform("g_World", m_VSConstantBuffer.world);
        lineShader.SetUniform("g_View", m_VSConstantBuffer.view);
        lineShader.SetUniform("g_Proj", m_VSConstantBuffer.projection);
        lineShader.SetUniform("g_Translation", m_VSConstantBuffer.translation);
        gl.LineWidth(2.0f);
        for (uint i = 0; i < geometry.Components.Length; i++)
        {
            var comp = geometry.Components[i];
            var part = geometry.Parts[comp.PartIndex];
            lineShader.SetUniform("g_Origin", comp.CompMatrix);
            partBuffers.GetPartBuffer(comp.PartIndex, out var vao, out var ebo);
            gl.BindVertexArray(vao);
            if (highlightType == HighlightType.Edge && highlightEdgeComp == i)
            {
                if (part.GetEdgeStartIndexAndLengthByIndexArrayIndex(highlightEdgeIndex,
                out var start, out var length))
                {
                    gl.LineWidth(4.0f);
                    m_PSConstantBuffer.objColor = new Vector4(1f, 0f, 0f, 1f);
                    lineShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
                    gl.DrawElements(GLEnum.Lines, (uint)length,
                    GLEnum.UnsignedInt, (void*)((start + part.EdgeStartIndex) * sizeof(uint)));
                    m_PSConstantBuffer.objColor = new Vector4(0f, 0f, 0f, 1f);
                    lineShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
                    gl.LineWidth(2.0f);
                }
            }
            gl.DrawElements(GLEnum.Lines, (uint)part.EdgeIndexLength,
            GLEnum.UnsignedInt, (void*)(part.EdgeStartIndex * sizeof(uint)));
        }

    }

    public void Dispose()
    {
        partBuffers?.Dispose();
        faceShader.Dispose();
        lineShader.Dispose();
        pickShader.Dispose();
        gl.Dispose();
        gl.DeleteFramebuffer(offSrcFramebuffer);
        gl.DeleteTexture(offSrcTexture);
        gl.DeleteRenderbuffer(offSrcRenderbuffer);

    }

    #region  private
    private void UpdateProjMatrix()
    {
        m_VSConstantBuffer.projection = Matrix.CreateOrthographicOffCenter(-orthoScale * aspectRatio,
        orthoScale * aspectRatio, -orthoScale, orthoScale, 0.1f, 100.0f);

    }

    #endregion

}
