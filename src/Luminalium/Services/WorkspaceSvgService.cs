using DotNetCampus.Inking;
using SkiaSharp;

namespace Luminalium.Services;

public static class WorkspaceSvgService
{
    public static async Task<string?> ExportAsync(
        IReadOnlyList<SkiaStroke> strokes,
        double width,
        double height,
        string path,
        CancellationToken cancellationToken = default)
    {
        if (strokes.Count == 0)
        {
            return null;
        }

        try
        {
            using var stream = File.Create(path);
            using var canvas = SKSvgCanvas.Create(
                SKRect.Create(0, 0, MathF.Max(1, (float)width), MathF.Max(1, (float)height)), stream);
            using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };

            foreach (var stroke in strokes)
            {
                if (stroke.Path is null) continue;
                paint.Color = stroke.Color;
                canvas.DrawPath(stroke.Path, paint);
            }

            await stream.FlushAsync(cancellationToken);
            return path;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
