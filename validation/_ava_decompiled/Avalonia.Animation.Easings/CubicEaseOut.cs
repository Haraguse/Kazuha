namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases out a <see cref="T:System.Double" /> value 
/// using a cubic equation.
/// </summary>
public class CubicEaseOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		double num = progress - 1.0;
		return num * num * num + 1.0;
	}
}
