using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace Luminalium.App.Converters;

/// <summary>
/// Converts a hex color string (e.g. "#3275F5") into a SolidColorBrush.
/// Returns a transparent brush when the input is not a valid color.
/// </summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public static HexToBrushConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                return new SolidColorBrush(Color.Parse(hex));
            }
            catch
            {
                // Fall through to transparent.
            }
        }

        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
