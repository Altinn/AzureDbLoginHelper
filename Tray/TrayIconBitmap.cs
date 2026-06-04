using SkiaSharp;

namespace AzureDbLoginHelper;

internal static class TrayIconBitmap
{
    public const int PixelSize = 32;

    private static readonly SKColor FillColor = new(0, 120, 212);

    public static MemoryStream CreatePngStream()
    {
        using var surface = SKSurface.Create(new SKImageInfo(PixelSize, PixelSize, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using var path = SKPath.ParseSvgPathData(TrayIconPath.Data);
        path.FillType = SKPathFillType.EvenOdd;

        const float padding = 1f;
        var scale = (PixelSize - padding * 2) / (float)TrayIconPath.ViewBoxSize;
        canvas.Translate(padding, padding);
        canvas.Scale(scale);

        using var paint = new SKPaint
        {
            Color = FillColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawPath(path, paint);

        var stream = new MemoryStream();
        using (var image = surface.Snapshot())
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            data.SaveTo(stream);

        stream.Position = 0;
        return stream;
    }
}
