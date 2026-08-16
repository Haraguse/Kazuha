using Luminalium.Core.Configuration;

namespace Luminalium.App.Services;

/// <summary>
/// Reports storage usage across the application's data directories and performs
/// user-triggered cleanup of safe, disposable caches. Cleanup targets are always
/// verified to live inside the application data root so that critical files such
/// as <c>settings.json</c>, <c>_active</c> and user profiles are never touched.
/// </summary>
public sealed class StorageService
{
    private const string DefaultBackupsSubDirectory = "backups";
    private const string DefaultUpdateCacheSubDirectory = "update_cache";
    private const string DefaultLogsSubDirectory = "logs";

    private readonly string _settingsDirectory;
    private readonly string _backupsDirectory;
    private readonly string _updateCacheDirectory;
    private readonly string _logsDirectory;
    private readonly string _appDataRoot;

    public StorageService(
        ConfigurationService configurationService,
        string? backupsDirectory = null,
        string? updateCacheDirectory = null,
        string? logsDirectory = null,
        string? appDataRoot = null)
    {
        _settingsDirectory = configurationService.SettingsDirectoryPath;
        _backupsDirectory = backupsDirectory ?? Path.Combine(_settingsDirectory, DefaultBackupsSubDirectory);
        _updateCacheDirectory = updateCacheDirectory ?? Path.Combine(_settingsDirectory, DefaultUpdateCacheSubDirectory);
        _logsDirectory = logsDirectory ?? Path.Combine(_settingsDirectory, DefaultLogsSubDirectory);
        // The application data root encompasses the settings directory and its
        // sub-directories; cleanup is only ever allowed underneath it.
        _appDataRoot = appDataRoot ?? _settingsDirectory;
    }

    public string SettingsDirectoryPath => _settingsDirectory;
    public string BackupsDirectoryPath => _backupsDirectory;
    public string UpdateCacheDirectoryPath => _updateCacheDirectory;
    public string LogsDirectoryPath => _logsDirectory;

    public StorageInfo GetStorageInfo() => new(
        SettingsDirectorySize: GetDirectorySize(_settingsDirectory),
        BackupsDirectorySize: GetDirectorySize(_backupsDirectory),
        UpdateCacheDirectorySize: GetDirectorySize(_updateCacheDirectory),
        LogsDirectorySize: GetDirectorySize(_logsDirectory));

    public StorageOperationResult CleanUpdateCache() =>
        CleanDirectory(_updateCacheDirectory, "更新缓存");

    public StorageOperationResult CleanBackups() =>
        CleanDirectory(_backupsDirectory, "备份");

    private StorageOperationResult CleanDirectory(string directory, string label)
    {
        if (!IsWithinAppDataRoot(directory))
        {
            return StorageOperationResult.Failure($"拒绝清理应用数据目录之外的路径。");
        }

        if (!Directory.Exists(directory))
        {
            return StorageOperationResult.Success();
        }

        try
        {
            foreach (var subDirectory in Directory.EnumerateDirectories(directory))
            {
                Directory.Delete(subDirectory, recursive: true);
            }

            foreach (var file in Directory.EnumerateFiles(directory))
            {
                File.Delete(file);
            }

            return StorageOperationResult.Success();
        }
        catch (IOException exception)
        {
            return StorageOperationResult.Failure($"清理{label}失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return StorageOperationResult.Failure($"清理{label}失败：{exception.Message}");
        }
    }

    private bool IsWithinAppDataRoot(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetFullPath(_appDataRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static long GetDirectorySize(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return 0;
        }

        long total = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                try
                {
                    total += new FileInfo(file).Length;
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return total;
    }
}

/// <summary>
/// Snapshot of the storage usage reported by <see cref="StorageService.GetStorageInfo"/>.
/// </summary>
public sealed record StorageInfo(
    long SettingsDirectorySize,
    long BackupsDirectorySize,
    long UpdateCacheDirectorySize,
    long LogsDirectorySize);