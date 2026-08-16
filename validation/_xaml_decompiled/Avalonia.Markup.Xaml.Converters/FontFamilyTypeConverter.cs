using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;

namespace Avalonia.Markup.Xaml.Converters;

public class FontFamilyTypeConverter : TypeConverter
{
	/// <inheritdoc />
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	/// <inheritdoc />
	public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		return FontFamily.Parse((string)value, context?.GetContextBaseUri());
	}
}
