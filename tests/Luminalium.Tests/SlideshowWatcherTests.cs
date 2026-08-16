using Luminalium.App.Services;
using Luminalium.Presentation;
using Xunit;

namespace Luminalium.Tests;

public sealed class SlideshowWatcherTests
{
    [Fact]
    public async Task OpeningTransitionInvokesOpenOverlayOnceUntilInactive()
    {
        var adapter = new StubSlideshowWindowAdapter();
        var calls = 0;
        var watcher = new SlideshowWatcher(
            adapter,
            openOverlay: () => calls++,
            pollInterval: TimeSpan.FromMilliseconds(1));

        adapter.Active = false;
        await watcher.PollOnceAsync();
        Assert.Equal(0, calls);

        adapter.Active = true;
        await watcher.PollOnceAsync();
        Assert.Equal(1, calls);

        await watcher.PollOnceAsync();
        Assert.Equal(1, calls);

        adapter.Active = false;
        await watcher.PollOnceAsync();
        Assert.Equal(1, calls);

        adapter.Active = true;
        await watcher.PollOnceAsync();
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task FailedPollDoesNotTerminateLoop()
    {
        var adapter = new StubSlideshowWindowAdapter { FailNext = true };
        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var watcher = new SlideshowWatcher(
            adapter,
            openOverlay: () => fired.TrySetResult(),
            pollInterval: TimeSpan.FromMilliseconds(1));

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var watcherHolder = watcher;
        watcher.Start();

        // First poll fails (adapter throws), the loop must survive and keep polling.
        adapter.Active = true;
        await fired.Task.WaitAsync(timeout.Token);
    }

    private sealed class StubSlideshowWindowAdapter : ISlideshowWindowAdapter
    {
        public bool Active { get; set; }
        public bool FailNext { get; set; }

        public Task<PresentationOperationResult<SlideshowWindowInfo?>> FindSlideshowWindowAsync(
            PresentationHostKind? preferredKind = null,
            CancellationToken cancellationToken = default)
        {
            if (FailNext)
            {
                FailNext = false;
                throw new InvalidOperationException("simulated poll failure");
            }

            var info = Active
                ? new SlideshowWindowInfo(1, PresentationHostKind.PowerPoint, "screenClass", "powerpnt.exe", "slide show", default)
                : null;
            return Task.FromResult(PresentationOperation.Success<SlideshowWindowInfo?>(info));
        }

        public Task<PresentationOperationResult<PresentationWindowRect>> GetWindowRectAsync(nint hwnd, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PresentationOperationResult> PostKeyAsync(nint hwnd, PresentationVirtualKey virtualKey, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PresentationOperationResult> PostCtrlShortcutAsync(nint hwnd, PresentationVirtualKey virtualKey, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PresentationOperationResult> FocusAsync(nint hwnd, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
