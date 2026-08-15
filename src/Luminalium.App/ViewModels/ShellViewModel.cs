using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using Luminalium.Core.Identity;
using Luminalium.Core.Localization;
using Luminalium.Core.Security;
using Luminalium.App.Features;
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
    private readonly LocalLogService _logService;
    private readonly IReadOnlyList<string> _knownFontFamilies;
    private bool _settingsUnlocked;
    private readonly AsyncSerialGate _settingsOperationGate = new();
    private readonly BuiltInFeatureLegacyProjection _legacyProjection = new();
    private long _accentRequestId;
    private readonly Stack<ShellPageViewModel> _backStack = new();
    private readonly Dictionary<BuiltInFeatureId, ShellPageViewModel> _nativePages;
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
        var localLogService = logService ?? new LocalLogService();
        _logService = localLogService;
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
        BuiltInFeatures = BuiltInFeatureCatalog.Default.Descriptors
            .Select(descriptor => new BuiltInFeatureEntryViewModel(descriptor, _localization))
            .ToArray();

        Overview = new OverviewViewModel(ProductName, _versionDisplay, _versionUnavailable, BuiltInFeatures, _localization);

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
        _nativePages = new Dictionary<BuiltInFeatureId, ShellPageViewModel>
        {
            [BuiltInFeatureId.Onboarding] = new OnboardingViewModel(_config, _configurationService, _localization),
            [BuiltInFeatureId.Logs] = new LogsViewModel(localLogService, _localization),
        };

        currentPage = _config.General.OnboardingCompleted
            ? Overview
            : _nativePages[BuiltInFeatureId.Onboarding];
        trayStatusText = _localization[_trayStatusKey];
        _localization.LanguageChanged += OnLanguageChanged;
        GoBackCommand = new RelayCommand(GoBack, () => CanGoBack);
        NavigateToOverviewCommand = new RelayCommand(NavigateToOverview);
        NavigateToSettingsCommand = new RelayCommand(NavigateToSettings);
        NavigateToPluginCommand = new RelayCommand<BuiltInFeatureEntryViewModel>(feature => NavigateToFeature(feature?.RouteKey));
    }

    public string ProductName { get; }

    public string VersionDisplay => _versionUnavailable ? _localization["Shell.VersionUnavailable"] : _versionDisplay;

    public ILocalizationService Localization => _localization;

    public IReadOnlyList<BuiltInFeatureEntryViewModel> BuiltInFeatures { get; }

    [Obsolete("Use BuiltInFeatures; retained for one compatibility release.")]
    public IReadOnlyList<BuiltInFeatureEntryViewModel> Plugins => LegacyPlugins;

    [Obsolete("Use BuiltInFeatures; retained for one compatibility release.")]
    public IReadOnlyList<BuiltInFeatureEntryViewModel> LegacyPlugins => _legacyProjection.Entries(_localization);

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

    public IRelayCommand<BuiltInFeatureEntryViewModel> NavigateToPluginCommand { get; }

    private static string DefaultVersionMetadataPath =>
        Path.Combine(AppContext.BaseDirectory, ProductIdentity.VersionMetadataFileName);

    public void NavigateToOverview() => NavigateTo(Overview);

    public void NavigateToSettings() => NavigateTo(Settings);

    public BuiltInFeatureActivationResult NavigateToFeature(string? route)
    {
        var result = new BuiltInFeatureRouteParser().Parse(route);
        if (!result.IsSuccess)
        {
            return result;
        }

        if (result.FeatureId == BuiltInFeatureId.Settings)
        {
            NavigateTo(Settings);
            return result;
        }

        if (_nativePages.TryGetValue(result.FeatureId!.Value, out var page))
        {
            NavigateTo(page);
            return result;
        }

        return BuiltInFeatureActivationResult.Failure(BuiltInFeatureActivationErrorCode.NotFound, route, result.FeatureId, result.CorrelationId);
    }

    [Obsolete("Use NavigateToFeature; retained for one compatibility release.")]
    public void NavigateToPlugin(BuiltInFeatureEntryViewModel? feature) => NavigateToFeature(feature?.RouteKey);

    [Obsolete("Use NavigateToFeature; retained for one compatibility release.")]
    public void NavigateToPlugin(ExtensionEntryViewModel? plugin) => NavigateToFeature(plugin?.Id);

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

    public Task<bool> ApplyThemeModeAsync(ShellThemeMode mode) =>
        RunSerializedSettingsOperationAsync(() => ApplyThemeModeCoreAsync(mode));

    private async Task<bool> ApplyThemeModeCoreAsync(ShellThemeMode mode)
    {
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

        Settings.SyncThemeMode(mode);

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

    public Task<bool> ApplyLanguageAsync(AppLanguage language) =>
        RunSerializedSettingsOperationAsync(() => ApplyLanguageCoreAsync(language));

    private async Task<bool> ApplyLanguageCoreAsync(AppLanguage language)
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
        _logService.Log(LogSeverity.Error, message, kind.ToString());
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
        var requestId = Interlocked.Increment(ref _accentRequestId);
        MonetPalette? palette = null;
        if (accentOption.Key == "monet")
        {
            var result = await _monetThemeService.BuildPaletteAsync().ConfigureAwait(true);
            palette = result.Palette;
        }

        return await RunSerializedSettingsOperationAsync(async () =>
        {
            if (!await UnlockSettingsIfRequiredAsync().ConfigureAwait(true))
            {
                return false;
            }

            return await ApplyAccentOptionCoreAsync(accentOption, persist: true, requestId, palette).ConfigureAwait(true);
        }).ConfigureAwait(true);
    }

    private async Task<bool> ApplyAccentOptionCoreAsync(
        AccentOptionViewModel accentOption,
        bool persist,
        long? requestIdOverride = null,
        MonetPalette? palette = null)
    {
        var requestId = requestIdOverride ?? Interlocked.Increment(ref _accentRequestId);
        try
        {
            if (accentOption.Key == "system")
            {
                if (IsCurrentAccentRequest(requestId))
                {
                    _themeService.UseSystemAccent();
                    return await PersistAccentCoreAsync(accentOption, persist).ConfigureAwait(true);
                }

                return true;
            }

            if (accentOption.Key == "monet")
            {
                palette ??= (await _monetThemeService.BuildPaletteAsync().ConfigureAwait(true)).Palette;
                if (IsCurrentAccentRequest(requestId))
                {
                    _themeService.ApplyMonetPalette(palette!);
                    return await PersistAccentCoreAsync(accentOption, persist).ConfigureAwait(true);
                }

                return true;
            }

            if (IsCurrentAccentRequest(requestId))
            {
                _themeService.ApplyAccent(accentOption.Key);
                return await PersistAccentCoreAsync(accentOption, persist).ConfigureAwait(true);
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
            _ = ApplyAccentOptionCoreAsync(accentOption, persist: false);
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

    private Task<bool> ApplyFontFamilyAsync(string family) =>
        RunSerializedSettingsOperationAsync(() => ApplyFontFamilyCoreAsync(family));

    private async Task<bool> ApplyFontFamilyCoreAsync(string family)
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

    private Task<bool> ApplySplashModeAsync(SplashMode mode) =>
        RunSerializedSettingsOperationAsync(() => ApplySplashModeCoreAsync(mode));

    private async Task<bool> ApplySplashModeCoreAsync(SplashMode mode)
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

    private Task<bool> ApplySplashStyleAsync(string style) =>
        RunSerializedSettingsOperationAsync(() => ApplySplashStyleCoreAsync(style));

    private async Task<bool> ApplySplashStyleCoreAsync(string style)
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

    private Task<bool> ApplyDetailedSplashAsync(bool showDetailed) =>
        RunSerializedSettingsOperationAsync(() => ApplyDetailedSplashCoreAsync(showDetailed));

    private async Task<bool> ApplyDetailedSplashCoreAsync(bool showDetailed)
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

    private Task<bool> ApplySplashTimeRangeAsync(string startTime, string endTime) =>
        RunSerializedSettingsOperationAsync(() => ApplySplashTimeRangeCoreAsync(startTime, endTime));

    private async Task<bool> ApplySplashTimeRangeCoreAsync(string startTime, string endTime)
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

    private async Task<bool> PersistAccentCoreAsync(AccentOptionViewModel accentOption, bool persist)
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
        await ApplyAccentOptionCoreAsync(previousOption, persist: false).ConfigureAwait(true);
        return false;
    }

    public Task<bool> UnlockSettingsAsync() =>
        RunSerializedSettingsOperationAsync(() => UnlockSettingsIfRequiredAsync(forcePrompt: true));

    public Task<bool> EnablePasswordProtectionAsync() =>
        RunSerializedSettingsOperationAsync(EnablePasswordProtectionCoreAsync);

    private async Task<bool> EnablePasswordProtectionCoreAsync()
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

    public Task<bool> ChangePasswordAsync() =>
        RunSerializedSettingsOperationAsync(ChangePasswordCoreAsync);

    private async Task<bool> ChangePasswordCoreAsync()
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

    public Task<bool> DisablePasswordProtectionAsync() =>
        RunSerializedSettingsOperationAsync(DisablePasswordProtectionCoreAsync);

    private async Task<bool> DisablePasswordProtectionCoreAsync()
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

    private async Task<bool> RunSerializedSettingsOperationAsync(Func<Task<bool>> operation)
    {
        return await _settingsOperationGate.RunAsync(operation).ConfigureAwait(true);
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
