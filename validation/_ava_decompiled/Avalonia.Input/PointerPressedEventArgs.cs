using Avalonia.Metadata;

namespace Avalonia.Input;

public class PointerPressedEventArgs : PointerEventArgs
{
	public int ClickCount { get; }

	public PointerPressedEventArgs(object? source, IPointer pointer, Visual rootVisual, Point rootVisualPosition, ulong timestamp, PointerPointProperties properties, KeyModifiers modifiers, int clickCount = 1)
		: base(InputElement.PointerPressedEvent, source, pointer, rootVisual, rootVisualPosition, timestamp, properties, modifiers)
	{
		ClickCount = clickCount;
	}

	[PrivateApi]
	public PointerPressedEventArgs(object? source, IPointer pointer, Visual rootVisual, Point rootVisualPosition, ulong timestamp, PointerPointProperties properties, KeyModifiers modifiers, int clickCount, object? platformInputEventCookie)
		: base(InputElement.PointerPressedEvent, source, pointer, rootVisual, rootVisualPosition, timestamp, properties, modifiers, null, platformInputEventCookie)
	{
		ClickCount = clickCount;
	}
}
