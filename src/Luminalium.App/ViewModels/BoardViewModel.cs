using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Board;
using Luminalium.App.Overlay;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public partial class BoardViewModel : ObservableObject
{
    private readonly BoardDocument _document = new();

    public BoardViewModel(ILocalizationService? localization = null)
    {
        Localization = localization ?? new LocalizationService();
    }

    public ILocalizationService Localization { get; }

    public BoardDocument Document => _document;

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

    public string StrokeCountText => string.Format(
        CultureInfo.InvariantCulture,
        Localization["Board.Status.Strokes"],
        _document.Count);

    public string WindowTitle => Localization["Board.Window.Title"];

    [RelayCommand]
    private void SelectRedColor() => StrokeColor = "#FF0000";

    [RelayCommand]
    private void ToggleEraser() => IsEraser = !IsEraser;

    [RelayCommand]
    private void ClearBoard()
    {
        _document.Clear();
        UpdateStrokeCount();
        SetStatus(Localization["Board.Status.Strokes"] == string.Empty
            ? string.Empty
            : string.Format(CultureInfo.InvariantCulture, Localization["Board.Status.Strokes"], 0));
    }

    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        var result = await BoardSaveService.SaveAsync(_document, path, cancellationToken).ConfigureAwait(false);

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

    /// <summary>
    /// Routes a finished canvas gesture: eraser gestures remove intersecting
    /// strokes, pen gestures append a stroke.
    /// </summary>
    public void RecordGesture(StrokeModel stroke)
    {
        if (IsEraser)
        {
            _document.Erase(stroke.Points);
        }
        else
        {
            _document.Add(stroke);
        }

        UpdateStrokeCount();
    }

    private void UpdateStrokeCount()
    {
        OnPropertyChanged(nameof(StrokeCountText));
        SetStatus(string.Format(
            CultureInfo.InvariantCulture,
            Localization["Board.Status.Strokes"],
            _document.Count));
    }

    private void SetStatus(string text)
    {
        StatusText = text;
    }

    partial void OnIsEraserChanged(bool value)
    {
        StrokeWidth = value ? 24.0 : 4.0;
    }
}
