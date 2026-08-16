using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media.Transformation;

namespace Avalonia.Media;

/// <summary>
/// Creates an <see cref="T:Avalonia.Media.ITransform" /> from a string representation.
/// </summary>
public class TransformConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
	{
		if (!(value is string s))
		{
			return null;
		}
		return TransformOperations.Parse(s);
	}
}
