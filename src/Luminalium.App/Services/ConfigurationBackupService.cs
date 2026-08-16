using System.Globalization;
using Luminalium.Core.Configuration;

namespace Luminalium.App.Services;

/// <summary>
/// Manages user-named backups of the active configuration. Backups are stored as
/// <c>&lt;name&gt;.json</c> files under the <c>backups</c> sub-directory of the settings
/// directory. Restoring writes the backup content back to the active configuration
/// (or a chosen profile) and rolls back on failure.
/// </summary>
public sealed class ConfigurationBackupService
{
    private readonly ConfigurationService _configurationService;
    private readonly string _settingsDirectory;
    private readonly string _backupsDirectory;

    public ConfigurationBackupService(
        ConfigurationService configurationService,
        string? backupsDirectory = null)
    {
        _configurationService = configurationService
            ?? throw new ArgumentNullException(nameof(configurationService));
        _settingsDirectory = configurationService.SettingsDirectoryPath;
        _backupsDirectory = backupsDirectory ?? Path.Combine(_settingsDirectory, "backups");
    }

    public string BackupsDirectoryPath => _backupsDirectory;

    public IReadOnlyList<string> ListBackups()
    {
        var names = new List<string>();
        if (!Directory.Exists(_backupsDirectory))
        {
            return names;
        }

        foreach (var file in Directory.EnumerateFiles(_backupsDirectory, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (name.Length > 0)
            {
                names.Add(name);
            }
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names;
    }

    public StorageOperationResult CreateBackup(string name)
    {
        var backupName = string.IsNullOrWhiteSpace(name)
            ? $"backup-{DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)}"
            : name.Trim();

        var validation = ValidateBackupName(backupName);
        if (validation is not null)
        {
            return StorageOperationResult.Failure(validation);
        }

        var backupPath = Path.Combine(_backupsDirectory, backupName + ".json");
        if (File.Exists(backupPath))
        {
            return StorageOperationResult.Failure($"备份“{backupName}”已存在。");
        }

        try
        {
            Directory.CreateDirectory(_backupsDirectory);
            var activePath = _configurationService.GetActiveSettingsPath();
            if (!File.Exists(activePath))
            {
                var save = _configurationService.Save(LuminaliumConfig.CreateDefault());
                if (!save.IsSuccess)
                {
                    return StorageOperationResult.Failure("无法保存当前配置。");
                }
            }

            File.Copy(activePath, backupPath, overwrite: false);
            return StorageOperationResult.Success();
        }
        catch (IOException exception)
        {
            return StorageOperationResult.Failure($"创建备份失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return StorageOperationResult.Failure($"创建备份失败：{exception.Message}");
        }
    }

    public StorageOperationResult RenameBackup(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName))
        {
            return StorageOperationResult.Failure("备份名称不能为空。");
        }

        var validation = ValidateBackupName(newName);
        if (validation is not null)
        {
            return StorageOperationResult.Failure(validation);
        }

        var oldPath = Path.Combine(_backupsDirectory, oldName.Trim() + ".json");
        var newPath = Path.Combine(_backupsDirectory, newName.Trim() + ".json");
        if (!File.Exists(oldPath))
        {
            return StorageOperationResult.Failure($"备份“{oldName}”不存在。");
        }

        if (File.Exists(newPath))
        {
            return StorageOperationResult.Failure($"备份“{newName}”已存在。");
        }

        try
        {
            File.Move(oldPath, newPath);
            return StorageOperationResult.Success();
        }
        catch (IOException exception)
        {
            return StorageOperationResult.Failure($"重命名备份失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return StorageOperationResult.Failure($"重命名备份失败：{exception.Message}");
        }
    }

    public StorageOperationResult DeleteBackup(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return StorageOperationResult.Failure("备份名称不能为空。");
        }

        var backupPath = Path.Combine(_backupsDirectory, name.Trim() + ".json");
        if (!File.Exists(backupPath))
        {
            return StorageOperationResult.Failure($"备份“{name}”不存在。");
        }

        try
        {
            File.Delete(backupPath);
            return StorageOperationResult.Success();
        }
        catch (IOException exception)
        {
            return StorageOperationResult.Failure($"删除备份失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return StorageOperationResult.Failure($"删除备份失败：{exception.Message}");
        }
    }

    /// <summary>
    /// Restores a backup into the active configuration when <paramref name="targetProfile"/>
    /// is <c>null</c> or <c>default</c>, otherwise into the named profile file. The current
    /// target content is snapshotted first and restored if the write fails.
    /// </summary>
    public StorageOperationResult RestoreBackup(string name, string? targetProfile = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return StorageOperationResult.Failure("备份名称不能为空。");
        }

        var backupPath = Path.Combine(_backupsDirectory, name.Trim() + ".json");
        if (!File.Exists(backupPath))
        {
            return StorageOperationResult.Failure($"备份“{name}”不存在。");
        }

        string targetPath;
        if (string.IsNullOrWhiteSpace(targetProfile) || ProfileService.IsDefaultProfile(targetProfile))
        {
            targetPath = _configurationService.GetActiveSettingsPath();
        }
        else
        {
            targetPath = Path.Combine(_settingsDirectory, targetProfile.Trim() + ".json");
        }

        byte[] original = [];
        var hadOriginal = File.Exists(targetPath);
        try
        {
            if (hadOriginal)
            {
                original = File.ReadAllBytes(targetPath);
            }

            var backupBytes = File.ReadAllBytes(backupPath);
            var temporaryPath = Path.Combine(
                _settingsDirectory,
                $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");

            File.WriteAllBytes(temporaryPath, backupBytes);
            File.Move(temporaryPath, targetPath, overwrite: true);
            return StorageOperationResult.Success();
        }
        catch (IOException exception)
        {
            Rollback(targetPath, hadOriginal, original);
            return StorageOperationResult.Failure($"恢复备份失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Rollback(targetPath, hadOriginal, original);
            return StorageOperationResult.Failure($"恢复备份失败：{exception.Message}");
        }
    }

    private static void Rollback(string targetPath, bool hadOriginal, byte[] original)
    {
        try
        {
            if (hadOriginal)
            {
                File.WriteAllBytes(targetPath, original);
            }
            else if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }
        catch (IOException)
        {
            // Best-effort rollback; the original failure is reported to the caller.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort rollback; the original failure is reported to the caller.
        }
    }

    private static string? ValidateBackupName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "备份名称不能为空。";
        }

        var trimmed = name.Trim();
        if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return "备份名称包含非法字符。";
        }

        if (!string.Equals(trimmed, Path.GetFileName(trimmed), StringComparison.Ordinal))
        {
            return "备份名称不能包含路径分隔符。";
        }

        return null;
    }
}