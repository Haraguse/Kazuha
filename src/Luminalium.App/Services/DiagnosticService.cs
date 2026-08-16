using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Luminalium.App.Services;

/// <summary>
/// Aggregates environment and log diagnostics for the About settings page.
/// All inputs are injected so the service stays testable without touching the
/// real file system, network, or configuration store.
/// </summary>
public sealed class DiagnosticService
{
    private readonly string _productName;
    private readonly string _applicationVersion;
    private readonly string _platform;
    private readonly int _configSchemaVersion;
    private readonly string _configDirectoryPath;
    private readonly string? _activeProfileName;
    private readonly string _logDirectory;
    private readonly string _updateCacheDirectory;
    private readonly LocalLogService? _logService;

    public DiagnosticService(
        string productName,
        string applicationVersion,
        string platform,
        int configSchemaVersion,
        string configDirectoryPath,
        string? activeProfileName,
        string logDirectory,
        string updateCacheDirectory,
        LocalLogService? logService = null)
    {
        _productName = string.IsNullOrWhiteSpace(productName) ? "Luminalium" : productName;
        _applicationVersion = applicationVersion;
        _platform = platform;
        _configSchemaVersion = configSchemaVersion;
        _configDirectoryPath = configDirectoryPath;
        _activeProfileName = activeProfileName;
        _logDirectory = logDirectory;
        _updateCacheDirectory = updateCacheDirectory;
        _logService = logService;
    }

    public string ProductName => _productName;

    public string ApplicationVersion => _applicationVersion;

    public string Platform => _platform;

    public int ConfigSchemaVersion => _configSchemaVersion;

    public string ConfigDirectoryPath => _configDirectoryPath;

    public string ActiveProfileName => _activeProfileName ?? "默认";

    public string LogDirectory => _logDirectory;

    public string UpdateCacheDirectory => _updateCacheDirectory;

    public int RecentLogCount => _logService?.Read().Count ?? 0;

    public string LastErrorLogText
    {
        get
        {
            var entries = _logService?.Read() ?? [];
            var lastError = entries.LastOrDefault(entry => entry.Severity >= LogSeverity.Error);
            return lastError is null
                ? "无"
                : $"{lastError.Timestamp:yyyy-MM-dd HH:mm:ss} [{lastError.Severity}] {lastError.Message}";
        }
    }

    /// <summary>
    /// Builds the localized, formatted diagnostic text that is exposed for
    /// copying to the clipboard.
    /// </summary>
    public string BuildDiagnosticText()
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"{_productName} 诊断信息");
        builder.AppendLine(CultureInfo.InvariantCulture, $"应用版本: {_applicationVersion}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"运行平台: {_platform}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"配置 Schema 版本: {_configSchemaVersion}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"配置目录: {_configDirectoryPath}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"活动档案: {ActiveProfileName}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"日志目录: {_logDirectory}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"更新缓存目录: {_updateCacheDirectory}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"最近日志条数: {RecentLogCount}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"最后错误级日志: {LastErrorLogText}");
        return builder.ToString();
    }

    /// <summary>
    /// Resolves the default update cache root used by the updater so the
    /// diagnostics page can display it without depending on the updater.
    /// </summary>
    public static string DefaultUpdateCacheDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = Path.GetTempPath();
        }

        return Path.Combine(localAppData, "Luminalium", "update_cache");
    }

    /// <summary>
    /// Resolves a human-readable runtime platform/OS description.
    /// </summary>
    public static string DefaultPlatform()
    {
        var identifier = RuntimeInformation.RuntimeIdentifier;
        return string.IsNullOrWhiteSpace(identifier)
            ? RuntimeInformation.OSDescription
            : $"{RuntimeInformation.OSDescription} ({identifier})";
    }
}