namespace PKToy.Views;
using System.Diagnostics.CodeAnalysis;
using Viewer.Graphic.Opengl;
using Viewer.IContract;
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

[method: DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(OpenglView))]
public class OpenglView() : MvuView
{
    protected override object Build()
    {
        var r = New<Grid>().Background(Colors.Transparent.ToBrush())
            .Children(New<OpenGlPageControl>().Ref(out GLControl));
        RegKeyAction(r);
        return r;
    }

    public OpenGlPageControl GLControl = null!;

    double scale = 1;

    public void UpdateScale(double newScale)
    {
        this.scale = newScale;
        this.GLControl.UpdateScale(newScale);
    }

    private DateTime lastMouseMoveTime = DateTime.MinValue;
    private int anyKeyPressed = 0;

    public void RegKeyAction(Control container)
    {

        container.PointerPressed += (sender, args) =>
        {
            if (args.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
            {
                // 记录当前坐标
                var p = args.GetPosition(this);
                args.Handled = true;
                GLControl.GlRender!.MouseDown(KeyCode.Middle, (int)p.X, (int)p.Y);
                anyKeyPressed++;
            }
            if (args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                // 记录当前坐标
                var p = args.GetPosition(this);
                args.Handled = true;
                GLControl.GlRender!.MouseDown(KeyCode.Left, (int)p.X, (int)p.Y);
                anyKeyPressed++;
            }
        };
        container.PointerReleased += (sender, args) =>
        {
            if (args.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.MiddleButtonReleased)
            {
                // 记录当前坐标
                var p = args.GetPosition(this);
                args.Handled = true;
                GLControl.GlRender!.MouseUp(KeyCode.Middle, (int)p.X, (int)p.Y);
                anyKeyPressed--;
            }
            if (args.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                // 记录当前坐标
                var p = args.GetPosition(this);
                args.Handled = true;
                GLControl.LeftReleased((int)(p.X * scale), (int)(p.Y * scale));
                anyKeyPressed--;
            }
        };
        container.PointerMoved += (sender, args) =>
        {
            // 限制采样率，每 50 毫秒处理一次
            if (anyKeyPressed == 0 && (DateTime.Now - lastMouseMoveTime).TotalMilliseconds < 50)
            {
                return;
            }
            lastMouseMoveTime = DateTime.Now;
            // 记录当前坐标
            var p = args.GetPosition(this);
            args.Handled = true;
            GLControl.GlRender!.MouseMove((int)p.X, (int)p.Y);
            GLControl.Hover((int)(p.X * scale), (int)(p.Y * scale));
        };
        container.PointerWheelChanged += (sender, args) =>
        {
            var p = args.GetCurrentPoint(this);
            GLControl.GlRender!.MouseWheel((int)args.Delta.Y * 100);
        };

    }

}
