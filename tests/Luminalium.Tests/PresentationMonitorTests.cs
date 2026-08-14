using System.Runtime.InteropServices;
using Luminalium.Presentation;
using Xunit;

namespace Luminalium.Tests;

public sealed class PresentationMonitorTests
{
    [Fact]
    public async Task HappyPathConnectsNavigatesAnnotatesZoomsAndReturnsExactState()
    {
        var host = new FakePresentationHost();
        var monitor = new PresentationMonitor([host], new AlwaysAllowCommandTimer());

        Assert.True((await monitor.ConnectAsync()).IsSuccess);
        Assert.True((await monitor.NavigateNextAsync()).IsSuccess);
        Assert.True((await monitor.NavigatePreviousAsync()).IsSuccess);
        Assert.True((await monitor.GotoSlideAsync(4)).IsSuccess);
        Assert.True((await monitor.ApplyPenColorAsync("#ff0000")).IsSuccess);
        Assert.True((await monitor.SetPointerTypeAsync(PresentationPointerType.Pen)).IsSuccess);
        Assert.True((await monitor.ZoomAsync(1.25)).IsSuccess);

        var state = (await monitor.GetStateAsync()).Value;
        Assert.Equal(new PresentationState(
            4,
            5,
            true,
            PresentationPointerType.Pen,
            "#FF0000",
            PresentationHostKind.PowerPoint,
            false,
            false), state);
        Assert.Equal(1.25, host.ZoomFactor);
    }

    [Fact]
    public async Task NavigationRateLimitingReturnsTypedCommandRejectedResult()
    {
        var timer = new BlockingCommandTimer();
        var monitor = new PresentationMonitor([new FakePresentationHost()], timer);

        Assert.True((await monitor.ConnectAsync()).IsSuccess);
        Assert.True((await monitor.NavigateNextAsync()).IsSuccess);
        var throttled = await monitor.NavigateNextAsync();

        Assert.False(throttled.IsSuccess);
        Assert.Equal(PresentationErrorCode.CommandRejected, throttled.Error!.Code);
        Assert.Contains("throttled", throttled.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExternalExceptionsMapToTypedPresentationErrors()
    {
        var comTimeout = await new PresentationMonitor([
            new ThrowingPresentationHost(nextException: Marshal.GetExceptionForHR(unchecked((int)0x8001010A))!)],
            new AlwaysAllowCommandTimer()).NavigateNextAsync();
        Assert.False(comTimeout.IsSuccess);
        Assert.Equal(PresentationErrorCode.ComTimeout, comTimeout.Error!.Code);

        var bitness = await new PresentationMonitor([
            new ThrowingPresentationHost(connectException: new TypeLoadException("COM type load failed"))],
            new AlwaysAllowCommandTimer()).ConnectAsync();
        Assert.False(bitness.IsSuccess);
        Assert.Equal(PresentationErrorCode.BitnessMismatch, bitness.Error!.Code);

        var unavailable = await new PresentationMonitor([
            new FakePresentationHost(connectError: PresentationErrors.HostUnavailable("No Office host"))],
            new AlwaysAllowCommandTimer()).ConnectAsync();
        Assert.False(unavailable.IsSuccess);
        Assert.Equal(PresentationErrorCode.HostUnavailable, unavailable.Error!.Code);
    }

    [Fact]
    public async Task SlideshowClosedMidOperationReturnsTypedClosedError()
    {
        var host = new FakePresentationHost { CloseOnNext = true };
        var monitor = new PresentationMonitor([host], new AlwaysAllowCommandTimer());

        Assert.True((await monitor.ConnectAsync()).IsSuccess);
        var result = await monitor.NavigateNextAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(PresentationErrorCode.SlideshowClosed, result.Error!.Code);
    }

    [Fact]
    public async Task ProtectedViewSetsStateFlagsAndRestrictsNavigationWithoutCrash()
    {
        var host = new FakePresentationHost
        {
            State = new PresentationState(
                1,
                5,
                true,
                PresentationPointerType.Arrow,
                "#000000",
                PresentationHostKind.PowerPoint,
                true,
                true),
        };
        var monitor = new PresentationMonitor([host], new AlwaysAllowCommandTimer());

        Assert.True((await monitor.ConnectAsync()).IsSuccess);
        var result = await monitor.NavigateNextAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(PresentationErrorCode.ProtectedViewRestricted, result.Error!.Code);
        Assert.True(monitor.CurrentState.IsProtectedView);
        Assert.True(monitor.CurrentState.IsReadOnly);
    }

    [Fact]
    public async Task NavigationRestoresPenPointerAfterHostResetsIt()
    {
        var host = new FakePresentationHost { ResetPointerOnNavigation = true };
        var monitor = new PresentationMonitor([host], new AlwaysAllowCommandTimer());

        Assert.True((await monitor.ConnectAsync()).IsSuccess);
        Assert.True((await monitor.SetPointerTypeAsync(PresentationPointerType.Pen)).IsSuccess);
        Assert.True((await monitor.NavigateNextAsync()).IsSuccess);

        Assert.Equal(PresentationPointerType.Pen, monitor.CurrentState.PointerType);
        Assert.Equal(PresentationPointerType.Pen, host.State.PointerType);
        Assert.Equal(2, host.PointerSetCalls);
    }

    [Fact]
    public async Task PowerPointHostUsesWin32FallbackWhenComIsUnavailable()
    {
        var comAdapter = new FakeOfficeComAdapter
        {
            ConnectResult = PresentationOperation.Failure<SlideshowConnection>(PresentationErrors.HostUnavailable("No COM")),
        };
        var windowAdapter = new FakeSlideshowWindowAdapter();
        var host = new PowerPointPresentationHost(comAdapter, windowAdapter);

        Assert.True((await host.ConnectAsync()).IsSuccess);
        Assert.True((await host.NavigateNextAsync()).IsSuccess);
        var state = await host.GetStateAsync();

        Assert.True(state.IsSuccess);
        Assert.True(state.Value.IsSlideShow);
        Assert.Equal(PresentationHostKind.PowerPoint, state.Value.HostKind);
        Assert.Equal([PresentationVirtualKey.Down], windowAdapter.PostedKeys);
    }

    private sealed class FakePresentationHost : IPresentationHost
    {
        private readonly PresentationError? _connectError;
        private bool _connected;

        public FakePresentationHost(PresentationError? connectError = null)
        {
            _connectError = connectError;
        }

        public PresentationHostKind HostKind => State.HostKind;

        public PresentationState State { get; set; } = new(
            1,
            5,
            true,
            PresentationPointerType.Arrow,
            "#000000",
            PresentationHostKind.PowerPoint,
            false,
            false);

        public double ZoomFactor { get; private set; } = 1.0;

        public bool CloseOnNext { get; set; }

        public bool ResetPointerOnNavigation { get; set; }

        public int PointerSetCalls { get; private set; }

        public Task<PresentationOperationResult> ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_connectError is not null)
            {
                return Task.FromResult(PresentationOperationResult.Failure(_connectError));
            }

            _connected = true;
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult<PresentationState>> GetStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_connected
                ? PresentationOperation.Success(State)
                : PresentationOperation.Failure<PresentationState>(PresentationErrors.HostUnavailable("Fake host is not connected."), State));

        public Task<PresentationOperationResult> NavigateNextAsync(CancellationToken cancellationToken = default)
        {
            if (CloseOnNext)
            {
                State = State with { IsSlideShow = false };
                return Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.SlideshowClosed()));
            }

