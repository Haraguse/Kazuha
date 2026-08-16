using System.IO;
using System.Text.Json;
using Luminalium.Core.Configuration;

namespace Luminalium.App.Services;

public sealed class SettingsCoordinator
{
    private readonly ConfigurationService? _configurationService;
    private readonly LuminaliumConfig _config;
    private readonly AsyncSerialGate _gate = new();

    public SettingsCoordinator(LuminaliumConfig config, ConfigurationService? configurationService = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
        _configurationService = configurationService;
    }

    public LuminaliumConfig Current => _config;

    public event EventHandler? Changed;

    public Task<bool> UpdateAsync(Action<LuminaliumConfig> update) =>
        _gate.RunAsync(() => UpdateCoreAsync(update));

    private Task<bool> UpdateCoreAsync(Action<LuminaliumConfig> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        var candidate = JsonSerializer.Deserialize<LuminaliumConfig>(
            JsonSerializer.Serialize(_config, ConfigurationJson.Options),
            ConfigurationJson.Options);
        if (candidate is null)
        {
            return Task.FromResult(false);
        }

        update(candidate);
        if (_configurationService is not null && !_configurationService.Save(candidate).IsSuccess)
        {
            return Task.FromResult(false);
        }

        Copy(candidate, _config);
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Switches to the named profile (writing the <c>_active</c> marker), reloads the
    /// target profile's configuration into <see cref="Current"/>, and raises
    /// <see cref="Changed"/> so runtime consumers (Overlay, theming, etc.) refresh.
    /// </summary>
    public Task<bool> ReloadFromProfileAsync(string profileName)
    {
        ArgumentNullException.ThrowIfNull(profileName);
        return _gate.RunAsync(() => ReloadFromProfileCoreAsync(profileName));
    }

    private Task<bool> ReloadFromProfileCoreAsync(string profileName)
    {
        if (_configurationService is null ||
            !TryWriteActiveMarker(profileName))
        {
            return Task.FromResult(false);
        }

        var load = _configurationService.Load();
        if (!load.IsSuccess || load.Config is null)
        {
            return Task.FromResult(false);
        }

        Copy(load.Config, _config);
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.FromResult(true);
    }

    private bool TryWriteActiveMarker(string profileName)
    {
        try
        {
            Directory.CreateDirectory(_configurationService!.SettingsDirectoryPath);
            var markerValue = ProfileService.IsDefaultProfile(profileName) ? "default" : profileName;
            var markerPath = Path.Combine(
                _configurationService.SettingsDirectoryPath,
                ConfigurationService.ActiveProfileFileName);
            File.WriteAllText(markerPath, markerValue);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void Copy(LuminaliumConfig source, LuminaliumConfig destination)
    {
        destination.SchemaVersion = source.SchemaVersion;
        destination.Appearance = source.Appearance;
        destination.General = source.General;
        destination.Toolbar = source.Toolbar;
        destination.Linkage = source.Linkage;
        destination.Overlay = source.Overlay;
        destination.Ppt = source.Ppt;
        destination.SelfPen = source.SelfPen;
        destination.Notifications = source.Notifications;
        destination.Security = source.Security;
        destination.BoardInBoard = source.BoardInBoard;
        destination.Timer = source.Timer;
        destination.Fonts = source.Fonts;
        destination.Updates = source.Updates;
    }
}
