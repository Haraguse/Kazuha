using Avalonia.Interactivity;

namespace Avalonia.Input;

public class HoldingRoutedEventArgs : RoutedEventArgs
{
	/// <summary>
	/// Gets the state of the <see cref="F:Avalonia.Input.InputElement.HoldingEvent" /> event.
	/// </summary>
	public HoldingState HoldingState { get; }

	/// <summary>
	/// Gets the location of the touch, mouse, or pen/stylus contact.
	/// </summary>
	public Point Position { get; }

	/// <summary>
	/// Gets the pointer type of the input source.
	/// </summary>
	public PointerType PointerType { get; }

	internal PointerEventArgs PointerEventArgs { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Input.HoldingRoutedEventArgs" /> class.
	/// </summary>
	internal HoldingRoutedEventArgs(HoldingState holdingState, Point position, PointerType pointerType, PointerEventArgs pointerEventArgs)
		: base(InputElement.HoldingEvent)
	{
		PointerEventArgs = pointerEventArgs;
		HoldingState = holdingState;
		Position = position;
		PointerType = pointerType;
	}
}
