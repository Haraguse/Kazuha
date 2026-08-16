using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Services;
using Luminalium.Core.Localization;
using Luminalium.Core.Identity;
using Luminalium.Core.Configuration;
using System.Globalization;
using System.Diagnostics;

namespace Luminalium.App.ViewModels;

public sealed partial class SettingsViewModel : ShellPageViewModel
{
    private readonly ILocalizationService _localization;
    private readonly Func<ShellThemeMode, Task<bool>> _themeChanged;
    private readonly Func<AppLanguage, Task<bool>> _languageChanged;
    private readonly Func<AccentOptionViewModel, Task<bool>> _accentChanged;
    private readonly Func<string, Task<bool>> _fontFamilyChanged;
    private readonly Func<SplashMode, Task<bool>> _splashModeChanged;
    private readonly Func<string, Task<bool>> _splashStyleChanged;
    private readonly Func<bool, Task<bool>> _detailedSplashChanged;
    private readonly Func<string, string, Task<bool>> _splashTimeRangeChanged;
    private readonly Func<Task<string>> _checkForUpdatesAsync;
    private readonly Func<Task<bool>> _enablePasswordProtection;
    private readonly Func<Task<bool>> _changePassword;
    private readonly Func<Task<bool>> _disablePasswordProtection;
    private readonly Func<Task<bool>> _unlockSettings;
    private string? _updateStatusKey = "Settings.Updates.Ready";
    private string? _updateStatusArgument;
    private bool _syncingThemeMode;
    private bool _syncingLanguage;
    private bool _initializingAppearance;
    private ShellThemeMode _lastAcceptedThemeMode;
    private AccentOptionViewModel? _lastAcceptedAccentOption;
    private LanguageOptionViewModel? _lastAcceptedLanguageOption;
    private FontFamilyOptionViewModel? _lastAcceptedFontFamilyOption;
    private SplashModeOptionViewModel? _lastAcceptedSplashModeOption;
    private SplashStyleOptionViewModel? _lastAcceptedSplashStyleOption;
    private bool _lastAcceptedShowDetailedSplash;
    private string _lastAcceptedSplashStartTime = "08:00";
    private string _lastAcceptedSplashEndTime = "20:00";

    [ObservableProperty]
    private ShellThemeMode selectedThemeMode;

    [ObservableProperty]
    private ThemeModeOptionViewModel selectedThemeModeOption;

    [ObservableProperty]
    private AccentOptionViewModel selectedAccentOption;

    [ObservableProperty]
    private LanguageOptionViewModel selectedLanguageOption;

    [ObservableProperty]
    private FontFamilyOptionViewModel selectedFontFamilyOption;

    [ObservableProperty]
    private SplashModeOptionViewModel selectedSplashModeOption;

    [ObservableProperty]
    private SplashStyleOptionViewModel selectedSplashStyleOption;

    [ObservableProperty]
    private bool showDetailedSplash;

    [ObservableProperty]
    private string splashStartTime = "08:00";

    [ObservableProperty]
    private string splashEndTime = "20:00";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool isCheckingForUpdates;

