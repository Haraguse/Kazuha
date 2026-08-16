using Avalonia.Interactivity;

namespace Avalonia.Input;

public class TappedEventArgs : RoutedEventArgs, IKeyModifiersEventArgs
{
	private readonly PointerEventArgs lastPointerEventArgs;

	public IPointer Pointer => lastPointerEventArgs.Pointer;

	public KeyModifiers KeyModifiers => lastPointerEventArgs.KeyModifiers;

	public ulong Timestamp => lastPointerEventArgs.Timestamp;

	public TappedEventArgs(RoutedEvent? routedEvent, PointerEventArgs lastPointerEventArgs)
		: base(routedEvent)
	{
		this.lastPointerEventArgs = lastPointerEventArgs;
	}

	public Point GetPosition(Visual? relativeTo)
	{
		return lastPointerEventArgs.GetPosition(relativeTo);
	}
}
