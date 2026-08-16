using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Luminalium.App.ViewModels;

/// <summary>
/// A single selectable color in the board's pen palette. Exposes the hex value,
/// a display name, and the brushes used to render and highlight the swatch.
/// </summary>
public sealed partial class BoardColorOption : ObservableObject
{
    public BoardColorOption(string hex, string name)
    {
        Hex = hex;
        Name = name;
        Brush = new SolidColorBrush(Color.Parse(hex));
    }

    public string Hex { get; }

    public string Name { get; }

    /// <summary>Fill brush for the swatch dot.</summary>
    public IBrush Brush { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBrush))]
    private bool _isSelected;

    /// <summary>Outline around the swatch when it is the active color.</summary>
    public IBrush SelectionBrush => IsSelected
        ? new SolidColorBrush(Colors.White)
        : new SolidColorBrush(Colors.Transparent);
}