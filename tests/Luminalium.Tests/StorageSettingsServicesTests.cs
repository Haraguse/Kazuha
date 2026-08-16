using System.Text.Json;
using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using Luminalium.Core.Configuration;
using Xunit;

namespace Luminalium.Tests;

public sealed class StorageSettingsServicesTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"Luminalium-Storage-{Guid.NewGuid():N}");

    private ConfigurationService NewConfigurationService() => new(_directory);

    [Fact]
    public void ProfileCreateSwitchListSucceeds()
    {
        var service = new ProfileService(NewConfigurationService());
        service.CreateProfile("演示档案");

        Assert.Contains("演示档案", service.ListProfiles());
        Assert.True(File.Exists(Path.Combine(_directory, "演示档案.json")));

        var switchResult = service.SwitchProfile("演示档案");
        Assert.True(switchResult.IsSuccess);
        Assert.Equal("演示档案", service.GetActiveProfileName());
    }

    [Fact]
    public void ProfileRenameSucceedsAndUpdatesActiveMarker()
    {
        var service = new ProfileService(NewConfigurationService());
        service.CreateProfile("旧名称");
        service.SwitchProfile("旧名称");

        var result = service.RenameProfile("旧名称", "新名称");

        Assert.True(result.IsSuccess);
        Assert.Contains("新名称", service.ListProfiles());
        Assert.DoesNotContain("旧名称", service.ListProfiles());
        Assert.Equal("新名称", service.GetActiveProfileName());
    }

    [Fact]
    public void ProfileDeleteSucceeds()
    {
        var service = new ProfileService(NewConfigurationService());
        service.CreateProfile("待删除");
        service.SwitchProfile("default");

        var result = service.DeleteProfile("待删除");

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("待删除", service.ListProfiles());
        Assert.False(File.Exists(Path.Combine(_directory, "待删除.json")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("default")]
    [InlineData("a\\b")]
    [InlineData("a/b")]
    [InlineData("bad*name")]
    public void ProfileInvalidNamesAreRejected(string name)
    {
        var service = new ProfileService(NewConfigurationService());
        var result = service.CreateProfile(name);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
    }

    [Fact]
    public void ProfileDeleteActiveProfileIsRejected()
    {
        var service = new ProfileService(NewConfigurationService());
        service.CreateProfile("活动档案");
        service.SwitchProfile("活动档案");

        var result = service.DeleteProfile("活动档案");

        Assert.False(result.IsSuccess);
        Assert.True(File.Exists(Path.Combine(_directory, "活动档案.json")));
    }

    [Fact]
    public void ProfileDeleteDefaultIsRejected()
    {
        var service = new ProfileService(NewConfigurationService());
        var result = service.DeleteProfile("default");

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
    }

    [Fact]
    public void BackupCreateAndRestoreSucceeds()
    {
        var configurationService = NewConfigurationService();
        var load = configurationService.Load();
        load.Config.Appearance.AccentColor = "#123456";
        configurationService.Save(load.Config);

        var service = new ConfigurationBackupService(configurationService);
        var create = service.CreateBackup("我的备份");
        Assert.True(create.IsSuccess);
        Assert.Contains("我的备份", service.ListBackups());
        Assert.True(File.Exists(Path.Combine(_directory, "backups", "我的备份.json")));

        load.Config.Appearance.AccentColor = "#000000";
        configurationService.Save(load.Config);

        var restore = service.RestoreBackup("我的备份");
        Assert.True(restore.IsSuccess);

        var reloaded = NewConfigurationService().Load();
        Assert.Equal("#123456", reloaded.Config.Appearance.AccentColor);
    }

    [Fact]
    public void BackupRestoreNonexistentFailsWithoutTouchingTarget()
    {
        var configurationService = NewConfigurationService();
        configurationService.Load();
        var service = new ConfigurationBackupService(configurationService);

        var before = configurationService.GetActiveSettingsPath();
        var original = File.ReadAllBytes(before);

        var result = service.RestoreBackup("不存在的备份");

        Assert.False(result.IsSuccess);
        Assert.Equal(original, File.ReadAllBytes(before));
    }

    [Fact]
    public void StorageCleanUpdateCacheDeletesCacheButKeepsSettings()
    {
        var configurationService = NewConfigurationService();
        configurationService.Load();
        var settingsPath = configurationService.GetActiveSettingsPath();
        var updateCache = Path.Combine(_directory, "update_cache");
        Directory.CreateDirectory(updateCache);
        File.WriteAllText(Path.Combine(updateCache, "staged.zip"), "zip");

        var service = new StorageService(configurationService);
        var result = service.CleanUpdateCache();

        Assert.True(result.IsSuccess);
        Assert.False(File.Exists(Path.Combine(updateCache, "staged.zip")));
        Assert.True(File.Exists(settingsPath));
    }

    [Fact]
    public void StorageCleanBackupsDeletesBackupsButKeepsProfileAndActive()
    {
        var configurationService = NewConfigurationService();
        configurationService.Load();
        var profileService = new ProfileService(configurationService);
        profileService.CreateProfile("档案A");
        profileService.SwitchProfile("档案A");

        var backups = Path.Combine(_directory, "backups");
        Directory.CreateDirectory(backups);
        File.WriteAllText(Path.Combine(backups, "old.json"), "{}");

        var service = new StorageService(configurationService);
        var result = service.CleanBackups();

        Assert.True(result.IsSuccess);
        Assert.False(File.Exists(Path.Combine(backups, "old.json")));
        Assert.True(File.Exists(Path.Combine(_directory, "档案A.json")));
        Assert.True(File.Exists(Path.Combine(_directory, ConfigurationService.ActiveProfileFileName)));
        Assert.True(File.Exists(Path.Combine(_directory, ConfigurationService.BaseSettingsFileName)));
    }

    [Fact]
    public void StorageRefusesToCleanOutsideAppDataRoot()
    {
        var configurationService = NewConfigurationService();
        var outside = Path.Combine(Path.GetTempPath(), $"Outside-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outside);
        try
        {
            // update cache is configured to a path outside the app data root.
            var service = new StorageService(
                configurationService,
                updateCacheDirectory: outside,
                appDataRoot: _directory);

            var result = service.CleanUpdateCache();

            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        }
        finally
        {
            if (Directory.Exists(outside))
            {
                Directory.Delete(outside, recursive: true);
            }
        }
    }

    [Fact]
    public void StorageGetStorageInfoReportsSizes()
    {
        var configurationService = NewConfigurationService();
        configurationService.Load();
        Directory.CreateDirectory(Path.Combine(_directory, "logs"));
        File.WriteAllText(Path.Combine(_directory, "logs", "app.log"), "hello");

        var service = new StorageService(configurationService);
        var info = service.GetStorageInfo();

        Assert.True(info.SettingsDirectorySize > 0);
        Assert.True(info.LogsDirectorySize > 0);
    }

    [Fact]
    public async Task CoordinatorReloadFromProfileUpdatesCurrentAndRaisesChanged()
    {
        var configurationService = NewConfigurationService();
        var config = configurationService.Load().Config;
        var profileService = new ProfileService(configurationService);
        Assert.True(profileService.CreateProfile("演示档案").IsSuccess);

        // Give the profile a distinctive value independent of the base config.
        var profileConfig = configurationService.Load().Config;
        profileConfig.Appearance.AccentColor = "#ABCDEF";
        File.WriteAllText(
            Path.Combine(_directory, "演示档案.json"),
            JsonSerializer.Serialize(profileConfig, ConfigurationJson.Options));

        var coordinator = new SettingsCoordinator(config, configurationService);
        var changes = 0;
        coordinator.Changed += (_, _) => changes++;

        Assert.True(await coordinator.ReloadFromProfileAsync("演示档案"));
        Assert.Equal("演示档案", profileService.GetActiveProfileName());
        Assert.Equal("#ABCDEF", coordinator.Current.Appearance.AccentColor);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task CoordinatorReloadFromProfileFallsBackWithNoConfigurationService()
    {
        var coordinator = new SettingsCoordinator(LuminaliumConfig.CreateDefault());

        Assert.False(await coordinator.ReloadFromProfileAsync("任意档案"));
    }

    [Fact]
    public void StorageViewModelSwitchRefreshesActiveProfileDisplay()
    {
        var configurationService = NewConfigurationService();
        configurationService.Load();
        var profileService = new ProfileService(configurationService);
        profileService.CreateProfile("演示档案");

        var viewModel = new StorageSettingsViewModel(profileService);
        viewModel.SelectedProfile = "演示档案";
        viewModel.SwitchProfileCommand.Execute(null);

        Assert.True(viewModel.HasStatus);
        Assert.Equal("演示档案", viewModel.ActiveProfileName);
        Assert.Equal("演示档案", profileService.GetActiveProfileName());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}