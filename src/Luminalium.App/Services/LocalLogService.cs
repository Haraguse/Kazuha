using System.Text;
using System.Text.Json;

namespace Luminalium.App.Services;

public enum LogSeverity
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
}

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    LogSeverity Severity,
    string Message,
    string Source);

public interface ILogFileSystem
{
    IEnumerable<string> ReadLines(string path);

    void WriteAllText(string path, string content);
}

public sealed class LocalLogFileSystem : ILogFileSystem
{
    public IEnumerable<string> ReadLines(string path) => File.Exists(path) ? File.ReadLines(path) : [];

    public void WriteAllText(string path, string content) => File.WriteAllText(path, content, new UTF8Encoding(false));
}

public sealed class LocalLogService
{
    public const int DefaultMaxEntries = 500;
    private const int MaxLineLength = 16 * 1024;
    private readonly ILogFileSystem _fileSystem;

    public LocalLogService(ILogFileSystem? fileSystem = null, string? logPath = null, int maxEntries = DefaultMaxEntries)
    {
        _fileSystem = fileSystem ?? new LocalLogFileSystem();
        LogPath = logPath ?? Path.Combine(AppContext.BaseDirectory, "logs", "luminalium.log");
        MaxEntries = maxEntries > 0 ? maxEntries : DefaultMaxEntries;
    }

    public string LogPath { get; }

    public int MaxEntries { get; }

    public IReadOnlyList<LogEntry> Read()
    {
        var entries = new List<LogEntry>(Math.Min(MaxEntries, 64));
        var linesRead = 0;
        var maxLines = MaxEntries * 4;
        IEnumerable<string> lines;
        try
        {
            lines = _fileSystem.ReadLines(LogPath);
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }

        try
        {
            foreach (var line in lines)
            {
                if (entries.Count >= MaxEntries || linesRead++ >= maxLines)
                {
                    break;
                }

                if (line.Length is 0 or > MaxLineLength || TryParse(line, out var entry) is false)
                {
                    continue;
                }

                entries.Add(entry!);
            }
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }

        return entries;
    }

    public IReadOnlyList<LogEntry> Filter(IEnumerable<LogEntry> entries, LogSeverity? minimumSeverity)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return entries
            .Where(entry => minimumSeverity is null || entry.Severity >= minimumSeverity.Value)
            .Take(MaxEntries)
            .ToArray();
    }

    public bool TryExport(IEnumerable<LogEntry> entries, string path)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var content = string.Join(
                Environment.NewLine,
                entries.Select(entry => JsonSerializer.Serialize(new
                {
                    Timestamp = entry.Timestamp,
                    Severity = entry.Severity.ToString(),
                    Message = entry.Message,
                    Source = entry.Source,
                })));
            _fileSystem.WriteAllText(path, content);
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
        catch (ArgumentException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryParse(string line, out LogEntry? entry)
    {
        entry = null;
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (root.ValueKind is not JsonValueKind.Object ||
                !root.TryGetProperty("timestamp", out var timestampProperty) ||
                !root.TryGetProperty("severity", out var severityProperty) ||
                !root.TryGetProperty("message", out var messageProperty))
            {
                return false;
            }

            if (!timestampProperty.TryGetDateTimeOffset(out var timestamp) ||
                !Enum.TryParse<LogSeverity>(severityProperty.GetString(), ignoreCase: true, out var severity))
            {
                return false;
            }

            var message = messageProperty.GetString();
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var source = root.TryGetProperty("source", out var sourceProperty)
                ? sourceProperty.GetString() ?? string.Empty
                : string.Empty;
            entry = new LogEntry(timestamp, severity, message, source);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
