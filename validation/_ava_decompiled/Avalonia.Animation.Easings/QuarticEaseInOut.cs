namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases a <see cref="T:System.Double" /> value 
/// using a piece-wise quartic equation.
/// </summary>
public class QuarticEaseInOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		if (progress < 0.5)
		{
			double num = progress * progress;
			return 8.0 * num * num;
		}
		double num2 = progress - 1.0;
		double num3 = num2 * num2;
		return -8.0 * num3 * num3 + 1.0;
	}
}
