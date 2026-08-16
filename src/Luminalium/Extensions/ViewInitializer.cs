using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Windowing;
using Luminalium.Controls;

namespace Luminalium.Extensions;

public partial class ViewInitializer : AvaloniaObject
{
    public static readonly AttachedProperty<ViewState?> StateProperty =
        AvaloniaProperty.RegisterAttached<ViewInitializer, Control, ViewState?>("State");
    public static void SetState(Control obj, ViewState? value) => obj.SetValue(StateProperty, value);
    public static ViewState? GetState(Control obj) => obj.GetValue(StateProperty);
    
    public static void InitializeView(Control control, bool useMica = true)
    {
        var state = new ViewState();
        SetState(control, state);

        if (control is FAAppWindow appWindow)
        {
            appWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(23, 0, 0, 0);
            appWindow.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(52, 0, 0, 0);
            appWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
        }
        
        TextOptions.SetTextRenderingMode(control, TextRenderingMode.Antialias);
        RenderOptions.SetBitmapInterpolationMode(control, BitmapInterpolationMode.HighQuality);
        RenderOptions.SetEdgeMode(control, EdgeMode.Antialias);

        control.Loaded += OnLoaded;
        return;
        
        void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (control is Window window && App.IsMicaSupported && useMica)
            {
                window.TransparencyLevelHint = [WindowTransparencyLevel.Mica];
                window.Background = Brushes.Transparent;
            }
            
            if (control is Window { Content: Visual visual })
            {
                AddAdorners(visual);
            }
            else
            {
                AddAdorners(control);
            }
        }

        void AddAdorners(Visual element)
        {
            if (state.IsAdornerAdded) return;

            var layer = AdornerLayer.GetAdornerLayer(element);
            var appToastAdorner = state.AppToastAdorner = new AppToastAdorner(control);
            layer?.Children.Add(appToastAdorner);
            AdornerLayer.SetAdornedElement(appToastAdorner, element);

            if (GlobalConstants.IsDevelopment)
            {
                var adorner = new DevelopmentBuildAdorner();
                layer?.Children.Add(adorner);
                AdornerLayer.SetAdornedElement(adorner, element);
            }

            state.IsAdornerAdded = true;
        }
    }
    
    public partial class ViewState : ObservableObject
    {
        [ObservableProperty] private bool _isAdornerAdded;
        [ObservableProperty] private AppToastAdorner? _appToastAdorner;
    }
}