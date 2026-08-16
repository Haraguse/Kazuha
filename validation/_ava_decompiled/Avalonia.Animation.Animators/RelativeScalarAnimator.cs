namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:Avalonia.RelativeScalar" /> properties.
/// </summary>
internal class RelativeScalarAnimator : Animator<RelativeScalar>
{
	private static readonly DoubleAnimator s_scalarAnimator = new DoubleAnimator();

	public override RelativeScalar Interpolate(double progress, RelativeScalar oldValue, RelativeScalar newValue)
	{
		if (oldValue.Unit != newValue.Unit)
		{
			if (!(progress >= 0.5))
			{
				return oldValue;
			}
			return newValue;
		}
		return new RelativeScalar(s_scalarAnimator.Interpolate(progress, oldValue.Scalar, newValue.Scalar), oldValue.Unit);
	}
}
