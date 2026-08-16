using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Avalonia.Data.Converters;

/// <summary>
/// Provides a set of useful <see cref="T:Avalonia.Data.Converters.IValueConverter" />s for working with bool values.
/// </summary>
public static class BoolConverters
{
	private class NotConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is bool flag)
			{
				return !flag;
			}
			return AvaloniaProperty.UnsetValue;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is bool flag)
			{
				return !flag;
			}
			return AvaloniaProperty.UnsetValue;
		}
	}

	/// <summary>
	/// A multi-value converter that returns true if all inputs are true.
	/// </summary>
	public static readonly IMultiValueConverter And = new FuncMultiValueConverter<bool, bool>((IEnumerable<bool> x) => x.All((bool y) => y));

	/// <summary>
	/// A multi-value converter that returns true if any of the inputs is true.
	/// </summary>
	public static readonly IMultiValueConverter Or = new FuncMultiValueConverter<bool, bool>((IEnumerable<bool> x) => x.Any((bool y) => y));

	/// <summary>
	/// A value converter that returns true when input is false and false when input is true.
	/// </summary>
	public static readonly IValueConverter Not = new NotConverter();
}
