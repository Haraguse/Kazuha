namespace Luminalium.App.Overlay;

public interface IAnnotationSink
{
    void RecordStroke(StrokeModel stroke);

    void ClearStrokes();
}
