using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AzureDbLoginHelper;

public partial class App : Application
{
    public static IHost? Host { get; private set; }

    public static void ConfigureHost(IHost host) => Host = host;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            if (Host != null)
            {
                var tray = Host.Services.GetRequiredService<TrayController>();
                tray.Initialize();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
