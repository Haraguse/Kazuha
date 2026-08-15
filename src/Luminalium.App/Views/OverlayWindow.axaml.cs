using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Luminalium.App.Overlay;
using Luminalium.App.ViewModels;
using Luminalium.Core.Localization;
using Luminalium.Presentation;
using System.Globalization;

namespace Luminalium.App.Views;

public partial class OverlayWindow : Window, IDisposable
{
    private readonly OverlayViewModel _viewModel;
    private readonly IScreenshotService _screenshotService;
    private readonly ILocalizationService _localization;
    private readonly WpsBridgeAutomationAdapter? _ownedAdapter;
    private bool _movingSpotlight;

    public OverlayWindow()
        : this(new LocalizationService())
    {
    }

    public OverlayWindow(ILocalizationService localization)
    {
        _localization = localization;
        InitializeComponent();
        ApplySystemDecorationsNone();
        _ownedAdapter = TryCreateWpsAdapter();
        _viewModel = CreateDefaultViewModel(_ownedAdapter, new AvaloniaOverlayScreenProvider(this), _localization);
        _screenshotService = new RenderTargetBitmapScreenshotService();
        AttachViewModel();
    }

    public OverlayWindow(OverlayViewModel viewModel, IScreenshotService? screenshotService = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _localization = viewModel.Localization;
        InitializeComponent();
        ApplySystemDecorationsNone();
        _viewModel = viewModel;
        _screenshotService = screenshotService ?? new RenderTargetBitmapScreenshotService();
        AttachViewModel();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Right:
            case Key.Space:
            case Key.PageDown:
                if (_viewModel.NextSlideCommand.CanExecute(null))
                {
                    _viewModel.NextSlideCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            case Key.Left:
            case Key.PageUp:
                if (_viewModel.PreviousSlideCommand.CanExecute(null))
                {
                    _viewModel.PreviousSlideCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            case Key.D:
                if (_viewModel.DrawCommand.CanExecute(null))
                {
                    _viewModel.DrawCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            case Key.S:
                if (_viewModel.SpotlightCommand.CanExecute(null))
                {
                    _viewModel.SpotlightCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            case Key.Z:
                if (_viewModel.ZoomCommand.CanExecute(null))
                {
                    _viewModel.ZoomCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.CloseRequested -= OnCloseRequested;
        Dispose();
        base.OnClosed(e);
    }

    public void Dispose()
    {
        _ownedAdapter?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static WpsBridgeAutomationAdapter? TryCreateWpsAdapter()
    {
        try
        {
            return new WpsBridgeAutomationAdapter();
        }
        catch (InvalidOperationException)
        {
            // Production WPS bridge requires LUMINALIUM_WPS_BRIDGE_TOKEN. Degrade
            // gracefully so the overlay can still open for annotation/screenshot use.
            return null;
        }
    }

    private static OverlayViewModel CreateDefaultViewModel(
        WpsBridgeAutomationAdapter? adapter,
        IOverlayScreenProvider screenProvider,
        ILocalizationService localization)
    {
        var hosts = adapter is not null
            ? new IPresentationHost[] { new WpsPresentationHost(adapter) }
            : Array.Empty<IPresentationHost>();
        var monitor = new PresentationMonitor(hosts, preferredKind: PresentationHostKind.Wps);
        return new OverlayViewModel(monitor, screenProvider, localizationService: localization);
    }

    private void ApplySystemDecorationsNone()
    {
        var property = GetType().BaseType?.GetProperty("SystemDecorations");
        if (property?.PropertyType.IsEnum == true)
        {
            property.SetValue(this, Enum.Parse(property.PropertyType, "None"));
        }
    }

    private void AttachViewModel()
    {
        DataContext = _viewModel;
        _viewModel.CloseRequested += OnCloseRequested;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(OverlayViewModel.IsSpotlightActive)
                or nameof(OverlayViewModel.SpotlightCenterX)
                or nameof(OverlayViewModel.SpotlightCenterY))
            {
                UpdateSpotlightMask();
            }
        };
        Opened += OnOpened;
        SizeChanged += (_, _) => UpdateSpotlightMask();
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        ApplyPlacement();
        UpdateSpotlightMask();
        await _viewModel.InitializeAsync().ConfigureAwait(true);
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();

    private async void OnScreenshotClicked(object? sender, RoutedEventArgs e)
    {
        if (!_viewModel.HasActiveSlideshow)
        {
            return;
        }

        try
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "Luminalium",
                "OverlayScreenshots",
                $"overlay-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}.png");
            var captured = await _screenshotService.CaptureAsync(InkCanvas, path).ConfigureAwait(true);
            _viewModel.ReportScreenshot(captured);
        }
        catch (Exception exception)
        {
            _viewModel.ReportScreenshotFailure(string.Format(CultureInfo.InvariantCulture, _localization["Overlay.Status.ScreenshotFailed"], exception.Message));
        }
    }

    private void OnSpotlightPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_viewModel.IsSpotlightActive)
        {
            return;
        }

        _movingSpotlight = true;
        UpdateSpotlightCenter(e);
        e.Pointer.Capture(SpotlightDim);
        e.Handled = true;
    }

    private void OnSpotlightPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_movingSpotlight || !_viewModel.IsSpotlightActive)
        {
            return;
        }

        UpdateSpotlightCenter(e);
        e.Handled = true;
    }

    private void OnSpotlightPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _movingSpotlight = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void ApplyPlacement()
    {
        var bounds = _viewModel.SelectedScreenBounds;
        Position = new PixelPoint(bounds.X, bounds.Y);
        Width = Math.Max(1, bounds.Width);
        Height = Math.Max(1, bounds.Height);
    }

    private void UpdateSpotlightCenter(PointerEventArgs e)
    {
        var position = e.GetPosition(SpotlightDim);
        _viewModel.SetSpotlightCenter(position.X, position.Y);
    }

    private void UpdateSpotlightMask()
    {
        if (!_viewModel.IsSpotlightActive || SpotlightDim.Bounds.Width <= 0 || SpotlightDim.Bounds.Height <= 0)
        {
            SpotlightDim.OpacityMask = null;
            return;
        }

        var center = new RelativePoint(
            _viewModel.SpotlightCenterX / SpotlightDim.Bounds.Width,
            _viewModel.SpotlightCenterY / SpotlightDim.Bounds.Height,
            RelativeUnit.Relative);
        var radius = Application.Current?.Resources["OverlaySpotlightRadius"] is double value ? value : 150.0;
        SpotlightDim.OpacityMask = new RadialGradientBrush
        {
            Center = center,
            GradientOrigin = center,
            RadiusX = new RelativeScalar(radius / SpotlightDim.Bounds.Width, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(radius / SpotlightDim.Bounds.Height, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Colors.Transparent, 0.0),
                new GradientStop(Colors.Transparent, 0.56),
                new GradientStop(Colors.Black, 0.64),
                new GradientStop(Colors.Black, 1.0),
            },
        };
    }

}
