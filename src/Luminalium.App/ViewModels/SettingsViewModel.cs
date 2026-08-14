using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.Core.Identity;

namespace Luminalium.App.ViewModels;

public sealed partial class SettingsViewModel : ShellPageViewModel
{
    private readonly Action<ShellThemeMode> _themeChanged;
    private readonly Action<AccentOptionViewModel> _accentChanged;
    private readonly Func<Task<string>> _checkForUpdatesAsync;

    [ObservableProperty]
    private ShellThemeMode selectedThemeMode;

    [ObservableProperty]
    private AccentOptionViewModel selectedAccentOption;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool isCheckingForUpdates;

    [ObservableProperty]
    private string updateStatusText = "Ready to check the Luminalium Windows release feed.";

    public SettingsViewModel(
        string versionDisplay,
        Func<Task> showAboutAsync,
        Func<Task<string>> checkForUpdatesAsync,
        Action clearError,
        Action<ShellThemeMode> themeChanged,
        Action<AccentOptionViewModel> accentChanged) : base("settings", "Settings")
    {
        VersionDisplay = versionDisplay;
        _checkForUpdatesAsync = checkForUpdatesAsync;
        _themeChanged = themeChanged;
        _accentChanged = accentChanged;
        ShowAboutCommand = new AsyncRelayCommand(showAboutAsync);
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync, () => !IsCheckingForUpdates);
        ClearErrorCommand = new RelayCommand(clearError);
        selectedAccentOption = AccentOptions[0];
    }

    public string ProductName { get; } = ProductIdentity.DisplayName;

    public string AppUserModelId { get; } = ProductIdentity.WindowsAppUserModelId;

    public string VersionDisplay { get; }

    public IReadOnlyList<ShellThemeMode> ThemeModes { get; } =
    [
        ShellThemeMode.System,
        ShellThemeMode.Light,
        ShellThemeMode.Dark,
    ];

    public IReadOnlyList<AccentOptionViewModel> AccentOptions { get; } =
    [
        new("system", "System accent"),
        new("blue", "Presentation blue"),
        new("teal", "Board teal"),
        new("green", "Status green"),
        new("orange", "Timer orange"),
        new("rose", "Alert rose"),
    ];

    public IAsyncRelayCommand ShowAboutCommand { get; }

    public IAsyncRelayCommand CheckForUpdatesCommand { get; }

    public IRelayCommand ClearErrorCommand { get; }

    private async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdates = true;
        UpdateStatusText = "Checking for updates...";

        try
        {
            UpdateStatusText = await _checkForUpdatesAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            UpdateStatusText = $"Update check failed: {exception.Message}";
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    partial void OnSelectedThemeModeChanged(ShellThemeMode value) => _themeChanged(value);

    partial void OnSelectedAccentOptionChanged(AccentOptionViewModel value) => _accentChanged(value);
}
