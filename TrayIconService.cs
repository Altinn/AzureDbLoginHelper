using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AzureDbLoginHelper;

public class TrayIconService : BackgroundService
{
    private readonly ILogger<TrayIconService> _logger;
    private NotifyIcon? _trayIcon;

    public TrayIconService(ILogger<TrayIconService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            InitializeTrayIcon();
            _logger.LogInformation("Tray icon service started");

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tray icon service");
        }
        finally
        {
            _trayIcon?.Dispose();
        }
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Azure DB Login Helper - Double-click to get token",
            Visible = true
        };

        _trayIcon.DoubleClick += OnTrayIconDoubleClick;
    }

    private void OnGetTokenClick(object? sender, EventArgs e)
    {
        // Use Task.Run to avoid blocking the UI thread
        _ = Task.Run(async () =>
        {
            try
            {
                // Update UI directly (no thread marshalling needed for tray icon)
                if (_trayIcon != null) _trayIcon.Text = "Azure DB Login Helper - Getting token...";

                var token = await GetAzureAccessTokenAsync();
                
                _logger.LogInformation("Token retrieved: {TokenLength} characters", token?.Length ?? 0);
                
                // Update UI directly
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        await SetClipboardTextAsync(token);
                        _logger.LogInformation("Token set to clipboard successfully");
                    }
                    catch (Exception clipboardEx)
                    {
                        _logger.LogError(clipboardEx, "Failed to set token to clipboard");
                    }
                    
                    if (_trayIcon != null) _trayIcon.Text = "Azure DB Login Helper - Token ready";
                    
                    // Show balloon tip
                    if (_trayIcon != null) _trayIcon.ShowBalloonTip(3000, "Success", "Access token copied to clipboard!", ToolTipIcon.Info);
                    
                    _logger.LogInformation("Access token successfully retrieved and copied to clipboard");
                }
                else
                {
                    if (_trayIcon != null) _trayIcon.Text = "Azure DB Login Helper - Error";
                    
                    // Check if Azure CLI was found
                    var azPath = FindAzureCliPath();
                    var errorMessage = string.IsNullOrEmpty(azPath) 
                        ? "Azure CLI not found. Please install Azure CLI from https://aka.ms/installazurecliwindows"
                        : "Failed to retrieve access token. Check if you're logged in with 'az login'";
                        
                    if (_trayIcon != null) _trayIcon.ShowBalloonTip(5000, "Error", errorMessage, ToolTipIcon.Error);
                    
                    _logger.LogWarning("Failed to retrieve access token");
                }
            }
            catch (Exception ex)
            {
                // Update UI directly
                if (_trayIcon != null) _trayIcon.Text = "Azure DB Login Helper - Error";
                if (_trayIcon != null) _trayIcon.ShowBalloonTip(5000, "Error", $"Error: {ex.Message}", ToolTipIcon.Error);
                
                _logger.LogError(ex, "Error retrieving access token");
            }
            finally
            {
                // Reset status after 5 seconds
                _ = Task.Delay(5000).ContinueWith(_ =>
                {
                    if (_trayIcon != null) _trayIcon.Text = "Azure DB Login Helper - Double-click to get token";
                });
            }
        });
    }

    private async Task<string?> GetAzureAccessTokenAsync()
    {
        try
        {
            // Try to find Azure CLI executable
            var azPath = FindAzureCliPath();
            if (string.IsNullOrEmpty(azPath))
            {
                _logger.LogError("Azure CLI not found. Please install Azure CLI and ensure it's in your PATH.");
                return null;
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = azPath,
                Arguments = "account get-access-token --resource-type oss-rdbms --query accessToken -o tsv",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var token = outputBuilder.ToString().Trim();
                return string.IsNullOrEmpty(token) ? null : token;
            }
            else
            {
                var error = errorBuilder.ToString();
                _logger.LogError("Azure CLI error: {Error}", error);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Azure CLI command");
            return null;
        }
    }

    private string? FindAzureCliPath()
    {
        // Common Azure CLI installation paths on Windows
        var possiblePaths = new[]
        {
            "az", // Try PATH first
            @"C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\az.cmd",
            @"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd",
            @"C:\Users\%USERNAME%\AppData\Local\Programs\Azure CLI\az.cmd",
            @"C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\az.exe",
            @"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.exe"
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                if (path == "az")
                {
                    // Test if 'az' is available in PATH
                    var testProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "az",
                            Arguments = "--version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    
                    testProcess.Start();
                    testProcess.WaitForExit(5000); // 5 second timeout
                    
                    if (testProcess.ExitCode == 0)
                    {
                        return "az";
                    }
                }
                else
                {
                    // Expand environment variables and check if file exists
                    var expandedPath = Environment.ExpandEnvironmentVariables(path);
                    if (File.Exists(expandedPath))
                    {
                        return expandedPath;
                    }
                }
            }
            catch
            {
                // Continue to next path
            }
        }

        return null;
    }

    private void OnTrayIconDoubleClick(object? sender, EventArgs e)
    {
        OnGetTokenClick(sender, e);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping tray icon service");
        _trayIcon?.Dispose();
        await base.StopAsync(cancellationToken);
    }

    private async Task SetClipboardTextAsync(string text)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        var thread = new Thread(() =>
        {
            try
            {
                Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
                Clipboard.SetText(text);
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        
        await tcs.Task;
    }
}
