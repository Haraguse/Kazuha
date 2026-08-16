using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Animation;

public class CueTypeConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		return Cue.Parse((string)value, culture);
	}
}
