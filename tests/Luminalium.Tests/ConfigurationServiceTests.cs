using System.Text;
using System.Text.Json;
using Luminalium.Core.Configuration;
using Xunit;

namespace Luminalium.Tests;

public sealed class ConfigurationServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"Luminalium-Configuration-{Guid.NewGuid():N}");

    public ConfigurationServiceTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void FreshInstallCreatesVersionedConfigAndRoundTripsThemeAndAccent()
    {
        var service = new ConfigurationService(_directory);

        var created = service.Load();

        Assert.True(created.WasCreated);
        Assert.True(created.IsSuccess);
        Assert.Equal(LuminaliumConfig.CurrentSchemaVersion, created.Config.SchemaVersion);
        Assert.Equal(Path.Combine(_directory, ConfigurationService.BaseSettingsFileName), created.Path);
        Assert.True(File.Exists(created.Path));
        Assert.False(File.Exists(Path.Combine(_directory, ConfigurationService.ActiveProfileFileName)));

        WriteFixture(ConfigurationService.ActiveProfileFileName, "default");

        created.Config.Appearance.ThemeMode = ThemeMode.Dark;
        created.Config.Appearance.AccentColor = "#0078D4";

        var saved = service.Save(created.Config);
        var reloaded = new ConfigurationService(_directory).Load();

        Assert.True(saved.IsSuccess);
        Assert.True(reloaded.IsSuccess);
        Assert.Equal(LuminaliumConfig.CurrentSchemaVersion, reloaded.Config.SchemaVersion);
        Assert.Equal(ThemeMode.Dark, reloaded.Config.Appearance.ThemeMode);
        Assert.Equal("#0078D4", reloaded.Config.Appearance.AccentColor);
        Assert.Equal("zh-CN", reloaded.Config.General.Language);
        Assert.Equal("default", reloaded.Config.Appearance.ThemeId);
        Assert.Equal("bottom", reloaded.Config.Overlay.ToolbarPosition.ToString().ToLowerInvariant());
        Assert.Equal("#000000", reloaded.Config.SelfPen.PenColor);

        var files = Directory.GetFiles(_directory, "*", SearchOption.TopDirectoryOnly)
            .Select(filePath => Path.GetFileName(filePath)!)
            .OrderBy(fileName => fileName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [ConfigurationService.ActiveProfileFileName, ConfigurationService.BaseSettingsFileName],
            files);

        var json = File.ReadAllText(created.Path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Assert.Contains("\"SchemaVersion\": 1", json, StringComparison.Ordinal);
        Assert.Contains("\"ThemeMode\": \"Dark\"", json, StringComparison.Ordinal);
        Assert.Contains("\"Language\": \"zh-CN\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void PlaintextPasswordIsRejectedWhenProtectionIsEnabled()
    {
        var service = new ConfigurationService(_directory);
        var config = LuminaliumConfig.CreateDefault();
        config.Security.PasswordProtectionEnabled = true;
        config.Security.PasswordHash = "plain-text-password";

        var result = service.Save(config);

        Assert.False(result.IsSuccess);
        Assert.Equal(ConfigurationSaveErrorCode.InvalidConfiguration, result.Error!.Code);
        Assert.Equal(nameof(SecuritySettings.PasswordHash), result.Error.Field);
        Assert.False(File.Exists(Path.Combine(_directory, ConfigurationService.BaseSettingsFileName)));
    }

    [Fact]
    public void CorruptJsonIsBackedUpAndReturnsStructuredDefaultWarning()
    {
        var service = new ConfigurationService(_directory);
        var path = Path.Combine(_directory, ConfigurationService.BaseSettingsFileName);
        var corruptBytes = Encoding.UTF8.GetBytes("{not-json");
        File.WriteAllBytes(path, corruptBytes);

        var result = service.Load();

        Assert.True(result.IsRecoveredDefault);
        Assert.False(result.IsSuccess);
        Assert.Equal(ConfigurationLoadWarningCode.MalformedJson, result.Warning!.Code);
        Assert.Equal(path, result.Warning.Path);
        Assert.NotNull(result.Warning.BackupPath);
        Assert.EndsWith(".bak", result.Warning.BackupPath, StringComparison.Ordinal);
        Assert.True(File.Exists(result.Warning.BackupPath));
        Assert.Equal(corruptBytes, File.ReadAllBytes(result.Warning.BackupPath));
        Assert.Equal(corruptBytes, File.ReadAllBytes(path));
        Assert.Equal(LuminaliumConfig.CurrentSchemaVersion, result.Config.SchemaVersion);
    }

    [Fact]
    public void SaveAtomicallyReplacesContentAndLeavesNoTemporaryFile()
    {
        var service = new ConfigurationService(_directory);
        var initial = service.Load();
        initial.Config.General.HideOnClose = false;
        Assert.True(service.Save(initial.Config).IsSuccess);

        var savedConfig = LuminaliumConfig.CreateDefault();
        savedConfig.Appearance.ThemeId = "nightly";
        savedConfig.Appearance.ThemeMode = ThemeMode.Dark;
        savedConfig.Security.PasswordProtectionEnabled = true;
        savedConfig.Security.PasswordHash = "scrypt$N=16384$r=8$p=1$salt$hash";

        var saveResult = service.Save(savedConfig);
        var path = Path.Combine(_directory, ConfigurationService.BaseSettingsFileName);
        var content = File.ReadAllText(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var persisted = JsonSerializer.Deserialize<LuminaliumConfig>(content, ConfigurationJson.Options);

        Assert.True(saveResult.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal("nightly", persisted.Appearance.ThemeId);
        Assert.Equal(ThemeMode.Dark, persisted.Appearance.ThemeMode);
        Assert.Equal("scrypt$N=16384$r=8$p=1$salt$hash", persisted.Security.PasswordHash);
        Assert.DoesNotContain(
            Directory.GetFiles(_directory, "*", SearchOption.TopDirectoryOnly),
            file => file.EndsWith(".tmp", StringComparison.Ordinal));
    }

    [Fact]
    public void ActiveProfileMarkerLoadsNamedProfile()
    {
        var profileName = "presentation";
        var profilePath = Path.Combine(_directory, profileName + ".json");
        var profile = LuminaliumConfig.CreateDefault();
        profile.Appearance.ThemeMode = ThemeMode.Dark;
        profile.Appearance.ThemeId = "presentation-dark";
        profile.Overlay.ToolbarPosition = ToolbarPosition.Right;
        WriteFixture(ConfigurationService.ActiveProfileFileName, profileName);
        File.WriteAllText(
            profilePath,
            JsonSerializer.Serialize(profile, ConfigurationJson.Options),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var result = new ConfigurationService(_directory).Load();

        Assert.True(result.IsSuccess);
        Assert.Equal(profilePath, result.Path);
        Assert.Equal(ThemeMode.Dark, result.Config.Appearance.ThemeMode);
        Assert.Equal("presentation-dark", result.Config.Appearance.ThemeId);
        Assert.Equal(ToolbarPosition.Right, result.Config.Overlay.ToolbarPosition);
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
}
