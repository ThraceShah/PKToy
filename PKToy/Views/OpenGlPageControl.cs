using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Silk.NET.OpenGL;
using Viewer.Graphic.Opengl;
using Viewer.IContract;

public class OpenGlPageControl : OpenGlControlBase
{

    bool inited = false;

    AsmGeometry? asmGeometry;
    bool updateAsm = false;

    uint width = 0;

    uint height = 0;

    bool updateSize = false;

    double scale = 1;
    public GlRender? GlRender { get; private set; }
    readonly Stopwatch sw = new();

    public void ResetWatch()
    {
        sw.Reset();
    }

    protected override unsafe void OnOpenGlInit(GlInterface gl)
    {
        if (inited is false)
        {
            var _gl = GL.GetApi(gl.GetProcAddress);
            GlRender = new GlRender(_gl);
            GlRender.GLControlLoad();
            inited = true;
            sw.Start();
        }
    }

    protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
    {
        //执行opengl相关操作的函数，必须在OnOpenGlRender或OnOpenGlInit内执行
        if (sw.ElapsedMilliseconds > 1000)
        {
            this.RequestNextFrameRendering();
            return;
        }
        //执行opengl相关操作的函数，必须在OnOpenGlRender或OnOpenGlInit内执行
        //包括使用Dispatcher.UIThread.Post到主线程执行opengl相关的函数都不行，具体原因还不清楚，可能是上下文错误
        this.GlRender!.Hover(hoverX, hoverY);
        if (leftReleased)
        {
            leftReleased = false;
            this.GlRender.MouseUp(KeyCode.Left, nx, ny);
        }
        if (updateAsm)
        {
            updateAsm = false;
            var watch = new Stopwatch();
            watch.Start();
            GlRender.UpdateGeometry(asmGeometry);
            watch.Stop();
            Console.WriteLine($"gpu:update geometry time={watch.ElapsedMilliseconds}ms");
        }
        if (updateSize)
        {
            updateSize = false;
            GlRender.GLControlResize(width, height);
        }
        GlRender.Render();
        this.RequestNextFrameRendering();
    }

    public unsafe void UpdateGeometry(AsmGeometry geometry)
    {
        this.asmGeometry = geometry;
        this.updateAsm = true;
        this.updateAsm = true;
        ResetWatch();
    }



    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        this.width = (uint)(e.NewSize.Width * this.scale);
        this.height = (uint)(e.NewSize.Height * this.scale);
        updateSize = true;
        ResetWatch();
    }

    public void UpdateScale(double newScale)
    {
        this.scale = newScale;
        this.width = (uint)(this.width * this.scale);
        this.height = (uint)(this.height * this.scale);
        updateSize = true;
        ResetWatch();
    }

    int nx = 0;
    int ny = 0;
    bool leftReleased = false;
    public void LeftReleased(int nx, int ny)
    {
        this.nx = nx;
        this.ny = ny;
        leftReleased = true;
        ResetWatch();
    }

    int hoverX = 0;
    int hoverY = 0;
    public void Hover(int x, int y)
    {
        hoverX = x;
        hoverY = y;
    }

}