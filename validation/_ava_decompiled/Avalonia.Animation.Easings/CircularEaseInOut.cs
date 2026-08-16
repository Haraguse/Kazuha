using System;

namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases a <see cref="T:System.Double" /> value 
/// using a piecewise unit circle function.
/// </summary>
public class CircularEaseInOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		if (progress < 0.5)
		{
			return 0.5 * (1.0 - Math.Sqrt(1.0 - 4.0 * progress * progress));
		}
		double num = 2.0 * progress;
		return 0.5 * (Math.Sqrt((3.0 - num) * (num - 1.0)) + 1.0);
	}
}
