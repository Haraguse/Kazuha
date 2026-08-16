using Avalonia.Interactivity;

namespace Avalonia.Input;

public class PinchEndedEventArgs : RoutedEventArgs
{
	public PinchEndedEventArgs()
		: base(InputElement.PinchEndedEvent)
	{
	}
}
