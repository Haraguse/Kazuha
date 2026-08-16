using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia.Collections;

/// <summary>
/// Creates an <see cref="T:Avalonia.Collections.AvaloniaList`1" /> from a string representation.
/// </summary>
[RequiresUnreferencedCode("Conversion methods are required for type conversion, including op_Implicit, op_Explicit, Parse and TypeConverter.")]
public class AvaloniaListConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
	{
		if (!(value is string text))
		{
			return null;
		}
		AvaloniaList<T> avaloniaList = new AvaloniaList<T>();
		string[] array = text.Split(',');
		foreach (string value2 in array)
		{
			if (TypeUtilities.TryConvert(typeof(T), value2, culture, out object result))
			{
				avaloniaList.Add((T)result);
				continue;
			}
			throw new InvalidCastException($"Could not convert '{value2}' to {typeof(T)}.");
		}
		return avaloniaList;
	}
}
