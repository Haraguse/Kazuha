using System.Text;
using Luminalium.Core.Platform;
using Luminalium.Core.Localization;
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
            ShellThemeMode.Dark,
            ShellThemeMode.System,
        ], themeService.AppliedModes);
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

        public bool SystemAccentUsed { get; private set; }

        public MonetPalette? MonetPalette { get; private set; }

        public void Apply(ShellThemeMode mode) => AppliedModes.Add(mode);

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
    }

}
