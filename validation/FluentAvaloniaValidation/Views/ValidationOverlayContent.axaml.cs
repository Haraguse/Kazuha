using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace FluentAvaloniaValidation.Views;

public partial class ValidationOverlayContent : UserControl
{
    public event Action? CloseRequested;

    public ValidationOverlayContent()
    {
        InitializeComponent();
        CloseOverlayButton.Click += (_, _) => CloseRequested?.Invoke();
        DragBar.PointerPressed += OnDragBarPointerPressed;
    }

    private void OnDragBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.BeginMoveDrag(e);
        }
    }
}
