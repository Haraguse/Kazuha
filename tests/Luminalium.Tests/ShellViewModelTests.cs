using System.Text;
using Luminalium.Core.Configuration;
using Luminalium.Core.Platform;
using Luminalium.Core.Localization;
using Luminalium.Core.Security;
using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using Luminalium.Theming;
using Luminalium.Updater;
using Xunit;

namespace Luminalium.Tests;

public sealed class ShellViewModelTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"Luminalium-ShellViewModel-{Guid.NewGuid():N}");

    public ShellViewModelTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void BuildsPluginListFromCatalogInDeterministicOrder()
    {
        var viewModel = new ShellViewModel();

        Assert.Equal(8, viewModel.Plugins.Count);
        Assert.Equal(
        [
            "settings",
            "onboarding",
            "board",
            "timer",
            "spotlight",
            "app_launcher",
            "logs",
            "status_bar",
        ], viewModel.Plugins.Select(plugin => plugin.Id));
        Assert.Equal(
        [
            "",
            "Onboarding",
            "板中板 - Luminalium",
            "Timer",
            "Spotlight",
            "App Launcher",
            "日志 - Luminalium",
            "Status Bar",
        ], viewModel.Plugins.Select(plugin => plugin.RawDisplayName));
        Assert.Equal("设置", viewModel.Plugins[0].DisplayName);
    }

    [Fact]
    public void NavigationBackStackDoesNotDuplicateSamePage()
    {
        var viewModel = new ShellViewModel();

        Assert.False(viewModel.CanGoBack);
        Assert.Same(viewModel.Overview, viewModel.CurrentPage);

        viewModel.NavigateToPlugin(viewModel.Plugins[3]);

        Assert.True(viewModel.CanGoBack);
        var timerPage = Assert.IsType<PluginPageViewModel>(viewModel.CurrentPage);
        Assert.Equal("timer", timerPage.Plugin.Id);

        viewModel.NavigateToPlugin(viewModel.Plugins[3]);
        viewModel.GoBack();

        Assert.False(viewModel.CanGoBack);
        Assert.Same(viewModel.Overview, viewModel.CurrentPage);
    }

    [Fact]
    public void NavigationBackReturnsToPreviousPage()
    {
        var viewModel = new ShellViewModel();

        viewModel.NavigateToSettings();
        viewModel.NavigateToPlugin(viewModel.Plugins[5]);

        Assert.True(viewModel.CanGoBack);
        Assert.IsType<PluginPageViewModel>(viewModel.CurrentPage);

        viewModel.GoBack();

        Assert.True(viewModel.CanGoBack);
        Assert.Same(viewModel.Settings, viewModel.CurrentPage);

        viewModel.GoBack();

        Assert.False(viewModel.CanGoBack);
        Assert.Same(viewModel.Overview, viewModel.CurrentPage);
    }

    [Fact]
    public void NativePluginNavigationUsesDedicatedPagesAndSharedInstances()
    {
        var config = LuminaliumConfig.CreateDefault();
        var configurationService = new ConfigurationService(_directory);
        var logPath = Path.Combine(_directory, "logs.jsonl");
        File.WriteAllText(
            logPath,
            "{\"timestamp\":\"2026-01-01T00:00:00Z\",\"severity\":\"Information\",\"message\":\"shell ready\"}");
        var logService = new LocalLogService(logPath: logPath);
        var viewModel = new ShellViewModel(
            config: config,
            configurationService: configurationService,
            logService: logService);

        viewModel.NavigateToPlugin(viewModel.Plugins.Single(plugin => plugin.Id == "onboarding"));
        var onboardingPage = Assert.IsType<OnboardingViewModel>(viewModel.CurrentPage);
        onboardingPage.CompleteCommand.Execute(null);

        viewModel.NavigateToPlugin(viewModel.Plugins.Single(plugin => plugin.Id == "logs"));
        var logsPage = Assert.IsType<LogsViewModel>(viewModel.CurrentPage);

        Assert.Single(logsPage.Entries);
        Assert.Equal("shell ready", logsPage.Entries[0].Message);
        Assert.True(configurationService.Load().Config.General.OnboardingCompleted);

        viewModel.NavigateToPlugin(viewModel.Plugins.Single(plugin => plugin.Id == "onboarding"));
        Assert.Same(onboardingPage, viewModel.CurrentPage);
        viewModel.NavigateToPlugin(viewModel.Plugins.Single(plugin => plugin.Id == "logs"));
        Assert.Same(logsPage, viewModel.CurrentPage);
    }

    [Fact]
    public void VersionDisplayUsesReaderAndFallsBackForMalformedMetadata()
    {
        var validPath = WriteFixture(
            "valid.json",
            """
            {
              "code_name": "Momokan",
              "code_name_CN": "桃缶（河原木桃香）",
              "version": "1.4.0.9-EMERGENCY",
              "versionnm": "1.4.0.9-EMERGENCY",
              "build": "00611.1409",
              "future_codename": "RyouYamada"
            }
            """);
        var malformedPath = WriteFixture("malformed.json", "{not-json");

        Assert.Equal("1.4.0.9-EMERGENCY | 00611.1409", ShellViewModel.LoadVersionDisplay(validPath));
        Assert.Equal(ShellViewModel.VersionUnavailableText, ShellViewModel.LoadVersionDisplay(malformedPath));
        Assert.Equal(new LocalizationService()["Shell.VersionUnavailable"], new ShellViewModel(malformedPath).VersionDisplay);
    }

    [Fact]
    public void ThemeModeTransitionsAreDeterministic()
    {
        var themeService = new RecordingThemeService();
        var viewModel = new ShellViewModel(themeService: themeService);

        viewModel.ApplyThemeMode(ShellThemeMode.Light);
        viewModel.Settings.SelectedThemeMode = ShellThemeMode.Dark;
        viewModel.ApplyThemeMode(ShellThemeMode.System);

        Assert.Equal(ShellThemeMode.System, viewModel.SelectedThemeMode);
        Assert.Equal(ShellThemeMode.System, viewModel.Settings.SelectedThemeMode);
        Assert.Equal(
        [
            ShellThemeMode.Light,
            ShellThemeMode.Light,
            ShellThemeMode.Dark,
            ShellThemeMode.System,
        ], themeService.AppliedModes);
    }

    [Fact]
    public void ConstructorRestoresConfiguredThemeAndStandardAccent()
    {
        var config = LuminaliumConfig.CreateDefault();
        config.Appearance.ThemeMode = ThemeMode.Dark;
        config.Appearance.AccentColor = "#038387";
        var themeService = new RecordingThemeService();
        var viewModel = new ShellViewModel(config: config, themeService: themeService);

        Assert.Equal(ShellThemeMode.Dark, viewModel.SelectedThemeMode);
        Assert.Equal(ShellThemeMode.Dark, viewModel.Settings.SelectedThemeMode);
        Assert.Equal("teal", viewModel.Settings.SelectedAccentOption.Key);
        Assert.Equal([ShellThemeMode.Dark], themeService.AppliedModes);
        Assert.Equal(["teal"], themeService.AppliedAccentKeys);
    }

    [Fact]
    public void ThemeModeChangePersistsAndReloadsAsSystemAuto()
    {
        var configurationService = new ConfigurationService(_directory);
        var loaded = configurationService.Load();
        var themeService = new RecordingThemeService();
        var viewModel = new ShellViewModel(
            config: loaded.Config,
            configurationService: configurationService,
            themeService: themeService);

        viewModel.Settings.SelectedThemeMode = ShellThemeMode.System;

        var reloaded = configurationService.Load();
        var restored = new ShellViewModel(config: reloaded.Config, themeService: new RecordingThemeService());

        Assert.Equal(ThemeMode.Auto, reloaded.Config.Appearance.ThemeMode);
        Assert.Equal(ShellThemeMode.System, restored.SelectedThemeMode);
        Assert.Equal(ShellThemeMode.System, restored.Settings.SelectedThemeMode);
    }

    [Fact]
    public async Task StandardAccentChangePersistsAndReloadsAsConfigHex()
    {
        var configurationService = new ConfigurationService(_directory);
        var loaded = configurationService.Load();
        var viewModel = new ShellViewModel(
            config: loaded.Config,
            configurationService: configurationService,
            themeService: new RecordingThemeService());
        var tealOption = Assert.Single(viewModel.Settings.AccentOptions, option => option.Key == "teal");

        viewModel.Settings.SelectedAccentOption = tealOption;

        var reloaded = configurationService.Load();
        var restored = new ShellViewModel(config: reloaded.Config, themeService: new RecordingThemeService());

        Assert.Equal("#038387", reloaded.Config.Appearance.AccentColor);
        Assert.Equal("teal", restored.Settings.SelectedAccentOption.Key);
    }

    [Fact]
    public async Task FontAndSplashSettingsPersistAndReloadThroughNativeSettings()
    {
        var configurationService = new ConfigurationService(_directory);
        var loaded = configurationService.Load();
        var viewModel = new ShellViewModel(
            config: loaded.Config,
            configurationService: configurationService,
            themeService: new RecordingThemeService(),
            knownFontFamilies: ["Segoe UI", "Noto Sans CJK"]);

        viewModel.Settings.SelectedFontFamilyOption = Assert.Single(
            viewModel.Settings.FontFamilyOptions,
            option => option.Family == "Noto Sans CJK");
        viewModel.Settings.SelectedSplashModeOption = Assert.Single(
            viewModel.Settings.SplashModeOptions,
            option => option.Mode == SplashMode.TimeRange);
        viewModel.Settings.SelectedSplashStyleOption = Assert.Single(
            viewModel.Settings.SplashStyleOptions,
            option => option.Style == "nina_iseri_1_2");
        viewModel.Settings.ShowDetailedSplash = true;
        viewModel.Settings.SplashStartTime = "22:00";
        viewModel.Settings.SplashEndTime = "06:00";

        await Task.Delay(50);
        var reloaded = configurationService.Load().Config;

        Assert.Equal("Noto Sans CJK", reloaded.Appearance.FontFamily);
        Assert.Equal(SplashMode.TimeRange, reloaded.General.SplashMode);
        Assert.Equal("nina_iseri_1_2", reloaded.General.SplashStyle);
        Assert.True(reloaded.General.ShowDetailedSplash);
        Assert.Equal("22:00", reloaded.General.SplashStartTime);
        Assert.Equal("06:00", reloaded.General.SplashEndTime);
    }

    [Fact]
    public async Task FontAndSplashSaveFailureRollsBackMemoryAndSettings()
    {
        var blockedPath = Path.Combine(_directory, "not-a-directory");
        File.WriteAllText(blockedPath, "locked");
        var configurationService = new ConfigurationService(blockedPath);
        var themeService = new RecordingThemeService();
        var viewModel = new ShellViewModel(
            config: LuminaliumConfig.CreateDefault(),
            configurationService: configurationService,
            themeService: themeService,
            knownFontFamilies: ["Segoe UI", "Noto Sans CJK"]);

        var requestedFont = Assert.Single(viewModel.Settings.FontFamilyOptions, option => option.Family == "Noto Sans CJK");
        viewModel.Settings.SelectedFontFamilyOption = requestedFont;
        viewModel.Settings.SelectedSplashModeOption = Assert.Single(viewModel.Settings.SplashModeOptions, option => option.Mode == SplashMode.Never);
        await Task.Delay(50);

        Assert.Equal("Segoe UI", viewModel.Settings.SelectedFontFamilyOption.Family);
        Assert.Equal(SplashMode.Always, viewModel.Settings.SelectedSplashModeOption.Mode);
        Assert.Contains(FontFamilyResolver.DefaultFontFamilyStack, themeService.AppliedFontFamilies);
    }

    [Fact]
    public async Task SystemAndMonetAccentChangesPersistSemanticRepresentations()
    {
        var configurationService = new ConfigurationService(_directory);
        var loaded = configurationService.Load();
        var themeService = new RecordingThemeService();
        var monetService = new MonetThemeService(new RecordingAccentProvider(RgbColor.FromHex("#0078D4")));
        var viewModel = new ShellViewModel(
            config: loaded.Config,
            configurationService: configurationService,
            themeService: themeService,
            monetThemeService: monetService);
        var systemOption = Assert.Single(viewModel.Settings.AccentOptions, option => option.Key == "system");
        var monetOption = Assert.Single(viewModel.Settings.AccentOptions, option => option.Key == "monet");

        await viewModel.ApplyAccentOptionAsync(systemOption);
        Assert.Equal("system", configurationService.Load().Config.Appearance.AccentColor);

        await viewModel.ApplyAccentOptionAsync(monetOption);

        var reloaded = configurationService.Load();
        var restored = new ShellViewModel(
            config: reloaded.Config,
            themeService: new RecordingThemeService(),
            monetThemeService: monetService);

        Assert.Equal("monet", reloaded.Config.Appearance.AccentColor);
        Assert.Equal("monet", restored.Settings.SelectedAccentOption.Key);
    }

    [Fact]
    public void UnknownAccentFallsBackToSystemWithoutRewritingConfig()
    {
        var config = LuminaliumConfig.CreateDefault();
        config.Appearance.AccentColor = "unknown-accent";
        var viewModel = new ShellViewModel(config: config, themeService: new RecordingThemeService());

        Assert.Equal("system", viewModel.Settings.SelectedAccentOption.Key);
        Assert.Equal("unknown-accent", config.Appearance.AccentColor);
    }

    [Fact]
    public async Task MonetAccentOptionAppliesComputedPaletteThroughThemeService()
    {
        var themeService = new RecordingThemeService();
        var monetService = new MonetThemeService(new RecordingAccentProvider(RgbColor.FromHex("#0078D4")));
        var viewModel = new ShellViewModel(themeService: themeService, monetThemeService: monetService);
        var monetOption = Assert.Single(
            viewModel.Settings.AccentOptions,
            option => option.Key == "monet");

        await viewModel.ApplyAccentOptionAsync(monetOption);

        Assert.Contains(
            viewModel.Settings.AccentOptions,
            option => option.DisplayName == "系统（Monet）");
        Assert.Equal("#0078D4", themeService.MonetPalette!.Seed.ToHexString());
        Assert.Empty(themeService.AppliedAccentKeys);
        Assert.False(themeService.SystemAccentUsed);
    }

    [Fact]
    public async Task LaterAccentSelectionWinsOverSlowMonetComputation()
    {
        var themeService = new RecordingThemeService();
        var accentProvider = new DelayedAccentProvider(RgbColor.FromHex("#0078D4"));
        var monetService = new MonetThemeService(accentProvider);
        var viewModel = new ShellViewModel(themeService: themeService, monetThemeService: monetService);
        var monetOption = Assert.Single(viewModel.Settings.AccentOptions, option => option.Key == "monet");
        var blueOption = Assert.Single(viewModel.Settings.AccentOptions, option => option.Key == "blue");

        var monetTask = viewModel.ApplyAccentOptionAsync(monetOption);
        await accentProvider.WaitForRequestAsync();
        await viewModel.ApplyAccentOptionAsync(blueOption);
        accentProvider.Complete();
        await monetTask;

        Assert.Null(themeService.MonetPalette);
        Assert.Equal(["blue"], themeService.AppliedAccentKeys);
    }

    [Fact]
    public async Task CheckForUpdatesCommandUsesDialogServiceStatus()
    {
        var dialogService = new RecordingDialogService("Update check completed.");
        var viewModel = new ShellViewModel(dialogService: dialogService);

        await viewModel.Settings.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.True(dialogService.UpdateDialogShown);
        Assert.Equal("Update check completed.", viewModel.Settings.UpdateStatusText);
        Assert.False(viewModel.Settings.IsCheckingForUpdates);
    }

    [Fact]
    public async Task WrongAndCancelledUnlockKeepSettingsLockedAndExposeLocalizedErrorOnlyForWrongPassword()
    {
        var passwordService = new PasswordHashService();
        var config = LuminaliumConfig.CreateDefault();
        config.General.Language = "en-US";
        config.Security.PasswordProtectionEnabled = true;
        config.Security.PasswordHash = passwordService.HashPassword("correct-password", PasswordHashService.MinimumIterations)!;
        var dialogService = new ScriptedDialogService(
            PasswordDialogResult.Submitted("wrong-password"),
            PasswordDialogResult.Cancelled);
        var localization = new LocalizationService();
        var viewModel = new ShellViewModel(config: config, dialogService: dialogService, localizationService: localization);

        var changed = await viewModel.ApplyThemeModeAsync(ShellThemeMode.Dark);

        Assert.False(changed);
        Assert.False(viewModel.IsSettingsUnlocked);
        Assert.Equal("Incorrect password", viewModel.ErrorText);
        Assert.Equal(ShellThemeMode.Light, viewModel.SelectedThemeMode);

        viewModel.ClearError();
        changed = await viewModel.ApplyLanguageAsync(AppLanguage.EnUs);

        Assert.False(changed);
        Assert.False(viewModel.IsSettingsUnlocked);
        Assert.Empty(viewModel.ErrorText);
        Assert.Equal(AppLanguage.EnUs, viewModel.Localization.CurrentLanguage);
    }

    [Fact]
    public async Task CorrectUnlockIsProcessLocalAndProtectedAppearanceMutationPersistsWithoutPlaintext()
    {
        var directory = Path.Combine(_directory, "protected");
        Directory.CreateDirectory(directory);
        var configurationService = new ConfigurationService(directory);
        var config = LuminaliumConfig.CreateDefault();
        config.Security.PasswordProtectionEnabled = true;
        config.Security.PasswordHash = new PasswordHashService().HashPassword("correct-password", PasswordHashService.MinimumIterations)!;
        var dialogService = new ScriptedDialogService(PasswordDialogResult.Submitted("correct-password"));
        var viewModel = new ShellViewModel(
            config: config,
            configurationService: configurationService,
            dialogService: dialogService,
            themeService: new RecordingThemeService());

        var changed = await viewModel.ApplyThemeModeAsync(ShellThemeMode.Dark);
        var persisted = File.ReadAllText(configurationService.GetActiveSettingsPath());

        Assert.True(changed);
        Assert.True(viewModel.IsSettingsUnlocked);
        Assert.Equal(ShellThemeMode.Dark, viewModel.Settings.SelectedThemeMode);
        Assert.Equal(ThemeMode.Dark, configurationService.Load().Config.Appearance.ThemeMode);
        Assert.DoesNotContain("correct-password", persisted, StringComparison.Ordinal);
        Assert.DoesNotContain("wrong-password", persisted, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnableChangeAndDisablePasswordProtectionPersistOnlyHashState()
    {
        var directory = Path.Combine(_directory, "password-lifecycle");
        Directory.CreateDirectory(directory);
        var configurationService = new ConfigurationService(directory);
        var dialogService = new ScriptedDialogService(
            PasswordDialogResult.Submitted("first-password", "first-password"),
            PasswordDialogResult.Submitted("second-password", "second-password"));
        var config = LuminaliumConfig.CreateDefault();
        var viewModel = new ShellViewModel(config: config, configurationService: configurationService, dialogService: dialogService);

        Assert.True(await viewModel.EnablePasswordProtectionAsync());
        var firstHash = config.Security.PasswordHash;
        Assert.True(config.Security.PasswordProtectionEnabled);
        Assert.True(new PasswordHashService().VerifyPassword("first-password", firstHash));

        Assert.True(await viewModel.ChangePasswordAsync());
        Assert.NotEqual(firstHash, config.Security.PasswordHash);
        Assert.True(new PasswordHashService().VerifyPassword("second-password", config.Security.PasswordHash));

        Assert.True(await viewModel.DisablePasswordProtectionAsync());
        var persisted = File.ReadAllText(configurationService.GetActiveSettingsPath());
        Assert.False(config.Security.PasswordProtectionEnabled);
        Assert.Equal(string.Empty, config.Security.PasswordHash);
        Assert.DoesNotContain("first-password", persisted, StringComparison.Ordinal);
        Assert.DoesNotContain("second-password", persisted, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveFailureRestoresProtectedThemeAndPasswordState()
    {
        var blockedPath = Path.Combine(_directory, "not-a-directory");
        File.WriteAllText(blockedPath, "locked");
        var configurationService = new ConfigurationService(blockedPath);
        var config = LuminaliumConfig.CreateDefault();
        var dialogService = new ScriptedDialogService(
            PasswordDialogResult.Submitted("new-password", "new-password"));
        var viewModel = new ShellViewModel(
            config: config,
            configurationService: configurationService,
            dialogService: dialogService,
            themeService: new RecordingThemeService());

        Assert.False(await viewModel.EnablePasswordProtectionAsync());
        Assert.False(config.Security.PasswordProtectionEnabled);
        Assert.Equal(string.Empty, config.Security.PasswordHash);

        var changed = await viewModel.ApplyThemeModeAsync(ShellThemeMode.Dark);

        Assert.False(changed);
        Assert.Equal(ThemeMode.Light, config.Appearance.ThemeMode);
        Assert.Equal(ShellThemeMode.Light, viewModel.Settings.SelectedThemeMode);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string WriteFixture(string fileName, string content)
    {
        var path = Path.Combine(_directory, fileName);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }

    private sealed class RecordingThemeService : IShellThemeService
    {
        public List<ShellThemeMode> AppliedModes { get; } = [];

        public List<string> AppliedAccentKeys { get; } = [];

        public List<string> AppliedFontFamilies { get; } = [];

        public bool SystemAccentUsed { get; private set; }

        public MonetPalette? MonetPalette { get; private set; }

        public void Apply(ShellThemeMode mode) => AppliedModes.Add(mode);

        public void ApplyFontFamily(string resolvedFontFamily) => AppliedFontFamilies.Add(resolvedFontFamily);

        public void UseSystemAccent()
        {
            SystemAccentUsed = true;
        }

        public void ApplyAccent(string accentKey) => AppliedAccentKeys.Add(accentKey);

        public void ApplyMonetPalette(MonetPalette palette)
        {
            MonetPalette = palette;
        }
    }

    private sealed class RecordingAccentProvider(RgbColor color) : IAccentProvider
    {
        public Task<PlatformOperationResult<RgbColor?>> TryGetAccentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PlatformOperation.Success<RgbColor?>(color));
    }

    private sealed class DelayedAccentProvider(RgbColor color) : IAccentProvider
    {
        private readonly TaskCompletionSource _requested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<PlatformOperationResult<RgbColor?>> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<PlatformOperationResult<RgbColor?>> TryGetAccentAsync(CancellationToken cancellationToken = default)
        {
            _requested.SetResult();
            return _result.Task;
        }

        public Task WaitForRequestAsync() => _requested.Task;

        public void Complete() => _result.SetResult(PlatformOperation.Success<RgbColor?>(color));
    }

    private sealed class RecordingDialogService(string updateStatus) : IDialogService
    {
        public bool UpdateDialogShown { get; private set; }

        public Task ShowAboutAsync(ShellViewModel shellViewModel) => Task.CompletedTask;

        public Task<string> ShowUpdateAsync(ShellViewModel shellViewModel, UpdateOrchestrator orchestrator, string installDirectory)
        {
            UpdateDialogShown = true;
            return Task.FromResult(updateStatus);
        }

        public Task<PasswordDialogResult> ShowPasswordAsync(ShellViewModel shellViewModel, PasswordDialogMode mode) =>
            Task.FromResult(PasswordDialogResult.Cancelled);

        public Task<RetryCloseDialogResult> ShowRetryCloseAsync(ShellViewModel shellViewModel, RetryCloseDialogRequest request) =>
            Task.FromResult(RetryCloseDialogResult.OwnerNotReady);
    }

    private sealed class ScriptedDialogService(params PasswordDialogResult[] passwordResults) : IDialogService
    {
        private readonly Queue<PasswordDialogResult> _passwordResults = new(passwordResults);

        public Task ShowAboutAsync(ShellViewModel shellViewModel) => Task.CompletedTask;

        public Task<string> ShowUpdateAsync(ShellViewModel shellViewModel, UpdateOrchestrator orchestrator, string installDirectory) =>
            Task.FromResult(string.Empty);

        public Task<PasswordDialogResult> ShowPasswordAsync(ShellViewModel shellViewModel, PasswordDialogMode mode) =>
            Task.FromResult(_passwordResults.Count == 0 ? PasswordDialogResult.Cancelled : _passwordResults.Dequeue());

        public Task<RetryCloseDialogResult> ShowRetryCloseAsync(ShellViewModel shellViewModel, RetryCloseDialogRequest request) =>
            Task.FromResult(RetryCloseDialogResult.OwnerNotReady);
    }

}
