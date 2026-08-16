using DotNetCampus.Inking;
using DotNetCampus.Inking.Primitive;
using DotNetCampus.Inking.StrokeRenderers;
using Luminalium.App.Board;
using Luminalium.App.ViewModels;
using SkiaSharp;
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
    public async Task SaveWritesWellFormedSvgDocument()
    {
        var strokes = new[]
        {
            CreateStroke("#FF0000", 4.0, new InkStylusPoint(0, 0, 1), new InkStylusPoint(50, 50, 1)),
            CreateStroke("#0000FF", 6.0, new InkStylusPoint(100, 100, 1), new InkStylusPoint(200, 200, 1)),
        };
        var path = Path.Combine(_directory, "board.svg");

        var result = await BoardSaveService.SaveAsync(strokes, 480, 360, path);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(path));

        var svg = await File.ReadAllTextAsync(path);
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("</svg>", svg.Replace("\r", "").Replace("\n", "").Trim(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmptyBoardSaveReturnsNothingToSaveAndWritesNoFile()
    {
        var path = Path.Combine(_directory, "empty.svg");

        var result = await BoardSaveService.SaveAsync([], 480, 360, path);

        Assert.False(result.IsSuccess);
        Assert.Equal("NothingToSave", result.Error);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task InvalidSavePathFailsWithoutMutatingStrokes()
    {
        var strokes = new List<SkiaStroke>
        {
            CreateStroke("#FF0000", 4.0, new InkStylusPoint(0, 0, 1), new InkStylusPoint(50, 50, 1)),
        };
        var path = Path.Combine(_directory, "missing-dir", "board.svg");

        var result = await BoardSaveService.SaveAsync(strokes, 480, 360, path);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Single(strokes);
    }

    [Fact]
    public void ClearBoardRaisesClearRequested()
    {
        var viewModel = new BoardViewModel();
        var cleared = false;
        viewModel.ClearRequested += (_, _) => cleared = true;

        viewModel.ClearBoardCommand.Execute(null);

        Assert.True(cleared);
    }

    [Fact]
    public void SetStrokeCountUpdatesCountTextAndStatus()
    {
        var viewModel = new BoardViewModel();

        viewModel.SetStrokeCount(3);

        Assert.Equal(3, viewModel.StrokeCount);
        Assert.Contains("3", viewModel.StrokeCountText);
        Assert.Contains("3", viewModel.StatusText);
    }

    [Fact]
    public void HandleSaveResultSuccessClearsErrorAndReportsPath()
    {
        var viewModel = new BoardViewModel();

        viewModel.HandleSaveResult(BoardSaveResult.Success(@"C:\tmp\board.svg"));

        Assert.False(viewModel.HasError);
        Assert.Contains("board.svg", viewModel.StatusText);
    }

    [Fact]
    public void HandleSaveResultNothingToSaveClearsErrorFlag()
    {
        var viewModel = new BoardViewModel();

        viewModel.HandleSaveResult(BoardSaveResult.NothingToSave());

        Assert.False(viewModel.HasError);
        Assert.NotEqual("Save failed", viewModel.StatusText);
    }

    [Fact]
    public void HandleSaveResultFailureSetsErrorFlag()
    {
        var viewModel = new BoardViewModel();

        viewModel.HandleSaveResult(BoardSaveResult.Failure("UnauthorizedAccessException"));

        Assert.True(viewModel.HasError);
        Assert.Contains("UnauthorizedAccessException", viewModel.StatusText);
    }

    [Fact]
    public void IsEraserTogglesStrokeWidthForEraser()
    {
        var viewModel = new BoardViewModel();
        Assert.False(viewModel.IsEraser);
        Assert.Equal(4.0, viewModel.StrokeWidth);

        viewModel.IsEraser = true;

        Assert.True(viewModel.IsEraser);
        Assert.Equal(24.0, viewModel.StrokeWidth);
    }

    [Fact]
    public void IsPenActiveTracksEraserState()
    {
        var viewModel = new BoardViewModel();
        Assert.True(viewModel.IsPenActive);
        Assert.False(viewModel.IsEraser);

        viewModel.IsEraser = true;

        Assert.False(viewModel.IsPenActive);
        Assert.True(viewModel.IsEraser);
    }

    [Fact]
    public void ActivateEraserAndPenCommandsAreMutuallyExclusive()
    {
        var viewModel = new BoardViewModel();

        viewModel.ActivateEraserCommand.Execute(null);
        Assert.True(viewModel.IsEraser);
        Assert.False(viewModel.IsPenActive);

        viewModel.ActivatePenCommand.Execute(null);
        Assert.False(viewModel.IsEraser);
        Assert.True(viewModel.IsPenActive);
    }

    [Fact]
    public void StrokeColorsInitializedWithDefaultFirstColor()
    {
        var viewModel = new BoardViewModel();

        Assert.Equal(6, viewModel.StrokeColors.Count);
        Assert.True(viewModel.StrokeColors[0].IsSelected);
        Assert.Equal("#FF0000", viewModel.StrokeColors[0].Hex);
        Assert.False(viewModel.StrokeColors[1].IsSelected);
    }

    [Fact]
    public void SelectColorUpdatesStrokeColorAndSelectionState()
    {
        var viewModel = new BoardViewModel();
        var target = viewModel.StrokeColors[3];

        viewModel.SelectColorCommand.Execute(target);

        Assert.Equal(target.Hex, viewModel.StrokeColor);
        Assert.True(target.IsSelected);
        foreach (var color in viewModel.StrokeColors)
        {
            Assert.Equal(ReferenceEquals(target, color), color.IsSelected);
        }
    }

    [Fact]
    public void SelectColorExitsEraserMode()
    {
        var viewModel = new BoardViewModel();
        viewModel.IsEraser = true;

        viewModel.SelectColorCommand.Execute(viewModel.StrokeColors[0]);

        Assert.False(viewModel.IsEraser);
        Assert.True(viewModel.IsPenActive);
    }

    [Fact]
    public void SelectColorWithNullIsIgnored()
    {
        var viewModel = new BoardViewModel();
        var original = viewModel.StrokeColor;

        viewModel.SelectColorCommand.Execute(null);

        Assert.Equal(original, viewModel.StrokeColor);
        Assert.True(viewModel.StrokeColors[0].IsSelected);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static SkiaStroke CreateStroke(string colorHex, double thickness, params InkStylusPoint[] points)
    {
        var stroke = new SkiaStroke(new InkId(0))
        {
            InkStrokeRenderer = new TestStrokeRenderer(),
            Color = SKColor.Parse(colorHex),
            InkThickness = (float)thickness,
        };

        foreach (var point in points)
        {
            stroke.AddPoint(point);
        }

        return stroke;
    }

    /// <summary>Minimal renderer that traces a polyline from the stylus points.</summary>
    private sealed class TestStrokeRenderer : ISkiaInkStrokeRenderer
    {
        public SKPath RenderInkToPath(IReadOnlyList<InkStylusPoint> points, double thickness)
        {
            var path = new SKPath();
            if (points.Count > 0)
            {
                path.MoveTo((float)points[0].X, (float)points[0].Y);
                for (var index = 1; index < points.Count; index++)
                {
                    path.LineTo((float)points[index].X, (float)points[index].Y);
                }
            }

            return path;
        }
    }
}