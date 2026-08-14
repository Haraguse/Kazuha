using Luminalium.App.Overlay;
using Luminalium.App.ViewModels;
using Luminalium.Presentation;
using Xunit;

namespace Luminalium.Tests;

public sealed class OverlayViewModelTests
{
    [Fact]
    public async Task FakeSlideshowConnectsNextAndGotoSlide()
    {
        var host = new FakePresentationHost();
        var viewModel = CreateViewModel(host);

        await viewModel.InitializeAsync();
        await viewModel.NextSlideCommand.ExecuteAsync(null);
        await viewModel.GotoSlideAsync(4);

        Assert.True(viewModel.HasActiveSlideshow);
        Assert.Equal(4, viewModel.CurrentSlide);
        Assert.Equal(5, viewModel.SlideCount);
        Assert.Equal(1, host.NextCalls);
        Assert.Equal(1, host.GotoCalls);
    }

    [Fact]
    public async Task OneStrokeModelCallRecordsExpectedColorAndPointsThenClearEmpties()
    {
        var viewModel = CreateViewModel(new FakePresentationHost());
        await viewModel.InitializeAsync();

        viewModel.RecordStroke(StrokeModel.Create(
            "#ff0000",
            4,
            [new StrokePoint(10, 20), new StrokePoint(30, 40, 0.5)]));

        var stroke = Assert.Single(viewModel.Strokes);
        Assert.Equal("#FF0000", stroke.ColorHex);
        Assert.Equal(4, stroke.Thickness);
        Assert.Equal([new StrokePoint(10, 20), new StrokePoint(30, 40, 0.5)], stroke.Points);

        viewModel.ClearCommand.Execute(null);

        Assert.Empty(viewModel.Strokes);
    }

    [Fact]
    public async Task ZoomTransitionsAreDeterministicAndDoNotChangePlacementModel()
    {
        var host = new FakePresentationHost();
        var viewModel = CreateViewModel(host, screenIndex: 1);
        var placement = viewModel.SelectedScreenBounds;

        await viewModel.InitializeAsync();
        await viewModel.ZoomCommand.ExecuteAsync(null);
        await viewModel.ZoomCommand.ExecuteAsync(null);
        await viewModel.ZoomCommand.ExecuteAsync(null);

        Assert.Equal([1.25, 1.5, 1.0], host.ZoomFactors);
        Assert.Equal(1.0, viewModel.ZoomScale);
        Assert.Equal(placement, viewModel.SelectedScreenBounds);
    }

    [Fact]
    public async Task NoSlideshowDisablesUnsafeCommandsAndSkipsMonitorCalls()
    {
        var host = new FakePresentationHost
        {
            State = PresentationState.Empty with
            {
                HostKind = PresentationHostKind.PowerPoint,
                IsSlideShow = false,
            },
        };
        var viewModel = CreateViewModel(host);

        await viewModel.InitializeAsync();
        host.ResetCommandCounts();

        Assert.False(viewModel.NextSlideCommand.CanExecute(null));
        Assert.False(viewModel.DrawCommand.CanExecute(null));
        Assert.False(viewModel.ZoomCommand.CanExecute(null));
        Assert.Equal(OverlayViewModel.NoActiveSlideshowHelpText, viewModel.SlideshowCommandHelpText);

        await viewModel.NextSlideCommand.ExecuteAsync(null);
        await viewModel.DrawCommand.ExecuteAsync(null);
        await viewModel.ZoomCommand.ExecuteAsync(null);

        Assert.Equal(0, host.NextCalls);
        Assert.Equal(0, host.PointerCalls);
        Assert.Equal(0, host.ZoomCalls);
    }

    [Fact]
    public void ScreenProviderSelectionPicksExpectedBounds()
    {
        var provider = new FixedScreenProvider([
            new OverlayScreenBounds(0, 0, 1024, 768, 1.0),
            new OverlayScreenBounds(1024, 0, 1920, 1080, 1.25),
        ]);

        var bounds = provider.GetScreen(1);
        var viewModel = CreateViewModel(new FakePresentationHost(), provider, 1);

        Assert.Equal(new OverlayScreenBounds(1024, 0, 1920, 1080, 1.25), bounds);
        Assert.Equal(bounds, viewModel.SelectedScreenBounds);
    }

    private static OverlayViewModel CreateViewModel(
        FakePresentationHost host,
        IOverlayScreenProvider? screenProvider = null,
        int? screenIndex = null) =>
        new(
            new PresentationMonitor([host], new AlwaysAllowCommandTimer()),
            screenProvider ?? new FixedScreenProvider([new OverlayScreenBounds(0, 0, 1280, 720, 1.0)]),
            screenIndex);

    private sealed class FakePresentationHost : IPresentationHost
    {
        private bool _connected;

        public PresentationHostKind HostKind => State.HostKind;

        public PresentationState State { get; set; } = new(
            1,
            5,
            true,
            PresentationPointerType.Arrow,
            "#FF0000",
            PresentationHostKind.PowerPoint,
            false,
            false);

        public int NextCalls { get; private set; }

        public int GotoCalls { get; private set; }

        public int PointerCalls { get; private set; }

        public int ZoomCalls { get; private set; }

        public List<double> ZoomFactors { get; } = [];

        public Task<PresentationOperationResult> ConnectAsync(CancellationToken cancellationToken = default)
        {
            _connected = true;
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult<PresentationState>> GetStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_connected
                ? PresentationOperation.Success(State)
                : PresentationOperation.Failure<PresentationState>(PresentationErrors.HostUnavailable("Fake host is not connected."), State));

        public Task<PresentationOperationResult> NavigateNextAsync(CancellationToken cancellationToken = default)
        {
            NextCalls++;
            State = State with { CurrentSlide = Math.Min(State.SlideCount, State.CurrentSlide + 1) };
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult> NavigatePreviousAsync(CancellationToken cancellationToken = default)
        {
            State = State with { CurrentSlide = Math.Max(1, State.CurrentSlide - 1) };
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult> GotoSlideAsync(int slideIndex, CancellationToken cancellationToken = default)
        {
            GotoCalls++;
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
            PointerCalls++;
            State = State with { PointerType = pointerType };
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public Task<PresentationOperationResult> ZoomAsync(double zoomFactor, CancellationToken cancellationToken = default)
        {
            ZoomCalls++;
            ZoomFactors.Add(zoomFactor);
            return Task.FromResult(PresentationOperationResult.Success());
        }

        public void ResetCommandCounts()
        {
            NextCalls = 0;
            GotoCalls = 0;
            PointerCalls = 0;
            ZoomCalls = 0;
            ZoomFactors.Clear();
        }
    }

    private sealed class AlwaysAllowCommandTimer : ICommandTimer
    {
        public PresentationOperationResult TryConsumePageTurn() => PresentationOperationResult.Success();
    }

    private sealed class FixedScreenProvider : IOverlayScreenProvider
    {
        private readonly IReadOnlyList<OverlayScreenBounds> _screens;

        public FixedScreenProvider(IReadOnlyList<OverlayScreenBounds> screens)
        {
            _screens = screens;
        }

        public IReadOnlyList<OverlayScreenBounds> GetScreens() => _screens;

        public OverlayScreenBounds GetScreen(int? screenIndex = null) =>
            screenIndex is int index && index >= 0 && index < _screens.Count ? _screens[index] : _screens[0];
    }
}
