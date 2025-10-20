using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace AzureDbLoginHelper;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var host = CreateHostBuilder().Build();
        
        // Run the service
        host.Run();
    }

    static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .UseWindowsService(options =>
            {
                options.ServiceName = "Azure DB Login Helper";
            })
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<TrayIconService>();
                services.AddLogging(builder =>
                {
                    builder.AddEventLog(new EventLogSettings
                    {
                        SourceName = "AzureDbLoginHelper",
                        LogName = "Application"
                    });
                });
            });
}
