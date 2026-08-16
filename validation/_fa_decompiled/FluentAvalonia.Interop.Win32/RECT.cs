using Avalonia;

namespace FluentAvalonia.Interop.Win32;

internal struct RECT
{
	public int left;

	public int top;

	public int right;

	public int bottom;

	public int Width => right - left;

	public int Height => bottom - top;

	public RECT(Rect rect)
	{
		left = (int)((Rect)(ref rect)).X;
		top = (int)((Rect)(ref rect)).Y;
		right = (int)(((Rect)(ref rect)).X + ((Rect)(ref rect)).Width);
		bottom = (int)(((Rect)(ref rect)).Y + ((Rect)(ref rect)).Height);
	}

	public RECT(int l, int t, int r, int b)
	{
		left = l;
		top = t;
		right = r;
		bottom = b;
	}
}
