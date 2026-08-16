using System;
using Avalonia.Platform;
using Avalonia.Platform.Internal;

namespace Avalonia.Media.Imaging;

internal static class PixelFormatTranscoder
{
	public unsafe static void Transcode(nint source, PixelSize srcSize, int sourceStride, PixelFormat srcFormat, AlphaFormat srcAlphaFormat, nint dest, int destStride, PixelFormat destFormat, AlphaFormat destAlphaFormat)
	{
		int num = srcSize.Width * srcSize.Height;
		using UnmanagedBlob unmanagedBlob = new UnmanagedBlob(num * sizeof(Rgba8888Pixel));
		Span<Rgba8888Pixel> span = new Span<Rgba8888Pixel>((void*)unmanagedBlob.Address, num);
		PixelFormatReader.Read(span, source, srcSize, sourceStride, srcFormat);
		PixelFormatWriter.Write(span, dest, srcSize, destStride, destFormat, destAlphaFormat, srcAlphaFormat);
	}
}
