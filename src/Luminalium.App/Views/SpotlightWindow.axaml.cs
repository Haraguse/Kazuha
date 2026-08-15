using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Luminalium.App.Overlay;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

/// <summary>
/// Full-screen dim with a rectangular "hole": the dim layer is a black border
/// whose opacity mask is a drawing with even-odd fill, so the selected
/// rectangle stays undimmed while everything else is dimmed.
/// </summary>
public partial class SpotlightWindow : Window
{
    private readonly SpotlightViewModel _viewModel;
    private readonly IOverlayScreenProvider _screenProvider;
    private bool _selecting;

    public SpotlightWindow()
        : this(new SpotlightViewModel())
    {
    }

    public SpotlightWindow(SpotlightViewModel viewModel, IOverlayScreenProvider? screenProvider = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        InitializeComponent();
        _screenProvider = screenProvider ?? new AvaloniaOverlayScreenProvider(this);
        DataContext = viewModel;

        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(SpotlightViewModel.HasSelection)
                or nameof(SpotlightViewModel.SelectionX1)
                or nameof(SpotlightViewModel.SelectionY1)
                or nameof(SpotlightViewModel.SelectionX2)
                or nameof(SpotlightViewModel.SelectionY2))
            {
                UpdateDimMask();
                UpdateOutline();
            }
        };

        Opened += OnOpened;
        SizeChanged += (_, _) =>
        {
            UpdateDimMask();
            UpdateOutline();
        };
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        ApplyPlacement();
        UpdateDimMask();
        UpdateOutline();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
    }

    private void OnSpotlightPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(SpotlightDim).Properties.IsLeftButtonPressed)
        {
            _selecting = true;
            var position = e.GetPosition(SpotlightDim);
            _viewModel.BeginSelection(position.X, position.Y);
            e.Pointer.Capture(SpotlightDim);
            e.Handled = true;
        }
    }

    private void OnSpotlightPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_selecting)
        {
            return;
        }

        var position = e.GetPosition(SpotlightDim);
        _viewModel.UpdateSelection(position.X, position.Y);
        e.Handled = true;
    }

    private void OnSpotlightPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_selecting)
        {
            return;
        }

        _selecting = false;
        var position = e.GetPosition(SpotlightDim);
        _viewModel.EndSelection(position.X, position.Y);
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnCloseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void ApplyPlacement()
    {
        var bounds = _screenProvider.GetScreen();
        Position = new PixelPoint(bounds.X, bounds.Y);
        Width = Math.Max(1, bounds.Width);
        Height = Math.Max(1, bounds.Height);
    }

    private void UpdateDimMask()
    {
        if (SpotlightDim.Bounds.Width <= 0 || SpotlightDim.Bounds.Height <= 0)
        {
            SpotlightDim.OpacityMask = null;
            return;
        }

        var rect = _viewModel.SelectionRect;
        var outer = new Rect(0, 0, SpotlightDim.Bounds.Width, SpotlightDim.Bounds.Height);

        if (rect is null)
        {
            SpotlightDim.OpacityMask = new DrawingBrush
            {
                Stretch = Stretch.None,
                Drawing = new GeometryDrawing
                {
                    Geometry = new RectangleGeometry(outer),
                    Brush = Brushes.Black,
                },
            };
            return;
        }

        var geometry = new GeometryGroup { FillRule = FillRule.EvenOdd };
        geometry.Children.Add(new RectangleGeometry(outer));
        geometry.Children.Add(new RectangleGeometry(
            new Rect(rect.Left, rect.Top, rect.Width, rect.Height)));

        SpotlightDim.OpacityMask = new DrawingBrush
        {
            Stretch = Stretch.None,
            Drawing = new GeometryDrawing
            {
                Geometry = geometry,
                Brush = Brushes.Black,
            },
        };
    }

    private void UpdateOutline()
    {
        var rect = _viewModel.SelectionRect;
        if (rect is null)
        {
            SpotlightHoleOutline.IsVisible = false;
            return;
        }

        SpotlightHoleOutline.IsVisible = true;
        Canvas.SetLeft(SpotlightHoleOutline, rect.Left);
        Canvas.SetTop(SpotlightHoleOutline, rect.Top);
        SpotlightHoleOutline.Width = rect.Width;
        SpotlightHoleOutline.Height = rect.Height;
    }
}
