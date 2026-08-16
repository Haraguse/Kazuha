namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases out a <see cref="T:System.Double" /> value 
/// using a quadratic function.
/// </summary>
public class QuadraticEaseOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		return 0.0 - progress * (progress - 2.0);
	}
}
