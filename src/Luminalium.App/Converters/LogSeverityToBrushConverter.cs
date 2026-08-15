using Avalonia.Data.Converters;
using Avalonia.Media;
using Luminalium.App.Services;
using System.Globalization;

namespace Luminalium.App.Converters;

public sealed class LogSeverityToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LogSeverity severity)
        {
            return severity switch
            {
                LogSeverity.Trace => SolidColorBrush.Parse("#9CA3AF"),
                LogSeverity.Debug => SolidColorBrush.Parse("#22D3EE"),
                LogSeverity.Information => SolidColorBrush.Parse("#60A5FA"),
                LogSeverity.Warning => SolidColorBrush.Parse("#FBBF24"),
                LogSeverity.Error => SolidColorBrush.Parse("#F87171"),
                LogSeverity.Critical => SolidColorBrush.Parse("#DC2626"),
                _ => SolidColorBrush.Parse("#9CA3AF"),
            };
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
