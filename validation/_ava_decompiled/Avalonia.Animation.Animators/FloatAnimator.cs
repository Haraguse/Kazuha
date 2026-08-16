namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:System.Single" /> properties.
/// </summary>
internal class FloatAnimator : Animator<float>
{
	/// <inheritdoc />
	public override float Interpolate(double progress, float oldValue, float newValue)
	{
		return (float)((double)(newValue - oldValue) * progress + (double)oldValue);
	}
}
