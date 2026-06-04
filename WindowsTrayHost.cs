#if USE_WINFORMS_TRAY
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TextCopy;

namespace AzureDbLoginHelper;

/// <summary>WinForms system tray — reliable right-click menu on Windows (Avalonia NativeMenu does not).</summary>
public sealed class WindowsTrayHost
{
    private readonly TokenService _tokenService;
    private readonly AzureDbLoginOptions _options;
    private readonly ILogger<WindowsTrayHost> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    private TrayApplicationContext? _context;
    private TaskCompletionSource? _loopEnded;

    public WindowsTrayHost(
        TokenService tokenService,
        IOptions<AzureDbLoginOptions> options,
        ILogger<WindowsTrayHost> logger,
        IHostApplicationLifetime lifetime)
    {
        _tokenService = tokenService;
        _options = options.Value;
        _logger = logger;
        _lifetime = lifetime;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _loopEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var uiThread = new Thread(RunUiMessageLoop)
        {
            IsBackground = false,
            Name = "TrayIconUI"
        };
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Start();

        using var stopRegistration = stoppingToken.Register(() => _context?.ExitThread());

        try
        {
            await _loopEnded.Task.WaitAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (!stoppingToken.IsCancellationRequested)
                _lifetime.StopApplication();
        }
    }

    private void RunUiMessageLoop()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            _context = new TrayApplicationContext(
                _tokenService, _options, _logger, _lifetime.StopApplication);
            Application.Run(_context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tray UI thread failed");
        }
        finally
        {
            _loopEnded?.TrySetResult();
        }
    }

    private sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly TokenService _tokenService;
        private readonly AzureDbLoginOptions _options;
        private readonly ILogger _logger;
        private readonly Action _exitApplication;

        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _menu;
        private readonly Icon _appIcon;
        private readonly Dictionary<string, ToolStripMenuItem> _roleMenuItems = new(StringComparer.OrdinalIgnoreCase);
        private ToolStripMenuItem? _plainTokenMenuItem;
        private DbRole? _activeRole;

        public TrayApplicationContext(
            TokenService tokenService,
            AzureDbLoginOptions options,
            ILogger logger,
            Action exitApplication)
        {
            _tokenService = tokenService;
            _options = options;
            _logger = logger;
            _exitApplication = exitApplication;
            _activeRole = tokenService.GetInitialRole();

            _appIcon = CreateTrayIcon();
            _menu = BuildMenu();

            _trayIcon = new NotifyIcon
            {
                Icon = _appIcon,
                Text = TrayText(),
                Visible = true,
                ContextMenuStrip = _menu
            };
            _trayIcon.DoubleClick += (_, _) => _ = CopyTokenAsync(forceFresh: false);

            _logger.LogInformation("Tray icon ready. Mode: {Mode}", DescribeActiveMode());
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add("Open PIM activation", null, (_, _) => _tokenService.OpenPimActivation());
            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(TrayLabels.RegenerateTokenMenuItem(_options.HasRoles), null,
                (_, _) => _ = CopyTokenAsync(forceFresh: true));
            menu.Items.Add("Copy token (use cache)", null,
                (_, _) => _ = CopyTokenAsync(forceFresh: false));

            if (_options.HasRoles)
            {
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(new ToolStripMenuItem("Role") { Enabled = false });

                _plainTokenMenuItem = new ToolStripMenuItem(TrayLabels.PlainTokenMenuItem)
                {
                    Checked = _activeRole == null,
                    CheckOnClick = false
                };
                _plainTokenMenuItem.Click += (_, _) => SetActiveRole(null);
                menu.Items.Add(_plainTokenMenuItem);

                _roleMenuItems.Clear();
                foreach (var role in _options.Roles)
                {
                    var item = new ToolStripMenuItem(role.DisplayName)
                    {
                        Tag = role,
                        Checked = _activeRole != null && role.Key == _activeRole.Key,
                        CheckOnClick = false
                    };
                    var captured = role;
                    item.Click += (_, _) => SetActiveRole(captured);
                    _roleMenuItems[role.Key] = item;
                    menu.Items.Add(item);
                }
            }

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) =>
            {
                ExitThread();
                _exitApplication();
            });

            return menu;
        }

        private string TrayText() => TrayLabels.TrayText(_activeRole);

        private void SetActiveRole(DbRole? role)
        {
            _activeRole = role;

            if (_plainTokenMenuItem != null)
                _plainTokenMenuItem.Checked = role == null;

            foreach (var (key, item) in _roleMenuItems)
                item.Checked = role != null && key == role.Key;

            _trayIcon.Text = TrayText();
            _logger.LogInformation("Active mode: {Mode}", DescribeActiveMode());

            var tip = role is { } r
                ? $"Now using: {r.DisplayName}"
                : TrayLabels.PlainTokenMenuItem;
            _trayIcon.ShowBalloonTip(2000, "Role selected", tip, ToolTipIcon.Info);
        }

        private string DescribeActiveMode() =>
            _activeRole is { } r
                ? $"role '{r.Key}'"
                : _options.HasRoles
                    ? "plain token (no role check)"
                    : "plain token (no roles configured)";

        private async Task CopyTokenAsync(bool forceFresh)
        {
            _trayIcon.Text = forceFresh ? "Getting fresh token..." : "Getting token...";

            try
            {
                var result = await _tokenService.AcquireTokenAsync(forceFresh, _activeRole);
                if (!result.Success)
                {
                    _trayIcon.ShowBalloonTip(7000, "Token error",
                        result.ErrorMessage ?? "Unknown error", ToolTipIcon.Warning);
                    return;
                }

                await ClipboardService.SetTextAsync(result.Token!);
                if (_activeRole is { } role)
                    _logger.LogInformation("Token for role {Role} copied to clipboard", role.Key);
                else
                    _logger.LogInformation("Token copied to clipboard");
                _trayIcon.ShowBalloonTip(3000, "Token copied",
                    TrayLabels.TokenCopiedMessage(_activeRole), ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy token");
                _trayIcon.ShowBalloonTip(5000, "Token error", ex.Message, ToolTipIcon.Error);
            }
            finally
            {
                _trayIcon.Text = TrayText();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIcon.Dispose();
                _menu.Dispose();
                _appIcon.Dispose();
            }

            base.Dispose(disposing);
        }

        private static Icon CreateTrayIcon()
        {
            using var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using var azure = new SolidBrush(Color.FromArgb(0, 120, 212));
                g.FillEllipse(azure, 1, 1, 30, 30);

                using var white = new SolidBrush(Color.White);
                g.FillRectangle(white, 9, 13, 14, 11);
                g.FillRectangle(white, 11, 8, 10, 6);
                g.FillEllipse(white, 13, 6, 6, 4);
            }

            var handle = bitmap.GetHicon();
            try
            {
                using var fromHandle = Icon.FromHandle(handle);
                return (Icon)fromHandle.Clone();
            }
            finally
            {
                DestroyIcon(handle);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);
    }
}
#endif
