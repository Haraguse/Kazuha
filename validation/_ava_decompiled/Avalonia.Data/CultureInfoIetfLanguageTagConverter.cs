using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Metadata;

namespace Avalonia.Data;

[PrivateApi]
public class CultureInfoIetfLanguageTagConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is string name)
		{
			return CultureInfo.GetCultureInfoByIetfLanguageTag(name);
		}
		throw GetConvertFromException(value);
	}
}
