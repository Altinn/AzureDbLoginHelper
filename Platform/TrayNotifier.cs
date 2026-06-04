using System.Diagnostics;

namespace AzureDbLoginHelper;

internal static class TrayNotifier
{
    public static void Show(string title, string message)
    {
        if (!OperatingSystem.IsMacOS())
            return;

        var escapedTitle = EscapeForAppleScript(title);
        var escapedMessage = EscapeForAppleScript(message);
        Process.Start(new ProcessStartInfo
        {
            FileName = "osascript",
            Arguments = $"-e 'display notification \"{escapedMessage}\" with title \"{escapedTitle}\"'",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    private static string EscapeForAppleScript(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
