using Avalonia.Interactivity;

namespace Avalonia.Input;

public sealed class ScrollGestureInertiaStartingEventArgs : RoutedEventArgs
{
	public int Id { get; }

	public Vector Inertia { get; }

	internal ScrollGestureInertiaStartingEventArgs(int id, Vector inertia)
		: base(InputElement.ScrollGestureInertiaStartingEvent)
	{
		Id = id;
		Inertia = inertia;
	}
}
