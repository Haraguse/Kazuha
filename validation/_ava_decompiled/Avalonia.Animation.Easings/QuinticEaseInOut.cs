namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases a <see cref="T:System.Double" /> value 
/// using a piece-wise quartic equation.
/// </summary>
public class QuinticEaseInOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		if (progress < 0.5)
		{
			double num = progress * progress;
			return 16.0 * num * num * progress;
		}
		double num2 = 2.0 * progress - 2.0;
		double num3 = num2 * num2;
		return 0.5 * num3 * num3 * num2 + 1.0;
	}
}
