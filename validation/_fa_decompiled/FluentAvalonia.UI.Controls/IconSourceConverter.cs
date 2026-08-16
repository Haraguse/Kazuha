using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Type converter for allowing strings in Xaml to be automatically interpreted as an IconElement
/// </summary>
public class IconSourceConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		if (sourceType == typeof(string) || sourceType == typeof(FASymbol))
		{
			return true;
		}
		return base.CanConvertFrom(context, sourceType);
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if (value is FASymbol symbol)
		{
			return new FASymbolIconSource
			{
				Symbol = symbol
			};
		}
		IImage val = (IImage)((value is IImage) ? value : null);
		if (val != null)
		{
			return new FAImageIconSource
			{
				Source = val
			};
		}
		if (value is string text)
		{
			if (Enum.TryParse<FASymbol>(text, out var result))
			{
				return new FASymbolIconSource
				{
					Symbol = result
				};
			}
			if (FAPathIcon.IsDataValid(text, out var g))
			{
				return new FAPathIconSource
				{
					Data = g
				};
			}
			try
			{
				if (Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out Uri result2))
				{
					return new FABitmapIconSource
					{
						UriSource = result2
					};
				}
			}
			catch
			{
			}
			return new FAFontIconSource
			{
				Glyph = text
			};
		}
		return base.ConvertFrom(context, culture, value);
	}
}
