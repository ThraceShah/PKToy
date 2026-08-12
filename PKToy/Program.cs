using System;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Win32;
using Silk.NET.OpenGL;
using Avalonia.Win32.OpenGl.Angle;
using PKToy.Views;
using PKToy.Automation;


var lifetime = new ClassicDesktopStyleApplicationLifetime { Args = args, ShutdownMode = ShutdownMode.OnLastWindowClose };

AppBuilder.Configure<Application>()
    .UsePlatformDetect()
    .With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.Wgl] })
    .With(new AvaloniaNativePlatformOptions { RenderingMode = [AvaloniaNativeRenderingMode.OpenGl] })
    .AfterSetup(b => b.Instance?.Styles.Add(new FluentTheme()))
    // uncomment the line below to enable rider ht reload workaround
    //.UseRiderHotReload()
    .SetupWithLifetime(lifetime);

var mainView = new MainView();
var mainWindow = new Window()
    .Title("PKToy")
    .Width(1280)
    .Height(720).Content(mainView);
lifetime.MainWindow = mainWindow;

var httpPort = GetHttpPort(args);
using var automationServer = new AutomationServer(mainView, mainWindow, httpPort);
automationServer.Start();
Console.WriteLine($"PKToy automation API: http://127.0.0.1:{httpPort}");
lifetime.Start(args);

static int GetHttpPort(string[] args)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i].StartsWith("--http-port=", StringComparison.Ordinal))
        {
            return ParsePort(args[i]["--http-port=".Length..]);
        }
        if (args[i] == "--http-port" && i + 1 < args.Length)
        {
            return ParsePort(args[i + 1]);
        }
    }
    return 5180;
}

static int ParsePort(string value)
{
    return int.TryParse(value, out var port) && port is > 0 and <= 65535
        ? port
        : throw new ArgumentException($"Invalid HTTP port '{value}'.");
}
