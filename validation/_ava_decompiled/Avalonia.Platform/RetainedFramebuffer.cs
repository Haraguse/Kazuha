using System;
using Avalonia.Platform.Internal;

namespace Avalonia.Platform;

internal class RetainedFramebuffer : IDisposable
{
	private UnmanagedBlob? _blob;

	public PixelSize Size { get; }

	public int RowBytes { get; }

	public PixelFormat Format { get; }

	public AlphaFormat AlphaFormat { get; }

	public nint Address => (_blob ?? throw new ObjectDisposedException("RetainedFramebuffer")).Address;

	private static PixelFormat ValidateKnownFormat(PixelFormat format)
	{
		if (format.BitsPerPixel % 8 != 0)
		{
			throw new ArgumentOutOfRangeException("format");
		}
		return format;
	}

	public RetainedFramebuffer(PixelSize size, PixelFormat format, AlphaFormat alphaFormat)
		: this(size, ValidateKnownFormat(format), alphaFormat, format.BitsPerPixel / 8 * size.Width)
	{
	}

	public RetainedFramebuffer(PixelSize size, PixelFormat format, AlphaFormat alphaFormat, int rowBytes)
	{
		if (size.Width <= 0 || size.Height <= 0)
		{
			throw new ArgumentOutOfRangeException("size");
		}
		if (size.Width * (format.BitsPerPixel / 8) > rowBytes)
		{
			throw new ArgumentOutOfRangeException("rowBytes");
		}
		Size = size;
		RowBytes = rowBytes;
		Format = format;
		AlphaFormat = alphaFormat;
		_blob = new UnmanagedBlob(RowBytes * size.Height);
	}

	public ILockedFramebuffer Lock(Vector dpi, Action<RetainedFramebuffer> blit)
	{
		if (_blob == null)
		{
			throw new ObjectDisposedException("RetainedFramebuffer");
		}
		return new LockedFramebuffer(_blob.Address, Size, RowBytes, dpi, Format, AlphaFormat, delegate
		{
			blit(this);
			GC.KeepAlive(this);
		});
	}

	public void Dispose()
	{
		_blob?.Dispose();
		_blob = null;
	}
}
