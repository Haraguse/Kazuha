using System;

namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases out a <see cref="T:System.Double" /> value 
/// using a exponential function.
/// </summary>
public class ExponentialEaseOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		if (progress != 1.0)
		{
			return 1.0 - Math.Pow(2.0, -10.0 * progress);
		}
		return progress;
	}
}
