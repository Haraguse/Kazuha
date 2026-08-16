using Avalonia.Interactivity;

namespace Avalonia.Input;

public class PullGestureEndedEventArgs : RoutedEventArgs
{
	public int Id { get; }

	public PullDirection PullDirection { get; }

	public PullGestureEndedEventArgs(int id, PullDirection pullDirection)
		: base(InputElement.PullGestureEndedEvent)
	{
		Id = id;
		PullDirection = pullDirection;
	}
}
