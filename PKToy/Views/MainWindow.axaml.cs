using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using PKToy.Lib;
using PKToy.ViewModels;
using Viewer.IContract;

namespace PKToy.Views;

public partial class MainWindow : Window
{

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(this);
        PKToy.Lib.PKSession.Init();
        this.GL.UpdateScale(this.DesktopScaling);
        this.ScalingChanged += (sender, args) => this.GL.UpdateScale(this.DesktopScaling);
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