            State = State with
            {
                CurrentSlide = Math.Min(State.SlideCount, State.CurrentSlide + 1),
                PointerType = ResetPointerOnNavigation ? PresentationPointerType.Arrow : State.PointerType,
            };
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult> NavigatePreviousAsync(CancellationToken cancellationToken = default)
        {
            State = State with { CurrentSlide = Math.Max(1, State.CurrentSlide - 1) };
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult> GotoSlideAsync(int slideIndex, CancellationToken cancellationToken = default)
        {
            if (slideIndex < 1 || slideIndex > State.SlideCount)
            {
                return Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.CommandRejected("Slide out of range.")));
            }

            State = State with { CurrentSlide = slideIndex };
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult> ApplyPenColorAsync(string colorHex, CancellationToken cancellationToken = default)
        {
            State = State with { PenColor = PresentationMonitor.NormalizeColorHex(colorHex) };
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult> SetPointerTypeAsync(PresentationPointerType pointerType, CancellationToken cancellationToken = default)
        {
            PointerSetCalls++;
            State = State with { PointerType = pointerType };
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult> ZoomAsync(double zoomFactor, CancellationToken cancellationToken = default)
        {
            ZoomFactor = zoomFactor;
            return Task.FromResult(PresentationOperationResult.Success());
        }
    }

    private sealed class ThrowingPresentationHost : IPresentationHost
    {
        private readonly Exception? _connectException;
        private readonly Exception? _nextException;

        public ThrowingPresentationHost(Exception? connectException = null, Exception? nextException = null)
        {
            _connectException = connectException;
            _nextException = nextException;
        }

        public PresentationHostKind HostKind => PresentationHostKind.PowerPoint;

        public Task<PresentationOperationResult> ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_connectException is not null)
            {
                throw _connectException;
            }

            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult<PresentationState>> GetStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperation.Success(PresentationState.Empty with
            {
                CurrentSlide = 1,
                SlideCount = 5,
                IsSlideShow = true,
                HostKind = PresentationHostKind.PowerPoint,
            }));

