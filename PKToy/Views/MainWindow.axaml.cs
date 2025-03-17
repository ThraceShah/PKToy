using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using PKToy.Exchange.Step2Mid;
using PKToy.Lib;
using Viewer.IContract;

namespace PKToy.Views;

public partial class MainWindow : Window
{
    AsmGeometry CreateCubeLine()
    {
        var cube = new AsmGeometry();
        // var points = new List<Vector3>
        // {
        //     new Vector3(0, 0, 0),
        //     new Vector3(1, 0, 0),
        //     new Vector3(1, 1, 0),
        //     new Vector3(0, 1, 0),
        //     new Vector3(0, 0, 1),
        //     new Vector3(1, 0, 1),
        //     new Vector3(1, 1, 1),
        //     new Vector3(0, 1, 1),
        // };
        // var edgeIndices = new List<int>
        // {
        //     0, 1, 1, 2, 2, 3, 3, 0,
        //     4, 5, 5, 6, 6, 7, 7, 4,
        //     0, 4, 1, 5, 2, 6, 3, 7,
        // };
        var points = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
        };
        var edgeIndices = new List<int>
        {
            0, 1, 1, 2, 2, 3, 3, 0
        };

        var edgePartBuilder = new EdgePartBuilder();
        for (int i = 0; i < edgeIndices.Count; i += 2)
        {
            var edgeBuilder = new EdgeBuilder();
            edgeBuilder.InsertNextPoint(points[edgeIndices[i]]);
            edgeBuilder.InsertNextPoint(points[edgeIndices[i + 1]]);
            edgePartBuilder.TagEdgeBuilders.Add(i / 2, edgeBuilder);
        }
        var edgePart = edgePartBuilder.Build();
        edgePart.Modified();
        cube.AddPart(edgePart);
        cube.AddCompnent(edgePart, Matrix4x4.Identity);
        return cube;
    }
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
                Title = "选择文件",
                FileTypeFilter = [new FilePickerFileType("pk files") { Patterns = ["*.x_t", "*.x_b"] },
                new FilePickerFileType("step files") { Patterns = ["*.step","*.stp"] },
                ],
            };
            var result = await this.StorageProvider.OpenFilePickerAsync(option);

            if (result.IsNullOrZero())
            {
                return;
            }

            string filename = result[0].TryGetLocalPath();
            var stop = new Stopwatch();
            stop.Start();

            var extension = System.IO.Path.GetExtension(filename).ToLower();
            if (extension is ".x_t" or ".x_b")
            {
                var geometry = PKSession.OpenPart(filename);
                this.GL.GLControl.UpdateGeometry(geometry);
                GC.Collect();
                stop.Stop();
                Console.WriteLine($"open part elapsed time:{stop.ElapsedMilliseconds} ms");
                long after = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
                Console.WriteLine($"after Memory Used: {after}MB");
                Console.WriteLine($"after-before={after - before}MB");
            }
            else if (extension is ".step" or ".stp")
            {
                var geometry = Step2Mid.ResolveStep2Mid(filename);
            }
        };
        this.CubeBtn.Click += (sender, args) =>
        {
            var cube = CreateCubeLine();
            this.GL.GLControl.UpdateGeometry(cube);
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

        return !collection.Any();
    }
}
