using System;

namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases in a <see cref="T:System.Double" /> value 
/// using the shifted fourth quadrant of
/// the unit circle.
/// </summary>
public class CircularEaseIn : Easing
{
	/// <inheritdoc />
	public override double Ease(double p)
	{
		return 1.0 - Math.Sqrt(1.0 - p * p);
	}
}
