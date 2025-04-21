using System;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Win32;
using Silk.NET.OpenGL;
using Avalonia.Win32.OpenGl.Angle;
using PKToy.Views;


var lifetime = new ClassicDesktopStyleApplicationLifetime { Args = args, ShutdownMode = ShutdownMode.OnLastWindowClose };

AppBuilder.Configure<Application>()
    .UsePlatformDetect()
    .With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.Wgl] })
    .AfterSetup(b => b.Instance?.Styles.Add(new FluentTheme()))
    // uncomment the line below to enable rider ht reload workaround
    //.UseRiderHotReload()
    .SetupWithLifetime(lifetime);

lifetime.MainWindow = new Window()
    .Title("PKToy MVU")
    .Width(1280)
    .Height(720).Content(new MainView());

#if DEBUG
lifetime.MainWindow.AttachDevTools();
#endif

lifetime.Start(args);
