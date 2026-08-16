using System;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.Imaging;

/// <summary>
/// Holds a bitmap image.
/// </summary>
public class Bitmap : IBitmap, IImage, IDisposable, IImageBrushSource
{
	private readonly bool _isTranscoded;

	/// <inheritdoc />
	public Vector Dpi => PlatformImpl.Item.Dpi;

	/// <inheritdoc />
	public Size Size => PlatformImpl.Item.PixelSize.ToSizeWithDpi(Dpi);

	/// <inheritdoc />
	public PixelSize PixelSize => PlatformImpl.Item.PixelSize;

	/// <summary>
	/// Gets the platform-specific bitmap implementation.
	/// </summary>
	internal IRef<IBitmapImpl> PlatformImpl { get; }

	IRef<IBitmapImpl> IBitmap.PlatformImpl => PlatformImpl;

	public virtual PixelFormat? Format => (PlatformImpl.Item as IReadableBitmapImpl)?.Format;

	public virtual AlphaFormat? AlphaFormat => (PlatformImpl.Item as IReadableBitmapImpl)?.AlphaFormat;

	IRef<IBitmapImpl>? IImageBrushSource.Bitmap
	{
		get
		{
			if (!PlatformImpl.IsAlive)
			{
				return null;
			}
			return PlatformImpl;
		}
	}

	/// <summary>
	/// Loads a Bitmap from a stream and decodes at the desired width. Aspect ratio is maintained.
	/// This is more efficient than loading and then resizing.
	/// </summary>
	/// <param name="stream">The stream to read the bitmap from. This can be any supported image format.</param>
	/// <param name="width">The desired width of the resulting bitmap.</param>
	/// <param name="interpolationMode">The <see cref="T:Avalonia.Media.Imaging.BitmapInterpolationMode" /> to use should any scaling be required.</param>
	/// <returns>An instance of the <see cref="T:Avalonia.Media.Imaging.Bitmap" /> class.</returns>
	public static Bitmap DecodeToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
	{
		return new Bitmap(GetFactory().LoadBitmapToWidth(stream, width, interpolationMode));
	}

	/// <summary>
	/// Loads a Bitmap from a stream and decodes at the desired height. Aspect ratio is maintained.
	/// This is more efficient than loading and then resizing.
	/// </summary>
	/// <param name="stream">The stream to read the bitmap from. This can be any supported image format.</param>
	/// <param name="height">The desired height of the resulting bitmap.</param>
	/// <param name="interpolationMode">The <see cref="T:Avalonia.Media.Imaging.BitmapInterpolationMode" /> to use should any scaling be required.</param>
	/// <returns>An instance of the <see cref="T:Avalonia.Media.Imaging.Bitmap" /> class.</returns>
	public static Bitmap DecodeToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
	{
		return new Bitmap(GetFactory().LoadBitmapToHeight(stream, height, interpolationMode));
	}

