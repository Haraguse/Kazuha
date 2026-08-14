using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.Core.Localization;
using Luminalium.Core.Identity;
using System.Globalization;

namespace Luminalium.App.ViewModels;

public sealed partial class SettingsViewModel : ShellPageViewModel
{
    private readonly ILocalizationService _localization;
    private readonly Action<ShellThemeMode> _themeChanged;
    private readonly Action<AppLanguage> _languageChanged;
    private readonly Func<AccentOptionViewModel, Task> _accentChanged;
    private readonly Func<Task<string>> _checkForUpdatesAsync;
    private string? _updateStatusKey = "Settings.Updates.Ready";
    private string? _updateStatusArgument;
    private bool _syncingThemeMode;
    private bool _syncingLanguage;

    [ObservableProperty]
    private ShellThemeMode selectedThemeMode;

    [ObservableProperty]
    private ThemeModeOptionViewModel selectedThemeModeOption;

    [ObservableProperty]
    private AccentOptionViewModel selectedAccentOption;

    [ObservableProperty]
    private LanguageOptionViewModel selectedLanguageOption;

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
        Func<AccentOptionViewModel, Task> accentChanged,
        Action<AppLanguage> languageChanged,
        ILocalizationService localization) : base("settings", "Navigation.Settings", localization)
    {
        _localization = localization;
        VersionDisplay = versionDisplay;
        _checkForUpdatesAsync = checkForUpdatesAsync;
        _themeChanged = themeChanged;
        _accentChanged = accentChanged;
        _languageChanged = languageChanged;
        ShowAboutCommand = new AsyncRelayCommand(showAboutAsync);
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync, () => !IsCheckingForUpdates);
        ClearErrorCommand = new RelayCommand(clearError);

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

        selectedThemeModeOption = ThemeModeOptions[0];
        selectedAccentOption = AccentOptions[0];
        selectedLanguageOption = LanguageOptions.First(option => option.Language == localization.CurrentLanguage);
        updateStatusText = BuildUpdateStatusText();
    }

    public string ProductName { get; } = ProductIdentity.DisplayName;

    public string AppUserModelId { get; } = ProductIdentity.WindowsAppUserModelId;

    public string VersionDisplay { get; }

    public IReadOnlyList<ThemeModeOptionViewModel> ThemeModeOptions { get; }

    public IReadOnlyList<AccentOptionViewModel> AccentOptions { get; }

    public IReadOnlyList<LanguageOptionViewModel> LanguageOptions { get; }

    public string ThemeModeLabel => Localization["Settings.ThemeMode.Label"];

    public string ThemeModeDescription => Localization["Settings.ThemeMode.Description"];

    public string ThemeModeHelpText => Localization["Settings.ThemeMode.HelpText"];

    public string AccentColorLabel => Localization["Settings.AccentColor.Label"];

    public string AccentColorDescription => Localization["Settings.AccentColor.Description"];

    public string AccentColorHelpText => Localization["Settings.AccentColor.HelpText"];

    public string LanguageLabel => Localization["Settings.Language.Label"];

    public string LanguageDescription => Localization["Settings.Language.Description"];

    public string LanguageHelpText => Localization["Settings.Language.HelpText"];

    public string UpdatesTitle => Localization["Settings.Updates.Title"];

    public string CheckForUpdatesText => Localization["Settings.Updates.CheckButton"];

    public string CheckForUpdatesHelpText => Localization["Settings.Updates.HelpText"];

    public string ApplicationIdentityTitle => Localization["Settings.Identity.Title"];

    public string AboutText => Localization["Settings.About.Button"];

    public string AboutName => Localization["Settings.About.Name"];

    public string AboutHelpText => Localization["Settings.About.HelpText"];

    public IAsyncRelayCommand ShowAboutCommand { get; }

    public IAsyncRelayCommand CheckForUpdatesCommand { get; }

    public IRelayCommand ClearErrorCommand { get; }

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

        _themeChanged(value);
    }

    partial void OnSelectedThemeModeOptionChanged(ThemeModeOptionViewModel value)
    {
        if (_syncingThemeMode || value is null)
        {
            return;
        }

        SelectedThemeMode = value.Mode;
    }

    partial void OnSelectedAccentOptionChanged(AccentOptionViewModel value) =>
        _ = _accentChanged(value);

    partial void OnSelectedLanguageOptionChanged(LanguageOptionViewModel value)
    {
        if (_syncingLanguage || value is null)
        {
            return;
        }

        _languageChanged(value.Language);
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
        OnPropertyChanged(nameof(UpdatesTitle));
        OnPropertyChanged(nameof(CheckForUpdatesText));
        OnPropertyChanged(nameof(CheckForUpdatesHelpText));
        OnPropertyChanged(nameof(ApplicationIdentityTitle));
        OnPropertyChanged(nameof(AboutText));
        OnPropertyChanged(nameof(AboutName));
        OnPropertyChanged(nameof(AboutHelpText));
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
