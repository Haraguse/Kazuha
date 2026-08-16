using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FluentAvalonia.UI.Media;

namespace FluentAvalonia.Converters;

/// <summary>
/// A converter that allows lightening or darkening a color by an amount 
/// specified in the parameter argument
/// </summary>
public sealed class FAColorShadeBrushConv : IValueConverter
{
	/// <inheritdoc />
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		Color2 color = (Color2)value;
		if (!float.TryParse(parameter.ToString(), out var result))
		{
			result = 0f;
		}
		return (object)new SolidColorBrush((Color)color.LightenPercent(result), 1.0);
	}

	/// <inheritdoc />
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return BindingOperations.DoNothing;
	}
}
