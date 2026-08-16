using System;
using Avalonia.Platform;

namespace Avalonia.Media.Imaging;

internal static class PixelFormatWriter
{
	public struct Rgb24PixelFormatWriter : IPixelFormatWriter
	{
		private unsafe byte* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			byte* address = _address;
			*address = pixel.R;
			address[1] = pixel.G;
			address[2] = pixel.B;
			_address += 3;
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Rgb32PixelFormatWriter : IPixelFormatWriter
	{
		private unsafe byte* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			byte* address = _address;
			*address = pixel.R;
			address[1] = pixel.G;
			address[2] = pixel.B;
			address[3] = byte.MaxValue;
			_address += 4;
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Rgba64PixelFormatWriter : IPixelFormatWriter
	{
		private unsafe Rgba64Pixel* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			*_address = new Rgba64Pixel((ushort)(pixel.R << 8), (ushort)(pixel.G << 8), (ushort)(pixel.B << 8), (ushort)(pixel.A << 8));
			_address++;
		}

		public unsafe void Reset(nint address)
		{
			_address = (Rgba64Pixel*)address;
		}
	}

	public struct Rgba8888PixelFormatWriter : IPixelFormatWriter
	{
		private unsafe Rgba8888Pixel* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			*_address = pixel;
			_address++;
		}

		public unsafe void Reset(nint address)
		{
			_address = (Rgba8888Pixel*)address;
		}
	}

	public struct Bgra8888PixelFormatWriter : IPixelFormatWriter
	{
		private unsafe byte* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			byte* address = _address;
			*address = pixel.B;
			address[1] = pixel.G;
			address[2] = pixel.R;
			address[3] = pixel.A;
			_address += 4;
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Bgr24PixelFormatWriter : IPixelFormatWriter
	{
		private unsafe byte* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			byte* address = _address;
			address[2] = pixel.R;
			address[1] = pixel.G;
			*address = pixel.B;
			_address += 3;
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Bgr32PixelFormatWriter : IPixelFormatWriter
	{
		private unsafe byte* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			byte* address = _address;
			*address = pixel.B;
			address[1] = pixel.G;
			address[2] = pixel.R;
			address[3] = byte.MaxValue;
			_address += 4;
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Bgra32PixelFormatWriter : IPixelFormatWriter
	{
		private unsafe byte* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			byte* address = _address;
			address[3] = pixel.A;
			address[2] = pixel.R;
			address[1] = pixel.G;
			*address = pixel.B;
			_address += 4;
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Bgr565PixelFormatWriter : IPixelFormatWriter
	{
		private unsafe ushort* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			*_address = Pack(pixel);
			_address++;
		}

		public unsafe void Reset(nint address)
		{
			_address = (ushort*)address;
		}

		private static ushort Pack(Rgba8888Pixel pixel)
		{
			return (ushort)((((int)Math.Round((float)(int)pixel.R / 255f * 31f) & 0x1F) << 11) | (((int)Math.Round((float)(int)pixel.G / 255f * 63f) & 0x3F) << 5) | ((int)Math.Round((float)(int)pixel.B / 255f * 31f) & 0x1F));
		}
	}

	public struct Bgr555PixelFormatWriter : IPixelFormatWriter
	{
		private unsafe ushort* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			*_address = Pack(pixel);
			_address++;
		}

		public unsafe void Reset(nint address)
		{
			_address = (ushort*)address;
		}

		private static ushort Pack(Rgba8888Pixel pixel)
		{
			return (ushort)((((int)Math.Round((float)(int)pixel.R / 255f * 31f) & 0x1F) << 10) | (((int)Math.Round((float)(int)pixel.G / 255f * 31f) & 0x1F) << 5) | ((int)Math.Round((float)(int)pixel.B / 255f * 31f) & 0x1F));
		}
	}

	public struct Gray32FloatPixelFormatWriter : IPixelFormatWriter
	{
		private unsafe float* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			*_address = Pack(pixel);
			_address++;
		}

		private static float Pack(Rgba8888Pixel pixel)
		{
			return (float)Math.Pow((float)(int)pixel.R / 255f, 2.2);
		}

		public unsafe void Reset(nint address)
		{
			_address = (float*)address;
		}
	}

	public struct BlackWhitePixelFormatWriter : IPixelFormatWriter
	{
		private int _bit;

