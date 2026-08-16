using DotNetCampus.Inking;
using SkiaSharp;

namespace Luminalium.App.Board;

public sealed record BoardSaveResult(bool IsSuccess, string? Path, string? Error)
{
    public static BoardSaveResult Success(string path) => new(true, path, null);

    public static BoardSaveResult NothingToSave() => new(false, null, "NothingToSave");

    public static BoardSaveResult Failure(string error) => new(false, null, error);
}

/// <summary>
/// Exports the board's collected strokes as a single vector SVG file. Empty
/// stroke sets produce a typed NothingToSave result; IO/access/path failures
/// produce typed failures and never mutate the source strokes.
/// </summary>
public static class BoardSaveService
{
    public static async Task<BoardSaveResult> SaveAsync(
        IReadOnlyList<SkiaStroke> strokes,
        double width,
        double height,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(strokes);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (strokes.Count == 0)
        {
            return BoardSaveResult.NothingToSave();
        }

        try
        {
            var bounds = SKRect.Create(0, 0, MathF.Max(1, (float)width), MathF.Max(1, (float)height));
            using var stream = File.Create(path);
            using var canvas = SKSvgCanvas.Create(bounds, stream);
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
            };

            foreach (var stroke in strokes)
            {
                if (stroke.Path is null)
                {
                    continue;
                }

                paint.Color = stroke.Color;
                canvas.DrawPath(stroke.Path, paint);
            }

            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            return BoardSaveResult.Success(path);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or DirectoryNotFoundException
            or NotSupportedException
            or ArgumentException)
        {
            return BoardSaveResult.Failure(exception.GetType().Name);
        }
    }
}