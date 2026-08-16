namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
/// Specifies the animation delay behavior.
/// </summary>
public enum AnimationDelayBehavior
{
	/// <summary>
	/// If a DelayTime is specified, it delays starting the animation according to delay time and after delay
	/// has expired it applies animation to the object property.
	/// </summary>
	SetInitialValueAfterDelay,
	/// <summary>
	/// Applies the initial value of the animation (i.e. the value at Keyframe 0) to the object before the delay time
	/// is elapsed (when there is a DelayTime specified), it then delays starting the animation according to the DelayTime.
	/// </summary>
	SetInitialValueBeforeDelay
}
