namespace Avalonia.Media.Imaging;

internal record struct Rgba8888Pixel
{
	public byte R;

	public byte G;

	public byte B;

	public byte A;

	public Rgba8888Pixel(byte r, byte g, byte b, byte a)
	{
		R = r;
		G = g;
		B = b;
		A = a;
	}
}
