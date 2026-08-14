using System.Text.Json;
using Luminalium.App.Overlay;

namespace Luminalium.App.Board;

public sealed record BoardSaveResult(bool IsSuccess, string? Path, string? Error)
{
    public static BoardSaveResult Success(string path) => new(true, path, null);

    public static BoardSaveResult NothingToSave() => new(false, null, "NothingToSave");

    public static BoardSaveResult Failure(string error) => new(false, null, error);
}

public sealed record BoardStrokeRecord(string Color, double Thickness, double[][] Points);

/// <summary>
/// Exports a board document as JSON stroke records. Empty documents produce a
/// typed NothingToSave result; IO/access/path failures produce typed failures
/// and never lose the in-memory strokes (this service never mutates the document).
/// </summary>
public static class BoardSaveService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static async Task<BoardSaveResult> SaveAsync(
        BoardDocument document,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (document.Count == 0)
        {
            return BoardSaveResult.NothingToSave();
        }

        try
        {
            var records = document.Strokes
                .Select(stroke => new BoardStrokeRecord(
                    stroke.ColorHex,
                    stroke.Thickness,
                    stroke.Points
                        .Select(point => new[] { point.X, point.Y, point.Pressure })
                        .ToArray()))
                .ToArray();

            var json = JsonSerializer.Serialize(records, SerializerOptions);
            await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
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
