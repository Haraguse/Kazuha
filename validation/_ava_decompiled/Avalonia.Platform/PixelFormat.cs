namespace Avalonia.Platform;

public record struct PixelFormat
{
	public int BitsPerPixel
	{
		get
		{
			if (FormatEnum == PixelFormatEnum.BlackWhite)
			{
				return 1;
			}
			if (FormatEnum == PixelFormatEnum.Gray2)
			{
				return 2;
			}
			if (FormatEnum == PixelFormatEnum.Gray4)
			{
				return 4;
			}
			if (FormatEnum == PixelFormatEnum.Gray8)
			{
				return 8;
			}
			if (FormatEnum == PixelFormatEnum.Rgb565 || FormatEnum == PixelFormatEnum.Bgr555 || FormatEnum == PixelFormatEnum.Bgr565 || FormatEnum == PixelFormatEnum.Gray16)
			{
				return 16;
			}
			PixelFormatEnum formatEnum = FormatEnum;
			if ((formatEnum == PixelFormatEnum.Rgb24 || formatEnum == PixelFormatEnum.Bgr24) ? true : false)
			{
				return 24;
			}
			if (FormatEnum == PixelFormatEnum.Rgba64)
			{
				return 64;
			}
			return 32;
		}
	}

	internal bool HasAlpha
	{
		get
		{
			if (FormatEnum != PixelFormatEnum.Rgba8888 && FormatEnum != PixelFormatEnum.Bgra8888)
			{
				return FormatEnum == PixelFormatEnum.Rgba64;
			}
			return true;
		}
	}

	public static PixelFormat Rgb565 => PixelFormats.Rgb565;

	public static PixelFormat Rgba8888 => PixelFormats.Rgba8888;

	public static PixelFormat Rgb32 => PixelFormats.Rgb32;

	public static PixelFormat Bgra8888 => PixelFormats.Bgra8888;

	internal PixelFormatEnum FormatEnum;

	internal PixelFormat(PixelFormatEnum format)
	{
		FormatEnum = format;
	}

	public override string ToString()
	{
		return FormatEnum.ToString();
	}
}
