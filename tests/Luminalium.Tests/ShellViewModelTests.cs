using System.Text;
using Luminalium.App.Services;
using Luminalium.App.ViewModels;
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
        Assert.Equal("settings", viewModel.Plugins[0].DisplayName);
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
        Assert.Equal(ShellViewModel.VersionUnavailableText, new ShellViewModel(malformedPath).VersionDisplay);
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

        public void Apply(ShellThemeMode mode) => AppliedModes.Add(mode);

        public void UseSystemAccent()
        {
        }

        public void ApplyAccent(string accentKey)
        {
        }
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
