namespace PKToy.Views;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using PKToy.Lib;
using PKToy.Script;
using System.Numerics;
using Viewer.IContract;
class MainView : MvuView
{
    private Window _window = null!;
    private OpenglView GL = null!;
    private TopolTreeView TopolTree = null!;
    protected override object Build() => New<Grid>()
    .Rows("30,20*")
    .Children(
        New<StackPanel>().Row(0).Orientation(Orientation.Horizontal)
        .Children(
            New<Button>().Content("File").Width(50).OnClick(FileClick),
            New<Button>().Content("Save").Width(50).OnClick(SaveClick),
            New<Button>().Content("Reset").Width(50).OnClick(ResetClick),
            New<Button>().Content("Cube").Width(50).OnClick(CubeClick),
            New<Button>().Content("RunScript").Width(50).OnClick(RunScriptClick)
        ),
        New<Grid>().Row(1).Cols("2*,5,5*").Children(
            New<TopolTreeView>().Ref(out TopolTree).Col(0),
            New<GridSplitter>().Col(1).Width(5).Background(Colors.Gray.ToBrush()).
            HorizontalAlignment(HorizontalAlignment.Stretch).VerticalAlignment(VerticalAlignment.Stretch).
            ResizeDirection(GridResizeDirection.Columns).ResizeBehavior(GridResizeBehavior.PreviousAndNext),
            New<OpenglView>().Ref(out GL).Col(2)
        )
    ).OnAttachedToLogicalTree(x =>
    {
        _window = (Window)x.Root;
        PKToy.Lib.PKSession.Init();
        this.GL.UpdateScale(_window.DesktopScaling);
        _window.ScalingChanged += (sender, args) => this.GL.UpdateScale(_window.DesktopScaling);
    });

    private async void FileClick(object obj)
    {
        long before = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
        Console.WriteLine($"before Memory Used: {before}MB");

        var option = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "选择文件",
            FileTypeFilter = [new ("step files") { Patterns = ["*.step","*.stp"] },
            new ("pk files") { Patterns = ["*.x_t", "*.x_b"] },
                ],
        };
        var result = await _window.StorageProvider.OpenFilePickerAsync(option);

        if (result.Count == 0)
        {
            return;
        }

        string filename = result[0].TryGetLocalPath()!;
        var stop = new Stopwatch();
        stop.Start();
        var extension = System.IO.Path.GetExtension(filename).ToLower();
        int partitionTag = 0;
        if (extension is ".x_t" or ".x_b")
        {
            var geometry = PKSession.OpenPart(filename, out partitionTag);
            this.GL.GLControl.UpdateGeometry(geometry);
        }
        else if (extension is ".step" or ".stp")
        {
            var geometry = PKSession.OpenStep(filename, out partitionTag);
            this.GL.GLControl.UpdateGeometry(geometry);
        }
        GC.Collect();
        stop.Stop();
        Console.WriteLine($"open part elapsed time:{stop.ElapsedMilliseconds} ms");
        long after = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
        Console.WriteLine($"after Memory Used: {after}MB");
        Console.WriteLine($"after-before={after - before}MB");

        this.TopolTree.UpdateTree(partitionTag);
    }

    private async void SaveClick(object obj)
    {
        var option = new FilePickerSaveOptions
        {
            SuggestedFileName = "part.x_t",
            Title = "保存文件",
            FileTypeChoices = [new FilePickerFileType("pk files") { Patterns = ["*.x_t", ".x_b"] }],
        };
        var result = await _window.StorageProvider.SaveFilePickerAsync(option);
        if (result is null)
        {
            return;
        }
        string filename = result.TryGetLocalPath()!;
        PKSession.SavePart(filename);
    }

    private void ResetClick(object obj)
    {
        PKSession.StopSession();
        PKSession.NewSession();
        var asm = new AsmGeometry();
        this.GL.GLControl.UpdateGeometry(asm);
        this.TopolTree.UpdateTree();
        GC.Collect();
    }

    private void CubeClick(object obj)
    {
        var cube = CreateCubeLine();
        this.GL.GLControl.UpdateGeometry(cube);
    }


    private async void RunScriptClick(object obj)
    {
        var option = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "选择文件",
            FileTypeFilter = [new("csx files") { Patterns = ["*.csx"] }],
        };
        var result = await _window.StorageProvider.OpenFilePickerAsync(option);

        if (result.Count == 0)
        {
            return;
        }

        string filename = result[0].TryGetLocalPath()!;
        var stop = new Stopwatch();
        stop.Start();
        var r = await CsxRunner.Run(filename);
        stop.Stop();
        if (r == false)
        {
            return;
        }
        Console.WriteLine($"run script elapsed time:{stop.ElapsedMilliseconds} ms");
        UpdateView();
    }

    private void UpdateView()
    {
        var watch = new Stopwatch();
        watch.Start();
        var asm = PKSession.OpenCurrentPartition();
        this.GL.GLControl.UpdateGeometry(asm);
        this.TopolTree.UpdateTree();
        watch.Stop();
        Console.WriteLine($"update view elapsed time:{watch.ElapsedMilliseconds} ms");
    }

    static AsmGeometry CreateCubeLine()
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
        cube.AddComponent(edgePart, Matrix4x4.Identity);
        return cube;
    }

}