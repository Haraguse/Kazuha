using Luminalium.Core.Configuration;

namespace Luminalium.App.Services;

/// <summary>
/// Manages named configuration profiles stored as <c>&lt;name&gt;.json</c> files in the
/// settings directory. The <c>default</c> profile maps to the base <c>settings.json</c>
/// and is never treated as an independent, deletable profile.
/// </summary>
public sealed class ProfileService
{
    private readonly ConfigurationService _configurationService;
    private readonly string _settingsDirectory;

    public ProfileService(ConfigurationService configurationService)
    {
        _configurationService = configurationService
            ?? throw new ArgumentNullException(nameof(configurationService));
        _settingsDirectory = configurationService.SettingsDirectoryPath;
    }

    public string SettingsDirectoryPath => _settingsDirectory;

    /// <summary>
    /// Returns the name of the active profile, or <c>null</c> when the base
    /// <c>settings.json</c> (the <c>default</c> profile) is active.
    /// </summary>
    public string? GetActiveProfileName()
    {
        var markerPath = Path.Combine(_settingsDirectory, ConfigurationService.ActiveProfileFileName);
        if (!File.Exists(markerPath))
        {
            return null;
        }

        try
        {
            var name = File.ReadAllText(markerPath).Trim();
            return name.Length == 0 || name == "default" ? null : name;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Lists all available profiles. The <c>default</c> entry (the base
    /// <c>settings.json</c>) is always included first so the user can switch back.
    /// </summary>
    public IReadOnlyList<string> ListProfiles()
    {
        var names = new List<string> { "default" };
        if (!Directory.Exists(_settingsDirectory))
        {
            return names;
        }

        foreach (var file in Directory.EnumerateFiles(_settingsDirectory, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (name.Length == 0 ||
                string.Equals(name, "settings", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(name);
            }
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names;
    }

    public StorageOperationResult CreateProfile(string name)
    {
        var validation = ValidateProfileName(name);
        if (validation is not null)
        {
            return StorageOperationResult.Failure(validation);
        }

        var profilePath = Path.Combine(_settingsDirectory, name + ".json");
        if (File.Exists(profilePath))
        {
            return StorageOperationResult.Failure($"档案“{name}”已存在。");
        }

        try
        {
            Directory.CreateDirectory(_settingsDirectory);
            var activePath = _configurationService.GetActiveSettingsPath();
            if (!File.Exists(activePath))
            {
                var save = _configurationService.Save(LuminaliumConfig.CreateDefault());
                if (!save.IsSuccess)
                {
                    return StorageOperationResult.Failure("无法创建默认配置。");
                }
            }

            File.Copy(activePath, profilePath, overwrite: false);
            return StorageOperationResult.Success();
        }
        catch (IOException exception)
        {
            return StorageOperationResult.Failure($"创建档案失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return StorageOperationResult.Failure($"创建档案失败：{exception.Message}");
        }
    }

    public StorageOperationResult RenameProfile(string oldName, string newName)
    {
        if (IsDefaultProfile(oldName))
        {
            return StorageOperationResult.Failure("不能重命名默认配置。");
        }

        var validation = ValidateProfileName(newName);
        if (validation is not null)
        {
            return StorageOperationResult.Failure(validation);
        }

        var oldPath = Path.Combine(_settingsDirectory, oldName + ".json");
        var newPath = Path.Combine(_settingsDirectory, newName + ".json");
        if (!File.Exists(oldPath))
        {
            return StorageOperationResult.Failure($"档案“{oldName}”不存在。");
        }

        if (File.Exists(newPath))
        {
            return StorageOperationResult.Failure($"档案“{newName}”已存在。");
        }

        try
        {
            File.Move(oldPath, newPath);
            if (string.Equals(GetActiveProfileName(), oldName, StringComparison.OrdinalIgnoreCase))
            {
                var markerResult = WriteActiveMarker(newName);
                if (!markerResult.IsSuccess)
                {
                    return markerResult;
                }
            }

            return StorageOperationResult.Success();
        }
        catch (IOException exception)
        {
            return StorageOperationResult.Failure($"重命名档案失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return StorageOperationResult.Failure($"重命名档案失败：{exception.Message}");
        }
    }

    public StorageOperationResult DeleteProfile(string name)
    {
        if (IsDefaultProfile(name))
        {
            return StorageOperationResult.Failure("不能删除默认配置 settings.json。");
        }

        if (string.Equals(GetActiveProfileName(), name, StringComparison.OrdinalIgnoreCase))
        {
            return StorageOperationResult.Failure($"不能删除当前活动的档案“{name}”。");
        }

        var profilePath = Path.Combine(_settingsDirectory, name + ".json");
        if (!File.Exists(profilePath))
        {
            return StorageOperationResult.Failure($"档案“{name}”不存在。");
        }

        try
        {
            File.Delete(profilePath);
            return StorageOperationResult.Success();
        }
        catch (IOException exception)
        {
            return StorageOperationResult.Failure($"删除档案失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return StorageOperationResult.Failure($"删除档案失败：{exception.Message}");
        }
    }

    public StorageOperationResult SwitchProfile(string name)
    {
        if (IsDefaultProfile(name))
        {
            return WriteActiveMarker("default");
        }

        var validation = ValidateProfileName(name);
        if (validation is not null)
        {
            return StorageOperationResult.Failure(validation);
        }

        var profilePath = Path.Combine(_settingsDirectory, name + ".json");
        if (!File.Exists(profilePath))
        {
            return StorageOperationResult.Failure($"档案“{name}”不存在。");
        }

        return WriteActiveMarker(name);
    }

    internal static bool IsDefaultProfile(string name) =>
        string.IsNullOrWhiteSpace(name) ||
        string.Equals(name.Trim(), "default", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name.Trim(), "settings", StringComparison.OrdinalIgnoreCase);

    private StorageOperationResult WriteActiveMarker(string name)
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);
            var markerPath = Path.Combine(_settingsDirectory, ConfigurationService.ActiveProfileFileName);
            File.WriteAllText(markerPath, name);
            return StorageOperationResult.Success();
        }
        catch (IOException exception)
        {
            return StorageOperationResult.Failure($"切换档案失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return StorageOperationResult.Failure($"切换档案失败：{exception.Message}");
        }
    }

    private static string? ValidateProfileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "档案名称不能为空。";
        }

        var trimmed = name.Trim();
        if (IsDefaultProfile(trimmed))
        {
            return trimmed == "default" ? "档案名称不能为 default。" : "档案名称与默认配置文件冲突。";
        }

        if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return "档案名称包含非法字符。";
        }

        if (!string.Equals(trimmed, Path.GetFileName(trimmed), StringComparison.Ordinal))
        {
            return "档案名称不能包含路径分隔符。";
        }

        return null;
    }
}