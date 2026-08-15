using System;
using System.Threading;
using System.Threading.Tasks;
using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using Luminalium.Core.Platform;
using Xunit;

namespace Luminalium.Tests;

/// <summary>
/// Headless behavioral tests for the operational plugins (timer, spotlight,
/// app launcher, status bar). All external dependencies are fakes so no real
/// process, audio, notification, or SMTC session is ever touched.
/// </summary>
public class OperationalPluginsTests
{
    // ---------------------------------------------------------------- Timer

    [Fact]
    public async Task ThreeSecondCountdownFinishesAndCuesOnce()
    {
        var clock = new FastClock();
        var audio = new CountingAudioCue();
        var notification = new CountingNotificationCue();
        var vm = new TimerViewModel(clock: clock, audioCue: audio, notificationCue: notification)
        {
            InputText = "00:00:03",
        };

        await vm.StartCommand.ExecuteAsync(null);

        Assert.True(vm.IsFinished);
        Assert.False(vm.IsRunning);
        Assert.Equal("00:00:00", vm.DisplayText);
        Assert.Equal(1, audio.CallCount);
        Assert.Equal(1, notification.CallCount);
        Assert.Equal(3, clock.DelayCalls);
    }

    [Fact]
    public async Task TerminateCancelsRunningCountdownAndNoTaskOutlives()
    {
        var clock = new BlockingClock();
        var audio = new CountingAudioCue();
        var vm = new TimerViewModel(clock: clock, audioCue: audio)
        {
            InputText = "00:00:03",
        };

        var task = vm.StartCommand.ExecuteAsync(null);
        await clock.WaitUntilBlockedAsync();

        vm.Terminate();

        await task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(vm.IsRunning);
        Assert.False(vm.IsFinished);
        Assert.Equal(0, audio.CallCount);
    }

    [Fact]
    public async Task PauseAfterElapsedTickPreservesRemainingTimeWhenResumed()
    {
        var clock = new PauseResumeClock();
        var vm = new TimerViewModel(clock: clock)
        {
            InputText = "00:00:03",
        };

        var initialRun = vm.StartCommand.ExecuteAsync(null);
        await clock.WaitUntilFirstTickAsync();
        clock.ReleaseFirstTick();
        await clock.WaitUntilSecondTickAsync();

        vm.PauseCommand.Execute(null);
        vm.InputText = "00:00:10";

        Assert.False(vm.IsRunning);
        Assert.Equal(2, vm.RemainingSeconds);
        Assert.Equal("00:00:02", vm.DisplayText);

        await initialRun.WaitAsync(TimeSpan.FromSeconds(5));

        var resumedRun = vm.StartCommand.ExecuteAsync(null);
        clock.ReleaseSecondTick();
        await resumedRun.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(vm.IsFinished);
        Assert.Equal(4, clock.DelayCalls);
    }

    [Theory]
    [InlineData("00:00:03", 3)]
    [InlineData("01:02:03", 3723)]
    [InlineData("02:30", 150)]
    [InlineData("45", 45)]
    [InlineData("00:00:00", 0)]
    [InlineData("", 0)]
    [InlineData("abc", 0)]
    [InlineData("00:-1:00", 0)]
    [InlineData("1:2:3:4", 0)]
    [InlineData("99:99:99", 362439)]
    public void TryParseInputProducesExpectedSeconds(string input, int expected)
    {
        var ok = TimerViewModel.TryParseInput(input, out var seconds);
        if (expected > 0)
        {
            Assert.True(ok);
            Assert.Equal(expected, seconds);
        }
        else
        {
            Assert.False(ok);
        }
    }

    // ------------------------------------------------------------- Spotlight

    [Fact]
    public void SelectionRectExcludesRectFromDimming()
    {
        var vm = new SpotlightViewModel();

        vm.BeginSelection(100, 100);
        vm.UpdateSelection(500, 400);
        vm.EndSelection(500, 400);

        var rect = Assert.IsType<SpotlightRect>(vm.SelectionRect);
        Assert.Equal(100, rect.Left);
        Assert.Equal(100, rect.Top);
        Assert.Equal(400, rect.Width);
        Assert.Equal(300, rect.Height);

        Assert.True(vm.IsDimmed(50, 50));    // outside, top-left
        Assert.False(vm.IsDimmed(300, 200)); // inside the rect
        Assert.True(vm.IsDimmed(600, 500));  // outside, bottom-right
    }

