using System;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Platform;

namespace Avalonia.Media.Imaging;

/// <summary>
/// Holds a writeable bitmap image.
/// </summary>
public class WriteableBitmap : Bitmap
{
	private readonly BitmapMemory? _pixelFormatMemory;

	public override PixelFormat? Format
	{
		get
		{
			BitmapMemory? pixelFormatMemory = _pixelFormatMemory;
			if (pixelFormatMemory == null)
			{
				return base.Format;
			}
			return pixelFormatMemory.Format;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Imaging.WriteableBitmap" /> class.
	/// </summary>
	/// <param name="size">The size of the bitmap in device pixels.</param>
	/// <param name="dpi">The DPI of the bitmap.</param>
	/// <param name="format">The pixel format (optional).</param>
	/// <param name="alphaFormat">The alpha format (optional).</param>
	/// <returns>An instance of the <see cref="T:Avalonia.Media.Imaging.WriteableBitmap" /> class.</returns>
	public WriteableBitmap(PixelSize size, Vector dpi, PixelFormat? format = null, AlphaFormat? alphaFormat = null)
		: this(CreatePlatformImpl(size, in dpi, format, alphaFormat))
	{
	}

	private WriteableBitmap((IBitmapImpl impl, BitmapMemory? mem) bitmapWithMem)
		: this(bitmapWithMem.impl, bitmapWithMem.mem)
	{
	}

	private WriteableBitmap(IBitmapImpl impl, BitmapMemory? pixelFormatMemory = null)
		: base(impl)
	{
		_pixelFormatMemory = pixelFormatMemory;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Imaging.WriteableBitmap" /> class with existing pixel data
	/// The data is copied to the bitmap
	/// </summary>
	/// <param name="format">The pixel format.</param>
	/// <param name="alphaFormat">The alpha format.</param>
	/// <param name="data">The pointer to the source bytes.</param>
	/// <param name="size">The size of the bitmap in device pixels.</param>
	/// <param name="dpi">The DPI of the bitmap.</param>
	/// <param name="stride">The number of bytes per row.</param>
	public unsafe WriteableBitmap(PixelFormat format, AlphaFormat alphaFormat, nint data, PixelSize size, Vector dpi, int stride)
		: this(size, dpi, format, alphaFormat)
	{
		int num = (format.BitsPerPixel * size.Width + 7) / 8;
		if (num > stride)
		{
			throw new ArgumentOutOfRangeException("stride");
		}
		using ILockedFramebuffer lockedFramebuffer = Lock();
		for (int i = 0; i < size.Height; i++)
		{
			Unsafe.CopyBlock(((IntPtr)(lockedFramebuffer.Address + lockedFramebuffer.RowBytes * i)).ToPointer(), ((IntPtr)(data + i * stride)).ToPointer(), (uint)num);
		}
	}

	public ILockedFramebuffer Lock()
	{
		if (_pixelFormatMemory == null)
		{
			return ((IWriteableBitmapImpl)base.PlatformImpl.Item).Lock();
		}
		return new LockedFramebuffer(_pixelFormatMemory.Address, _pixelFormatMemory.Size, _pixelFormatMemory.RowBytes, base.Dpi, _pixelFormatMemory.Format, _pixelFormatMemory.AlphaFormat, delegate
		{
			using ILockedFramebuffer lockedFramebuffer = ((IWriteableBitmapImpl)base.PlatformImpl.Item).Lock();
			_pixelFormatMemory.CopyToRgba(lockedFramebuffer.AlphaFormat, lockedFramebuffer.Address, lockedFramebuffer.RowBytes);
		});
	}

	public override void CopyPixels(PixelRect sourceRect, nint buffer, int bufferSize, int stride)
	{
		using ILockedFramebuffer fb = Lock();
		CopyPixelsCore(sourceRect, buffer, bufferSize, stride, fb);
	}

	public static WriteableBitmap Decode(Stream stream)
	{
		return new WriteableBitmap(GetFactory().LoadWriteableBitmap(stream));
	}

	/// <summary>
	/// Loads a WriteableBitmap from a stream and decodes at the desired width. Aspect ratio is maintained.
	/// This is more efficient than loading and then resizing.
	/// </summary>
	/// <param name="stream">The stream to read the bitmap from. This can be any supported image format.</param>
	/// <param name="width">The desired width of the resulting bitmap.</param>
	/// <param name="interpolationMode">The <see cref="T:Avalonia.Media.Imaging.BitmapInterpolationMode" /> to use should any scaling be required.</param>
	/// <returns>An instance of the <see cref="T:Avalonia.Media.Imaging.WriteableBitmap" /> class.</returns>
	public new static WriteableBitmap DecodeToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
	{
		return new WriteableBitmap(GetFactory().LoadWriteableBitmapToWidth(stream, width, interpolationMode));
	}

	/// <summary>
	/// Loads a Bitmap from a stream and decodes at the desired height. Aspect ratio is maintained.
	/// This is more efficient than loading and then resizing.
	/// </summary>
	/// <param name="stream">The stream to read the bitmap from. This can be any supported image format.</param>
	/// <param name="height">The desired height of the resulting bitmap.</param>
	/// <param name="interpolationMode">The <see cref="T:Avalonia.Media.Imaging.BitmapInterpolationMode" /> to use should any scaling be required.</param>
	/// <returns>An instance of the <see cref="T:Avalonia.Media.Imaging.WriteableBitmap" /> class.</returns>
	public new static WriteableBitmap DecodeToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
	{
		return new WriteableBitmap(GetFactory().LoadWriteableBitmapToHeight(stream, height, interpolationMode));
	}

	private static (IBitmapImpl, BitmapMemory?) CreatePlatformImpl(PixelSize size, in Vector dpi, PixelFormat? format, AlphaFormat? alphaFormat)
	{
		if (size.Width <= 0 || size.Height <= 0)
		{
			throw new ArgumentException("Size should be >= (1,1)", "size");
		}
		IPlatformRenderInterface factory = GetFactory();
		PixelFormat pixelFormat = format ?? factory.DefaultPixelFormat;
		AlphaFormat alphaFormat2 = alphaFormat ?? factory.DefaultAlphaFormat;
		if (factory.IsSupportedBitmapPixelFormat(pixelFormat))
		{
			return (factory.CreateWriteableBitmap(size, dpi, pixelFormat, alphaFormat2), null);
		}
		if (!PixelFormatReader.SupportsFormat(pixelFormat))
		{
			throw new NotSupportedException($"Pixel format {pixelFormat} is not supported");
		}
		alphaFormat2 = (pixelFormat.HasAlpha ? alphaFormat2 : Avalonia.Platform.AlphaFormat.Opaque);
		return (factory.CreateWriteableBitmap(size, dpi, PixelFormat.Rgba8888, alphaFormat2), new BitmapMemory(pixelFormat, alphaFormat2, size));
	}

	private static IPlatformRenderInterface GetFactory()
	{
		return AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
	}
}
