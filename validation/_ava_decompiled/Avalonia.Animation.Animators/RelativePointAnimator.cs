namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:Avalonia.RelativePoint" /> properties.
/// </summary>
internal class RelativePointAnimator : Animator<RelativePoint>
{
	private static readonly PointAnimator s_pointAnimator = new PointAnimator();

	public override RelativePoint Interpolate(double progress, RelativePoint oldValue, RelativePoint newValue)
	{
		if (oldValue.Unit != newValue.Unit)
		{
			if (!(progress >= 0.5))
			{
				return oldValue;
			}
			return newValue;
		}
		return new RelativePoint(s_pointAnimator.Interpolate(progress, oldValue.Point, newValue.Point), oldValue.Unit);
	}
}
