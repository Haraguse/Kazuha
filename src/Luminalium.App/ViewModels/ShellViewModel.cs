using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using Luminalium.Core.Identity;
using Luminalium.Core.Localization;
using Luminalium.Core.Security;
using Luminalium.Plugins;
using Luminalium.Theming;
using Luminalium.Updater;
using System.Globalization;
using Avalonia.Media;

namespace Luminalium.App.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    public const string VersionUnavailableText = "Version metadata unavailable";

    private static readonly Dictionary<string, string> AccentConfigValues =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["blue"] = "#0078D4",
            ["teal"] = "#038387",
            ["green"] = "#107C10",
            ["orange"] = "#CA5010",
            ["rose"] = "#E74856",
        };

    private readonly ILocalizationService _localization;
    private readonly ConfigurationService? _configurationService;
    private readonly LuminaliumConfig _config;
    private readonly IShellThemeService _themeService;
    private readonly MonetThemeService _monetThemeService;
    private readonly UpdateOrchestrator _updateOrchestrator;
    private readonly string _versionDisplay;
    private readonly bool _versionUnavailable;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IReadOnlyList<string> _knownFontFamilies;
    private bool _settingsUnlocked;
    private long _accentRequestId;
    private readonly Stack<ShellPageViewModel> _backStack = new();
    private readonly Dictionary<string, ShellPageViewModel> _pluginPages;
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
        ILocalizationService? localizationService = null,
        LocalLogService? logService = null,
        IPasswordHashService? passwordHashService = null,
        IEnumerable<string>? knownFontFamilies = null)
    {
        _config = config ?? LuminaliumConfig.CreateDefault();
        _knownFontFamilies = (knownFontFamilies ?? ResolveKnownFontFamilies()).ToArray();
        _configurationService = configurationService;
        _passwordHashService = passwordHashService ?? new PasswordHashService();
        _localization = localizationService ?? new LocalizationService();
        if (AppLanguageExtensions.TryParseCode(_config.General.Language, out var configuredLanguage))
        {
            _localization.SetLanguage(configuredLanguage);
        }

        _themeService = themeService ?? NullShellThemeService.Instance;
        _monetThemeService = monetThemeService ?? MonetThemeServiceFactory.CreateDefault();
        DialogService = dialogService ?? NullDialogService.Instance;
        _updateOrchestrator = updateOrchestrator ?? new UpdateOrchestrator();
        _settingsUnlocked = !_config.Security.PasswordProtectionEnabled;

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
            ApplyThemeModeAsync,
            ApplyAccentOptionGuardedAsync,
            ApplyLanguageAsync,
            _localization,
            EnablePasswordProtectionAsync,
            ChangePasswordAsync,
            DisablePasswordProtectionAsync,
            UnlockSettingsAsync,
            _config.Security.PasswordProtectionEnabled,
            _settingsUnlocked,
            ApplyFontFamilyAsync,
            ApplySplashModeAsync,
            ApplySplashStyleAsync,
            ApplyDetailedSplashAsync,
            ApplySplashTimeRangeAsync,
            _knownFontFamilies);
        InitializeAppearance();
        InitializeFontAndSplash();
        _pluginPages = Plugins.ToDictionary<PluginEntryViewModel, string, ShellPageViewModel>(
            plugin => plugin.Id,
            plugin => plugin.Id switch
            {
                "onboarding" => new OnboardingViewModel(_config, _configurationService, _localization),
                "logs" => new LogsViewModel(logService ?? new LocalLogService(), _localization),
                _ => new PluginPageViewModel(plugin, _localization),
            },
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

    public bool PasswordProtectionEnabled => _config.Security.PasswordProtectionEnabled;

    public bool IsSettingsUnlocked => _settingsUnlocked;

    public IAsyncRelayCommand EnablePasswordProtectionCommand => Settings.EnablePasswordProtectionCommand;

    public IAsyncRelayCommand ChangePasswordCommand => Settings.ChangePasswordCommand;

    public IAsyncRelayCommand DisablePasswordProtectionCommand => Settings.DisablePasswordProtectionCommand;

    public IAsyncRelayCommand UnlockSettingsCommand => Settings.UnlockSettingsCommand;

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

    public void ApplyThemeMode(ShellThemeMode mode) => _ = ApplyThemeModeAsync(mode);

    public async Task<bool> ApplyThemeModeAsync(ShellThemeMode mode)
    {
        if (_syncingThemeMode)
        {
            return true;
        }

        if (!await UnlockSettingsIfRequiredAsync().ConfigureAwait(true))
        {
            return false;
        }

        var previousConfigMode = _config.Appearance.ThemeMode;
        var previousMode = ToShellThemeMode(previousConfigMode);

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
        _config.Appearance.ThemeMode = ToConfigThemeMode(mode);
        if (!SaveConfiguration())
        {
            _config.Appearance.ThemeMode = previousConfigMode;
            SelectedThemeMode = previousMode;
            Settings.InitializeAppearance(previousMode, Settings.SelectedAccentOption);
            _themeService.Apply(previousMode);
            return false;
        }

        return true;
    }

    public void ApplyLanguage(AppLanguage language) => _ = ApplyLanguageAsync(language);

    public async Task<bool> ApplyLanguageAsync(AppLanguage language)
    {
        if (!await UnlockSettingsIfRequiredAsync().ConfigureAwait(true))
        {
            return false;
        }

        var previousLanguage = _localization.CurrentLanguage;
        var previousCode = _config.General.Language;
        var result = _localization.SetLanguage(language);
        if (!result.IsSuccess)
        {
            ReportError(ShellErrorKind.Localization, result.Error ?? "Failed to apply language.");
            return false;
        }

        _config.General.Language = result.Code;
        if (!SaveConfiguration())
        {
            _config.General.Language = previousCode;
            _localization.SetLanguage(previousLanguage);
            return false;
        }

        Settings.SyncLanguage(language);
        return true;
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
        await ApplyAccentOptionGuardedAsync(accentOption).ConfigureAwait(true);
    }

    private async Task<bool> ApplyAccentOptionGuardedAsync(AccentOptionViewModel accentOption)
    {
        if (!await UnlockSettingsIfRequiredAsync().ConfigureAwait(true))
        {
            return false;
        }

        return await ApplyAccentOptionAsync(accentOption, persist: true).ConfigureAwait(true);
    }

    private async Task<bool> ApplyAccentOptionAsync(AccentOptionViewModel accentOption, bool persist)
    {
        var requestId = Interlocked.Increment(ref _accentRequestId);
        try
        {
            if (accentOption.Key == "system")
            {
                if (IsCurrentAccentRequest(requestId))
                {
                    _themeService.UseSystemAccent();
                    return await PersistAccentAsync(accentOption, persist).ConfigureAwait(true);
                }

                return true;
            }

            if (accentOption.Key == "monet")
            {
                var result = await _monetThemeService.BuildPaletteAsync().ConfigureAwait(true);
                if (IsCurrentAccentRequest(requestId))
                {
                    _themeService.ApplyMonetPalette(result.Palette);
                    return await PersistAccentAsync(accentOption, persist).ConfigureAwait(true);
                }

                return true;
            }

            if (IsCurrentAccentRequest(requestId))
            {
                _themeService.ApplyAccent(accentOption.Key);
                return await PersistAccentAsync(accentOption, persist).ConfigureAwait(true);
            }

            return true;
        }
        catch (Exception exception) when (IsCurrentAccentRequest(requestId))
        {
            ReportLocalizedError(ShellErrorKind.Theme, "Shell.Error.ThemeUpdateFailed", exception.Message);
            return false;
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

    private void InitializeAppearance()
    {
        var themeMode = ToShellThemeMode(_config.Appearance.ThemeMode);
        var accentOption = ResolveAccentOption(_config.Appearance.AccentColor);
        SelectedThemeMode = themeMode;
        Settings.InitializeAppearance(themeMode, accentOption);
        _themeService.Apply(themeMode);

        if (IsKnownAccentValue(_config.Appearance.AccentColor))
        {
            _ = ApplyAccentOptionAsync(accentOption, persist: false);
        }
    }

    private void InitializeFontAndSplash()
    {
        Settings.InitializeFontAndSplash(
            _config.Appearance.FontFamily,
            _config.General.SplashMode,
            _config.General.SplashStyle,
            _config.General.ShowDetailedSplash,
            _config.General.SplashStartTime,
            _config.General.SplashEndTime);
        _themeService.ApplyFontFamily(FontFamilyResolver.Resolve(
            _config.Appearance.FontFamily,
            _knownFontFamilies));
    }

    private async Task<bool> ApplyFontFamilyAsync(string family)
    {
        if (!await UnlockSettingsIfRequiredAsync().ConfigureAwait(true))
        {
            return false;
        }

        if (!TryApplyConfiguration(
                () => _config.Appearance.FontFamily,
                value => _config.Appearance.FontFamily = value,
                family,
                value => _themeService.ApplyFontFamily(FontFamilyResolver.Resolve(value, _knownFontFamilies))))
        {
            return false;
        }

        return true;
    }

    private async Task<bool> ApplySplashModeAsync(SplashMode mode)
    {
        if (!await UnlockSettingsIfRequiredAsync().ConfigureAwait(true))
        {
            return false;
        }

        return TryApplyConfiguration(
            () => _config.General.SplashMode,
            value => _config.General.SplashMode = value,
            mode,
            _ => { });
    }

    private async Task<bool> ApplySplashStyleAsync(string style)
    {
        if (!await UnlockSettingsIfRequiredAsync().ConfigureAwait(true))
        {
            return false;
        }

        return TryApplyConfiguration(
            () => _config.General.SplashStyle,
            value => _config.General.SplashStyle = value,
            style,
            _ => { });
    }

    private async Task<bool> ApplyDetailedSplashAsync(bool showDetailed)
    {
        if (!await UnlockSettingsIfRequiredAsync().ConfigureAwait(true))
        {
            return false;
        }

        return TryApplyConfiguration(
            () => _config.General.ShowDetailedSplash,
            value => _config.General.ShowDetailedSplash = value,
            showDetailed,
            _ => { });
    }

    private async Task<bool> ApplySplashTimeRangeAsync(string startTime, string endTime)
    {
        if (!await UnlockSettingsIfRequiredAsync().ConfigureAwait(true))
        {
            return false;
        }

        if (!TimeOnly.TryParseExact(startTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
            !TimeOnly.TryParseExact(endTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return false;
        }

        var previousStart = _config.General.SplashStartTime;
        var previousEnd = _config.General.SplashEndTime;
        _config.General.SplashStartTime = startTime;
        _config.General.SplashEndTime = endTime;
        if (SaveConfiguration())
        {
            return true;
        }

        _config.General.SplashStartTime = previousStart;
        _config.General.SplashEndTime = previousEnd;
        return false;
    }

    private bool TryApplyConfiguration<T>(
        Func<T> read,
        Action<T> write,
        T value,
        Action<T> apply)
    {
        var previous = read();
        write(value);
        apply(value);
        if (SaveConfiguration())
        {
            return true;
        }

        write(previous);
        apply(previous);
        return false;
    }

    private static IReadOnlyList<string> ResolveKnownFontFamilies()
    {
        try
        {
            return FontManager.Current.SystemFonts.Select(font => font.Name).ToArray();
        }
        catch
        {
            return FontFamilyResolver.DefaultFontFamilies;
        }
    }

    private async Task<bool> PersistAccentAsync(AccentOptionViewModel accentOption, bool persist)
    {
        if (!persist || !TryGetConfigAccentValue(accentOption.Key, out var configValue))
        {
            return true;
        }

        var previousValue = _config.Appearance.AccentColor;
        _config.Appearance.AccentColor = configValue;
        if (SaveConfiguration())
        {
            return true;
        }

        _config.Appearance.AccentColor = previousValue;
        var previousOption = ResolveAccentOption(previousValue);
        await ApplyAccentOptionAsync(previousOption, persist: false).ConfigureAwait(true);
        return false;
    }

    public Task<bool> UnlockSettingsAsync() => UnlockSettingsIfRequiredAsync(forcePrompt: true);

    public async Task<bool> EnablePasswordProtectionAsync()
    {
        var result = await DialogService.ShowPasswordAsync(this, PasswordDialogMode.Enable).ConfigureAwait(true);
        if (!IsSubmittedPassword(result) || !StringComparer.Ordinal.Equals(result.Password, result.Confirmation))
        {
            if (result.Outcome == PasswordDialogOutcome.Submitted)
            {
                ReportLocalizedError(ShellErrorKind.Authorization, "Settings.Password.Mismatch");
            }

            return false;
        }

        var hash = _passwordHashService.HashPassword(result.Password);
        if (hash is null)
        {
            return false;
        }

        var previousEnabled = _config.Security.PasswordProtectionEnabled;
        var previousHash = _config.Security.PasswordHash;
        _config.Security.PasswordProtectionEnabled = true;
        _config.Security.PasswordHash = hash;
        if (!SaveConfiguration())
        {
            _config.Security.PasswordProtectionEnabled = previousEnabled;
            _config.Security.PasswordHash = previousHash;
            return false;
        }

        _settingsUnlocked = true;
        Settings.SetPasswordProtectionState(true, true);
        OnPropertyChanged(nameof(PasswordProtectionEnabled));
        OnPropertyChanged(nameof(IsSettingsUnlocked));
        return true;
    }

    public async Task<bool> ChangePasswordAsync()
    {
        if (!await UnlockSettingsIfRequiredAsync().ConfigureAwait(true))
        {
            return false;
        }

        var result = await DialogService.ShowPasswordAsync(this, PasswordDialogMode.Change).ConfigureAwait(true);
        if (!IsSubmittedPassword(result) || !StringComparer.Ordinal.Equals(result.Password, result.Confirmation))
        {
            if (result.Outcome == PasswordDialogOutcome.Submitted)
            {
                ReportLocalizedError(ShellErrorKind.Authorization, "Settings.Password.Mismatch");
            }

            return false;
        }

        var hash = _passwordHashService.HashPassword(result.Password);
        if (hash is null)
        {
            return false;
        }

        var previousHash = _config.Security.PasswordHash;
        _config.Security.PasswordHash = hash;
        if (!SaveConfiguration())
        {
            _config.Security.PasswordHash = previousHash;
            return false;
        }

        return true;
    }

    public async Task<bool> DisablePasswordProtectionAsync()
    {
        if (!await UnlockSettingsIfRequiredAsync().ConfigureAwait(true))
        {
            return false;
        }

        var previousEnabled = _config.Security.PasswordProtectionEnabled;
        var previousHash = _config.Security.PasswordHash;
        _config.Security.PasswordProtectionEnabled = false;
        _config.Security.PasswordHash = string.Empty;
        if (!SaveConfiguration())
        {
            _config.Security.PasswordProtectionEnabled = previousEnabled;
            _config.Security.PasswordHash = previousHash;
            return false;
        }

        _settingsUnlocked = true;
        Settings.SetPasswordProtectionState(false, true);
        OnPropertyChanged(nameof(PasswordProtectionEnabled));
        OnPropertyChanged(nameof(IsSettingsUnlocked));
        return true;
    }

    private async Task<bool> UnlockSettingsIfRequiredAsync(bool forcePrompt = false)
    {
        if (!forcePrompt && (!_config.Security.PasswordProtectionEnabled || _settingsUnlocked))
        {
            return true;
        }

        var result = await DialogService.ShowPasswordAsync(this, PasswordDialogMode.Unlock).ConfigureAwait(true);
        if (!IsSubmittedPassword(result) || !_passwordHashService.VerifyPassword(result.Password, _config.Security.PasswordHash))
        {
            if (result.Outcome == PasswordDialogOutcome.Submitted)
            {
                ReportLocalizedError(ShellErrorKind.Authorization, "Settings.Password.Incorrect");
            }

            return false;
        }

        _settingsUnlocked = true;
        Settings.SetPasswordProtectionState(true, true);
        OnPropertyChanged(nameof(IsSettingsUnlocked));
        ClearError();
        return true;
    }

    private bool SaveConfiguration()
    {
        var result = _configurationService?.Save(_config);
        if (result is null || result.IsSuccess)
        {
            return true;
        }

        ReportLocalizedError(ShellErrorKind.Authorization, "Settings.Password.SaveFailed");
        return false;
    }

    private static bool IsSubmittedPassword(PasswordDialogResult result) =>
        result.Outcome == PasswordDialogOutcome.Submitted && !string.IsNullOrEmpty(result.Password);

    private AccentOptionViewModel ResolveAccentOption(string configuredValue)
    {
        var configuredKey = TryGetAccentKey(configuredValue);
        return Settings.AccentOptions.FirstOrDefault(
                   option => StringComparer.Ordinal.Equals(option.Key, configuredKey))
               ?? Settings.AccentOptions[0];
    }

    private static string? TryGetAccentKey(string configuredValue)
    {
        if (configuredValue is "system" or "monet")
        {
            return configuredValue;
        }

        return AccentConfigValues.FirstOrDefault(
            pair => StringComparer.OrdinalIgnoreCase.Equals(pair.Value, configuredValue)).Key;
    }

    private static bool IsKnownAccentValue(string configuredValue) =>
        TryGetAccentKey(configuredValue) is not null;

    private static bool TryGetConfigAccentValue(string accentKey, out string configValue)
    {
        if (accentKey is "system" or "monet")
        {
            configValue = accentKey;
            return true;
        }

        return AccentConfigValues.TryGetValue(accentKey, out configValue!);
    }

    private static ShellThemeMode ToShellThemeMode(ThemeMode mode) =>
        mode switch
        {
            ThemeMode.Dark => ShellThemeMode.Dark,
            ThemeMode.Auto => ShellThemeMode.System,
            _ => ShellThemeMode.Light,
        };

    private static ThemeMode ToConfigThemeMode(ShellThemeMode mode) =>
        mode switch
        {
            ShellThemeMode.Dark => ThemeMode.Dark,
            ShellThemeMode.System => ThemeMode.Auto,
            _ => ThemeMode.Light,
        };
}
