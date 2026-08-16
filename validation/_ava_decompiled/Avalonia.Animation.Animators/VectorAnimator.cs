namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:Avalonia.Vector" /> properties.
/// </summary>
internal class VectorAnimator : Animator<Vector>
{
	public override Vector Interpolate(double progress, Vector oldValue, Vector newValue)
	{
		return (newValue - oldValue) * progress + oldValue;
	}
}
