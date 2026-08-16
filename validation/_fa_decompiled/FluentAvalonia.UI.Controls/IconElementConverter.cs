using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Type converter for allowing strings in Xaml to be automatically interpreted as an IconElement
/// </summary>
public class IconElementConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		if (sourceType == typeof(string) || sourceType == typeof(FASymbol) || sourceType == typeof(FAIconSource))
		{
			return true;
		}
		return base.CanConvertFrom(context, sourceType);
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if (value is FASymbol symbol)
		{
			return new FASymbolIcon
			{
				Symbol = symbol
			};
		}
		if (value is FAIconSource fAIconSource)
		{
			if (fAIconSource is FAFontIconSource fis)
			{
				return FAIconHelpers.CreateFontIconFromFontIconSource(fis);
			}
			if (fAIconSource is FASymbolIconSource sis)
			{
				return FAIconHelpers.CreateSymbolIconFromSymbolIconSource(sis);
			}
			if (fAIconSource is FAPathIconSource pis)
			{
				return FAIconHelpers.CreatePathIconFromPathIconSource(pis);
			}
			if (fAIconSource is FABitmapIconSource bis)
			{
				return FAIconHelpers.CreateBitmapIconFromBitmapIconSource(bis);
			}
		}
		else
		{
			IImage val = (IImage)((value is IImage) ? value : null);
			if (val != null)
			{
				return new FAImageIcon
				{
					Source = val
				};
			}
			if (value is string text)
			{
				if (Enum.TryParse<FASymbol>(text, out var result))
				{
					return new FASymbolIcon
					{
						Symbol = result
					};
				}
				if (FAPathIcon.IsDataValid(text, out var g))
				{
					return new FAPathIcon
					{
						Data = g
					};
				}
				try
				{
					if (Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out Uri result2))
					{
						return new FABitmapIcon
						{
							UriSource = result2
						};
					}
				}
				catch
				{
				}
				return new FAFontIcon
				{
					Glyph = text
				};
			}
		}
		return base.ConvertFrom(context, culture, value);
	}
}
