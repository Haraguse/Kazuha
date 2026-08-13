using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FluentAvaloniaValidation.Views;

public partial class InputPage : UserControl
{
    private const int MaxLogLines = 16;
    private readonly List<string> _log = new();

    public InputPage()
    {
        InitializeComponent();
        InputSurface.PointerPressed += OnPointerPressed;
        InputSurface.PointerMoved += OnPointerMoved;
        InputSurface.PointerReleased += OnPointerReleased;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e) =>
        Log($"PRESS   {Describe(e)}");

    private void OnPointerMoved(object? sender, PointerEventArgs e) =>
        Log($"MOVE    {Describe(e)}");

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) =>
        Log($"RELEASE {Describe(e)}");

    private string Describe(PointerEventArgs e)
    {
        var pt = e.GetCurrentPoint(this);
        return $"{e.Pointer.Type,-9} @ {pt.Position.X:F0},{pt.Position.Y:F0}  update={pt.Properties.PointerUpdateKind}";
    }

    private void Log(string line)
    {
        _log.Add(line);
        if (_log.Count > MaxLogLines) _log.RemoveRange(0, _log.Count - MaxLogLines);
        EventLog.Text = string.Join("\n", _log);
        SurfaceHint.IsVisible = _log.Count == 0;
    }
}
