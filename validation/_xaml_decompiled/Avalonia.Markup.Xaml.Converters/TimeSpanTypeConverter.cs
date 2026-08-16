using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters;

public class TimeSpanTypeConverter : TimeSpanConverter
{
	/// <inheritdoc />
	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		string text = (string)value;
		if (!text.Contains(':'))
		{
			return TimeSpan.FromSeconds(double.Parse(text, CultureInfo.InvariantCulture));
		}
		return base.ConvertFrom(context, culture, value);
	}
}
