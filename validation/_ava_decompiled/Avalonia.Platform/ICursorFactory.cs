using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;

namespace Avalonia.Platform;

[PrivateApi]
public interface ICursorFactory
{
	ICursorImpl GetCursor(StandardCursorType cursorType);

	ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot);
}
