using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Overlay;
using Luminalium.Core.Localization;
using Luminalium.Presentation;
using System.Globalization;

namespace Luminalium.App.ViewModels;

public sealed partial class OverlayViewModel : ObservableObject, IAnnotationSink
{
    public const string NoActiveSlideshowHelpText = "No active slideshow";
    private static readonly double[] ZoomStops = [1.0, 1.25, 1.5];

    private readonly ILocalizationService _localization;
    private readonly PresentationMonitor _monitor;
    private string? _statusKey = "Overlay.Status.NoActiveSlideshow";
    private object[] _statusArguments = [];
    private string? _externalStatusText;

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
    private string statusText = string.Empty;

    [ObservableProperty]
    private string lastScreenshotPath = string.Empty;

    public OverlayViewModel(
        PresentationMonitor monitor,
        IOverlayScreenProvider screenProvider,
        int? selectedScreenIndex = null,
        ILocalizationService? localizationService = null)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(screenProvider);

        _localization = localizationService ?? new LocalizationService();
        _monitor = monitor;
        SelectedScreenBounds = NormalizeBounds(screenProvider.GetScreen(selectedScreenIndex));
        SpotlightCenterX = SelectedScreenBounds.Width / 2.0;
        SpotlightCenterY = SelectedScreenBounds.Height / 2.0;
        StatusText = BuildStatusText();
        _localization.LanguageChanged += (_, _) => RefreshLocalizedText();

        NextSlideCommand = new AsyncRelayCommand(NextSlideAsync, CanUseSlideshowCommand);
        PreviousSlideCommand = new AsyncRelayCommand(PreviousSlideAsync, CanUseSlideshowCommand);
        DrawCommand = new AsyncRelayCommand(ActivateDrawAsync, CanUseSlideshowCommand);
        SpotlightCommand = new RelayCommand(ToggleSpotlight, CanUseSlideshowCommand);
        ZoomCommand = new AsyncRelayCommand(ZoomAsync, CanUseSlideshowCommand);
        ClearCommand = new RelayCommand(ClearStrokes);
        CloseOverlayCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
    }

    public ObservableCollection<StrokeModel> Strokes { get; } = [];

    public ILocalizationService Localization => _localization;

    public OverlayScreenBounds SelectedScreenBounds { get; }

    public bool HasNoActiveSlideshow => !HasActiveSlideshow;

    public string SlideshowCommandHelpText => HasActiveSlideshow ? string.Empty : _localization["Overlay.Status.NoActiveSlideshow"];

    public string NoActiveSlideshowTitle => _localization["Overlay.NoActiveSlideshow.Title"];

    public string NoActiveSlideshowDescription => _localization["Overlay.NoActiveSlideshow.Description"];

    public string PreviousSlideText => _localization["Overlay.Toolbar.Previous"];

    public string NextSlideText => _localization["Overlay.Toolbar.Next"];

    public string DrawText => _localization["Overlay.Toolbar.Draw"];

    public string SpotlightText => _localization["Overlay.Toolbar.Spotlight"];

    public string ZoomText => _localization["Overlay.Toolbar.Zoom"];

    public string ClearText => _localization["Overlay.Toolbar.Clear"];

    public string ClearHelpText => _localization["Overlay.Toolbar.Clear.HelpText"];

    public string ScreenshotText => _localization["Overlay.Toolbar.Screenshot"];

    public string CloseText => _localization["Overlay.Toolbar.Close"];

    public string CloseHelpText => _localization["Overlay.Toolbar.Close.HelpText"];

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
        SetStatus("Overlay.Status.AnnotationStrokes", Strokes.Count);
    }

    public void ClearStrokes()
    {
        Strokes.Clear();
        SetSlideOrUnavailableStatus();
    }

    public void SetSpotlightCenter(double x, double y)
    {
        SpotlightCenterX = Math.Clamp(x, 0, SelectedScreenBounds.Width);
        SpotlightCenterY = Math.Clamp(y, 0, SelectedScreenBounds.Height);
    }

    public void ReportScreenshot(string filePath)
    {
        LastScreenshotPath = filePath;
        SetStatus("Overlay.Status.ScreenshotCaptured");
    }

    public void ReportScreenshotFailure(string message) => SetExternalStatus(message);

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
        if (IsSpotlightActive)
        {
            SetStatus("Overlay.Status.SpotlightActive");
            return;
        }

        SetSlideOrUnavailableStatus();
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
            SetStatus("Overlay.Status.Zoom", ZoomScale.ToString("0.##", CultureInfo.InvariantCulture));
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

        SetExternalStatus(result.Error?.Message);
    }

    private void ApplyState(PresentationState state)
    {
        CurrentSlide = state.CurrentSlide;
        SlideCount = state.SlideCount;
        PenColor = StrokeModel.NormalizeColorHex(state.PenColor);
        HasActiveSlideshow = state.IsSlideShow;
        SetSlideOrUnavailableStatus();
    }

    private void ApplyUnavailable(string message)
    {
        HasActiveSlideshow = false;
        IsDrawingActive = false;
        IsSpotlightActive = false;
        SetExternalStatus(message);
    }

    private bool CanUseSlideshowCommand() => HasActiveSlideshow;

    private void SetSlideOrUnavailableStatus()
    {
        if (!HasActiveSlideshow)
        {
            SetStatus("Overlay.Status.NoActiveSlideshow");
            return;
        }

        if (SlideCount > 0)
        {
            SetStatus("Overlay.Status.Slide", CurrentSlide, SlideCount);
            return;
        }

        SetStatus("Overlay.Status.SlideshowActive");
    }

    private void SetStatus(string key, params object[] arguments)
    {
        _statusKey = key;
        _statusArguments = arguments;
        _externalStatusText = null;
        StatusText = BuildStatusText();
    }

    private void SetExternalStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || StringComparer.Ordinal.Equals(status, NoActiveSlideshowHelpText))
        {
            SetStatus("Overlay.Status.NoActiveSlideshow");
            return;
        }

        _statusKey = null;
        _statusArguments = [];
        _externalStatusText = status;
        StatusText = BuildStatusText();
    }

    private string BuildStatusText()
    {
        if (_statusKey is null)
        {
            return _externalStatusText ?? _localization["Overlay.Status.NoActiveSlideshow"];
        }

        var template = _localization[_statusKey];
        return _statusArguments.Length == 0 ? template : string.Format(CultureInfo.InvariantCulture, template, _statusArguments);
    }

    private void RefreshLocalizedText()
    {
        StatusText = BuildStatusText();
        OnPropertyChanged(nameof(SlideshowCommandHelpText));
        OnPropertyChanged(nameof(NoActiveSlideshowTitle));
        OnPropertyChanged(nameof(NoActiveSlideshowDescription));
        OnPropertyChanged(nameof(PreviousSlideText));
        OnPropertyChanged(nameof(NextSlideText));
        OnPropertyChanged(nameof(DrawText));
        OnPropertyChanged(nameof(SpotlightText));
        OnPropertyChanged(nameof(ZoomText));
        OnPropertyChanged(nameof(ClearText));
        OnPropertyChanged(nameof(ClearHelpText));
        OnPropertyChanged(nameof(ScreenshotText));
        OnPropertyChanged(nameof(CloseText));
        OnPropertyChanged(nameof(CloseHelpText));
    }

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