    [ObservableProperty]
    private string updateStatusText = "Ready to check the Luminalium Windows release feed.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EnablePasswordProtectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisablePasswordProtectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnlockSettingsCommand))]
    private bool passwordProtectionEnabled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisablePasswordProtectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnlockSettingsCommand))]
    private bool settingsUnlocked;

    public SettingsViewModel(
        string versionDisplay,
        Func<Task> showAboutAsync,
        Func<Task<string>> checkForUpdatesAsync,
        Action clearError,
         Func<ShellThemeMode, Task<bool>> themeChanged,
         Func<AccentOptionViewModel, Task<bool>> accentChanged,
         Func<AppLanguage, Task<bool>> languageChanged,
         ILocalizationService localization,
         Func<Task<bool>> enablePasswordProtection,
         Func<Task<bool>> changePassword,
          Func<Task<bool>> disablePasswordProtection,
          Func<Task<bool>> unlockSettings,
          bool passwordProtectionEnabled,
          bool settingsUnlocked,
          Func<string, Task<bool>>? fontFamilyChanged = null,
          Func<SplashMode, Task<bool>>? splashModeChanged = null,
          Func<string, Task<bool>>? splashStyleChanged = null,
          Func<bool, Task<bool>>? detailedSplashChanged = null,
          Func<string, string, Task<bool>>? splashTimeRangeChanged = null,
          IEnumerable<string>? fontFamilies = null) : base("settings", "Navigation.Settings", localization)
    {
        _localization = localization;
        VersionDisplay = versionDisplay;
        _checkForUpdatesAsync = checkForUpdatesAsync;
        _themeChanged = themeChanged;
        _accentChanged = accentChanged;
        _languageChanged = languageChanged;
        _fontFamilyChanged = fontFamilyChanged ?? (_ => Task.FromResult(true));
        _splashModeChanged = splashModeChanged ?? (_ => Task.FromResult(true));
        _splashStyleChanged = splashStyleChanged ?? (_ => Task.FromResult(true));
        _detailedSplashChanged = detailedSplashChanged ?? (_ => Task.FromResult(true));
        _splashTimeRangeChanged = splashTimeRangeChanged ?? ((_, _) => Task.FromResult(true));
        _enablePasswordProtection = enablePasswordProtection;
        _changePassword = changePassword;
        _disablePasswordProtection = disablePasswordProtection;
        _unlockSettings = unlockSettings;
        this.passwordProtectionEnabled = passwordProtectionEnabled;
        this.settingsUnlocked = settingsUnlocked;
        ShowAboutCommand = new AsyncRelayCommand(showAboutAsync);
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync, () => !IsCheckingForUpdates);
        ClearErrorCommand = new RelayCommand(clearError);
        EnablePasswordProtectionCommand = new AsyncRelayCommand(EnablePasswordProtectionAsync, () => !PasswordProtectionEnabled);
        ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync, () => PasswordProtectionEnabled);
        DisablePasswordProtectionCommand = new AsyncRelayCommand(DisablePasswordProtectionAsync, () => PasswordProtectionEnabled);
        UnlockSettingsCommand = new AsyncRelayCommand(UnlockSettingsAsync, () => PasswordProtectionEnabled && !SettingsUnlocked);

        ThemeModeOptions =
        [
            new(ShellThemeMode.System, "Settings.Theme.System", localization),
            new(ShellThemeMode.Light, "Settings.Theme.Light", localization),
            new(ShellThemeMode.Dark, "Settings.Theme.Dark", localization),
        ];
        AccentOptions =
        [
            new("system", "Settings.Accent.System", localization),
            new("monet", "Settings.Accent.Monet", localization),
            new("blue", "Settings.Accent.Blue", localization),
            new("teal", "Settings.Accent.Teal", localization),
            new("green", "Settings.Accent.Green", localization),
            new("orange", "Settings.Accent.Orange", localization),
            new("rose", "Settings.Accent.Rose", localization),
        ];
        LanguageOptions =
        [
            new(AppLanguage.ZhCn, "Settings.Language.ZhCn", localization),
            new(AppLanguage.ZhTw, "Settings.Language.ZhTw", localization),
            new(AppLanguage.YueHk, "Settings.Language.YueHk", localization),
            new(AppLanguage.JaJp, "Settings.Language.JaJp", localization),
            new(AppLanguage.EnUs, "Settings.Language.EnUs", localization),
            new(AppLanguage.UgCn, "Settings.Language.UgCn", localization),
        ];
        FontFamilyOptions = (fontFamilies ?? FontFamilyResolver.DefaultFontFamilies)
            .Where(family => !string.IsNullOrWhiteSpace(family))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .DefaultIfEmpty(FontFamilyResolver.DefaultFontFamilies[0])
            .Select(family => new FontFamilyOptionViewModel(family, localization))
            .ToArray();
        SplashModeOptions =
        [
            new(SplashMode.Always, "Settings.SplashMode.Always", localization),
            new(SplashMode.Never, "Settings.SplashMode.Never", localization),
            new(SplashMode.HideOnAutoStart, "Settings.SplashMode.HideOnAutoStart", localization),
            new(SplashMode.TimeRange, "Settings.SplashMode.TimeRange", localization),
        ];
        SplashStyleOptions =
        [
            new(SplashPolicyService.DefaultStyle, "Settings.SplashStyle.Default", localization),
            new("nina_iseri_1_2", "Settings.SplashStyle.NinaIseri", localization),
        ];

        selectedThemeModeOption = ThemeModeOptions[0];
        selectedAccentOption = AccentOptions[0];
        selectedLanguageOption = LanguageOptions.First(option => option.Language == localization.CurrentLanguage);
        _lastAcceptedThemeMode = selectedThemeMode;
        _lastAcceptedAccentOption = selectedAccentOption;
        _lastAcceptedLanguageOption = selectedLanguageOption;
        selectedFontFamilyOption = FontFamilyOptions[0];
        selectedSplashModeOption = SplashModeOptions[0];
        selectedSplashStyleOption = SplashStyleOptions[0];
        _lastAcceptedFontFamilyOption = selectedFontFamilyOption;
        _lastAcceptedSplashModeOption = selectedSplashModeOption;
        _lastAcceptedSplashStyleOption = selectedSplashStyleOption;
        updateStatusText = BuildUpdateStatusText();
    }

    public string ProductName { get; } = ProductIdentity.DisplayName;

    public string AppUserModelId { get; } = ProductIdentity.WindowsAppUserModelId;

    public string VersionDisplay { get; }

    public IReadOnlyList<ThemeModeOptionViewModel> ThemeModeOptions { get; }

    public IReadOnlyList<AccentOptionViewModel> AccentOptions { get; }

    public IReadOnlyList<LanguageOptionViewModel> LanguageOptions { get; }

    public IReadOnlyList<FontFamilyOptionViewModel> FontFamilyOptions { get; }

    public IReadOnlyList<SplashModeOptionViewModel> SplashModeOptions { get; }

    public IReadOnlyList<SplashStyleOptionViewModel> SplashStyleOptions { get; }

    public string ThemeModeLabel => Localization["Settings.ThemeMode.Label"];

    public string ThemeModeDescription => Localization["Settings.ThemeMode.Description"];

    public string ThemeModeHelpText => Localization["Settings.ThemeMode.HelpText"];

    public string AccentColorLabel => Localization["Settings.AccentColor.Label"];

    public string AccentColorDescription => Localization["Settings.AccentColor.Description"];

    public string AccentColorHelpText => Localization["Settings.AccentColor.HelpText"];

    public string LanguageLabel => Localization["Settings.Language.Label"];

    public string LanguageDescription => Localization["Settings.Language.Description"];

    public string LanguageHelpText => Localization["Settings.Language.HelpText"];

    public string FontFamilyLabel => Localization["Settings.FontFamily.Label"];

    public string FontFamilyDescription => Localization["Settings.FontFamily.Description"];

    public string FontFamilyHelpText => Localization["Settings.FontFamily.HelpText"];

    public string SplashTitle => Localization["Settings.Splash.Title"];

    public string SplashDescription => Localization["Settings.Splash.Description"];

    public string SplashModeLabel => Localization["Settings.SplashMode.Label"];

    public string SplashModeHelpText => Localization["Settings.SplashMode.HelpText"];

    public string SplashStyleLabel => Localization["Settings.SplashStyle.Label"];

    public string SplashStyleHelpText => Localization["Settings.SplashStyle.HelpText"];

    public string SplashDetailedLabel => Localization["Settings.SplashDetailed.Label"];

    public string SplashStartTimeLabel => Localization["Settings.SplashStartTime.Label"];

    public string SplashEndTimeLabel => Localization["Settings.SplashEndTime.Label"];

    public string UpdatesTitle => Localization["Settings.Updates.Title"];

    public string CheckForUpdatesText => Localization["Settings.Updates.CheckButton"];

    public string CheckForUpdatesHelpText => Localization["Settings.Updates.HelpText"];

    public string ApplicationIdentityTitle => Localization["Settings.Identity.Title"];

    public string AboutText => Localization["Settings.About.Button"];

    public string AboutName => Localization["Settings.About.Name"];

    public string AboutHelpText => Localization["Settings.About.HelpText"];

    public string PasswordProtectionTitle => Localization["Settings.Password.Title"];

    public string PasswordProtectionDescription => Localization["Settings.Password.Description"];

    public string PasswordProtectionStatusText => Localization[SettingsUnlocked
        ? "Settings.Password.Unlocked"
        : PasswordProtectionEnabled ? "Settings.Password.Locked" : "Settings.Password.Disabled"];

    public string EnablePasswordProtectionText => Localization["Settings.Password.EnableButton"];

    public string ChangePasswordText => Localization["Settings.Password.ChangeButton"];

    public string DisablePasswordProtectionText => Localization["Settings.Password.DisableButton"];

    public string UnlockSettingsText => Localization["Settings.Password.UnlockButton"];

    public IAsyncRelayCommand ShowAboutCommand { get; }

    public IAsyncRelayCommand CheckForUpdatesCommand { get; }

    public IRelayCommand ClearErrorCommand { get; }

    public IAsyncRelayCommand EnablePasswordProtectionCommand { get; }

    public IAsyncRelayCommand ChangePasswordCommand { get; }

    public IAsyncRelayCommand DisablePasswordProtectionCommand { get; }

    public IAsyncRelayCommand UnlockSettingsCommand { get; }

    public void SetPasswordProtectionState(bool enabled, bool unlocked)
    {
        PasswordProtectionEnabled = enabled;
        SettingsUnlocked = unlocked;
        NotifyPasswordPropertiesChanged();
    }

    public void InitializeAppearance(ShellThemeMode themeMode, AccentOptionViewModel accentOption)
    {
        _initializingAppearance = true;
        try
        {
            SelectedThemeMode = themeMode;
            SelectedThemeModeOption = ThemeModeOptions.First(option => option.Mode == themeMode);
            SelectedAccentOption = accentOption;
            _lastAcceptedThemeMode = themeMode;
            _lastAcceptedAccentOption = accentOption;
        }
        finally
        {
            _initializingAppearance = false;
        }
    }

    public void SyncThemeMode(ShellThemeMode mode)
    {
        _syncingThemeMode = true;
        try
        {
            SelectedThemeMode = mode;
            SelectedThemeModeOption = ThemeModeOptions.First(option => option.Mode == mode);
        }
        finally
        {
            _syncingThemeMode = false;
        }
    }

    public void InitializeFontAndSplash(
        string fontFamily,
        SplashMode splashMode,
        string splashStyle,
        bool showDetailedSplash,
        string splashStartTime,
        string splashEndTime)
    {
        _initializingAppearance = true;
        try
        {
            SelectedFontFamilyOption = FontFamilyOptions.FirstOrDefault(option =>
                string.Equals(option.Family, fontFamily, StringComparison.OrdinalIgnoreCase)) ?? FontFamilyOptions[0];
            SelectedSplashModeOption = SplashModeOptions.First(option => option.Mode == splashMode);
            SelectedSplashStyleOption = SplashStyleOptions.FirstOrDefault(option =>
                string.Equals(option.Style, splashStyle, StringComparison.OrdinalIgnoreCase)) ?? SplashStyleOptions[0];
            ShowDetailedSplash = showDetailedSplash;
            SplashStartTime = splashStartTime;
            SplashEndTime = splashEndTime;
            _lastAcceptedFontFamilyOption = SelectedFontFamilyOption;
            _lastAcceptedSplashModeOption = SelectedSplashModeOption;
            _lastAcceptedSplashStyleOption = SelectedSplashStyleOption;
            _lastAcceptedShowDetailedSplash = ShowDetailedSplash;
            _lastAcceptedSplashStartTime = SplashStartTime;
            _lastAcceptedSplashEndTime = SplashEndTime;
        }
        finally
        {
            _initializingAppearance = false;
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdates = true;
        SetUpdateStatus("Settings.Updates.Checking");

        try
        {
            SetExternalUpdateStatus(await _checkForUpdatesAsync().ConfigureAwait(true));
        }
        catch (Exception exception)
        {
            SetUpdateStatus("Settings.Updates.Failed", exception.Message);
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    partial void OnSelectedThemeModeChanged(ShellThemeMode value)
    {
        if (_initializingAppearance)
        {
            return;
        }

        if (!_syncingThemeMode)
        {
            _syncingThemeMode = true;
            try
            {
                SelectedThemeModeOption = ThemeModeOptions.First(option => option.Mode == value);
            }
            finally
            {
                _syncingThemeMode = false;
            }
        }

        if (!_syncingThemeMode)
        {
            ObserveCallback(ApplyThemeAsync(value));
        }
    }

    partial void OnSelectedThemeModeOptionChanged(ThemeModeOptionViewModel value)
    {
        if (_initializingAppearance || _syncingThemeMode || value is null)
        {
            return;
        }

        SelectedThemeMode = value.Mode;
    }

    partial void OnSelectedAccentOptionChanged(AccentOptionViewModel value)
    {
        if (_initializingAppearance)
        {
            return;
        }

        ObserveCallback(ApplyAccentAsync(value));
    }

    partial void OnSelectedLanguageOptionChanged(LanguageOptionViewModel value)
    {
        if (_syncingLanguage || value is null)
        {
            return;
        }

        ObserveCallback(ApplyLanguageAsync(value));
    }

    partial void OnSelectedFontFamilyOptionChanged(FontFamilyOptionViewModel value)
    {
        if (_initializingAppearance || value is null)
        {
            return;
        }

        ObserveCallback(ApplyFontFamilyAsync(value));
    }

    partial void OnSelectedSplashModeOptionChanged(SplashModeOptionViewModel value)
    {
        if (_initializingAppearance || value is null)
        {
            return;
        }

        ObserveCallback(ApplySplashModeAsync(value));
    }

    partial void OnSelectedSplashStyleOptionChanged(SplashStyleOptionViewModel value)
    {
        if (!_initializingAppearance && value is not null)
        {
            ObserveCallback(ApplySplashStyleAsync(value));
        }
    }

    partial void OnShowDetailedSplashChanged(bool value)
    {
        if (!_initializingAppearance)
        {
            ObserveCallback(ApplyDetailedSplashAsync(value));
        }
    }

    partial void OnSplashStartTimeChanged(string value)
    {
        if (!_initializingAppearance)
        {
            ObserveCallback(ApplySplashTimeRangeAsync(value, SplashEndTime));
        }
    }

    partial void OnSplashEndTimeChanged(string value)
    {
        if (!_initializingAppearance)
        {
            ObserveCallback(ApplySplashTimeRangeAsync(SplashStartTime, value));
        }
    }

    public void SyncLanguage(AppLanguage language)
    {
        _syncingLanguage = true;
        try
        {
            SelectedLanguageOption = LanguageOptions.First(option => option.Language == language);
        }
        finally
        {
            _syncingLanguage = false;
        }
    }

    protected override void RefreshLocalizedText()
    {
        base.RefreshLocalizedText();
        UpdateStatusText = BuildUpdateStatusText();
        OnPropertyChanged(nameof(ThemeModeLabel));
        OnPropertyChanged(nameof(ThemeModeDescription));
        OnPropertyChanged(nameof(ThemeModeHelpText));
        OnPropertyChanged(nameof(AccentColorLabel));
        OnPropertyChanged(nameof(AccentColorDescription));
        OnPropertyChanged(nameof(AccentColorHelpText));
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(LanguageDescription));
        OnPropertyChanged(nameof(LanguageHelpText));
        OnPropertyChanged(nameof(FontFamilyLabel));
        OnPropertyChanged(nameof(FontFamilyDescription));
        OnPropertyChanged(nameof(FontFamilyHelpText));
        OnPropertyChanged(nameof(SplashTitle));
        OnPropertyChanged(nameof(SplashDescription));
        OnPropertyChanged(nameof(SplashModeLabel));
        OnPropertyChanged(nameof(SplashModeHelpText));
        OnPropertyChanged(nameof(SplashStyleLabel));
        OnPropertyChanged(nameof(SplashStyleHelpText));
        OnPropertyChanged(nameof(SplashDetailedLabel));
        OnPropertyChanged(nameof(SplashStartTimeLabel));
        OnPropertyChanged(nameof(SplashEndTimeLabel));
        OnPropertyChanged(nameof(UpdatesTitle));
        OnPropertyChanged(nameof(CheckForUpdatesText));
        OnPropertyChanged(nameof(CheckForUpdatesHelpText));
        OnPropertyChanged(nameof(ApplicationIdentityTitle));
        OnPropertyChanged(nameof(AboutText));
        OnPropertyChanged(nameof(AboutName));
        OnPropertyChanged(nameof(AboutHelpText));
        NotifyPasswordPropertiesChanged();
    }

    private async Task ApplyThemeAsync(ShellThemeMode value)
    {
        if (await _themeChanged(value).ConfigureAwait(true))
        {
            _lastAcceptedThemeMode = value;
            return;
        }

        _syncingThemeMode = true;
        try
        {
            SelectedThemeMode = _lastAcceptedThemeMode;
            SelectedThemeModeOption = ThemeModeOptions.First(option => option.Mode == _lastAcceptedThemeMode);
        }
        finally
        {
            _syncingThemeMode = false;
        }
    }

    private async Task ApplyAccentAsync(AccentOptionViewModel value)
    {
        if (await _accentChanged(value).ConfigureAwait(true))
        {
            _lastAcceptedAccentOption = value;
            return;
        }

        if (_lastAcceptedAccentOption is not null)
        {
            SelectedAccentOption = _lastAcceptedAccentOption;
        }
    }

    private async Task ApplyLanguageAsync(LanguageOptionViewModel value)
    {
        if (await _languageChanged(value.Language).ConfigureAwait(true))
        {
            _lastAcceptedLanguageOption = value;
            return;
        }

        if (_lastAcceptedLanguageOption is not null)
        {
            _syncingLanguage = true;
            try
            {
                SelectedLanguageOption = _lastAcceptedLanguageOption;
            }
            finally
            {
                _syncingLanguage = false;
            }
        }
    }

    private async Task ApplyFontFamilyAsync(FontFamilyOptionViewModel value)
    {
        if (await _fontFamilyChanged(value.Family).ConfigureAwait(true))
        {
            _lastAcceptedFontFamilyOption = value;
            return;
        }

        SelectedFontFamilyOption = _lastAcceptedFontFamilyOption ?? FontFamilyOptions[0];
    }

    private async Task ApplySplashModeAsync(SplashModeOptionViewModel value)
    {
        if (await _splashModeChanged(value.Mode).ConfigureAwait(true))
        {
            _lastAcceptedSplashModeOption = value;
            return;
        }

        SelectedSplashModeOption = _lastAcceptedSplashModeOption ?? SplashModeOptions[0];
    }

    private async Task ApplySplashStyleAsync(SplashStyleOptionViewModel value)
    {
        if (await _splashStyleChanged(value.Style).ConfigureAwait(true))
        {
            _lastAcceptedSplashStyleOption = value;
            return;
        }

        SelectedSplashStyleOption = _lastAcceptedSplashStyleOption ?? SplashStyleOptions[0];
    }

    private async Task ApplyDetailedSplashAsync(bool value)
    {
        if (await _detailedSplashChanged(value).ConfigureAwait(true))
        {
            _lastAcceptedShowDetailedSplash = value;
            return;
        }

        ShowDetailedSplash = _lastAcceptedShowDetailedSplash;
    }

    private async Task ApplySplashTimeRangeAsync(string startTime, string endTime)
    {
        if (await _splashTimeRangeChanged(startTime, endTime).ConfigureAwait(true))
        {
            _lastAcceptedSplashStartTime = startTime;
            _lastAcceptedSplashEndTime = endTime;
            return;
        }

        _initializingAppearance = true;
        try
        {
            SplashStartTime = _lastAcceptedSplashStartTime;
            SplashEndTime = _lastAcceptedSplashEndTime;
        }
        finally
        {
            _initializingAppearance = false;
        }
    }

    private async Task EnablePasswordProtectionAsync()
    {
        if (await _enablePasswordProtection().ConfigureAwait(true))
        {
            NotifyPasswordPropertiesChanged();
        }
    }

    private async Task ChangePasswordAsync()
    {
        if (await _changePassword().ConfigureAwait(true))
        {
            NotifyPasswordPropertiesChanged();
        }
    }

    private async Task DisablePasswordProtectionAsync()
    {
        if (await _disablePasswordProtection().ConfigureAwait(true))
        {
            NotifyPasswordPropertiesChanged();
        }
    }

    private async Task UnlockSettingsAsync()
    {
        if (await _unlockSettings().ConfigureAwait(true))
        {
            SettingsUnlocked = true;
            NotifyPasswordPropertiesChanged();
        }
    }

    private void NotifyPasswordPropertiesChanged()
    {
        OnPropertyChanged(nameof(PasswordProtectionStatusText));
        OnPropertyChanged(nameof(EnablePasswordProtectionText));
        OnPropertyChanged(nameof(ChangePasswordText));
        OnPropertyChanged(nameof(DisablePasswordProtectionText));
        OnPropertyChanged(nameof(UnlockSettingsText));
        EnablePasswordProtectionCommand.NotifyCanExecuteChanged();
        ChangePasswordCommand.NotifyCanExecuteChanged();
        DisablePasswordProtectionCommand.NotifyCanExecuteChanged();
        UnlockSettingsCommand.NotifyCanExecuteChanged();
    }

    private static void ObserveCallback(Task task)
    {
        _ = ObserveCallbackAsync(task);
    }

    private static async Task ObserveCallbackAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Settings callback failed: {exception}");
        }
    }

    private void SetUpdateStatus(string key, string? argument = null)
    {
        _updateStatusKey = key;
        _updateStatusArgument = argument;
        UpdateStatusText = BuildUpdateStatusText();
    }

    private void SetExternalUpdateStatus(string status)
    {
        _updateStatusKey = null;
        _updateStatusArgument = null;
        UpdateStatusText = status;
    }

    private string BuildUpdateStatusText()
    {
        if (_updateStatusKey is null)
        {
            return UpdateStatusText;
        }

        var template = _localization[_updateStatusKey];
        return _updateStatusArgument is null ? template : string.Format(CultureInfo.InvariantCulture, template, _updateStatusArgument);
    }
}
