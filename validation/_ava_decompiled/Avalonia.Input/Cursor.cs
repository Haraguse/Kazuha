using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Input;

public class Cursor : IDisposable
{
	public static readonly Cursor Default = new Cursor(StandardCursorType.Arrow);

	private readonly string _name;

	internal ICursorImpl PlatformImpl { get; }

	private Cursor(ICursorImpl platformImpl, string name)
	{
		PlatformImpl = platformImpl;
		_name = name;
	}

	public Cursor(StandardCursorType cursorType)
		: this(GetCursorFactory().GetCursor(cursorType), cursorType.ToString())
	{
	}

	public Cursor(Bitmap cursor, PixelPoint hotSpot)
		: this(GetCursorFactory().CreateCursor(cursor, hotSpot), "BitmapCursor")
	{
	}

	public void Dispose()
	{
		PlatformImpl.Dispose();
	}

	public static Cursor Parse(string s)
	{
		if (!Enum.TryParse<StandardCursorType>(s, ignoreCase: true, out var result))
		{
			throw new ArgumentException("Unrecognized cursor type '" + s + "'.");
		}
		return new Cursor(result);
	}

	private static ICursorFactory GetCursorFactory()
	{
		return AvaloniaLocator.Current.GetRequiredService<ICursorFactory>();
	}

	public override string ToString()
	{
		return _name;
	}
}
