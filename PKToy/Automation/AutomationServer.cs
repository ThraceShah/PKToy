using System.Globalization;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using PKToy.Lib;
using PKToy.Views;
using Viewer.Graphic.Opengl;

namespace PKToy.Automation;

internal sealed class AutomationServer : IDisposable
{
    internal static readonly string[] StandardViews =
    [
        "front", "back", "left", "right", "top", "bottom",
        "front-top-left", "front-top-right", "front-bottom-left", "front-bottom-right",
        "back-top-left", "back-top-right", "back-bottom-left", "back-bottom-right"
    ];

    private readonly MainView mainView;
    private readonly Window window;
    private readonly WebApplication application;
    private bool started;
    private int disposed;

    public AutomationServer(MainView mainView, Window window, int port)
    {
        this.mainView = mainView;
        this.window = window;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        application = builder.Build();
        MapRoutes();
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        application.StartAsync().GetAwaiter().GetResult();
        started = true;
    }

    private void MapRoutes()
    {
        application.MapGet("/api/health", Guard(Health));
        application.MapPost("/api/files/open", Guard(OpenFile));
        application.MapPost("/api/files/save", Guard(SaveFile));
        application.MapPost("/api/session/reset", Guard(Reset));
        application.MapPost("/api/geometry/cube", Guard(Cube));
        application.MapPost("/api/scripts/run", Guard(RunScript));
        application.MapGet("/api/topology", Guard(Topology));
        application.MapPost("/api/view/fit", Guard(FitDisplay));
        application.MapPost("/api/view/orientation", Guard(SetOrientation));
        application.MapPost("/api/view/rotate", Guard(Rotate));
        application.MapPost("/api/view/zoom", Guard(Zoom));
        application.MapPost("/api/view/select", Guard(Select));
        application.MapGet("/api/screenshots/view", Guard(ViewScreenshot));
        application.MapGet("/api/screenshots/views", Guard(ViewsScreenshot));
        application.MapGet("/api/screenshots/window", Guard(WindowScreenshot));
    }

