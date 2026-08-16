using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace FluentAvalonia.Converters;

/// <summary>
/// Special converter for the NumericUpDown to pass info from the NumericUpDown to the textbox
/// </summary>
public sealed class FANUDSpinLocationConverter : IValueConverter
{
	/// <inheritdoc />
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return BindingOperations.DoNothing;
		}
		return (int)Unsafe.Unbox<Location>(value) == 1;
	}

	/// <inheritdoc />
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
