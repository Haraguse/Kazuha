using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FluentAvalonia.UI.Media;

namespace FluentAvalonia.Converters;

/// <summary>
/// Converter that converts a color to a SolidColorBrush
/// </summary>
public class FAColorToBrushConv : IValueConverter
{
	/// <inheritdoc />
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		if (value is Color val)
		{
			return (object)new SolidColorBrush(val, 1.0);
		}
		if (value is Color2 color)
		{
			return (object)new SolidColorBrush((Color)color, 1.0);
		}
		return BindingOperations.DoNothing;
	}

	/// <inheritdoc />
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		ISolidColorBrush val = (ISolidColorBrush)((value is ISolidColorBrush) ? value : null);
		if (val != null)
		{
			return val.Color;
		}
		return BindingOperations.DoNothing;
	}
}
