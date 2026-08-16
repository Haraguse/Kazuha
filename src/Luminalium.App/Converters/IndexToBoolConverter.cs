using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;

namespace Luminalium.App.Converters;

/// <summary>
/// Converts an integer index to a boolean by comparing it with the integer parameter.
/// Useful for showing/hiding tab panels based on a SelectedIndex binding.
/// </summary>
public sealed class IndexToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index && parameter is string param && int.TryParse(param, NumberStyles.Integer, CultureInfo.InvariantCulture, out var target))
        {
            return index == target;
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is string param && int.TryParse(param, NumberStyles.Integer, CultureInfo.InvariantCulture, out var target))
        {
            return target;
        }

        return BindingOperations.DoNothing;
    }
}
