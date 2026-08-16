using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.Converters;

public class PointsListTypeConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		List<Point> list = new List<Point>();
		using SpanStringTokenizer spanStringTokenizer = new SpanStringTokenizer((string)value, CultureInfo.InvariantCulture, "Invalid PointsList.");
		double result;
		while (spanStringTokenizer.TryReadDouble(out result))
		{
			list.Add(new Point(result, spanStringTokenizer.ReadDouble()));
		}
		return list;
	}
}
