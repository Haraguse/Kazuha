using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FluentAvaloniaValidation.Views;

public partial class OverlayPage : UserControl
{
    private Window? _overlayWindow;
    private bool _initialized;

    public OverlayPage()
    {
        InitializeComponent();
        OpenOverlayWindow.Click += OnOpenOverlayWindowClicked;
        CloseOverlayWindow.Click += OnCloseOverlayWindowClicked;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_initialized) return;
        _initialized = true;

        if (TopLevel.GetTopLevel(this) is Window win)
        {
            UpdateDpiInfo(win);
            win.PositionChanged += (_, _) => UpdateDpiInfo(win);
        }
    }

    private void OnOpenOverlayWindowClicked(object? sender, RoutedEventArgs e)
    {
        if (_overlayWindow is not null) return;

        var overlay = new Window
        {
            Width = 480,
            Height = 340,
            Title = "ValidationOverlay",
            WindowDecorations = WindowDecorations.None,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
            Background = Avalonia.Media.Brushes.Transparent,
            ShowInTaskbar = false,
            Topmost = true,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        var content = new ValidationOverlayContent();
        content.CloseRequested += () => overlay.Close();
        overlay.Content = content;
        overlay.Closed += (_, _) =>
        {
            _overlayWindow = null;
            CloseOverlayWindow.IsEnabled = false;
        };

        _overlayWindow = overlay;
        CloseOverlayWindow.IsEnabled = true;
        overlay.Show();
    }

    private void OnCloseOverlayWindowClicked(object? sender, RoutedEventArgs e)
    {
        _overlayWindow?.Close();
    }

    private void UpdateDpiInfo(Window win)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"RenderScaling (window)   : {win.RenderScaling:F2}");
        sb.AppendLine($"DesktopScaling (window)  : {win.DesktopScaling:F2}");
        sb.AppendLine($"Window size / position   : {win.Width:F0} x {win.Height:F0} DIP  @ {win.Position}");

        var screens = win.Screens;
        if (screens is not null)
        {
            sb.AppendLine($"Monitor count            : {screens.All.Count}");
            var index = 0;
            foreach (var sc in screens.All)
            {
                // Screen.Bounds is a PixelRect in physical pixels; there is no Screen.PixelSize in Avalonia 12.
                sb.AppendLine($"  [{index++}] PhysicalBounds={sc.Bounds}  Work={sc.WorkingArea}  Scale={sc.Scaling:F2}");
            }

            var current = screens.ScreenFromWindow(win);
            if (current is not null)
            {
                sb.AppendLine($"Current monitor          : Bounds={current.Bounds}  Scale={current.Scaling:F2}");
            }
        }

        DpiText.Text = sb.ToString();
    }
}
