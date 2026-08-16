using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Animation;

/// <summary>
/// Converts string values to <see cref="T:Avalonia.Animation.KeySpline" /> values
/// </summary>
public class KeySplineTypeConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		return KeySpline.Parse((string)value, CultureInfo.InvariantCulture);
	}
}
