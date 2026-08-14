using System.Collections.ObjectModel;
using Luminalium.App.Overlay;

namespace Luminalium.App.Board;

/// <summary>
/// In-memory board drawing model: ordered strokes plus deterministic erase and
/// clear operations. Erase removes every stroke whose bounding box intersects
/// the eraser path's bounding box.
/// </summary>
public sealed class BoardDocument
{
    private readonly ObservableCollection<StrokeModel> _strokes = [];

    public IReadOnlyList<StrokeModel> Strokes => _strokes;

    public int Count => _strokes.Count;

    public void Add(StrokeModel stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        _strokes.Add(stroke);
    }

    public void Clear() => _strokes.Clear();

    public int Erase(IReadOnlyList<StrokePoint> eraserPoints)
    {
        ArgumentNullException.ThrowIfNull(eraserPoints);

        var eraserBounds = ComputeBounds(eraserPoints);
        if (eraserBounds is null)
        {
            return 0;
        }

        var removed = _strokes
            .Where(stroke => Intersects(stroke, eraserBounds.Value))
            .ToArray();

        foreach (var stroke in removed)
        {
            _strokes.Remove(stroke);
        }

        return removed.Length;
    }

    private static (double Left, double Top, double Right, double Bottom)? ComputeBounds(
        IReadOnlyList<StrokePoint> points)
    {
        if (points.Count == 0)
        {
            return null;
        }

        var left = double.MaxValue;
        var top = double.MaxValue;
        var right = double.MinValue;
        var bottom = double.MinValue;

        foreach (var point in points)
        {
            left = Math.Min(left, point.X);
            top = Math.Min(top, point.Y);
            right = Math.Max(right, point.X);
            bottom = Math.Max(bottom, point.Y);
        }

        return (left, top, right, bottom);
    }

    private static bool Intersects(
        StrokeModel stroke,
        (double Left, double Top, double Right, double Bottom) bounds)
    {
        var strokeBounds = ComputeBounds(stroke.Points);
        if (strokeBounds is null)
        {
            return false;
        }

        return strokeBounds.Value.Left <= bounds.Right &&
            strokeBounds.Value.Right >= bounds.Left &&
            strokeBounds.Value.Top <= bounds.Bottom &&
            strokeBounds.Value.Bottom >= bounds.Top;
    }
}
