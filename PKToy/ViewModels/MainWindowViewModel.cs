using Avalonia.Controls;
using Avalonia.Platform.Storage;
using PKToy.Lib;
using PKToy.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Viewer.IContract;

namespace PKToy.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly MainWindow _window;
    public IReactiveCommand FileCommand { get; }
    public IReactiveCommand SaveCommand { get; }
    public IReactiveCommand ResetCommand { get; }
    public IReactiveCommand CubeCommand { get; }

    public MainWindowViewModel(MainWindow window)
    {
        _window = window;
        FileCommand = ReactiveCommand.Create(FileCommandExecute);
        SaveCommand = ReactiveCommand.Create(SaveCommandExecute);
        CubeCommand = ReactiveCommand.Create(CubeCommandExecute);
        ResetCommand = ReactiveCommand.Create(ResetCommandExecute);
    }

    private async void FileCommandExecute()
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

        string filename = result[0].TryGetLocalPath();
        var stop = new Stopwatch();
        stop.Start();
        var extension = System.IO.Path.GetExtension(filename).ToLower();
        int partionTag = 0;
        if (extension is ".x_t" or ".x_b")
        {
            var geometry = PKSession.OpenPart(filename, out partionTag);
            _window.GL.GLControl.UpdateGeometry(geometry);
        }
        else if (extension is ".step" or ".stp")
        {
            var geometry = PKSession.OpenStep(filename, out partionTag);
            _window.GL.GLControl.UpdateGeometry(geometry);
        }
        GC.Collect();
        stop.Stop();
        Console.WriteLine($"open part elapsed time:{stop.ElapsedMilliseconds} ms");
        long after = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
        Console.WriteLine($"after Memory Used: {after}MB");
        Console.WriteLine($"after-before={after - before}MB");

        if (_window.TopolTree.DataContext is TopolTreeViewModel topolTreeVM)
        {
            topolTreeVM.UpdateTree(partionTag);
        }
    }

    private async void SaveCommandExecute()
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
        string filename = result.TryGetLocalPath();
        PKSession.SavePart(filename);
    }

    private void ResetCommandExecute()
    {
        PKSession.StopSession();
        PKSession.NewSession();
        var asm = new AsmGeometry();
        _window.GL.GLControl.UpdateGeometry(asm);
        if (_window.TopolTree.DataContext is TopolTreeViewModel topolTreeVM)
        {
            topolTreeVM.UpdateTree();
        }
        GC.Collect();
    }

    private void CubeCommandExecute()
    {
        var cube = CreateCubeLine();
        _window.GL.GLControl.UpdateGeometry(cube);
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
