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
public class OpenglView() : ViewBase
{
    protected override object Build()
    {
        var r = New<Grid>().Background(Brushes.Transparent)
            .Children(New<OpenGlPageControl>().Ref(out GLControl));
        GLControl.UpdateScale(scale);
        RegKeyAction(r);
        return r;
    }

    public OpenGlPageControl GLControl = null!;

    double scale = 1;

    public void UpdateScale(double newScale)
    {
        this.scale = newScale;
        this.GLControl?.UpdateScale(newScale);
    }

    private DateTime lastMouseMoveTime = DateTime.MinValue;
    private int anyKeyPressed = 0;

    public void RegKeyAction(Control container)
    {
        // 添加触摸板手势支持变量
        var isGestureMode = false;
        var gestureStartPos = new Point(0, 0);
        var lastGesturePos = new Point(0, 0);
        var scrollStartTime = DateTime.MinValue;
        var isZoomGesture = false;
        var zoomStartY = 0.0;

        // 确保容器可以接收键盘事件
        container.Focusable = true;
        container.Focus();

        // 监听键盘事件来检测手势模式
        container.KeyDown += (sender, args) =>
        {
            // 使用 Alt 键 + 鼠标来模拟三指旋转手势
            if (args.Key == Key.LeftAlt || args.Key == Key.RightAlt)
            {
                isGestureMode = true;
                args.Handled = true;
            }
        };

        container.KeyUp += (sender, args) =>
        {
            if (args.Key == Key.LeftAlt || args.Key == Key.RightAlt)
            {
                if (isGestureMode && anyKeyPressed > 0)
                {
                    // 结束手势模式，释放鼠标中键（用于旋转）或结束缩放手势
                    if (!isZoomGesture)
                    {
                        GLControl.GlRender!.MouseUp(KeyCode.Middle, (int)lastGesturePos.X, (int)lastGesturePos.Y);
                        anyKeyPressed--;
                    }
                    else
                    {
                        isZoomGesture = false;
                        anyKeyPressed--;
                    }
                }
                isGestureMode = false;
                args.Handled = true;
            }
        };

        // 合并的 PointerPressed 事件处理
        container.PointerPressed += (sender, args) =>
        {
            var pointer = args.GetCurrentPoint(this);
            var pos = args.GetPosition(this);

            // 如果是手势模式且是右键，转换为缩放手势
            if (isGestureMode && pointer.Properties.IsRightButtonPressed)
            {
                gestureStartPos = pos;
                lastGesturePos = pos;
                zoomStartY = pos.Y;
                isZoomGesture = true;
                anyKeyPressed++;
                args.Handled = true;
                return;
            }

            // 如果是手势模式且是左键，转换为中键旋转
            if (isGestureMode && pointer.Properties.IsLeftButtonPressed)
            {
                gestureStartPos = pos;
                lastGesturePos = pos;
                isZoomGesture = false;
                GLControl.GlRender!.MouseDown(KeyCode.Middle, (int)pos.X, (int)pos.Y);
                anyKeyPressed++;
                args.Handled = true;
                return;
            }

            // 原有的中键处理
            if (pointer.Properties.IsMiddleButtonPressed)
            {
                GLControl.GlRender!.MouseDown(KeyCode.Middle, (int)pos.X, (int)pos.Y);
                anyKeyPressed++;
                args.Handled = true;
                return;
            }

            // 原有的左键处理
            if (pointer.Properties.IsLeftButtonPressed)
            {
                GLControl.GlRender!.MouseDown(KeyCode.Left, (int)pos.X, (int)pos.Y);
                anyKeyPressed++;
                args.Handled = true;
                return;
            }
        };

        // 合并的 PointerReleased 事件处理
        container.PointerReleased += (sender, args) =>
        {
            var pointer = args.GetCurrentPoint(this);
            var pos = args.GetPosition(this);

            // 如果是手势模式且是右键释放，结束缩放手势
            if (isGestureMode && pointer.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased && isZoomGesture)
            {
                isZoomGesture = false;
                anyKeyPressed--;
                args.Handled = true;
                return;
            }

            // 如果是手势模式且是左键释放，转换为中键释放
            if (isGestureMode && pointer.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased && !isZoomGesture && anyKeyPressed > 0)
            {
                GLControl.GlRender!.MouseUp(KeyCode.Middle, (int)pos.X, (int)pos.Y);
                anyKeyPressed--;
                args.Handled = true;
                return;
            }

            // 原有的中键释放处理
            if (pointer.Properties.PointerUpdateKind == PointerUpdateKind.MiddleButtonReleased)
            {
                GLControl.GlRender!.MouseUp(KeyCode.Middle, (int)pos.X, (int)pos.Y);
                anyKeyPressed--;
                args.Handled = true;
                return;
            }

            // 原有的左键释放处理
            if (pointer.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                GLControl.LeftReleased((int)(pos.X * scale), (int)(pos.Y * scale));
                anyKeyPressed--;
                args.Handled = true;
                return;
            }
        };

        // 合并的 PointerMoved 事件处理
        container.PointerMoved += (sender, args) =>
        {
            var pos = args.GetPosition(this);

            // 如果是手势模式且是缩放手势，模拟滚轮缩放
            if (isGestureMode && isZoomGesture && anyKeyPressed > 0)
            {
                var deltaY = pos.Y - zoomStartY;
                var wheelDelta = (int)(deltaY * 5); // 调整灵敏度，向下拖拽为正值（放大），向上拖拽为负值（缩小）

                if (Math.Abs(wheelDelta) > 5) // 避免过于敏感
                {
                    GLControl.GlRender!.MouseWheel(-wheelDelta); // 反转方向使其更直观：向下拖拽放大，向上拖拽缩小
                    zoomStartY = pos.Y; // 更新起始位置，实现连续缩放
                }
                lastGesturePos = pos;
                args.Handled = true;
                return;
            }

            // 如果是手势模式且有按键按下且不是缩放手势，模拟中键拖拽
            if (isGestureMode && !isZoomGesture && anyKeyPressed > 0)
            {
                GLControl.GlRender!.MouseMove((int)pos.X, (int)pos.Y);
                lastGesturePos = pos;
                args.Handled = true;
                return;
            }

            // 原有的鼠标移动处理
            // 限制采样率，每 50 毫秒处理一次
            if (anyKeyPressed == 0 && (DateTime.Now - lastMouseMoveTime).TotalMilliseconds < 50)
            {
                return;
            }
            lastMouseMoveTime = DateTime.Now;

            GLControl.GlRender!.MouseMove((int)pos.X, (int)pos.Y);
            GLControl.Hover((int)(pos.X * scale), (int)(pos.Y * scale));
            args.Handled = true;
        };

        // 合并的 PointerWheelChanged 事件处理
        container.PointerWheelChanged += (sender, args) =>
        {
            GLControl.GlRender!.MouseWheel((int)args.Delta.Y * 100);
        };

    }

}
