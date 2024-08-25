using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PKToy.Lib;
using Silk.NET.OpenGL;
using Viewer.Avalonia.Entry.ViewModels;
using Viewer.Graphic.Opengl;
using Viewer.IContract;

namespace Viewer.Avalonia.Entry.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        PKToy.Lib.PKSession.Init();
        this.GL.UpdateScale(this.DesktopScaling);
        this.ScalingChanged += (sender, args) => this.GL.UpdateScale(this.DesktopScaling);
        this.OpenFileBtn.Click += async (sender, args) =>
        {
            long before = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
            Console.WriteLine($"before Memory Used: {before}MB");

            var option = new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "选择mem文件",
                FileTypeFilter = [ new FilePickerFileType("files"){Patterns = ["*.mem","*.x_t",".x_b"]} ],
            };
            var result = await this.StorageProvider.OpenFilePickerAsync(option);

            if (result.IsNullOrZero())
            {
                return;
            }
            string filename = result[0].TryGetLocalPath();
            var stop = new Stopwatch();
            stop.Start();

            if (filename.EndsWith(".mem"))
            {
                Viewer.Geometry.Adapter.GetGeometryByPath(filename, out IContract.AsmGeometry geometry);
                this.GL.GLControl.UpdateGeometry(ref geometry);
            }
            else
            {
                PKSession.OpenPart(filename, out IContract.AsmGeometry geometry);
                this.GL.GLControl.UpdateGeometry(ref geometry);
            }
            GC.Collect();
            stop.Stop();
            Console.WriteLine(stop.ElapsedMilliseconds);
            long after = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
            Console.WriteLine($"after Memory Used: {after}MB");
            Console.WriteLine($"after-before={after - before}MB");
        };
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        this.GL.GLControl.GlRender.KeyDown(ConvertKeyToWinformKey(e.Key));

    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        this.GL.GLControl.GlRender.KeyUp(ConvertKeyToWinformKey(e.Key));

    }

    private static KeyCode ConvertKeyToWinformKey(Key key) => key switch
    {
        Key.LeftCtrl => KeyCode.Control,
        Key.RightCtrl => KeyCode.Control,
        _ => KeyCode.None,
    };

}

static class Extension
{
    public static bool IsNullOrZero<T>(this IEnumerable<T> collection)
    {
        if (collection is null)
        {
            return true;
        }
        if (!collection.Any())
        {
            return true;
        }
        return false;
    }
}
