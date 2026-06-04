using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureDbLoginHelper;

public sealed class TrayController : IDisposable
{
    private readonly TokenService _tokenService;
    private readonly AzureDbLoginOptions _options;
    private readonly ILogger<TrayController> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    private TrayIcon? _trayIcon;
    private NativeMenu? _menu;
    private readonly Dictionary<string, NativeMenuItem> _roleMenuItems = new(StringComparer.OrdinalIgnoreCase);
    private NativeMenuItem? _plainTokenMenuItem;
    private DbRole? _activeRole;

    public TrayController(
        TokenService tokenService,
        Microsoft.Extensions.Options.IOptions<AzureDbLoginOptions> options,
        ILogger<TrayController> logger,
        IHostApplicationLifetime lifetime)
    {
        _tokenService = tokenService;
        _options = options.Value;
        _logger = logger;
        _lifetime = lifetime;
        _activeRole = _tokenService.GetInitialRole();
    }

    public void Initialize()
    {
        if (Application.Current is not Application app)
            throw new InvalidOperationException("Application must be initialized before the tray icon.");

        _menu = BuildMenu();
        _trayIcon = new TrayIcon
        {
            Icon = TrayIconFactory.Create(),
            ToolTipText = TrayText(),
            Menu = _menu,
            IsVisible = true,
            // Windows: left-click runs command; right-click shows Menu. macOS: click shows Menu.
            Command = new RelayCommand(() => _ = CopyTokenAsync(forceFresh: false))
        };

        TrayIcon.SetIcons(app, new TrayIcons { _trayIcon });

        _logger.LogInformation("Tray icon ready. Mode: {Mode}", DescribeActiveMode());
    }

    private NativeMenu BuildMenu()
    {
        var menu = new NativeMenu();

        AddItem(menu, "Open PIM activation", () => _tokenService.OpenPimActivation());
        menu.Items.Add(new NativeMenuItemSeparator());

        AddItem(menu, TrayLabels.RegenerateTokenMenuItem(_options.HasRoles),
            () => _ = CopyTokenAsync(forceFresh: true));
        AddItem(menu, "Copy token (use cache)", () => _ = CopyTokenAsync(forceFresh: false));

        if (_options.HasRoles)
        {
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(new NativeMenuItem { Header = "Role", IsEnabled = false });

            _plainTokenMenuItem = new NativeMenuItem
            {
                Header = TrayLabels.PlainTokenMenuItem,
                ToggleType = NativeMenuItemToggleType.CheckBox,
                IsChecked = _activeRole == null
            };
            _plainTokenMenuItem.Click += (_, _) => SetActiveRole(null);
            menu.Items.Add(_plainTokenMenuItem);

            _roleMenuItems.Clear();
            foreach (var role in _options.Roles)
            {
                var item = new NativeMenuItem
                {
                    Header = role.DisplayName,
                    ToggleType = NativeMenuItemToggleType.CheckBox,
                    IsChecked = _activeRole != null && role.Key == _activeRole.Key
                };
                var captured = role;
                item.Click += (_, _) => SetActiveRole(captured);
                _roleMenuItems[role.Key] = item;
                menu.Items.Add(item);
            }
        }

        menu.Items.Add(new NativeMenuItemSeparator());
        AddItem(menu, "Exit", Shutdown);

        return menu;
    }

    private static void AddItem(NativeMenu menu, string header, Action action)
    {
        var item = new NativeMenuItem { Header = header };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    private string TrayText() => TrayLabels.TrayText(_activeRole);

    private void SetActiveRole(DbRole? role)
    {
        _activeRole = role;

        if (_plainTokenMenuItem != null)
            _plainTokenMenuItem.IsChecked = role == null;

        foreach (var (key, item) in _roleMenuItems)
            item.IsChecked = role != null && key == role.Key;

        if (_trayIcon != null)
            _trayIcon.ToolTipText = TrayText();

        _logger.LogInformation("Active mode: {Mode}", DescribeActiveMode());

        var message = role is { } r
            ? $"Now using: {r.DisplayName}"
            : TrayLabels.PlainTokenMenuItem;
        TrayNotifier.Show("Role selected", message);
    }

    private string DescribeActiveMode() =>
        _activeRole is { } r
            ? $"role '{r.Key}'"
            : _options.HasRoles
                ? "plain token (no role check)"
                : "plain token (no roles configured)";

    private async Task CopyTokenAsync(bool forceFresh)
    {
        if (_trayIcon != null)
            _trayIcon.ToolTipText = forceFresh ? "Getting fresh token..." : "Getting token...";

        try
        {
            var result = await _tokenService.AcquireTokenAsync(forceFresh, _activeRole);
            if (!result.Success)
            {
                TrayNotifier.Show("Token error", result.ErrorMessage ?? "Unknown error");
                return;
            }

            await TextCopy.ClipboardService.SetTextAsync(result.Token!);
            if (_activeRole is { } role)
                _logger.LogInformation("Token for role {Role} copied to clipboard", role.Key);
            else
                _logger.LogInformation("Token copied to clipboard");
            TrayNotifier.Show("Token copied", TrayLabels.TokenCopiedMessage(_activeRole));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy token");
            TrayNotifier.Show("Token error", ex.Message);
        }
        finally
        {
            if (_trayIcon != null)
                _trayIcon.ToolTipText = TrayText();
        }
    }

    private void Shutdown()
    {
        _lifetime.StopApplication();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    public void Dispose()
    {
        if (Application.Current is Application app)
            TrayIcon.SetIcons(app, null);

        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private sealed class RelayCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();
    }
}
