using System;

namespace Avalonia.Platform;

public class LockedFramebuffer : ILockedFramebuffer, IDisposable
{
	private readonly Action? _onDispose;

	public nint Address { get; }

	public PixelSize Size { get; }

	public int RowBytes { get; }

	public Vector Dpi { get; }

	public PixelFormat Format { get; }

	public AlphaFormat AlphaFormat { get; }

	public LockedFramebuffer(nint address, PixelSize size, int rowBytes, Vector dpi, PixelFormat format, AlphaFormat alphaFormat, Action? onDispose)
	{
		_onDispose = onDispose;
		Address = address;
		Size = size;
		RowBytes = rowBytes;
		Dpi = dpi;
		Format = format;
		AlphaFormat = alphaFormat;
	}

	public void Dispose()
	{
		_onDispose?.Invoke();
	}
}
