using System.Diagnostics;
using Luminalium.App.Overlay;

namespace Luminalium.App.Board;

/// <summary>
/// Deterministic board load/save benchmark: builds a synthetic document and
/// measures a JSON round-trip. Records a baseline number only - no perf
/// assertions live in production code.
/// </summary>
public static class BoardBenchmark
{
    public static TimeSpan Run(int strokeCount = 500, int pointsPerStroke = 64)
    {
        var stopwatch = Stopwatch.StartNew();

        var document = new BoardDocument();
        for (var strokeIndex = 0; strokeIndex < strokeCount; strokeIndex++)
        {
            var points = new StrokePoint[pointsPerStroke];
            for (var pointIndex = 0; pointIndex < pointsPerStroke; pointIndex++)
            {
                points[pointIndex] = new StrokePoint(pointIndex, (strokeIndex + pointIndex) % 480);
            }

            document.Add(StrokeModel.Create("#FF0000", 4.0, points));
        }

        var records = document.Strokes
            .Select(stroke => new BoardStrokeRecord(
                stroke.ColorHex,
                stroke.Thickness,
                stroke.Points.Select(point => new[] { point.X, point.Y, point.Pressure }).ToArray()))
            .ToArray();
        _ = System.Text.Json.JsonSerializer.Serialize(records);

        stopwatch.Stop();
        return stopwatch.Elapsed;
    }
}
