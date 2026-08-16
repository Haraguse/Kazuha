using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Board;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public partial class BoardViewModel : ObservableObject
{
    public BoardViewModel(ILocalizationService? localization = null)
    {
        Localization = localization ?? new LocalizationService();
        StrokeColors =
        [
            new BoardColorOption("#FF0000", Localization["Board.Toolbar.Color.Red"]),
            new BoardColorOption("#FF8C00", Localization["Board.Toolbar.Color.Orange"]),
            new BoardColorOption("#FFD700", Localization["Board.Toolbar.Color.Yellow"]),
            new BoardColorOption("#00A000", Localization["Board.Toolbar.Color.Green"]),
            new BoardColorOption("#0078D4", Localization["Board.Toolbar.Color.Blue"]),
            new BoardColorOption("#000000", Localization["Board.Toolbar.Color.Black"]),
        ];
        StrokeColors[0].IsSelected = true;
    }

    public ILocalizationService Localization { get; }

    /// <summary>The pen colors offered in the palette (first item is the default).</summary>
    public IReadOnlyList<BoardColorOption> StrokeColors { get; }

    /// <summary>
    /// Raised when the user clears the board so the host can reset the
    /// InkCanvas stroke store (the canvas owns the actual strokes).
    /// </summary>
    public event EventHandler? ClearRequested;

    [ObservableProperty]
    private string _strokeColor = "#FF0000";

    [ObservableProperty]
    private double _strokeWidth = 4.0;

    [ObservableProperty]
    private bool _isEraser;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public string WindowTitle => Localization["Board.Window.Title"];

    public string StrokeCountText => string.Format(
        CultureInfo.InvariantCulture,
        Localization["Board.Status.Strokes"],
        StrokeCount);

    public int StrokeCount { get; private set; }

    /// <summary>True while the pen tool is active (the inverse of the eraser).</summary>
    public bool IsPenActive => !IsEraser;

    [RelayCommand]
    private void SelectColor(BoardColorOption? option)
    {
        if (option is null)
        {
            return;
        }

        StrokeColor = option.Hex;
        foreach (var color in StrokeColors)
        {
            color.IsSelected = ReferenceEquals(color, option);
        }

        IsEraser = false;
    }

    [RelayCommand]
    private void ActivatePen() => IsEraser = false;

    [RelayCommand]
    private void ActivateEraser() => IsEraser = true;

    [RelayCommand]
    private void ClearBoard() => ClearRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Updates the stroke count shown in the status bar after a stroke is collected or erased.</summary>
    public void SetStrokeCount(int count)
    {
        StrokeCount = Math.Max(0, count);
        OnPropertyChanged(nameof(StrokeCountText));
        SetStatus(string.Format(
            CultureInfo.InvariantCulture,
            Localization["Board.Status.Strokes"],
            StrokeCount));
    }

    /// <summary>Applies a save result to the status bar and error flag.</summary>
    public void HandleSaveResult(BoardSaveResult? result)
    {
        if (result is null)
        {
            return;
        }

        if (result.IsSuccess)
        {
            HasError = false;
            SetStatus(string.Format(
                CultureInfo.InvariantCulture,
                Localization["Board.Status.Saved"],
                result.Path));
            return;
        }

        if (string.Equals(result.Error, "NothingToSave", StringComparison.Ordinal))
        {
            HasError = false;
            SetStatus(Localization["Board.Status.NothingToSave"]);
            return;
        }

        HasError = true;
        SetStatus(string.Format(
            CultureInfo.InvariantCulture,
            Localization["Board.Status.SaveFailed"],
            result.Error));
    }

    private void SetStatus(string text) => StatusText = text;

    partial void OnIsEraserChanged(bool value)
    {
        StrokeWidth = value ? 24.0 : 4.0;
        OnPropertyChanged(nameof(IsPenActive));
    }
}