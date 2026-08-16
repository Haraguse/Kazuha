namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:System.Boolean" /> properties.
/// </summary>
internal class BoolAnimator : Animator<bool>
{
	/// <inheritdoc />
	public override bool Interpolate(double progress, bool oldValue, bool newValue)
	{
		if (progress >= 1.0)
		{
			return newValue;
		}
		return oldValue;
	}
}
