using AzureDbLoginHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static class Program
{
    private const string LocalSettingsPath = "appsettings.local.json";

    public static async Task Main(string[] args)
    {
        var host = CreateHost(args);
        await host.StartAsync();
        WriteStartupMessage();

        try
        {
#if USE_WINFORMS_TRAY
            var tray = host.Services.GetRequiredService<WindowsTrayHost>();
            await tray.RunAsync(host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
#elif USE_AVALONIA_TRAY
            await AvaloniaEntry.RunAsync(host, args);
#endif
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static IHost CreateHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        UseLocalAppSettings(builder);

        builder.Services
            .AddOptions<AzureDbLoginOptions>()
            .Bind(builder.Configuration.GetSection(AzureDbLoginOptions.SectionName))
            .Validate(o => !o.HasRoles || string.IsNullOrWhiteSpace(o.DefaultRole) || o.FindRole(o.DefaultRole) != null,
                "AzureDbLogin:DefaultRole must match a configured Roles[].Key when set.")
            .Validate(o => !o.HasRoles || o.Roles.TrueForAll(r =>
                    !string.IsNullOrWhiteSpace(r.PgUser) && !string.IsNullOrWhiteSpace(r.GroupObjectId)),
                "When roles are configured, each role must have PgUser and GroupObjectId.")
            .ValidateOnStart();

        builder.Services.AddSingleton<TokenService>();

#if USE_WINFORMS_TRAY
        builder.Services.AddSingleton<WindowsTrayHost>();
#elif USE_AVALONIA_TRAY
        builder.Services.AddSingleton<TrayController>();
#endif

        return builder.Build();
    }

    private static void UseLocalAppSettings(HostApplicationBuilder builder)
    {
        for (var i = builder.Configuration.Sources.Count - 1; i >= 0; i--)
        {
            if (builder.Configuration.Sources[i] is FileConfigurationSource { Path: var path }
                && path.Equals("appsettings.json", StringComparison.OrdinalIgnoreCase))
            {
                builder.Configuration.Sources.RemoveAt(i);
            }
        }

        builder.Configuration.AddJsonFile(LocalSettingsPath, optional: false, reloadOnChange: true);
    }

    private static void WriteStartupMessage()
    {
        Console.WriteLine("Azure DB Login Helper is running.");

        if (OperatingSystem.IsMacOS())
        {
            Console.WriteLine("  Look for the icon in the menu bar (top right).");
            Console.WriteLine("  Click the icon for roles and tokens.");
        }
        else
        {
            Console.WriteLine("  Look for the tray icon (click ^ in the taskbar if hidden).");
            Console.WriteLine("  Right-click the tray icon for the menu (double-click copies cached token).");
        }

        Console.WriteLine("  Press Ctrl+C here, or choose Exit from the menu, to quit.");
        Console.WriteLine();
    }
}
