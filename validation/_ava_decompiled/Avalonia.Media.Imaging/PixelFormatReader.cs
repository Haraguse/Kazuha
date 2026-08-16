using System;
using Avalonia.Platform;

namespace Avalonia.Media.Imaging;

internal static class PixelFormatReader
{
	public struct BlackWhitePixelFormatReader : IPixelFormatReader
	{
		private int _bit;

		private unsafe byte* _address;

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
			_bit = 0;
		}

		public unsafe Rgba8888Pixel ReadNext()
		{
			int num = 7 - _bit;
			int num2 = (*_address >> num) & 1;
			_bit++;
			if (_bit == 8)
			{
				_address++;
				_bit = 0;
			}
			if (num2 != 1)
			{
				return s_black;
			}
			return s_white;
		}
	}

	public struct Gray2PixelFormatReader : IPixelFormatReader
	{
		private int _bit;

		private unsafe byte* _address;

		private static readonly Rgba8888Pixel[] Palette = new Rgba8888Pixel[4]
		{
			s_black,
			new Rgba8888Pixel
			{
				A = byte.MaxValue,
				B = 85,
				G = 85,
				R = 85
			},
			new Rgba8888Pixel
			{
				A = byte.MaxValue,
				B = 170,
				G = 170,
				R = 170
			},
			s_white
		};

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
			_bit = 0;
		}

		public unsafe Rgba8888Pixel ReadNext()
		{
			int num = 6 - _bit;
			byte b = (byte)(*_address >> num);
			b &= 3;
			_bit += 2;
			if (_bit == 8)
			{
				_address++;
				_bit = 0;
			}
			return Palette[b];
		}
	}

	public struct Gray4PixelFormatReader : IPixelFormatReader
	{
		private int _bit;

		private unsafe byte* _address;

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
			_bit = 0;
		}

		public unsafe Rgba8888Pixel ReadNext()
		{
			int num = 4 - _bit;
			byte b = (byte)(*_address >> num);
			b &= 0xF;
			b = (byte)(b | (b << 4));
			_bit += 4;
			if (_bit == 8)
			{
				_address++;
				_bit = 0;
			}
			return new Rgba8888Pixel
			{
				A = byte.MaxValue,
				B = b,
				G = b,
				R = b
			};
		}
	}

	public struct Gray8PixelFormatReader : IPixelFormatReader
	{
		private unsafe byte* _address;

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}

		public unsafe Rgba8888Pixel ReadNext()
		{
			byte address = *_address;
			_address++;
			return new Rgba8888Pixel
			{
				A = byte.MaxValue,
				B = address,
				G = address,
				R = address
			};
		}
	}

	public struct Gray16PixelFormatReader : IPixelFormatReader
	{
		private unsafe ushort* _address;

		public unsafe Rgba8888Pixel ReadNext()
		{
			ushort address = *_address;
			_address++;
			byte b = (byte)(address >> 8);
			return new Rgba8888Pixel
			{
				A = byte.MaxValue,
				B = b,
				G = b,
				R = b
			};
		}

		public unsafe void Reset(nint address)
		{
			_address = (ushort*)address;
		}
	}

	public struct Gray32FloatPixelFormatReader : IPixelFormatReader
	{
		private unsafe byte* _address;

		public unsafe Rgba8888Pixel ReadNext()
		{
			byte b = (byte)(Math.Pow(*(float*)_address, 0.45454545454545453) * 255.0);
			_address += 4;
			return new Rgba8888Pixel
			{
				A = byte.MaxValue,
				B = b,
				G = b,
				R = b
			};
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Rgba64PixelFormatReader : IPixelFormatReader
	{
		private unsafe Rgba64Pixel* _address;

		public unsafe Rgba8888Pixel ReadNext()
		{
			Rgba64Pixel address = *_address;
			_address++;
			return new Rgba8888Pixel
			{
				A = (byte)(address.A >> 8),
				B = (byte)(address.B >> 8),
				G = (byte)(address.G >> 8),
				R = (byte)(address.R >> 8)
			};
		}

		public unsafe void Reset(nint address)
		{
			_address = (Rgba64Pixel*)address;
		}
	}

	public struct Rgb24PixelFormatReader : IPixelFormatReader
	{
		private unsafe byte* _address;

		public unsafe Rgba8888Pixel ReadNext()
		{
			byte* address = _address;
			_address += 3;
			return new Rgba8888Pixel
			{
				R = *address,
				G = address[1],
				B = address[2],
				A = byte.MaxValue
			};
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Bgr24PixelFormatReader : IPixelFormatReader
	{
		private unsafe byte* _address;

		public unsafe Rgba8888Pixel ReadNext()
		{
			byte* address = _address;
			_address += 3;
			return new Rgba8888Pixel
			{
				R = address[2],
				G = address[1],
				B = *address,
				A = byte.MaxValue
			};
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Bgr555PixelFormatReader : IPixelFormatReader
	{
		private unsafe byte* _address;

		public unsafe Rgba8888Pixel ReadNext()
		{
			ushort* address = (ushort*)_address;
			_address += 2;
			return UnPack(*address);
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}

		private static Rgba8888Pixel UnPack(ushort value)
		{
			byte r = (byte)Math.Round((float)((value >> 10) & 0x1F) / 31f * 255f);
			byte g = (byte)Math.Round((float)((value >> 5) & 0x1F) / 31f * 255f);
			byte b = (byte)Math.Round((float)(value & 0x1F) / 31f * 255f);
			return new Rgba8888Pixel(r, g, b, byte.MaxValue);
		}
	}

	public struct Bgr565PixelFormatReader : IPixelFormatReader
	{
		private unsafe byte* _address;

		public unsafe Rgba8888Pixel ReadNext()
		{
			ushort* address = (ushort*)_address;
			_address += 2;
			return UnPack(*address);
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}

		private static Rgba8888Pixel UnPack(ushort value)
		{
			byte r = (byte)Math.Round((float)((value >> 11) & 0x1F) / 31f * 255f);
			byte g = (byte)Math.Round((float)((value >> 5) & 0x3F) / 63f * 255f);
			byte b = (byte)Math.Round((float)(value & 0x1F) / 31f * 255f);
			return new Rgba8888Pixel(r, g, b, byte.MaxValue);
		}
	}

	public struct Rgba8888PixelFormatReader : IPixelFormatReader
	{
		private unsafe Rgba8888Pixel* _address;

		public unsafe Rgba8888Pixel ReadNext()
		{
			Rgba8888Pixel address = *_address;
			_address++;
			return address;
		}

		public unsafe void Reset(nint address)
		{
			_address = (Rgba8888Pixel*)address;
		}
	}

	public struct Rgb32PixelFormatReader : IPixelFormatReader
	{
		private unsafe Rgba8888Pixel* _address;

		public unsafe Rgba8888Pixel ReadNext()
		{
			byte* address = (byte*)_address;
			Rgba8888Pixel result = new Rgba8888Pixel(*address, address[1], address[2], byte.MaxValue);
			_address++;
			return result;
		}

		public unsafe void Reset(nint address)
		{
			_address = (Rgba8888Pixel*)address;
		}
	}

	public struct Bgra8888PixelFormatReader : IPixelFormatReader
	{
		private unsafe byte* _address;

		public unsafe Rgba8888Pixel ReadNext()
		{
			byte* address = _address;
			_address += 4;
			return new Rgba8888Pixel(address[2], address[1], *address, address[3]);
		}

		public unsafe void Reset(nint address)
		{
			_address = (byte*)address;
		}
	}

	public struct Bgr32PixelFormatReader : IPixelFormatReader
	{
		private unsafe Rgba8888Pixel* _address;

		public unsafe Rgba8888Pixel ReadNext()
		{
			byte* address = (byte*)_address;
			Rgba8888Pixel result = new Rgba8888Pixel(address[2], address[1], *address, byte.MaxValue);
			_address++;
			return result;
		}

		public unsafe void Reset(nint address)
		{
			_address = (Rgba8888Pixel*)address;
		}
	}

	private static readonly Rgba8888Pixel s_white = new Rgba8888Pixel
	{
		A = byte.MaxValue,
		B = byte.MaxValue,
		G = byte.MaxValue,
		R = byte.MaxValue
	};

	private static readonly Rgba8888Pixel s_black = new Rgba8888Pixel
	{
		A = byte.MaxValue,
		B = 0,
		G = 0,
		R = 0
	};

	public static bool SupportsFormat(PixelFormat format)
	{
		PixelFormatEnum formatEnum = format.FormatEnum;
		if ((uint)formatEnum <= 15u)
		{
			return true;
		}
		return false;
	}

	private static void Read<T>(Span<Rgba8888Pixel> pixels, nint source, PixelSize size, int stride) where T : struct, IPixelFormatReader
	{
		T val = new T();
		int width = size.Width;
		int height = size.Height;
		int num = 0;
		for (int i = 0; i < height; i++)
		{
			val.Reset(source + stride * i);
			for (int j = 0; j < width; j++)
			{
				pixels[num++] = val.ReadNext();
			}
		}
	}

	public static void Read(Span<Rgba8888Pixel> pixels, nint source, PixelSize size, int stride, PixelFormat format)
	{
		switch (format.FormatEnum)
		{
		case PixelFormatEnum.Rgb565:
			Read<Bgr565PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Rgba8888:
			Read<Rgba8888PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Bgra8888:
			Read<Bgra8888PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.BlackWhite:
			Read<BlackWhitePixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Gray2:
			Read<Gray2PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Gray4:
			Read<Gray4PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Gray8:
			Read<Gray8PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Gray16:
			Read<Gray16PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Gray32Float:
			Read<Gray32FloatPixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Rgba64:
			Read<Rgba64PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Rgb24:
			Read<Rgb24PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Rgb32:
			Read<Rgb32PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Bgr24:
			Read<Bgr24PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Bgr32:
			Read<Bgr32PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Bgr555:
			Read<Bgr555PixelFormatReader>(pixels, source, size, stride);
			return;
		case PixelFormatEnum.Bgr565:
			Read<Bgr565PixelFormatReader>(pixels, source, size, stride);
			return;
		}
		throw new NotSupportedException($"Pixel format {format} is not supported");
	}
}
