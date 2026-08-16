using Avalonia.Interactivity;

namespace Avalonia.Input;

/// <summary>
/// Provides data for the swipe gesture ended event.
/// </summary>
public class SwipeGestureEndedEventArgs : RoutedEventArgs
{
	/// <summary>
	/// Gets the unique identifier for this gesture sequence.
	/// </summary>
	public int Id { get; }

	/// <summary>
	/// Gets the swipe velocity at release in pixels per second.
	/// </summary>
	public Vector Velocity { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Input.SwipeGestureEndedEventArgs" /> class.
	/// </summary>
	/// <param name="id">The unique identifier for this gesture.</param>
	/// <param name="velocity">The swipe velocity at release in pixels per second.</param>
	public SwipeGestureEndedEventArgs(int id, Vector velocity)
		: base(InputElement.SwipeGestureEndedEvent)
	{
		Id = id;
		Velocity = velocity;
	}
}
