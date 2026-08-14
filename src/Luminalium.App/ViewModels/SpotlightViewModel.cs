using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

/// <summary>
/// Spotlight view model: a full-screen dim with a rectangular hole. The
/// selection rectangle is normalized so (x1,y1)-(x2,y2) is order-independent;
/// the dim model excludes the selected rectangle from the dimmed area.
/// </summary>
public partial class SpotlightViewModel : ObservableObject
{
    public SpotlightViewModel(ILocalizationService? localization = null)
    {
        Localization = localization ?? new LocalizationService();
    }

    public ILocalizationService Localization { get; }

    public string WindowTitle => Localization["Spotlight.Window.Title"];

    [ObservableProperty]
    private double _selectionX1;

    [ObservableProperty]
    private double _selectionY1;

    [ObservableProperty]
    private double _selectionX2;

    [ObservableProperty]
    private double _selectionY2;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>
    /// Normalized selection rectangle (left/top/width/height). Empty when no
    /// selection has been made.
    /// </summary>
    public SpotlightRect? SelectionRect
    {
        get
        {
            if (!HasSelection)
            {
                return null;
            }

            var left = Math.Min(SelectionX1, SelectionX2);
            var top = Math.Min(SelectionY1, SelectionY2);
            var width = Math.Abs(SelectionX2 - SelectionX1);
            var height = Math.Abs(SelectionY2 - SelectionY1);
            return new SpotlightRect(left, top, width, height);
        }
    }

    /// <summary>
    /// True when the point lies OUTSIDE the selected rectangle, i.e. it must be
    /// dimmed. With no selection the whole screen is dimmed.
    /// </summary>
    public bool IsDimmed(double x, double y)
    {
        var rect = SelectionRect;
        if (rect is null)
        {
            return true;
        }

        return x < rect.Left || x > rect.Left + rect.Width
            || y < rect.Top || y > rect.Top + rect.Height;
    }

    public void BeginSelection(double x, double y)
    {
        SelectionX1 = x;
        SelectionY1 = y;
        SelectionX2 = x;
        SelectionY2 = y;
        HasSelection = true;
        StatusText = Localization["Spotlight.Status.Selecting"];
    }

    public void UpdateSelection(double x, double y)
    {
        if (!HasSelection)
        {
            return;
        }

        SelectionX2 = x;
        SelectionY2 = y;
    }

    public void EndSelection(double x, double y)
    {
        if (!HasSelection)
        {
            return;
        }

        UpdateSelection(x, y);
        StatusText = Localization["Spotlight.Status.Selected"];
    }

    public void ClearSelection()
    {
        HasSelection = false;
        SelectionX1 = 0;
        SelectionY1 = 0;
        SelectionX2 = 0;
        SelectionY2 = 0;
        StatusText = Localization["Spotlight.Status.SelectHint"];
    }

    [RelayCommand]
    private void ClearSelectionCommand() => ClearSelection();
}

/// <summary>
/// Normalized spotlight rectangle in screen coordinates.
/// </summary>
public sealed record SpotlightRect(double Left, double Top, double Width, double Height);