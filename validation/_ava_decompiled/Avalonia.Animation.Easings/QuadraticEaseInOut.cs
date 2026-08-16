namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases a <see cref="T:System.Double" /> value 
/// using a piece-wise quadratic function.
/// </summary>
public class QuadraticEaseInOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		if (progress < 0.5)
		{
			return 2.0 * progress * progress;
		}
		return progress * (-2.0 * progress + 4.0) - 1.0;
	}
}
