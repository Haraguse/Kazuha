using System.Text.Json;
using Luminalium.App.Board;
using Luminalium.App.Overlay;
using Luminalium.App.ViewModels;
using Xunit;

namespace Luminalium.Tests;

public sealed class BoardTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"Luminalium-Board-{Guid.NewGuid():N}");

    public BoardTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public async Task SaveRoundTripPreservesThreeRedStrokes()
    {
        var document = CreateDocument(3, "#FF0000");
        var path = Path.Combine(_directory, "board.json");

        var result = await BoardSaveService.SaveAsync(document, path);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(path));

        var records = JsonSerializer.Deserialize<BoardStrokeRecord[]>(await File.ReadAllTextAsync(path));
        Assert.NotNull(records);
        Assert.Equal(3, records!.Length);
        Assert.All(records, record => Assert.Equal("#FF0000", record.Color));
        Assert.All(records, record => Assert.NotEmpty(record.Points));
    }

    [Fact]
    public async Task EmptyBoardSaveReturnsNothingToSaveAndWritesNoFile()
    {
        var path = Path.Combine(_directory, "empty.json");

        var result = await BoardSaveService.SaveAsync(new BoardDocument(), path);

        Assert.False(result.IsSuccess);
        Assert.Equal("NothingToSave", result.Error);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task InvalidSavePathFailsWithoutLosingStrokes()
    {
        var document = CreateDocument(3, "#FF0000");
        var path = Path.Combine(_directory, "missing-dir", "board.json");

        var result = await BoardSaveService.SaveAsync(document, path);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(3, document.Count);
    }

    [Fact]
    public void EraseRemovesOnlyIntersectingStrokes()
    {
        var document = new BoardDocument();
        document.Add(StrokeAt("#FF0000", 10, 10));   // inside eraser path
        document.Add(StrokeAt("#00FF00", 300, 300)); // outside eraser path
        document.Add(StrokeAt("#0000FF", 20, 20));   // inside eraser path

        var removed = document.Erase(
        [
            new StrokePoint(0, 0),
            new StrokePoint(100, 100),
        ]);

        Assert.Equal(2, removed);
        Assert.Equal(1, document.Count);
        Assert.Equal("#00FF00", document.Strokes[0].ColorHex);
    }

    [Fact]
    public void ClearEmptiesDeterministically()
    {
        var document = CreateDocument(5, "#FF0000");

        document.Clear();

        Assert.Equal(0, document.Count);
    }

    [Fact]
    public void ViewModelRecordsPenAndEraserGestures()
    {
        var viewModel = new BoardViewModel();
        viewModel.StrokeColor = "#FF0000";

        viewModel.RecordGesture(StrokeAt("#FF0000", 10, 10));
        viewModel.RecordGesture(StrokeAt("#FF0000", 200, 200));

        Assert.Equal(2, viewModel.Document.Count);

        viewModel.IsEraser = true;
        viewModel.RecordGesture(
            StrokeModel.Create("#000000", 24.0, [new StrokePoint(0, 0), new StrokePoint(50, 50)]));

        Assert.Equal(1, viewModel.Document.Count);
        Assert.Equal("#FF0000", viewModel.Document.Strokes[0].ColorHex);
    }

    [Fact]
    public void BenchmarkRunsAndReturnsDuration()
    {
        var elapsed = BoardBenchmark.Run(strokeCount: 100, pointsPerStroke: 16);

        Assert.True(elapsed >= TimeSpan.Zero);
        Assert.True(elapsed < TimeSpan.FromSeconds(30));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static BoardDocument CreateDocument(int count, string colorHex)
    {
        var document = new BoardDocument();
        for (var index = 0; index < count; index++)
        {
            document.Add(StrokeAt(colorHex, 10 + index, 10 + index));
        }

        return document;
    }

    private static StrokeModel StrokeAt(string colorHex, double x, double y) =>
        StrokeModel.Create(colorHex, 4.0,
        [
            new StrokePoint(x, y),
            new StrokePoint(x + 20, y + 20),
        ]);
}
