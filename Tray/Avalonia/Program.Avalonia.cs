using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AzureDbLoginHelper;

internal static class AvaloniaEntry
{
    public static Task RunAsync(IHost host, string[] args)
    {
        App.ConfigureHost(host);

        var appLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var uiThread = new Thread(() =>
        {
            appLifetime.ApplicationStopping.Register(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    Dispatcher.UIThread.Post(desktop.Shutdown);
            });

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                ended.TrySetException(ex);
                return;
            }

            ended.TrySetResult();
        })
        {
            IsBackground = false,
            Name = "AvaloniaUI"
        };

        try
        {
            uiThread.SetApartmentState(ApartmentState.STA);
        }
        catch (PlatformNotSupportedException)
        {
        }

        uiThread.Start();
        return ended.Task;
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
