using System;
using System.Globalization;

namespace Avalonia.Data.Converters;

/// <summary>
/// A value converter which calls <see cref="M:System.String.Format(System.String,System.Object)" />
/// </summary>
public class StringFormatValueConverter : IValueConverter
{
	/// <summary>
	/// Gets an inner value converter which will be called before the string format takes place.
	/// </summary>
	public IValueConverter? Inner { get; }

	/// <summary>
	/// Gets the format string.
	/// </summary>
	public string Format { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.Converters.StringFormatValueConverter" /> class.
	/// </summary>
	/// <param name="format">The format string.</param>
	/// <param name="inner">
	/// An optional inner converter to be called before the format takes place.
	/// </param>
	public StringFormatValueConverter(string format, IValueConverter? inner)
	{
		Format = format ?? throw new ArgumentNullException("format");
		Inner = inner;
	}

	/// <inheritdoc />
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		value = Inner?.Convert(value, targetType, parameter, culture) ?? value;
		string text = Format;
		if (!text.Contains('{'))
		{
			text = "{0:" + text + "}";
		}
		return string.Format(culture, text, value);
	}

	/// <inheritdoc />
	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException("Two way bindings are not supported with a string format");
	}
}
