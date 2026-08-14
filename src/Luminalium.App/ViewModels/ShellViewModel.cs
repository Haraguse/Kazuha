using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Services;
using Luminalium.Core.Identity;
using Luminalium.Plugins;

namespace Luminalium.App.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    public const string VersionUnavailableText = "Version metadata unavailable";

    private readonly IShellThemeService _themeService;
    private readonly Stack<ShellPageViewModel> _backStack = new();
    private readonly Dictionary<string, PluginPageViewModel> _pluginPages;
    private bool _syncingThemeMode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    private bool canGoBack;

    [ObservableProperty]
    private ShellPageViewModel currentPage;

    [ObservableProperty]
    private ShellThemeMode selectedThemeMode;

    [ObservableProperty]
    private ShellErrorKind errorKind;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorText = string.Empty;

    [ObservableProperty]
    private string trayStatusText = "Tray icon pending";

    public ShellViewModel(
        string? versionMetadataPath = null,
        IShellThemeService? themeService = null,
        IDialogService? dialogService = null)
    {
        _themeService = themeService ?? NullShellThemeService.Instance;
        DialogService = dialogService ?? NullDialogService.Instance;

        ProductName = ProductIdentity.DisplayName;
        VersionDisplay = LoadVersionDisplay(versionMetadataPath ?? DefaultVersionMetadataPath);
        Plugins = BuiltInPluginCatalog.CreateDefaultRegistry()
            .Enumerate()
            .Select(plugin => new PluginEntryViewModel(plugin.Metadata))
            .ToArray();

        Overview = new OverviewViewModel(ProductName, VersionDisplay, Plugins);
        Settings = new SettingsViewModel(
            VersionDisplay,
            ShowAboutAsync,
            ClearError,
            ApplyThemeMode,
            ApplyAccentOption);
        _pluginPages = Plugins.ToDictionary(
            plugin => plugin.Id,
            plugin => new PluginPageViewModel(plugin),
            StringComparer.Ordinal);

        currentPage = Overview;
        GoBackCommand = new RelayCommand(GoBack, () => CanGoBack);
        NavigateToOverviewCommand = new RelayCommand(NavigateToOverview);
        NavigateToSettingsCommand = new RelayCommand(NavigateToSettings);
        NavigateToPluginCommand = new RelayCommand<PluginEntryViewModel>(NavigateToPlugin);
    }

    public string ProductName { get; }

    public string VersionDisplay { get; }

    public IReadOnlyList<PluginEntryViewModel> Plugins { get; }

    public OverviewViewModel Overview { get; }

    public SettingsViewModel Settings { get; }

    public IDialogService DialogService { get; set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

    public IRelayCommand GoBackCommand { get; }

    public IRelayCommand NavigateToOverviewCommand { get; }

    public IRelayCommand NavigateToSettingsCommand { get; }

    public IRelayCommand<PluginEntryViewModel> NavigateToPluginCommand { get; }

    private static string DefaultVersionMetadataPath =>
        Path.Combine(AppContext.BaseDirectory, ProductIdentity.VersionMetadataFileName);

    public void NavigateToOverview() => NavigateTo(Overview);

    public void NavigateToSettings() => NavigateTo(Settings);

    public void NavigateToPlugin(PluginEntryViewModel? plugin)
    {
        if (plugin is not null && _pluginPages.TryGetValue(plugin.Id, out var page))
        {
            NavigateTo(page);
        }
    }

    public void GoBack()
    {
        if (_backStack.Count == 0)
        {
            return;
        }

        SetCurrentPage(_backStack.Pop());
        UpdateBackState();
    }

    public void ApplyThemeMode(ShellThemeMode mode)
    {
        if (_syncingThemeMode)
        {
            return;
        }

        if (SelectedThemeMode != mode)
        {
            SelectedThemeMode = mode;
        }

        _syncingThemeMode = true;
        try
        {
            if (Settings.SelectedThemeMode != mode)
            {
                Settings.SelectedThemeMode = mode;
            }
        }
        finally
        {
            _syncingThemeMode = false;
        }

        _themeService.Apply(mode);
    }

    public void ReportError(ShellErrorKind kind, string message)
    {
        ErrorKind = kind;
        ErrorText = message;
    }

    public void ClearError()
    {
        ErrorKind = ShellErrorKind.None;
        ErrorText = string.Empty;
    }

    public static string LoadVersionDisplay(string path)
    {
        var result = VersionMetadataReader.Load(path);
        return result.IsSuccess
            ? $"{result.Metadata!.VersionName} | {result.Metadata.Build}"
            : VersionUnavailableText;
    }

    private void NavigateTo(ShellPageViewModel page)
    {
        if (StringComparer.Ordinal.Equals(CurrentPage.NavigationKey, page.NavigationKey))
        {
            return;
        }

        _backStack.Push(CurrentPage);
        SetCurrentPage(page);
        UpdateBackState();
    }

    private void SetCurrentPage(ShellPageViewModel page)
    {
        CurrentPage = page;
        ClearError();
    }

    private void UpdateBackState() => CanGoBack = _backStack.Count > 0;

    private Task ShowAboutAsync() => DialogService.ShowAboutAsync(this);

    private void ApplyAccentOption(AccentOptionViewModel accentOption)
    {
        if (accentOption.Key == "system")
        {
            _themeService.UseSystemAccent();
            return;
        }

        _themeService.ApplyAccent(accentOption.Key);
    }
}
