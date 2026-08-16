using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters;

public class AvaloniaUriTypeConverter : TypeConverter
{
	/// <inheritdoc />
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	/// <inheritdoc />
	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (!(value is string text))
		{
			return null;
		}
		UriKind uriKind = (text.StartsWith("/") ? UriKind.Relative : UriKind.RelativeOrAbsolute);
		if (!Uri.TryCreate(text, uriKind, out Uri result))
		{
			throw new ArgumentException("Unable to parse URI: " + text);
		}
		return result;
	}
}
