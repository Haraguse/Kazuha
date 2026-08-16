namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:Avalonia.Size" /> properties.
/// </summary>
internal class SizeAnimator : Animator<Size>
{
	public override Size Interpolate(double progress, Size oldValue, Size newValue)
	{
		return (newValue - oldValue) * progress + oldValue;
	}
}