        public Task<PresentationOperationResult> NavigateNextAsync(CancellationToken cancellationToken = default)
        {
            if (_nextException is not null)
            {
                throw _nextException;
            }

            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult> NavigatePreviousAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());

        public Task<PresentationOperationResult> GotoSlideAsync(int slideIndex, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());

        public Task<PresentationOperationResult> ApplyPenColorAsync(string colorHex, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());

        public Task<PresentationOperationResult> SetPointerTypeAsync(PresentationPointerType pointerType, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());

        public Task<PresentationOperationResult> ZoomAsync(double zoomFactor, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());
    }

    private sealed class AlwaysAllowCommandTimer : ICommandTimer
    {
        public PresentationOperationResult TryConsumePageTurn() => PresentationOperationResult.Success();
    }

    private sealed class BlockingCommandTimer : ICommandTimer
    {
        private bool _used;

        public PresentationOperationResult TryConsumePageTurn()
        {
            if (_used)
            {
                return PresentationOperationResult.Failure(PresentationErrors.CommandRejected("The presentation page-turn command was throttled."));
            }

            _used = true;
            return PresentationOperationResult.Success();
        }
    }

    private sealed class FakeOfficeComAdapter : IOfficeComAdapter
    {
        public PresentationOperationResult<SlideshowConnection> ConnectResult { get; set; } =
            PresentationOperation.Success(new SlideshowConnection(new object(), 123, PresentationHostKind.PowerPoint));

        public Task<PresentationOperationResult<SlideshowConnection>> ConnectPowerPointAsync(PresentationHostKind? preferredKind = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(ConnectResult);

        public Task<PresentationOperationResult<PresentationState>> GetStateAsync(SlideshowConnection connection, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperation.Success(PresentationState.Empty with
            {
                CurrentSlide = 1,
                SlideCount = 5,
                IsSlideShow = true,
                HostKind = PresentationHostKind.PowerPoint,
            }));

        public Task<PresentationOperationResult> NavigateNextAsync(SlideshowConnection connection, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.HostUnavailable("No COM")));

        public Task<PresentationOperationResult> NavigatePreviousAsync(SlideshowConnection connection, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.HostUnavailable("No COM")));

        public Task<PresentationOperationResult> GotoSlideAsync(SlideshowConnection connection, int slideIndex, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.HostUnavailable("No COM")));

        public Task<PresentationOperationResult> ApplyPenColorAsync(SlideshowConnection connection, string colorHex, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.HostUnavailable("No COM")));

        public Task<PresentationOperationResult> ApplyPenColorViaPaletteAsync(SlideshowConnection connection, string colorHex, ISlideshowWindowAdapter windowAdapter, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.HostUnavailable("No COM")));

        public Task<PresentationOperationResult> SetPointerTypeAsync(SlideshowConnection connection, PresentationPointerType pointerType, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.HostUnavailable("No COM")));
    }

    private sealed class FakeSlideshowWindowAdapter : ISlideshowWindowAdapter
    {
        private readonly SlideshowWindowInfo _window = new(
            123,
            PresentationHostKind.PowerPoint,
            "screenClass",
            "powerpnt.exe",
            "Slide Show - Deck",
            new PresentationWindowRect(10, 20, 1280, 720));

        public List<PresentationVirtualKey> PostedKeys { get; } = [];

        public Task<PresentationOperationResult<SlideshowWindowInfo?>> FindSlideshowWindowAsync(PresentationHostKind? preferredKind = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperation.Success<SlideshowWindowInfo?>(_window));

        public Task<PresentationOperationResult<PresentationWindowRect>> GetWindowRectAsync(nint hwnd, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperation.Success(_window.Rect));

        public Task<PresentationOperationResult> PostKeyAsync(nint hwnd, PresentationVirtualKey virtualKey, CancellationToken cancellationToken = default)
        {
            PostedKeys.Add(virtualKey);
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult> PostCtrlShortcutAsync(nint hwnd, PresentationVirtualKey virtualKey, CancellationToken cancellationToken = default)
        {
            PostedKeys.Add(virtualKey);
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult> FocusAsync(nint hwnd, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());
    }
}