    [Fact]
    public void ClearSelectionRemovesHighlight()
    {
        var vm = new SpotlightViewModel();
        vm.BeginSelection(0, 0);
        vm.UpdateSelection(200, 200);
        vm.EndSelection(200, 200);

        vm.ClearSelection();

        Assert.Null(vm.SelectionRect);
        Assert.True(vm.IsDimmed(100, 100));
    }

    // ---------------------------------------------------------- App launcher

    [Fact]
    public void MissingPathReportsErrorAndStartsNothing()
    {
        var launcher = new CountingProcessLaunchService(path => false);
        var vm = new AppLauncherViewModel(processLauncher: launcher);
        vm.AddEntry("App", @"C:\definitely\missing\app.exe");
        vm.SelectedEntry = vm.Entries[0];

        vm.LaunchCommand.Execute(null);

        Assert.True(vm.HasError);
        Assert.Equal(0, launcher.StartCount);
    }

    [Fact]
    public void ValidPathStartsExactlyOnce()
    {
        var launcher = new CountingProcessLaunchService(path => true);
        var vm = new AppLauncherViewModel(processLauncher: launcher);
        vm.AddEntry("App", @"C:\tools\app.exe");
        vm.SelectedEntry = vm.Entries[0];

        vm.LaunchCommand.Execute(null);

        Assert.False(vm.HasError);
        Assert.Equal(1, launcher.StartCount);
        Assert.Equal(@"C:\tools\app.exe", launcher.LastPath);
    }

    [Fact]
    public void EmptyPathDoesNotStart()
    {
        var launcher = new CountingProcessLaunchService(path => true);
        var vm = new AppLauncherViewModel(processLauncher: launcher);

        vm.LaunchCommand.Execute(null);

        Assert.Equal(0, launcher.StartCount);
    }

    // ------------------------------------------------------------ Status bar

    [Fact]
    public async Task WithSlideShowsCurrentAndTotal()
    {
        var vm = new StatusBarViewModel(
            presentationStatus: new FakePresentationStatusSource(
                new PresentationStatusSnapshot(1, 5, true)),
            mediaStatus: new FakeMediaStatusSource(MediaStatusSnapshot.Empty));

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Contains("1", vm.PresentationText);
        Assert.Contains("5", vm.PresentationText);
    }

    [Fact]
    public async Task EmptyMediaShowsPlaceholderWithoutThrowing()
    {
        var vm = new StatusBarViewModel(
            presentationStatus: new FakePresentationStatusSource(PresentationStatusSnapshot.Empty),
            mediaStatus: new FakeMediaStatusSource(MediaStatusSnapshot.Empty));

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.False(string.IsNullOrWhiteSpace(vm.MediaText));
    }

    [Fact]
    public async Task WithMediaShowsTitleAndArtist()
    {
        var vm = new StatusBarViewModel(
            presentationStatus: new FakePresentationStatusSource(PresentationStatusSnapshot.Empty),
            mediaStatus: new FakeMediaStatusSource(new MediaStatusSnapshot("Some Song", "Some Artist", true)));

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Contains("Some Song", vm.MediaText);
        Assert.Contains("Some Artist", vm.MediaText);
    }

