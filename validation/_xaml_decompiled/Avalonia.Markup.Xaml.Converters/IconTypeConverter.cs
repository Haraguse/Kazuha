using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.Converters;

public class IconTypeConverter : TypeConverter
{
	/// <inheritdoc />
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	/// <inheritdoc />
	public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is string s)
		{
			return CreateIconFromPath(context, s);
		}
		if (value is Bitmap bitmap)
		{
			return new WindowIcon(bitmap);
		}
		throw new NotSupportedException();
	}

	private static WindowIcon CreateIconFromPath(ITypeDescriptorContext? context, string s)
	{
		Uri uri = (s.StartsWith("/") ? new Uri(s, UriKind.Relative) : new Uri(s, UriKind.RelativeOrAbsolute));
		if (uri.IsAbsoluteUri && uri.IsFile)
		{
			return new WindowIcon(uri.LocalPath);
		}
		return new WindowIcon(AvaloniaLocator.Current.GetRequiredService<IAssetLoader>().Open(uri, context?.GetContextBaseUri()));
	}
}
