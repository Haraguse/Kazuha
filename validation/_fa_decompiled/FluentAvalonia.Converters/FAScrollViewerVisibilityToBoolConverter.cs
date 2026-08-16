using System;
using System.Globalization;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;

namespace FluentAvalonia.Converters;

/// <summary>
/// Special converter to convert ScrollBarVisbility enum values to bool
/// </summary>
public sealed class FAScrollViewerVisibilityToBoolConverter : IValueConverter
{
	/// <inheritdoc />
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)(ScrollBarVisibility)value == 3;
	}

	/// <inheritdoc />
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
