namespace Avalonia.Media.Imaging;

internal interface IPixelFormatWriter
{
	void WriteNext(Rgba8888Pixel pixel);

	void Reset(nint address);
}
