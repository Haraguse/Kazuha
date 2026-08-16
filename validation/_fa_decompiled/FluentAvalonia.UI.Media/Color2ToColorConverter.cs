using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;

namespace FluentAvalonia.UI.Media;

/// <summary>
/// Special converter to auto convert Color2 to Avalonia.Media.Color
/// </summary>
public class Color2ToColorConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		return sourceType == typeof(Color);
	}

	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
	{
		return destinationType == typeof(Color);
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if (value is Color val)
		{
			return Color2.FromUInt(((Color)(ref val)).ToUInt32());
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (value is Color2 color)
		{
			color.GetRGB(out var r, out var g, out var b, out var a);
			return (object)new Color(a, r, g, b);
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}
