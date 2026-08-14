using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Luminalium.App.Overlay;

public sealed class AnnotationCanvas : Control
{
    public static readonly StyledProperty<IReadOnlyList<StrokeModel>> StrokesProperty =
        AvaloniaProperty.Register<AnnotationCanvas, IReadOnlyList<StrokeModel>>(nameof(Strokes), []);

    public static readonly StyledProperty<IAnnotationSink?> AnnotationSinkProperty =
        AvaloniaProperty.Register<AnnotationCanvas, IAnnotationSink?>(nameof(AnnotationSink));

    public static readonly StyledProperty<bool> CanDrawProperty =
        AvaloniaProperty.Register<AnnotationCanvas, bool>(nameof(CanDraw));

    public static readonly StyledProperty<string> StrokeColorProperty =
        AvaloniaProperty.Register<AnnotationCanvas, string>(nameof(StrokeColor), "#FF0000");

    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<AnnotationCanvas, double>(nameof(StrokeWidth), 4.0);

    private readonly List<StrokePoint> _pendingPoints = [];
    private INotifyCollectionChanged? _observableStrokes;
    private bool _isDrawing;

    static AnnotationCanvas()
    {
        AffectsRender<AnnotationCanvas>(StrokesProperty);
        StrokesProperty.Changed.AddClassHandler<AnnotationCanvas>((canvas, args) => canvas.OnStrokesChanged(args));
    }

    public IReadOnlyList<StrokeModel> Strokes
    {
        get => GetValue(StrokesProperty);
        set => SetValue(StrokesProperty, value);
    }

    public IAnnotationSink? AnnotationSink
    {
        get => GetValue(AnnotationSinkProperty);
        set => SetValue(AnnotationSinkProperty, value);
    }

    public bool CanDraw
    {
        get => GetValue(CanDrawProperty);
        set => SetValue(CanDrawProperty, value);
    }

    public string StrokeColor
    {
        get => GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public double StrokeWidth
    {
        get => GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        foreach (var stroke in Strokes)
        {
            DrawStroke(context, stroke);
        }

        if (_pendingPoints.Count > 0 && CanDraw)
        {
            DrawStroke(context, StrokeModel.Create(StrokeColor, StrokeWidth, _pendingPoints));
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new ControlAutomationPeer(this);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!CanDraw || IsMouseWithoutPrimaryPress(e))
        {
            return;
        }

        _pendingPoints.Clear();
        AddPoint(e);
        _isDrawing = true;
        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isDrawing)
        {
            return;
        }

        AddPoint(e);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        FinishStroke(e.Pointer);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        FinishStroke(e.Pointer);
    }

    private void FinishStroke(IPointer pointer)
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        pointer.Capture(null);

        if (CanDraw && _pendingPoints.Count > 0)
        {
            AnnotationSink?.RecordStroke(StrokeModel.Create(StrokeColor, StrokeWidth, _pendingPoints));
        }

        _pendingPoints.Clear();
        InvalidateVisual();
    }

    private void AddPoint(PointerEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        var pressure = point.Properties.Pressure;
        _pendingPoints.Add(new StrokePoint(point.Position.X, point.Position.Y, pressure <= 0 ? 1.0 : pressure));
    }

    private static bool IsMouseWithoutPrimaryPress(PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(null);
        return point.Pointer.Type == PointerType.Mouse && !point.Properties.IsLeftButtonPressed;
    }

    private void OnStrokesChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (_observableStrokes is not null)
        {
            _observableStrokes.CollectionChanged -= OnStrokeCollectionChanged;
        }

        _observableStrokes = args.NewValue as INotifyCollectionChanged;
        if (_observableStrokes is not null)
        {
            _observableStrokes.CollectionChanged += OnStrokeCollectionChanged;
        }

        InvalidateVisual();
    }

    private void OnStrokeCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    private static void DrawStroke(DrawingContext context, StrokeModel stroke)
    {
        if (stroke.Points.Count == 0)
        {
            return;
        }

        var brush = new SolidColorBrush(Color.Parse(stroke.ColorHex));
        var pen = new Pen(brush, Math.Max(1, stroke.Thickness), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        if (stroke.Points.Count == 1)
        {
            var point = ToPoint(stroke.Points[0]);
            context.DrawEllipse(brush, null, point, stroke.Thickness / 2, stroke.Thickness / 2);
            return;
        }

        for (var index = 1; index < stroke.Points.Count; index++)
        {
            context.DrawLine(pen, ToPoint(stroke.Points[index - 1]), ToPoint(stroke.Points[index]));
        }
    }

    private static Point ToPoint(StrokePoint point) => new(point.X, point.Y);
}
