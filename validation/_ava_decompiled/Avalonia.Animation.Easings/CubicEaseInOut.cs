namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases a <see cref="T:System.Double" /> value 
/// using a piece-wise cubic equation.
/// </summary>
public class CubicEaseInOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		if (progress < 0.5)
		{
			return 4.0 * progress * progress * progress;
		}
		double num = 2.0 * (progress - 1.0);
		return 0.5 * num * num * num + 1.0;
	}
}
