using Avalonia.Interactivity;

namespace Avalonia.Input;

public class PullGestureEventArgs : RoutedEventArgs
{
	private static int _nextId = 1;

	public int Id { get; }

	public Vector Delta { get; }

	public PullDirection PullDirection { get; }

	internal static int GetNextFreeId()
	{
		return _nextId++;
	}

	public PullGestureEventArgs(int id, Vector delta, PullDirection pullDirection)
		: base(InputElement.PullGestureEvent)
	{
		Id = id;
		Delta = delta;
		PullDirection = pullDirection;
	}
}
