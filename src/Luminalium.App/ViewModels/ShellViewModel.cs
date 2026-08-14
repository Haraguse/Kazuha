using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using Luminalium.Core.Identity;
using Luminalium.Core.Localization;
using Luminalium.Plugins;
using Luminalium.Theming;
using Luminalium.Updater;
using System.Globalization;

namespace Luminalium.App.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    public const string VersionUnavailableText = "Version metadata unavailable";

    private readonly ILocalizationService _localization;
    private readonly ConfigurationService? _configurationService;
    private readonly LuminaliumConfig _config;
    private readonly IShellThemeService _themeService;
    private readonly MonetThemeService _monetThemeService;
    private readonly UpdateOrchestrator _updateOrchestrator;
    private readonly string _versionDisplay;
    private readonly bool _versionUnavailable;
    private long _accentRequestId;
    private readonly Stack<ShellPageViewModel> _backStack = new();
    private readonly Dictionary<string, PluginPageViewModel> _pluginPages;
    private bool _syncingThemeMode;
    private string _trayStatusKey = "Tray.Status.Pending";

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
    private string trayStatusText = string.Empty;

    [ObservableProperty]
    private int localizedTextVersion;

    public ShellViewModel(
        string? versionMetadataPath = null,
        IShellThemeService? themeService = null,
        MonetThemeService? monetThemeService = null,
        IDialogService? dialogService = null,
        UpdateOrchestrator? updateOrchestrator = null,
        LuminaliumConfig? config = null,
        ConfigurationService? configurationService = null,
        ILocalizationService? localizationService = null)
    {
        _config = config ?? LuminaliumConfig.CreateDefault();
        _configurationService = configurationService;
        _localization = localizationService ?? new LocalizationService();
        if (AppLanguageExtensions.TryParseCode(_config.General.Language, out var configuredLanguage))
        {
            _localization.SetLanguage(configuredLanguage);
        }

        _themeService = themeService ?? NullShellThemeService.Instance;
        _monetThemeService = monetThemeService ?? MonetThemeServiceFactory.CreateDefault();
        DialogService = dialogService ?? NullDialogService.Instance;
        _updateOrchestrator = updateOrchestrator ?? new UpdateOrchestrator();

        ProductName = ProductIdentity.DisplayName;
        (_versionDisplay, _versionUnavailable) = LoadVersionDisplayState(versionMetadataPath ?? DefaultVersionMetadataPath);
        Plugins = BuiltInPluginCatalog.CreateDefaultRegistry()
            .Enumerate()
            .Select(plugin => new PluginEntryViewModel(plugin.Metadata, _localization))
            .ToArray();

        Overview = new OverviewViewModel(ProductName, _versionDisplay, _versionUnavailable, Plugins, _localization);
        Settings = new SettingsViewModel(
            VersionDisplay,
            ShowAboutAsync,
            CheckForUpdatesAsync,
            ClearError,
            ApplyThemeMode,
            ApplyAccentOptionAsync,
            ApplyLanguage,
            _localization);
        _pluginPages = Plugins.ToDictionary(
            plugin => plugin.Id,
            plugin => new PluginPageViewModel(plugin, _localization),
            StringComparer.Ordinal);

        currentPage = Overview;
        trayStatusText = _localization[_trayStatusKey];
        _localization.LanguageChanged += OnLanguageChanged;
        GoBackCommand = new RelayCommand(GoBack, () => CanGoBack);
        NavigateToOverviewCommand = new RelayCommand(NavigateToOverview);
        NavigateToSettingsCommand = new RelayCommand(NavigateToSettings);
        NavigateToPluginCommand = new RelayCommand<PluginEntryViewModel>(NavigateToPlugin);
    }

    public string ProductName { get; }

    public string VersionDisplay => _versionUnavailable ? _localization["Shell.VersionUnavailable"] : _versionDisplay;

    public ILocalizationService Localization => _localization;

    public IReadOnlyList<PluginEntryViewModel> Plugins { get; }

    public OverviewViewModel Overview { get; }

    public SettingsViewModel Settings { get; }

    public IDialogService DialogService { get; set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

    public string OpenOverlayText => _localization["Shell.OpenOverlay.Button"];

    public string OpenOverlayHelpText => _localization["Shell.OpenOverlay.HelpText"];

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

    public void ApplyLanguage(AppLanguage language)
    {
        var result = _localization.SetLanguage(language);
        if (!result.IsSuccess)
        {
            ReportError(ShellErrorKind.Localization, result.Error ?? "Failed to apply language.");
            return;
        }

        _config.General.Language = result.Code;
        _configurationService?.Save(_config);
        Settings.SyncLanguage(language);
    }

    public void ReportError(ShellErrorKind kind, string message)
    {
        ErrorKind = kind;
        ErrorText = message;
    }

    public void ReportLocalizedError(ShellErrorKind kind, string key, string? argument = null)
    {
        var template = _localization[key];
        ReportError(kind, argument is null ? template : string.Format(CultureInfo.InvariantCulture, template, argument));
    }

    public void SetTrayStatus(string key)
    {
        _trayStatusKey = key;
        TrayStatusText = _localization[key];
    }

    public void ClearError()
    {
        ErrorKind = ShellErrorKind.None;
        ErrorText = string.Empty;
    }

    public static string LoadVersionDisplay(string path)
    {
        var (display, unavailable) = LoadVersionDisplayState(path);
        return unavailable ? VersionUnavailableText : display;
    }

    private static (string Display, bool Unavailable) LoadVersionDisplayState(string path)
    {
        var result = VersionMetadataReader.Load(path);
        return result.IsSuccess
            ? ($"{result.Metadata!.VersionName} | {result.Metadata.Build}", false)
            : (VersionUnavailableText, true);
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

    private Task<string> CheckForUpdatesAsync() =>
        DialogService.ShowUpdateAsync(this, _updateOrchestrator, ResolveInstallDirectory());

    private static string ResolveInstallDirectory() =>
        Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;

    public async Task ApplyAccentOptionAsync(AccentOptionViewModel accentOption)
    {
        var requestId = Interlocked.Increment(ref _accentRequestId);
        try
        {
            if (accentOption.Key == "system")
            {
                if (IsCurrentAccentRequest(requestId))
                {
                    _themeService.UseSystemAccent();
                }

                return;
            }

            if (accentOption.Key == "monet")
            {
                var result = await _monetThemeService.BuildPaletteAsync().ConfigureAwait(true);
                if (IsCurrentAccentRequest(requestId))
                {
                    _themeService.ApplyMonetPalette(result.Palette);
                }

                return;
            }

            if (IsCurrentAccentRequest(requestId))
            {
                _themeService.ApplyAccent(accentOption.Key);
            }
        }
        catch (Exception exception) when (IsCurrentAccentRequest(requestId))
        {
            ReportLocalizedError(ShellErrorKind.Theme, "Shell.Error.ThemeUpdateFailed", exception.Message);
        }
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs args)
    {
        TrayStatusText = _localization[_trayStatusKey];
        Settings.SyncLanguage(args.Language);
        OnPropertyChanged(nameof(VersionDisplay));
        OnPropertyChanged(nameof(OpenOverlayText));
        OnPropertyChanged(nameof(OpenOverlayHelpText));
        LocalizedTextVersion++;
    }

    private bool IsCurrentAccentRequest(long requestId) =>
        Interlocked.Read(ref _accentRequestId) == requestId;
}
