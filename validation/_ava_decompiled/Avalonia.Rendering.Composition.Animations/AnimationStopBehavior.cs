namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
/// Specifies the behavior of an animation when it stops.
/// </summary>
public enum AnimationStopBehavior
{
	/// <summary>
	/// Leave the animation at its current value.
	/// </summary>
	LeaveCurrentValue,
	/// <summary>
	/// Reset the animation to its initial value.
	/// </summary>
	SetToInitialValue,
	/// <summary>
	/// Set the animation to its final value.
	/// </summary>
	SetToFinalValue
}
