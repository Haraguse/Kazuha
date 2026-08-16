using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace FluentAvalonia.Converters;

internal class IBitmapToImageConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		if (value != null)
		{
			Bitmap val = (Bitmap)((value is Bitmap) ? value : null);
			if (val != null)
			{
				return (object)new Image
				{
					Source = (IImage)(object)val
				};
			}
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
