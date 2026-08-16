using System.Text.Json;
using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using Luminalium.Core.Configuration;
using Xunit;

namespace Luminalium.Tests;

public sealed class AboutSettingsTests
{
    [Fact]
    public void UpdateSettingsSectionHasExpectedDefaults()
    {
        var config = LuminaliumConfig.CreateDefault();

        Assert.NotNull(config.Updates);
        Assert.False(config.Updates.AutoCheck);
        Assert.Equal(UpdateSource.GitHub, config.Updates.Source);
    }

    [Fact]
    public void UpdateSettingsRoundTripsThroughJson()
    {
        var config = LuminaliumConfig.CreateDefault();
        config.Updates.AutoCheck = true;
        config.Updates.Source = UpdateSource.Mirror;

        var json = JsonSerializer.Serialize(config, ConfigurationJson.Options);
        var reloaded = JsonSerializer.Deserialize<LuminaliumConfig>(json, ConfigurationJson.Options);

        Assert.NotNull(reloaded);
        Assert.True(reloaded!.Updates.AutoCheck);
        Assert.Equal(UpdateSource.Mirror, reloaded.Updates.Source);
        Assert.Contains("\"AutoCheck\": true", json, StringComparison.Ordinal);
        Assert.Contains("\"Source\": \"Mirror\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigurationServiceRejectsInvalidUpdateSourceEnum()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"Luminalium-About-{Guid.NewGuid():N}");
        try
        {
            var service = new ConfigurationService(directory);
            var config = LuminaliumConfig.CreateDefault();
            config.Updates.Source = (UpdateSource)999;

            var result = service.Save(config);

            Assert.False(result.IsSuccess);
            Assert.Equal("Configuration contains an unsupported enum value.", result.Error!.Message);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void DiagnosticTextAggregatesVersionPlatformAndPaths()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"Luminalium-Log-{Guid.NewGuid():N}");
        var logPath = Path.Combine(directory, "luminalium.log");
        var logService = new LocalLogService(new LocalLogFileSystem(), logPath, maxEntries: 100);
        logService.Log(LogSeverity.Information, "started", "app");
        logService.Log(LogSeverity.Error, "boom", "updater");

        try
        {
            var service = new DiagnosticService(
                productName: "Luminalium",
                applicationVersion: "1.4.0.9",
                platform: "Microsoft Windows 10.0.22631",
                configSchemaVersion: 1,
                configDirectoryPath: @"C:\Config\Luminalium",
                activeProfileName: "presentation",
                logDirectory: directory,
                updateCacheDirectory: @"C:\Cache\update_cache",
                logService: logService);

            var text = service.BuildDiagnosticText();

            Assert.Contains("Luminalium", text, StringComparison.Ordinal);
            Assert.Contains("1.4.0.9", text, StringComparison.Ordinal);
            Assert.Contains("Microsoft Windows 10.0.22631", text, StringComparison.Ordinal);
            Assert.Contains(@"C:\Config\Luminalium", text, StringComparison.Ordinal);
            Assert.Contains("presentation", text, StringComparison.Ordinal);
            Assert.Contains(@"C:\Cache\update_cache", text, StringComparison.Ordinal);
            Assert.Contains("最近日志条数: 2", text, StringComparison.Ordinal);
            Assert.Contains("Error", text, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void DiagnosticTextFallsBackWithoutLogService()
    {
        var service = new DiagnosticService(
            productName: "Luminalium",
            applicationVersion: "1.0",
            platform: "test-platform",
            configSchemaVersion: 1,
            configDirectoryPath: "C:\\cfg",
            activeProfileName: null,
            logDirectory: "C:\\logs",
            updateCacheDirectory: "C:\\cache");

        var text = service.BuildDiagnosticText();

        Assert.Contains("默认", text, StringComparison.Ordinal);
        Assert.Contains("最近日志条数: 0", text, StringComparison.Ordinal);
        Assert.Contains("最后错误级日志: 无", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructorMapsUpdateSettingsFromConfig()
    {
        var config = new LuminaliumConfig();
        config.Updates.AutoCheck = true;
        config.Updates.Source = UpdateSource.Mirror;

        var vm = new AboutSettingsViewModel(config: config);

        Assert.True(vm.AutoCheckEnabled);
        Assert.Equal(1, vm.SelectedUpdateSourceIndex);
    }

    [Fact]
    public async Task AutoCheckChangePersistsToUpdateSettings()
    {
        var config = new LuminaliumConfig();
        var coordinator = new SettingsCoordinator(config);
        var vm = new AboutSettingsViewModel(config: config, coordinator: coordinator);

        Assert.False(vm.AutoCheckEnabled);
        vm.AutoCheckEnabled = true;

        await WaitUntilAsync(() => config.Updates.AutoCheck);
        Assert.True(config.Updates.AutoCheck);
    }

    [Fact]
    public async Task UpdateSourceChangePersistsToConfig()
    {
        var config = new LuminaliumConfig();
        var coordinator = new SettingsCoordinator(config);
        var vm = new AboutSettingsViewModel(config: config, coordinator: coordinator);

        vm.SelectedUpdateSourceIndex = 1;

        await WaitUntilAsync(() => config.Updates.Source == UpdateSource.Mirror);
        Assert.Equal(UpdateSource.Mirror, config.Updates.Source);
    }

    [Fact]
    public void CopyDiagnosticsCommandCopiesGeneratedText()
    {
        var copied = false;

        var vm = new AboutSettingsViewModel(
            versionDisplay: "1.0.0",
            diagnosticService: new DiagnosticService(
                productName: "Luminalium",
                applicationVersion: "1.0.0",
                platform: "test",
                configSchemaVersion: 1,
                configDirectoryPath: "C:\\cfg",
                activeProfileName: null,
                logDirectory: "C:\\logs",
                updateCacheDirectory: "C:\\cache"),
            copyToClipboard: text => { copied = true; return Task.CompletedTask; });

        vm.CopyDiagnosticsCommand.Execute(null);

        Assert.True(copied);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new Xunit.Sdk.XunitException("Timed out waiting for condition.");
            }

            await Task.Delay(10);
        }
    }
}