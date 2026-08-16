using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;

namespace Luminalium.App.Converters;

/// <summary>
/// Converts a boolean value to a short Chinese status string ("开" / "关").
/// </summary>
public sealed class BoolToStatusTextConverter : IValueConverter
{
    public static BoolToStatusTextConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "开" : "关";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