    [Fact]
    public void RefreshResumesOnTheCallingSynchronizationContext()
    {
        var context = new PumpSynchronizationContext();
        var presentation = new ControllablePresentationStatusSource();
        var media = new ControllableMediaStatusSource();
        var vm = new StatusBarViewModel(
            presentationStatus: presentation,
            mediaStatus: media);
        var notificationContexts = new List<SynchronizationContext?>();
        vm.PropertyChanged += (_, _) => notificationContexts.Add(SynchronizationContext.Current);
        var previousContext = SynchronizationContext.Current;

        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var refreshTask = vm.RefreshCommand.ExecuteAsync(null);

            Assert.False(refreshTask.IsCompleted);
            Assert.True(presentation.WasRequested);

            presentation.Completion.SetResult(new PresentationStatusSnapshot(2, 8, true));
            Assert.True(context.RunNext());
            Assert.True(media.WasRequested);

            media.Completion.SetResult(new MediaStatusSnapshot("Song", "Artist", true));
            Assert.True(context.RunNext());
            Assert.True(refreshTask.IsCompletedSuccessfully);

            Assert.NotEmpty(notificationContexts);
            Assert.All(notificationContexts, notificationContext => Assert.Same(context, notificationContext));
            Assert.Contains("2", vm.PresentationText);
            Assert.Contains("8", vm.PresentationText);
            Assert.Contains("Song", vm.MediaText);
            Assert.Contains("Artist", vm.MediaText);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    // ---------------------------------------------------------------- Fakes

    private sealed class FastClock : IClockService
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UnixEpoch;

        public int DelayCalls { get; private set; }

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            DelayCalls++;
            UtcNow = UtcNow.Add(delay);
            return Task.CompletedTask;
        }
    }

    private sealed class BlockingClock : IClockService
    {
        private readonly TaskCompletionSource _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UnixEpoch;

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            _entered.TrySetResult();
            return _release.Task.WaitAsync(cancellationToken);
        }

        public Task WaitUntilBlockedAsync() =>
            _entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class PauseResumeClock : IClockService
    {
        private readonly TaskCompletionSource _firstTickEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _firstTickRelease =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _secondTickEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _secondTickRelease =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UnixEpoch;

        public int DelayCalls { get; private set; }

        public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            DelayCalls++;
            UtcNow = UtcNow.Add(delay);

            if (DelayCalls == 1)
            {
                _firstTickEntered.TrySetResult();
                await _firstTickRelease.Task.WaitAsync(cancellationToken);
            }
            else if (DelayCalls == 2)
            {
                _secondTickEntered.TrySetResult();
                await _secondTickRelease.Task.WaitAsync(cancellationToken);
            }
        }

        public Task WaitUntilFirstTickAsync() =>
            _firstTickEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public void ReleaseFirstTick() => _firstTickRelease.TrySetResult();

        public Task WaitUntilSecondTickAsync() =>
            _secondTickEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public void ReleaseSecondTick() => _secondTickRelease.TrySetResult();
    }

    private sealed class CountingAudioCue : IAudioCueService
    {
        public int CallCount { get; private set; }

        public void PlayFinishedCue() => CallCount++;
    }

    private sealed class CountingNotificationCue : INotificationCueService
    {
        public int CallCount { get; private set; }

        public void ShowFinished(string? title, string? body) => CallCount++;
    }

    private sealed class CountingProcessLaunchService : IProcessLaunchService
    {
        private readonly Func<string, bool> _exists;

        public CountingProcessLaunchService(Func<string, bool> exists) => _exists = exists;

        public int StartCount { get; private set; }

        public string? LastPath { get; private set; }

        public PlatformOperationResult Launch(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !_exists(path))
            {
                return PlatformOperationResult.Failure(new PlatformOperationError(
                    PlatformOperationErrorCode.NotFound,
                    "Path not found"));
            }

            StartCount++;
            LastPath = path;
            return PlatformOperationResult.Success();
        }
    }

    private sealed class FakePresentationStatusSource : IPresentationStatusSource
    {
        private readonly PresentationStatusSnapshot _snapshot;

        public FakePresentationStatusSource(PresentationStatusSnapshot snapshot) =>
            _snapshot = snapshot;

        public Task<PresentationStatusSnapshot> GetStatusAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(_snapshot);
    }

    private sealed class FakeMediaStatusSource : IMediaStatusSource
    {
        private readonly MediaStatusSnapshot _snapshot;

        public FakeMediaStatusSource(MediaStatusSnapshot snapshot) => _snapshot = snapshot;

        public Task<MediaStatusSnapshot> GetStatusAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(_snapshot);
    }

    private sealed class ControllablePresentationStatusSource : IPresentationStatusSource
    {
        public TaskCompletionSource<PresentationStatusSnapshot> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool WasRequested { get; private set; }

        public Task<PresentationStatusSnapshot> GetStatusAsync(
            CancellationToken cancellationToken = default)
        {
            WasRequested = true;
            return Completion.Task;
        }
    }

    private sealed class ControllableMediaStatusSource : IMediaStatusSource
    {
        public TaskCompletionSource<MediaStatusSnapshot> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool WasRequested { get; private set; }

        public Task<MediaStatusSnapshot> GetStatusAsync(
            CancellationToken cancellationToken = default)
        {
            WasRequested = true;
            return Completion.Task;
        }
    }

    private sealed class PumpSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _work = new();

        public override void Post(SendOrPostCallback callback, object? state) =>
            _work.Enqueue((callback, state));

        public bool RunNext()
        {
            if (_work.Count == 0)
            {
                return false;
            }

            var (callback, state) = _work.Dequeue();
            callback(state);
            return true;
        }
    }
}
