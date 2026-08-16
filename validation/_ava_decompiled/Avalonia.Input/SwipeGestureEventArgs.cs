using System;
using System.Threading;
using Avalonia.Interactivity;

namespace Avalonia.Input;

/// <summary>
/// Provides data for swipe gesture events.
/// </summary>
public class SwipeGestureEventArgs : RoutedEventArgs
{
	private static int s_nextId;

	/// <summary>
	/// Gets the unique identifier for this gesture sequence.
	/// </summary>
	public int Id { get; }

	/// <summary>
	/// Gets the pixel delta since the last event.
	/// </summary>
	public Vector Delta { get; }

	/// <summary>
	/// Gets the current swipe velocity in pixels per second.
	/// </summary>
	public Vector Velocity { get; }

	/// <summary>
	/// Gets the direction of the dominant swipe axis.
	/// </summary>
	public SwipeDirection SwipeDirection { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Input.SwipeGestureEventArgs" /> class.
	/// </summary>
	/// <param name="id">The unique identifier for this gesture.</param>
	/// <param name="delta">The pixel delta since the last event.</param>
	/// <param name="velocity">The current swipe velocity in pixels per second.</param>
	public SwipeGestureEventArgs(int id, Vector delta, Vector velocity)
		: base(InputElement.SwipeGestureEvent)
	{
		Id = id;
		Delta = delta;
		Velocity = velocity;
		SwipeDirection = ((!(Math.Abs(delta.X) >= Math.Abs(delta.Y))) ? ((delta.Y <= 0.0) ? SwipeDirection.Down : SwipeDirection.Up) : ((delta.X <= 0.0) ? SwipeDirection.Right : SwipeDirection.Left));
	}

	internal static int GetNextFreeId()
	{
		return Interlocked.Increment(ref s_nextId);
	}
}
