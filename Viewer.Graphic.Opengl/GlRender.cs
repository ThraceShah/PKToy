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

    private float mouseXOffset = 0;

    private float mouseYOffset = 0;

    private KeyCode keyCode = KeyCode.None;
    private float orthoScale = 1;
    private Quaternion rotation = new Quaternion(1, 0, 0, 0);
    private Vector3 bBoxCenter=Vector3.Zero;

    // 逻辑函数
    public static float Sigmoid(float x)
    {
        return 1.0f / (1.0f + float.Exp(-x));
    }
    private void ProcessMouseMovement(float xoffset, float yoffset)
    {
        mouseXOffset += xoffset * 0.8f;
        mouseYOffset += yoffset * 0.8f;

        var x= 0.5f * xoffset;
        var y=0.5f*yoffset;
        if(float.Abs(x)>float.Abs(y))
        {
            y=0.15f*y;
        }
        else
        {
            x=0.15f*x;
        }
        const float l=3;
        if(x>l)
        {
            x=l;
        }
        else if(x<-l)
        {
            x=-l;
        }
        if(y>l)
        {
            y=l;
        }
        else if(y<-l)
        {
            y=-l;
        }
        Console.WriteLine($"x:{x},y:{y}");
        var m=float.Sqrt(x * x + y * y);
        var deltaRotation=Quaternion.CreateFromAxisAngle(new Vector3(-y, x, 0.0f), Radians(m));
        rotation = Quaternion.Normalize(deltaRotation * rotation);

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
        // gl.PrimitiveRestartIndex(0xFFFFFFFF); // 设置重启索引值

        // fbo=gl.GenFramebuffer();
        // texture= gl.GenTexture();

        // faceShader = new Shader(gl,"GLSL/faceShader.vert",
        // "GLSL/faceShader.frag","GLSL/faceShader.geom");

        faceShader = new Shader(gl, "GLSL/faceShader.vert",
        "GLSL/faceShader.frag");
        lineShader = new Shader(gl, "GLSL/lineShader.vert",
        "GLSL/lineShader.frag");
        pickShader = new Shader(gl, "GLSL/pickShader.vert",
        "GLSL/pickShader.frag");
    }

    private PartBuffers partBuffers;

    private AsmGeometry geometry;

    public void UpdateGeometry(ref AsmGeometry asmGeometry)
    {
        this.keyCode = KeyCode.None;
        orthoScale = 1.0f;
        mouseXOffset = 0;
        mouseYOffset = 0;

        m_VSConstantBuffer = VSConstantBuffer.GetDefault();
        UpdateProjMatrix();
        bBoxCenter = asmGeometry.GetBBoxCenter();
        asmGeometry.CreateAsmWorldRH(1, 1, out world);
        partBuffers?.Dispose();
        partBuffers = PartBuffers.GenPartBuffers(gl, asmGeometry);
        geometry.Dispose();
        geometry = asmGeometry;
        
    }

    private uint width;

    private uint height;

    public void GLControlResize(uint width,uint height)
    {
        this.width = width;
        this.height = height;
        gl.Viewport(0, 0,width,height);
        this.UpdateProjMatrix();
        
    }

    private bool first = true;
    public unsafe void Render()
    {
        if(geometry.Parts.Length==0&&first)
        {
            first = false;
            return;
        }

        gl.ClearColor(0.3725f, 0.6196f, 0.6275f, 1.0f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // also clear the depth buffer now!
                                                                                   // 应用四元数旋转
        // mat4 modelMatrix = glm::toMat4(rotationQuaternion);

        // // 平移到bBox中心
        // modelMatrix = glm::translate(modelMatrix, bBoxCenter);

        // // 平移回到原始位置
        // modelMatrix = glm::translate(modelMatrix, -bBoxCenter.x, -bBoxCenter.y, -bBoxCenter.z);

        var modelMatrix=Matrix.CreateTranslation(bBoxCenter);
        modelMatrix = Matrix.CreateFromQuaternion(rotation)*modelMatrix;
        modelMatrix = Matrix.CreateTranslation(-bBoxCenter)*modelMatrix;
        // // Prepare matrices
        // var xRadian = Radians(mouseXOffset);
        // var yRadian = Radians(mouseYOffset);
        // Matrix W =
        // world *Matrix.CreateFromQuaternion(rotation);
        // m_VSConstantBuffer.world = W;

        gl.Enable(GLEnum.PolygonOffsetFill);//开启深度偏移
        faceShader.Use();
        var W = modelMatrix;
        m_VSConstantBuffer.world = W;
        if (Matrix.Invert(W, out var result))
        {
            var nm = Matrix.Transpose(result);
            ReadOnlySpan<float> normalModel = [nm.M11, nm.M12, nm.M13, nm.M21, nm.M22, nm.M23, nm.M31, nm.M32, nm.M33];
            faceShader.UniformMatrix3("g_WIT", normalModel);
        }
        m_PSConstantBuffer.objColor = new Vector4(0.5882353f, 0.5882353f, 0.5882353f, 1f);
        faceShader.SetUniform("g_World", m_VSConstantBuffer.world);
        faceShader.SetUniform("g_View", m_VSConstantBuffer.view);
        faceShader.SetUniform("g_Proj", m_VSConstantBuffer.projection);
        faceShader.SetUniform("g_Translation", m_VSConstantBuffer.translation);
        faceShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);


        for (uint i = 0; i < geometry.Components.Length;i++)
        {
            var comp = geometry.Components[i];
            var part = geometry.Parts[comp.PartIndex];
            faceShader.SetUniform("g_Origin", comp.CompMatrix);
            partBuffers.GetPartBuffer(comp.PartIndex, out var vao, out var ebo);
            gl.BindVertexArray(vao);
            if (highlightType == HighlightType.Face && highlightFaceComp == i)
            {
                if (part.GetFaceStartIndexAndLengthByIndexArrayIndex(highlightFaceIndex,
                out var start, out var length))
                {
                    //去除高亮面的深度值加值,使得有多个面重叠的情况下,高亮面总是显示在最上面
                    gl.Disable(GLEnum.PolygonOffsetFill);
                    m_PSConstantBuffer.objColor = new Vector4(1f, 0.501f, 0f, 1f);
                    faceShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
                    gl.DrawElements(GLEnum.TriangleStrip, (uint)length,
                    GLEnum.UnsignedInt, (void*)(start*sizeof(uint)));
                    gl.Enable(GLEnum.PolygonOffsetFill);//开启深度偏移
                    m_PSConstantBuffer.objColor = new Vector4(0.5882353f, 0.5882353f, 0.5882353f, 1f);
                    faceShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
                }
            }
            // gl.DrawElements(GLEnum.Triangles, (uint)part.FaceIndexLength,
            // GLEnum.UnsignedInt, (void*)0);
            gl.DrawElements(GLEnum.TriangleStrip, part.FaceIndexLength,GLEnum.UnsignedInt, (void*)0);

        }

        gl.Disable(GLEnum.PolygonOffsetFill);
        lineShader.Use();
        m_PSConstantBuffer.objColor = new Vector4(0f, 0f, 0f, 1f);
        lineShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
        lineShader.SetUniform("g_World", m_VSConstantBuffer.world);
        lineShader.SetUniform("g_View", m_VSConstantBuffer.view);
        lineShader.SetUniform("g_Proj", m_VSConstantBuffer.projection);
        lineShader.SetUniform("g_Translation", m_VSConstantBuffer.translation);
        for (uint i = 0; i < geometry.Components.Length; i++)
        {
            var comp = geometry.Components[i];
            var part = geometry.Parts[comp.PartIndex];
            lineShader.SetUniform("g_Origin", comp.CompMatrix);
            partBuffers.GetPartBuffer(comp.PartIndex, out var vao, out var ebo);
            gl.BindVertexArray(vao);
            if (highlightType==HighlightType.Edge&&highlightEdgeComp == i)
            {
                if (part.GetEdgeStartIndexAndLengthByIndexArrayIndex(highlightEdgeIndex,
                out var start, out var length))
                {
                    gl.LineWidth(2.0f);
                    m_PSConstantBuffer.objColor = new Vector4(1f, 0f, 0f, 1f);
                    lineShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
                    gl.DrawElements(GLEnum.Lines, length,
                    GLEnum.UnsignedInt, (void*)((start + part.EdgeStartIndex) * sizeof(uint)));
                    m_PSConstantBuffer.objColor = new Vector4(0f, 0f, 0f, 1f);
                    lineShader.SetUniform("objectColor", m_PSConstantBuffer.objColor);
                    gl.LineWidth(1.0f);
                }
            }
            gl.DrawElements(GLEnum.Lines, part.EdgeIndexLength,
            GLEnum.UnsignedInt, (void*)(part.EdgeStartIndex * sizeof(uint)));
        }

    }

    float lastX = 0;
    float lastY = 0;


    public void MouseDown(KeyCode keyCode, int x, int y)
    {
        lastX = x;
        lastY = y;
        this.keyCode |= keyCode;
    }

    public void MouseUp(KeyCode keyCode, int x, int y)
    {
        if (keyCode ==KeyCode.Left &&keyCode == KeyCode.Left)
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
        float xPos = x;
        float yPos = y;

        float xOffset = xPos - lastX;
        float yOffset = lastY - yPos; // reversed since y-coordinates go from bottom to top

        lastX = xPos;
        lastY = yPos;
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
        var aspectRatio = float.Max(width, 1) / float.Max(height, 1);
        m_VSConstantBuffer.projection = Matrix.CreateOrthographicOffCenter(-orthoScale * aspectRatio,
        orthoScale * aspectRatio, -orthoScale, orthoScale, 0.1f, 100.0f);

        
    }

    #endregion

}
