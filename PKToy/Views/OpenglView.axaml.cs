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
using PKToy.ViewModels;
using Viewer.Graphic.Opengl;
using Viewer.IContract;
using static System.Collections.Specialized.BitVector32;

namespace PKToy.Views;

public partial class OpenglView : UserControl
{
    public OpenglView()
    {
        InitializeComponent();
        //必须要设置背景色才能触发点击测试
        this.Background = Brushes.Transparent;
        this.RegKeyAction();
    }

    double scale = 1;

    public void UpdateScale(double newScale)
    {
        this.scale = newScale;
        this.GLControl.UpdateScale(newScale);
    }


    public void RegKeyAction()
    {

        this.PointerPressed += (sender, args) =>
        {
            if (args.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
            {
                // 记录当前坐标
                var p = args.GetPosition(this);
                args.Handled = true;
                GLControl.GlRender.MouseDown(KeyCode.Middle, (int)p.X, (int)p.Y);
            }
            if (args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                // 记录当前坐标
                var p = args.GetPosition(this);
                args.Handled = true;
                GLControl.GlRender.MouseDown(KeyCode.Left, (int)p.X, (int)p.Y);
            }
        };
        this.PointerReleased += (sender, args) =>
        {
            if (args.GetCurrentPoint(this).Properties.PointerUpdateKind==PointerUpdateKind.MiddleButtonReleased)
            {
                // 记录当前坐标
                var p = args.GetPosition(this);
                args.Handled = true;
                GLControl.GlRender.MouseUp(KeyCode.Middle, (int)p.X, (int)p.Y);
            }
            if (args.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                // 记录当前坐标
                var p = args.GetPosition(this);
                args.Handled = true;
                GLControl.LeftReleased((int)(p.X * scale), (int)(p.Y * scale));
            }
        };
        this.PointerMoved += (sender, args) =>
        {
            // 记录当前坐标
            var p = args.GetPosition(this);
            args.Handled = true;
            GLControl.GlRender.MouseMove((int)p.X, (int)p.Y);
        };
        this.PointerWheelChanged += (sender, args) =>
        {
            var p = args.GetCurrentPoint(this);
            GLControl.GlRender.MouseWheel((int)args.Delta.Y*100);
        };

    }

}

public class OpenGlPageControl : OpenGlControlBase
{

    bool inited = false;

    AsmGeometry asmGeometry;
    bool updateAsm = false;

    uint width = 0;

    uint height = 0;

    bool updateSize = false;

    double scale = 1;
    public GlRender GlRender{ get; private set; }
    readonly Stopwatch sw = new();

    public void ResetWatch()
    {
        sw.Reset();
    }

    protected override unsafe void OnOpenGlInit(GlInterface gl)
    {
        if(inited is false)
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
        if (leftReleased)
        {
            leftReleased = false;
            this.GlRender.MouseUp(KeyCode.Left, nx, ny);
        }
        if(updateAsm)
        {
            updateAsm = false;
            GlRender.UpdateGeometry(ref asmGeometry);
        }
        if(updateSize)
        {
            updateSize = false;
            GlRender.GLControlResize(width, height);
        }
        GlRender.Render();
        this.RequestNextFrameRendering();
    }

    public unsafe void UpdateGeometry(ref AsmGeometry geometry)
    {
        this.asmGeometry = geometry;
        this.updateAsm = true;
        this.updateAsm = true;
        ResetWatch();
    }



    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        this.width = (uint)(e.NewSize.Width*this.scale);
        this.height = (uint)(e.NewSize.Height*this.scale);
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
    public void LeftReleased(int nx,int ny)
    {
        this.nx = nx;
        this.ny = ny;
        leftReleased = true;
        ResetWatch();
    }

}