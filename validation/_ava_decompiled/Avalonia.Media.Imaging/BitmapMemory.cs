using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace Avalonia.Media.Imaging;

internal class BitmapMemory : IDisposable
{
	private readonly int _memorySize;

	public nint Address { get; private set; }

	public PixelSize Size { get; }

	public int RowBytes { get; }

	public PixelFormat Format { get; }

	public AlphaFormat AlphaFormat { get; }

	public BitmapMemory(PixelFormat format, AlphaFormat alphaFormat, PixelSize size)
	{
		Format = format;
		AlphaFormat = alphaFormat;
		Size = size;
		int num = (format.BitsPerPixel + 7) / 8;
		RowBytes = 4 * ((size.Width * num + 3) / 4);
		_memorySize = RowBytes * size.Height;
		Address = Marshal.AllocHGlobal(_memorySize);
		GC.AddMemoryPressure(_memorySize);
	}

	private void ReleaseUnmanagedResources()
	{
		if (Address != IntPtr.Zero)
		{
			GC.RemoveMemoryPressure(_memorySize);
			Marshal.FreeHGlobal(Address);
		}
	}

	public void Dispose()
	{
		ReleaseUnmanagedResources();
		GC.SuppressFinalize(this);
	}

	~BitmapMemory()
	{
		ReleaseUnmanagedResources();
	}

	public void CopyToRgba(AlphaFormat alphaFormat, nint buffer, int stride)
	{
		PixelFormatTranscoder.Transcode(Address, Size, RowBytes, Format, AlphaFormat, buffer, stride, PixelFormat.Rgba8888, alphaFormat);
	}
}
