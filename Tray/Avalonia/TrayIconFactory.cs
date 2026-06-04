using Avalonia.Controls;

namespace AzureDbLoginHelper;

internal static class TrayIconFactory
{
    public static WindowIcon Create()
    {
        using var stream = TrayIconBitmap.CreatePngStream();
        return new WindowIcon(stream);
    }
}
