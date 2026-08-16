using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Media;

public class GeometryTypeConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		if (sourceType == typeof(string))
		{
			return true;
		}
		return base.CanConvertFrom(context, sourceType);
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value == null)
		{
			throw GetConvertFromException(value);
		}
		if (value is string s)
		{
			return Geometry.Parse(s);
		}
		return base.ConvertFrom(context, culture, value);
	}
}
