using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AzureDbLoginHelper;

internal static class TrayIconFactory
{
    public static WindowIcon Create()
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(32, 32), new Vector(96, 96));
        using (var ctx = bitmap.CreateDrawingContext())
        {
            var azure = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            ctx.DrawEllipse(azure, null, new Rect(1, 1, 30, 30));

            var white = Brushes.White;
            ctx.DrawRectangle(white, null, new Rect(9, 13, 14, 11));
            ctx.DrawRectangle(white, null, new Rect(11, 8, 10, 6));
            ctx.DrawEllipse(white, null, new Rect(13, 6, 6, 4));
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream);
        stream.Position = 0;
        return new WindowIcon(stream);
    }
}