	/// <summary>
	/// Creates a Bitmap scaled to a specified size from the current bitmap.
	/// </summary>        
	/// <param name="destinationSize">The destination size.</param>
	/// <param name="interpolationMode">The <see cref="T:Avalonia.Media.Imaging.BitmapInterpolationMode" /> to use should any scaling be required.</param>
	/// <returns>An instance of the <see cref="T:Avalonia.Media.Imaging.Bitmap" /> class.</returns>
	public Bitmap CreateScaledBitmap(PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
	{
		return new Bitmap(GetFactory().ResizeBitmap(PlatformImpl.Item, destinationSize, interpolationMode));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Imaging.Bitmap" /> class.
	/// </summary>
	/// <param name="fileName">The filename of the bitmap.</param>
	public Bitmap(string fileName)
	{
		PlatformImpl = RefCountable.Create(GetFactory().LoadBitmap(fileName));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Imaging.Bitmap" /> class.
	/// </summary>
	/// <param name="stream">The stream to read the bitmap from.</param>
	public Bitmap(Stream stream)
	{
		PlatformImpl = RefCountable.Create(GetFactory().LoadBitmap(stream));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Imaging.Bitmap" /> class.
	/// </summary>
	/// <param name="impl">A platform-specific bitmap implementation.</param>
	internal Bitmap(IRef<IBitmapImpl> impl)
	{
		PlatformImpl = impl.Clone();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Imaging.Bitmap" /> class.
	/// </summary>
	/// <param name="impl">A platform-specific bitmap implementation. Bitmap class takes the ownership.</param>
	protected Bitmap(IBitmapImpl impl)
	{
		PlatformImpl = RefCountable.Create(impl);
	}

	/// <inheritdoc />
	public virtual void Dispose()
	{
		PlatformImpl.Dispose();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Imaging.Bitmap" /> class.
	/// </summary>
	/// <param name="format">The pixel format.</param>
	/// <param name="alphaFormat">The alpha format.</param>
	/// <param name="data">The pointer to the source bytes.</param>
	/// <param name="size">The size of the bitmap in device pixels.</param>
	/// <param name="dpi">The DPI of the bitmap.</param>
	/// <param name="stride">The number of bytes per row.</param>
	public Bitmap(PixelFormat format, AlphaFormat alphaFormat, nint data, PixelSize size, Vector dpi, int stride)
	{
		IPlatformRenderInterface factory = GetFactory();
		if (factory.IsSupportedBitmapPixelFormat(format))
		{
			PlatformImpl = RefCountable.Create(factory.LoadBitmap(format, alphaFormat, data, size, dpi, stride));
			return;
		}
		using (BitmapMemory bitmapMemory = new BitmapMemory(PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Unpremul, size))
		{
			AlphaFormat alphaFormat2 = (format.HasAlpha ? alphaFormat : Avalonia.Platform.AlphaFormat.Opaque);
			PixelFormatTranscoder.Transcode(data, size, stride, format, alphaFormat, bitmapMemory.Address, bitmapMemory.RowBytes, bitmapMemory.Format, alphaFormat2);
			PlatformImpl = RefCountable.Create(factory.LoadBitmap(PixelFormat.Rgba8888, alphaFormat2, bitmapMemory.Address, size, dpi, bitmapMemory.RowBytes));
		}
		_isTranscoded = true;
	}

	/// <summary>
	/// Saves the bitmap to a file, in PNG format.
	/// </summary>
	/// <param name="fileName">The filename.</param>
	/// <param name="quality">
	/// The optional quality for compression. 
	/// The quality value is interpreted from 0 - 100. If quality is null the default quality 
	/// setting is applied.
	/// </param>
	[Obsolete("Use the overload accepting BitmapEncoderOptions instead.")]
	public void Save(string fileName, int? quality = null)
	{
		Save(fileName, PngBitmapEncoderOptions.Default);
	}

	/// <summary>
	/// Saves the bitmap to a file with the specified options.
	/// </summary>
	/// <param name="fileName">The filename.</param>
	/// <param name="options">
	/// The options specifying the format and settings to use.
	/// Typical usages include <see cref="T:Avalonia.Media.Imaging.PngBitmapEncoderOptions" /> and <see cref="T:Avalonia.Media.Imaging.JpegBitmapEncoderOptions" />.
	/// </param>
	public void Save(string fileName, BitmapEncoderOptions options)
	{
		using FileStream stream = File.Create(fileName);
		Save(stream, options);
	}

	/// <summary>
	/// Saves the bitmap to a stream, in PNG format.
	/// </summary>
	/// <param name="stream">The stream.</param>
	/// <param name="quality">
	/// The optional quality for compression.
	/// The quality value is interpreted from 0 - 100. If quality is null the default quality
	/// setting is applied.
	/// </param>
	[Obsolete("Use the overload accepting BitmapEncoderOptions instead.")]
	public void Save(Stream stream, int? quality = null)
	{
		PlatformImpl.Item.Save(stream, PngBitmapEncoderOptions.Default);
	}

	/// <inheritdoc />
	public void Save(Stream stream, BitmapEncoderOptions options)
	{
		PlatformImpl.Item.Save(stream, options);
	}

	private PixelRect ValidateSourceRect(PixelRect sourceRect)
	{
		if ((sourceRect.Width <= 0 || sourceRect.Height <= 0) && (sourceRect.X != 0 || sourceRect.Y != 0))
		{
			throw new ArgumentOutOfRangeException("sourceRect");
		}
		if (sourceRect.X < 0 || sourceRect.Y < 0)
		{
			throw new ArgumentOutOfRangeException("sourceRect");
		}
		if (sourceRect.Width <= 0)
		{
			sourceRect = sourceRect.WithWidth(PixelSize.Width);
		}
		if (sourceRect.Height <= 0)
		{
			sourceRect = sourceRect.WithHeight(PixelSize.Height);
		}
		if (sourceRect.Right > PixelSize.Width || sourceRect.Bottom > PixelSize.Height)
		{
			throw new ArgumentOutOfRangeException("sourceRect");
		}
		return sourceRect;
	}

	/// <summary>
	/// Performs a row-by-row copy of pixels from a source buffer into a destination buffer.
	/// </summary>
	/// <remarks>
	/// <paramref name="sourceRowBytes" /> is signed and may be negative: a negative value means the
	/// source rows are laid out bottom-up, with <paramref name="sourceAddress" /> pointing at the
	/// first (top) row. The destination <paramref name="stride" /> must be positive and at least the
	/// tightly-packed row size. The caller is responsible for validating <paramref name="sourceRect" />
	/// against the source bounds (e.g. via <see cref="M:Avalonia.Media.Imaging.Bitmap.ValidateSourceRect(Avalonia.PixelRect)" />).
	/// </remarks>
	internal unsafe static void CopyPixelsCore(PixelRect sourceRect, nint sourceAddress, int sourceRowBytes, PixelFormat sourceFormat, nint buffer, int bufferSize, int stride)
	{
		int num = checked(sourceRect.Width * sourceFormat.BitsPerPixel + 7) / 8;
		if (stride < num)
		{
			throw new ArgumentOutOfRangeException("stride");
		}
		long num2 = (long)stride * (long)sourceRect.Height;
		if (num2 > bufferSize)
		{
			throw new ArgumentOutOfRangeException("bufferSize");
		}
		int num3 = checked(sourceRect.X * sourceFormat.BitsPerPixel + 7) / 8;
		if (num3 == 0 && sourceRowBytes == stride && stride == num)
		{
			Unsafe.CopyBlock(((IntPtr)buffer).ToPointer(), ((IntPtr)(sourceAddress + sourceRowBytes * sourceRect.Y)).ToPointer(), (uint)num2);
			return;
		}
		for (int i = 0; i < sourceRect.Height; i++)
		{
			nint num4 = sourceAddress + sourceRowBytes * (sourceRect.Y + i) + num3;
			Unsafe.CopyBlock(((IntPtr)(buffer + stride * i)).ToPointer(), ((IntPtr)num4).ToPointer(), (uint)num);
		}
	}

	/// <summary>
	/// Validates <paramref name="sourceRect" /> against this bitmap and copies pixels out of the
	/// given framebuffer. Self-contained convenience wrapper around the static
	/// <see cref="M:Avalonia.Media.Imaging.Bitmap.CopyPixelsCore(Avalonia.PixelRect,System.IntPtr,System.Int32,Avalonia.Platform.PixelFormat,System.IntPtr,System.Int32,System.Int32)" /> for inheritors.
	/// </summary>
	private protected void CopyPixelsCore(PixelRect sourceRect, nint buffer, int bufferSize, int stride, ILockedFramebuffer fb)
	{
		sourceRect = ValidateSourceRect(sourceRect);
		CopyPixelsCore(sourceRect, fb.Address, fb.RowBytes, fb.Format, buffer, bufferSize, stride);
	}

	public virtual void CopyPixels(PixelRect sourceRect, nint buffer, int bufferSize, int stride)
	{
		if (!Format.HasValue || !(PlatformImpl.Item is IReadableBitmapImpl readableBitmapImpl) || Format != readableBitmapImpl.Format)
		{
			throw new NotSupportedException("CopyPixels is not supported for this bitmap type");
		}
		if (_isTranscoded)
		{
			throw new NotSupportedException("CopyPixels is not supported for transcoded bitmaps");
		}
		using ILockedFramebuffer fb = readableBitmapImpl.Lock();
		CopyPixelsCore(sourceRect, buffer, bufferSize, stride, fb);
	}

	/// <summary>
	/// Copies pixels to the target buffer and transcodes the pixel and alpha format if needed.
	/// </summary>
	/// <param name="buffer">The target buffer.</param>
	/// <exception cref="T:System.NotSupportedException"></exception>
	public void CopyPixels(ILockedFramebuffer buffer)
	{
		if (!(PlatformImpl.Item is IReadableBitmapImpl { Format: not null, AlphaFormat: not null } readableBitmapImpl))
		{
			using (RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(PixelSize))
			{
				using (DrawingContext drawingContext = renderTargetBitmap.CreateDrawingContext())
				{
					drawingContext.DrawImage(this, new Rect(renderTargetBitmap.Size));
				}
				renderTargetBitmap.CopyPixels(buffer);
				return;
			}
		}
		PixelFormat format = buffer.Format;
		PixelFormat? format2 = readableBitmapImpl.Format;
		if (!format2.HasValue || format != format2.GetValueOrDefault() || buffer.AlphaFormat != readableBitmapImpl.AlphaFormat)
		{
			using (ILockedFramebuffer lockedFramebuffer = readableBitmapImpl.Lock())
			{
				PixelFormatTranscoder.Transcode(lockedFramebuffer.Address, lockedFramebuffer.Size, lockedFramebuffer.RowBytes, lockedFramebuffer.Format, lockedFramebuffer.AlphaFormat, buffer.Address, buffer.RowBytes, buffer.Format, buffer.AlphaFormat);
				return;
			}
		}
		using ILockedFramebuffer lockedFramebuffer2 = readableBitmapImpl.Lock();
		CopyPixelsCore(new PixelRect(lockedFramebuffer2.Size), buffer.Address, buffer.RowBytes * buffer.Size.Height, lockedFramebuffer2.RowBytes, lockedFramebuffer2);
	}

	/// <inheritdoc />
	void IImage.Draw(DrawingContext context, Rect sourceRect, Rect destRect)
	{
		context.DrawBitmap(PlatformImpl, 1.0, sourceRect, destRect);
	}

	private static IPlatformRenderInterface GetFactory()
	{
		return AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
	}
}
