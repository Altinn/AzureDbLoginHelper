using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AzureDbLoginHelper;

internal static class AvaloniaEntry
{
    /// <summary>
    /// Blocks the caller thread until Avalonia exits. On macOS this must be the process main thread.
    /// </summary>
    public static void Run(IHost host, string[] args)
    {
        App.ConfigureHost(host);

        host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                Dispatcher.UIThread.Post(() => desktop.Shutdown());
        });

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
