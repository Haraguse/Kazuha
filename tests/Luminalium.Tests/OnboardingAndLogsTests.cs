using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using Luminalium.Core.Configuration;
using Luminalium.Core.Localization;
using System.Text.Json;
using Xunit;

namespace Luminalium.Tests;

public sealed class OnboardingAndLogsTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"Luminalium-OnboardingLogs-{Guid.NewGuid():N}");

    public OnboardingAndLogsTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void CompletingOnboardingPersistsAndReloads()
    {
        var service = new ConfigurationService(_directory);
        var config = service.Load().Config;
        var viewModel = new OnboardingViewModel(config, service, new LocalizationService());

        viewModel.CompleteCommand.Execute(null);

        Assert.True(viewModel.IsCompleted);
        Assert.True(new ConfigurationService(_directory).Load().Config.General.OnboardingCompleted);
    }

    [Fact]
    public void LogsIgnoreMalformedEntriesAndFilterByMinimumSeverity()
    {
        var path = Path.Combine(_directory, "logs.jsonl");
        File.WriteAllLines(path,
        [
            "{\"timestamp\":\"2026-01-01T00:00:00Z\",\"severity\":\"Information\",\"message\":\"ready\"}",
            "not-json",
            "{\"timestamp\":\"2026-01-01T00:00:01Z\",\"severity\":\"Error\",\"message\":\"failed\"}",
        ]);
        var service = new LocalLogService(logPath: path);

        var entries = service.Read();

        Assert.Equal(2, entries.Count);
        Assert.Single(service.Filter(entries, LogSeverity.Error));
        Assert.Equal("failed", service.Filter(entries, LogSeverity.Error)[0].Message);
    }

    [Fact]
    public void LogsRetainNewestValidEntriesWhenMalformedLinesArePresent()
    {
        var path = Path.Combine(_directory, "bounded.jsonl");
        File.WriteAllLines(path,
        [
            EntryJson("old", 0),
            "not-json",
            EntryJson("middle", 1),
            "{\"timestamp\":\"bad\",\"severity\":\"Error\",\"message\":\"invalid\"}",
            EntryJson("newest", 2),
        ]);
        var service = new LocalLogService(logPath: path, maxEntries: 2);

        var entries = service.Read();

        Assert.Equal(["middle", "newest"], entries.Select(entry => entry.Message));
    }

    [Fact]
    public void AppendCreatesDirectoriesAndWritesOneValidJsonLine()
    {
        var path = Path.Combine(_directory, "nested", "logs.jsonl");
        var service = new LocalLogService(logPath: path);
        var entry = new LogEntry(DateTimeOffset.UnixEpoch, LogSeverity.Warning, "watch", "test");

        service.Append(entry);

        var lines = File.ReadAllLines(path);
        Assert.Single(lines);
        using var document = JsonDocument.Parse(lines[0]);
        Assert.Equal("watch", document.RootElement.GetProperty("message").GetString());
        Assert.Single(service.Read());
    }

    [Fact]
    public async Task ConcurrentAppendsRemainValidJsonLines()
    {
        var path = Path.Combine(_directory, "concurrent", "logs.jsonl");
        var service = new LocalLogService(logPath: path);
        var tasks = Enumerable.Range(0, 32)
            .Select(index => Task.Run(() => service.Append(new LogEntry(
                DateTimeOffset.UnixEpoch.AddSeconds(index),
                LogSeverity.Information,
                $"message-{index}",
                "test"))))
            .ToArray();

        await Task.WhenAll(tasks);

        var lines = File.ReadAllLines(path);
        Assert.Equal(32, lines.Length);
        foreach (var line in lines)
        {
            using var document = JsonDocument.Parse(line);
            Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        }
    }

    [Fact]
    public void ExportReportsSuccessAndFailureWithoutThrowing()
    {
        var fileSystem = new RecordingLogFileSystem();
        var service = new LocalLogService(fileSystem, Path.Combine(_directory, "logs.jsonl"));
        var entries = new[] { new LogEntry(DateTimeOffset.UnixEpoch, LogSeverity.Warning, "watch", "test") };

        Assert.True(service.TryExport(entries, Path.Combine(_directory, "export.jsonl")));
        Assert.Contains("watch", fileSystem.LastContent, StringComparison.Ordinal);
        fileSystem.ThrowOnWrite = true;
        Assert.False(service.TryExport(entries, Path.Combine(_directory, "failed.jsonl")));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static string EntryJson(string message, int seconds) => JsonSerializer.Serialize(new
    {
        Timestamp = DateTimeOffset.UnixEpoch.AddSeconds(seconds),
        Severity = LogSeverity.Information.ToString(),
        Message = message,
        Source = "test",
    }, JsonOptions);

    private sealed class RecordingLogFileSystem : ILogFileSystem
    {
        public bool ThrowOnWrite { get; set; }

        public string LastContent { get; private set; } = string.Empty;

        public IEnumerable<string> ReadLines(string path) => [];

        public void WriteAllText(string path, string content)
        {
            if (ThrowOnWrite)
            {
                throw new IOException("write failed");
            }

            LastContent = content;
        }

        public void AppendAllText(string path, string content) => WriteAllText(path, content);
    }
}
