namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:Avalonia.Thickness" /> properties.
/// </summary>
internal class ThicknessAnimator : Animator<Thickness>
{
	public override Thickness Interpolate(double progress, Thickness oldValue, Thickness newValue)
	{
		return (newValue - oldValue) * progress + oldValue;
	}
}