    private static RequestDelegate Guard(Func<HttpContext, Task> handler) => async context =>
    {
        try
        {
            await handler(context);
        }
        catch (ArgumentException exception)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            await WriteError(context, StatusCodes.Status409Conflict, exception.Message);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            await WriteError(context, StatusCodes.Status500InternalServerError, exception.Message);
        }
    };

    private Task Health(HttpContext context) => WriteJson(context, writer =>
    {
        writer.WriteString("status", "ok");
        writer.WriteStartArray("views");
        foreach (var view in StandardViews)
        {
            writer.WriteStringValue(view);
        }
        writer.WriteEndArray();
    });

    private async Task OpenFile(HttpContext context)
    {
        var path = RequiredQuery(context, "path");
        await Dispatcher.UIThread.InvokeAsync(() => mainView.OpenFile(path));
        await WriteOk(context);
    }

    private async Task SaveFile(HttpContext context)
    {
        var path = RequiredQuery(context, "path");
        await Dispatcher.UIThread.InvokeAsync(() => mainView.SaveFile(path));
        await WriteOk(context);
    }

    private async Task Reset(HttpContext context)
    {
        await Dispatcher.UIThread.InvokeAsync(mainView.Reset);
        await WriteOk(context);
    }

    private async Task Cube(HttpContext context)
    {
        await Dispatcher.UIThread.InvokeAsync(mainView.ShowCube);
        await WriteOk(context);
    }

    private async Task RunScript(HttpContext context)
    {
        var path = RequiredQuery(context, "path");
        var succeeded = await Dispatcher.UIThread.InvokeAsync(() => mainView.RunScript(path));
        if (!succeeded)
        {
            throw new InvalidOperationException("The script did not complete successfully.");
        }
        await WriteOk(context);
    }

    private async Task Topology(HttpContext context)
    {
        var tables = await Dispatcher.UIThread.InvokeAsync(mainView.GetTopology);
        await WriteJson(context, writer =>
        {
            writer.WriteStartArray("bodies");
            foreach (var table in tables)
            {
                writer.WriteStartObject();
                writer.WriteStartArray("nodes");
                foreach (var node in table.Nodes)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("tag", node.Tag);
                    writer.WriteString("type", node.TypeName);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteStartArray("relations");
                foreach (var relation in table.Relations)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("parent", relation.Parent);
                    writer.WriteNumber("child", relation.Child);
                    writer.WriteString("sense", relation.Sense);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        });
    }

    private async Task FitDisplay(HttpContext context)
    {
        await Dispatcher.UIThread.InvokeAsync(mainView.FitDisplay);
        await WriteOk(context);
    }

    private async Task SetOrientation(HttpContext context)
    {
        var name = RequiredQuery(context, "name");
        var found = await Dispatcher.UIThread.InvokeAsync(() => mainView.SetStandardView(name));
        if (!found)
        {
            throw new ArgumentException($"Unknown view '{name}'.", "name");
        }
        await WriteOk(context);
    }

    private async Task Rotate(HttpContext context)
    {
        var yaw = RequiredFloat(context, "yaw");
        var pitch = RequiredFloat(context, "pitch");
        await Dispatcher.UIThread.InvokeAsync(() => mainView.RotateView(yaw, pitch));
        await WriteOk(context);
    }

    private async Task Zoom(HttpContext context)
    {
        var delta = RequiredInt(context, "delta");
        await Dispatcher.UIThread.InvokeAsync(() => mainView.ZoomView(delta));
        await WriteOk(context);
    }

    private async Task Select(HttpContext context)
    {
        var x = RequiredInt(context, "x");
        var y = RequiredInt(context, "y");
        await Dispatcher.UIThread.InvokeAsync(() => mainView.SelectView(x, y));
        await WriteOk(context);
    }

    private async Task ViewScreenshot(HttpContext context)
    {
        var name = context.Request.Query["name"].FirstOrDefault();
        var capture = await Dispatcher.UIThread.InvokeAsync(() => mainView.CaptureView(name));
        await WritePng(context, PngEncoder.Encode(capture));
    }

    private async Task ViewsScreenshot(HttpContext context)
    {
        var captures = new ViewCapture[StandardViews.Length];
        for (var i = 0; i < StandardViews.Length; i++)
        {
            var name = StandardViews[i];
            captures[i] = await Dispatcher.UIThread.InvokeAsync(() => mainView.CaptureView(name));
        }
        await WritePng(context, PngEncoder.Encode(PngEncoder.Combine(captures, 4)));
    }

    private async Task WindowScreenshot(HttpContext context)
    {
        var viewCapture = await Dispatcher.UIThread.InvokeAsync(() => mainView.CaptureView());
        var png = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var scaling = window.RenderScaling;
            var size = PixelSize.FromSize(window.ClientSize, scaling);
            using var bitmap = new RenderTargetBitmap(size, new Vector(96 * scaling, 96 * scaling));
            bitmap.Render(window);
            using var viewBitmap = PngEncoder.CreateBitmap(viewCapture, 96 * scaling);
            var viewPosition = mainView.ViewControl.TranslatePoint(default, window) ?? default;
            using (var drawingContext = bitmap.CreateDrawingContext(clear: false))
            {
                drawingContext.DrawImage(viewBitmap, new Rect(viewPosition, mainView.ViewControl.Bounds.Size));
            }
            using var stream = new MemoryStream();
            bitmap.Save(stream);
            return stream.ToArray();
        });
        await WritePng(context, png);
    }

    private static string RequiredQuery(HttpContext context, string name)
    {
        var value = context.Request.Query[name].FirstOrDefault();
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"Query parameter '{name}' is required.", name)
            : value;
    }

    private static int RequiredInt(HttpContext context, string name)
    {
        var value = RequiredQuery(context, name);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new ArgumentException($"Query parameter '{name}' must be an integer.", name);
    }

    private static float RequiredFloat(HttpContext context, string name)
    {
        var value = RequiredQuery(context, name);
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new ArgumentException($"Query parameter '{name}' must be a number.", name);
    }

    private static Task WriteOk(HttpContext context) => WriteJson(context, writer => writer.WriteBoolean("ok", true));

    private static Task WriteError(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        return WriteJson(context, writer => writer.WriteString("error", message));
    }

    private static async Task WriteJson(HttpContext context, Action<Utf8JsonWriter> content)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        await using var writer = new Utf8JsonWriter(context.Response.Body);
        writer.WriteStartObject();
        content(writer);
        writer.WriteEndObject();
        await writer.FlushAsync(context.RequestAborted);
    }

    private static async Task WritePng(HttpContext context, byte[] png)
    {
        context.Response.ContentType = "image/png";
        context.Response.ContentLength = png.Length;
        await context.Response.Body.WriteAsync(png, context.RequestAborted);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        if (started)
        {
            application.StopAsync().GetAwaiter().GetResult();
        }
        application.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
