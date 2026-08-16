namespace Avalonia.Input;

public class PointerReleasedEventArgs : PointerEventArgs
{
	/// <summary>
	/// Gets the mouse button that triggered the corresponding PointerPressed event
	/// </summary>
	public MouseButton InitialPressMouseButton { get; }

	public PointerReleasedEventArgs(object? source, IPointer pointer, Visual rootVisual, Point rootVisualPosition, ulong timestamp, PointerPointProperties properties, KeyModifiers modifiers, MouseButton initialPressMouseButton)
		: base(InputElement.PointerReleasedEvent, source, pointer, rootVisual, rootVisualPosition, timestamp, properties, modifiers)
	{
		InitialPressMouseButton = initialPressMouseButton;
	}
}
