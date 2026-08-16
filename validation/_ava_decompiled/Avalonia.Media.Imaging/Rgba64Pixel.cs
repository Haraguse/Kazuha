namespace Avalonia.Media.Imaging;

internal record struct Rgba64Pixel
{
	public ushort R;

	public ushort G;

	public ushort B;

	public ushort A;

	public Rgba64Pixel(ushort r, ushort g, ushort b, ushort a)
	{
		R = r;
		G = g;
		B = b;
		A = a;
	}
}
