using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.Converters;

public class BitmapTypeConverter : TypeConverter
{
	/// <inheritdoc />
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	/// <inheritdoc />
	public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		string text = (string)value;
		Uri uri = (text.StartsWith("/") ? new Uri(text, UriKind.Relative) : new Uri(text, UriKind.RelativeOrAbsolute));
		if (uri.IsAbsoluteUri && uri.IsFile)
		{
			return new Bitmap(uri.LocalPath);
		}
		return new Bitmap(AvaloniaLocator.Current.GetRequiredService<IAssetLoader>().Open(uri, context?.GetContextBaseUri()));
	}
}
