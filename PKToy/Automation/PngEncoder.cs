using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Viewer.Graphic.Opengl;

namespace PKToy.Automation;

internal static class PngEncoder
{
    public static unsafe byte[] Encode(ViewCapture capture)
    {
        using var bitmap = CreateBitmap(capture, 96);
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        return stream.ToArray();
    }

    public static unsafe Bitmap CreateBitmap(ViewCapture capture, double dpi)
    {
        var width = checked((int)capture.Width);
        var height = checked((int)capture.Height);
        fixed (byte* data = capture.Rgba)
        {
            return new Bitmap(
                PixelFormats.Rgba8888,
                AlphaFormat.Opaque,
                (nint)data,
                new PixelSize(width, height),
                new Vector(dpi, dpi),
                checked(width * 4));
        }
    }

    public static ViewCapture Combine(IReadOnlyList<ViewCapture> captures, int columns)
    {
        if (captures.Count == 0)
        {
            throw new ArgumentException("At least one capture is required.", nameof(captures));
        }

        var cellWidth = checked((int)captures[0].Width);
        var cellHeight = checked((int)captures[0].Height);
        var rows = (captures.Count + columns - 1) / columns;
        var width = checked(cellWidth * columns);
        var height = checked(cellHeight * rows);
        var rgba = GC.AllocateUninitializedArray<byte>(checked(width * height * 4));
        for (var i = 0; i < rgba.Length; i += 4)
        {
            rgba[i] = 95;
            rgba[i + 1] = 158;
            rgba[i + 2] = 160;
            rgba[i + 3] = 255;
        }

        for (var index = 0; index < captures.Count; index++)
        {
            var capture = captures[index];
            if (capture.Width != (uint)cellWidth || capture.Height != (uint)cellHeight)
            {
                throw new InvalidOperationException("All view captures must have the same size.");
            }

            var column = index % columns;
            var row = index / columns;
            for (var y = 0; y < cellHeight; y++)
            {
                var source = capture.Rgba.AsSpan(y * cellWidth * 4, cellWidth * 4);
                var destinationOffset = ((row * cellHeight + y) * width + column * cellWidth) * 4;
                source.CopyTo(rgba.AsSpan(destinationOffset, cellWidth * 4));
            }
        }

        return new ViewCapture(checked((uint)width), checked((uint)height), rgba);
    }
}
