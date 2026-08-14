using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Overlay;
using Luminalium.Presentation;

namespace Luminalium.App.ViewModels;

public sealed partial class OverlayViewModel : ObservableObject, IAnnotationSink
{
    public const string NoActiveSlideshowHelpText = "No active slideshow";
    private static readonly double[] ZoomStops = [1.0, 1.25, 1.5];

    private readonly PresentationMonitor _monitor;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextSlideCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousSlideCommand))]
    [NotifyCanExecuteChangedFor(nameof(DrawCommand))]
    [NotifyCanExecuteChangedFor(nameof(SpotlightCommand))]
    [NotifyCanExecuteChangedFor(nameof(ZoomCommand))]
    [NotifyPropertyChangedFor(nameof(HasNoActiveSlideshow))]
    [NotifyPropertyChangedFor(nameof(SlideshowCommandHelpText))]
    private bool hasActiveSlideshow;

    [ObservableProperty]
    private int currentSlide;

    [ObservableProperty]
    private int slideCount;

    [ObservableProperty]
    private bool isDrawingActive;

    [ObservableProperty]
    private bool isSpotlightActive;

    [ObservableProperty]
    private double zoomScale = 1.0;

    [ObservableProperty]
    private string penColor = "#FF0000";

    [ObservableProperty]
    private double strokeThickness = 4.0;

    [ObservableProperty]
    private double spotlightCenterX;

    [ObservableProperty]
    private double spotlightCenterY;

    [ObservableProperty]
    private string statusText = NoActiveSlideshowHelpText;

    [ObservableProperty]
    private string lastScreenshotPath = string.Empty;

    public OverlayViewModel(
        PresentationMonitor monitor,
        IOverlayScreenProvider screenProvider,
        int? selectedScreenIndex = null)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(screenProvider);

        _monitor = monitor;
        SelectedScreenBounds = NormalizeBounds(screenProvider.GetScreen(selectedScreenIndex));
        SpotlightCenterX = SelectedScreenBounds.Width / 2.0;
        SpotlightCenterY = SelectedScreenBounds.Height / 2.0;

        NextSlideCommand = new AsyncRelayCommand(NextSlideAsync, CanUseSlideshowCommand);
        PreviousSlideCommand = new AsyncRelayCommand(PreviousSlideAsync, CanUseSlideshowCommand);
        DrawCommand = new AsyncRelayCommand(ActivateDrawAsync, CanUseSlideshowCommand);
        SpotlightCommand = new RelayCommand(ToggleSpotlight, CanUseSlideshowCommand);
        ZoomCommand = new AsyncRelayCommand(ZoomAsync, CanUseSlideshowCommand);
        ClearCommand = new RelayCommand(ClearStrokes);
        CloseOverlayCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
    }

    public ObservableCollection<StrokeModel> Strokes { get; } = [];

    public OverlayScreenBounds SelectedScreenBounds { get; }

    public bool HasNoActiveSlideshow => !HasActiveSlideshow;

    public string SlideshowCommandHelpText => HasActiveSlideshow ? string.Empty : NoActiveSlideshowHelpText;

    public IAsyncRelayCommand NextSlideCommand { get; }

    public IAsyncRelayCommand PreviousSlideCommand { get; }

    public IAsyncRelayCommand DrawCommand { get; }

    public IRelayCommand SpotlightCommand { get; }

    public IAsyncRelayCommand ZoomCommand { get; }

    public IRelayCommand ClearCommand { get; }

    public IRelayCommand CloseOverlayCommand { get; }

    public event EventHandler? CloseRequested;

    public async Task InitializeAsync(CancellationToken cancellationToken = default) =>
        await RefreshStateAsync(cancellationToken).ConfigureAwait(true);

    public async Task RefreshStateAsync(CancellationToken cancellationToken = default)
    {
        var result = await _monitor.GetStateAsync(cancellationToken).ConfigureAwait(true);
        if (result.IsSuccess && result.Value.IsSlideShow)
        {
            ApplyState(result.Value);
            return;
        }

        ApplyUnavailable(result.Error?.Message ?? NoActiveSlideshowHelpText);
    }

    public async Task GotoSlideAsync(int slideIndex, CancellationToken cancellationToken = default)
    {
        if (!CanUseSlideshowCommand())
        {
            return;
        }

        var result = await _monitor.GotoSlideAsync(slideIndex, cancellationToken).ConfigureAwait(true);
        ApplyCommandResult(result);
    }

    public void RecordStroke(StrokeModel stroke)
    {
        if (!HasActiveSlideshow)
        {
            return;
        }

        Strokes.Add(StrokeModel.Create(stroke.ColorHex, stroke.Thickness, stroke.Points));
        StatusText = $"{Strokes.Count} annotation stroke(s)";
    }

    public void ClearStrokes()
    {
        Strokes.Clear();
        StatusText = HasActiveSlideshow ? BuildSlideStatus() : NoActiveSlideshowHelpText;
    }

    public void SetSpotlightCenter(double x, double y)
    {
        SpotlightCenterX = Math.Clamp(x, 0, SelectedScreenBounds.Width);
        SpotlightCenterY = Math.Clamp(y, 0, SelectedScreenBounds.Height);
    }

    public void ReportScreenshot(string filePath)
    {
        LastScreenshotPath = filePath;
        StatusText = "Screenshot captured";
    }

    public void ReportScreenshotFailure(string message) => StatusText = message;

    private async Task NextSlideAsync()
    {
        if (!CanUseSlideshowCommand())
        {
            return;
        }

        var result = await _monitor.NavigateNextAsync().ConfigureAwait(true);
        ApplyCommandResult(result);
    }

    private async Task PreviousSlideAsync()
    {
        if (!CanUseSlideshowCommand())
        {
            return;
        }

        var result = await _monitor.NavigatePreviousAsync().ConfigureAwait(true);
        ApplyCommandResult(result);
    }

    private async Task ActivateDrawAsync()
    {
        if (!CanUseSlideshowCommand())
        {
            return;
        }

        var result = await _monitor.SetPointerTypeAsync(PresentationPointerType.Pen).ConfigureAwait(true);
        if (result.IsSuccess)
        {
            IsDrawingActive = true;
            ApplyState(_monitor.CurrentState);
            return;
        }

        ApplyCommandResult(result);
    }

    private void ToggleSpotlight()
    {
        if (!CanUseSlideshowCommand())
        {
            return;
        }

        IsSpotlightActive = !IsSpotlightActive;
        StatusText = IsSpotlightActive ? "Spotlight active" : BuildSlideStatus();
    }

    private async Task ZoomAsync()
    {
        if (!CanUseSlideshowCommand())
        {
            return;
        }

        var nextScale = NextZoomScale();
        var result = await _monitor.ZoomAsync(nextScale).ConfigureAwait(true);
        if (result.IsSuccess || result.Error?.Code is PresentationErrorCode.Unsupported)
        {
            ZoomScale = nextScale;
            StatusText = $"Zoom {ZoomScale:0.##}x";
            return;
        }

        ApplyCommandResult(result);
    }

    private void ApplyCommandResult(PresentationOperationResult result)
    {
        if (result.IsSuccess)
        {
            ApplyState(_monitor.CurrentState);
            return;
        }

        if (result.Error?.Code is PresentationErrorCode.SlideshowClosed or PresentationErrorCode.HostUnavailable or PresentationErrorCode.DisconnectedHost)
        {
            ApplyUnavailable(result.Error.Message);
            return;
        }

        StatusText = result.Error?.Message ?? NoActiveSlideshowHelpText;
    }

    private void ApplyState(PresentationState state)
    {
        CurrentSlide = state.CurrentSlide;
        SlideCount = state.SlideCount;
        PenColor = StrokeModel.NormalizeColorHex(state.PenColor);
        HasActiveSlideshow = state.IsSlideShow;
        StatusText = HasActiveSlideshow ? BuildSlideStatus() : NoActiveSlideshowHelpText;
    }

    private void ApplyUnavailable(string message)
    {
        HasActiveSlideshow = false;
        IsDrawingActive = false;
        IsSpotlightActive = false;
        StatusText = string.IsNullOrWhiteSpace(message) ? NoActiveSlideshowHelpText : message;
    }

    private bool CanUseSlideshowCommand() => HasActiveSlideshow;

    private string BuildSlideStatus() => SlideCount > 0 ? $"Slide {CurrentSlide} / {SlideCount}" : "Slideshow active";

    private double NextZoomScale()
    {
        for (var index = 0; index < ZoomStops.Length; index++)
        {
            if (ZoomScale < ZoomStops[index] - 0.001)
            {
                return ZoomStops[index];
            }
        }

        return ZoomStops[0];
    }

    private static OverlayScreenBounds NormalizeBounds(OverlayScreenBounds bounds) =>
        bounds.IsEmpty ? new OverlayScreenBounds(0, 0, 1280, 720, 1.0) : bounds;
}
