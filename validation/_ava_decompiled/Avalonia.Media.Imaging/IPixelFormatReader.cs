namespace Avalonia.Media.Imaging;

internal interface IPixelFormatReader
{
	Rgba8888Pixel ReadNext();

	void Reset(nint address);
}
