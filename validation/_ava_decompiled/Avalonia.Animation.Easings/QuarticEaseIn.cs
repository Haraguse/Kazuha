namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases in a <see cref="T:System.Double" /> value 
/// using a quartic equation.
/// </summary>
public class QuarticEaseIn : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		double num = progress * progress;
		return num * num;
	}
}
