using System.Diagnostics;

namespace AzureDbLoginHelper;

internal static class PlatformLauncher
{
    public static void OpenUrl(string url)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            Process.Start("open", url);
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            Process.Start(new ProcessStartInfo("xdg-open", url) { UseShellExecute = true });
            return;
        }

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