		private unsafe byte* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			byte* address = _address;
			int num = ((Math.Round(0.299f * (float)(int)pixel.R + 0.587f * (float)(int)pixel.G + 0.114f * (float)(int)pixel.B) > 127.0) ? 1 : 0);
			int num2 = 7 - _bit;
			int num3 = 1 << num2;
			*address = (byte)((*address & ~num3) | (num << num2));
			_bit++;
			if (_bit == 8)
			{
				_address++;
				_bit = 0;
			}
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Gray2PixelFormatWriter : IPixelFormatWriter
	{
		private int _bit;

		private unsafe byte* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			byte* address = _address;
			int num = 0;
			byte b = (byte)Math.Round(0.299f * (float)(int)pixel.R + 0.587f * (float)(int)pixel.G + 0.114f * (float)(int)pixel.B);
			if (b > 0 && b <= 85)
			{
				num = 1;
			}
			if (b > 85 && b <= 170)
			{
				num = 2;
			}
			if (b > 170)
			{
				num = 3;
			}
			int num2 = 6 - _bit;
			int num3 = 3 << num2;
			*address = (byte)((*address & ~num3) | (num << num2));
			_bit += 2;
			if (_bit == 8)
			{
				_address++;
				_bit = 0;
			}
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Gray4PixelFormatWriter : IPixelFormatWriter
	{
		private int _bit;

		private unsafe byte* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			byte* address = _address;
			byte b = (byte)((float)(int)(byte)Math.Round(0.299f * (float)(int)pixel.R + 0.587f * (float)(int)pixel.G + 0.114f * (float)(int)pixel.B) / 255f * 15f);
			int num = 4 - _bit;
			int num2 = 15 << num;
			*address = (byte)((*address & ~num2) | (b << num));
			_bit += 4;
			if (_bit == 8)
			{
				_address++;
				_bit = 0;
			}
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Gray8PixelFormatWriter : IPixelFormatWriter
	{
		private unsafe byte* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			byte* address = _address;
			byte b = (byte)Math.Round(0.299f * (float)(int)pixel.R + 0.587f * (float)(int)pixel.G + 0.114f * (float)(int)pixel.B);
			*address = b;
			_address++;
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Gray16PixelFormatWriter : IPixelFormatWriter
	{
		private unsafe ushort* _address;

		public unsafe void WriteNext(Rgba8888Pixel pixel)
		{
			ushort* address = _address;
			ushort num = (ushort)Math.Round((0.299f * (float)(int)pixel.R + 0.587f * (float)(int)pixel.G + 0.114f * (float)(int)pixel.B) * 257f);
			*address = num;
			_address++;
		}

		public unsafe void Reset(nint address)
		{
			_address = (ushort*)address;
		}
	}

	private static void Write<T>(ReadOnlySpan<Rgba8888Pixel> pixels, nint dest, PixelSize size, int stride, AlphaFormat alphaFormat, AlphaFormat srcAlphaFormat) where T : struct, IPixelFormatWriter
	{
		T val = new T();
		int width = size.Width;
		int height = size.Height;
		int num = 0;
		for (int i = 0; i < height; i++)
		{
			val.Reset(dest + stride * i);
			for (int j = 0; j < width; j++)
			{
				val.WriteNext(GetConvertedPixel(pixels[num++], srcAlphaFormat, alphaFormat));
			}
		}
	}

	private static Rgba8888Pixel GetConvertedPixel(Rgba8888Pixel pixel, AlphaFormat sourceAlpha, AlphaFormat destAlpha)
	{
		if (sourceAlpha != destAlpha)
		{
			if (sourceAlpha == AlphaFormat.Premul && destAlpha != AlphaFormat.Premul)
			{
				return ConvertFromPremultiplied(pixel);
			}
			if (sourceAlpha != AlphaFormat.Premul && destAlpha == AlphaFormat.Premul)
			{
				return ConvertToPremultiplied(pixel);
			}
		}
		return pixel;
	}

	private static Rgba8888Pixel ConvertToPremultiplied(Rgba8888Pixel pixel)
	{
		float num = (float)(int)pixel.A / 255f;
		return new Rgba8888Pixel
		{
			R = (byte)((float)(int)pixel.R * num),
			G = (byte)((float)(int)pixel.G * num),
			B = (byte)((float)(int)pixel.B * num),
			A = pixel.A
		};
	}

	private static Rgba8888Pixel ConvertFromPremultiplied(Rgba8888Pixel pixel)
	{
		float num = 1f / ((float)(int)pixel.A / 255f);
		return new Rgba8888Pixel
		{
			R = (byte)((float)(int)pixel.R * num),
			G = (byte)((float)(int)pixel.G * num),
			B = (byte)((float)(int)pixel.B * num),
			A = pixel.A
		};
	}

	public static void Write(ReadOnlySpan<Rgba8888Pixel> pixels, nint dest, PixelSize size, int stride, PixelFormat format, AlphaFormat alphaFormat, AlphaFormat srcAlphaFormat)
	{
		switch (format.FormatEnum)
		{
		case PixelFormatEnum.Rgb565:
			Write<Bgr565PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Rgba8888:
			Write<Rgba8888PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Bgra8888:
			Write<Bgra8888PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.BlackWhite:
			Write<BlackWhitePixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Gray2:
			Write<Gray2PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Gray4:
			Write<Gray4PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Gray8:
			Write<Gray8PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Gray16:
			Write<Gray16PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Gray32Float:
			Write<Gray32FloatPixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Rgba64:
			Write<Rgba64PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Rgb24:
			Write<Rgb24PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Rgb32:
			Write<Rgb32PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Bgr24:
			Write<Bgr24PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Bgr32:
			Write<Bgr32PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Bgr555:
			Write<Bgr555PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		case PixelFormatEnum.Bgr565:
			Write<Bgr565PixelFormatWriter>(pixels, dest, size, stride, alphaFormat, srcAlphaFormat);
			return;
		}
		throw new NotSupportedException($"Pixel format {format} is not supported");
	}
}
