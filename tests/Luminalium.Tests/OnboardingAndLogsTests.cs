using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using Luminalium.Core.Configuration;
using Luminalium.Core.Localization;
using Xunit;

namespace Luminalium.Tests;

public sealed class OnboardingAndLogsTests : IDisposable
{
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
    }
}
