using System;

namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases in a <see cref="T:System.Double" /> value 
/// using a exponential function.
/// </summary>
public class ExponentialEaseIn : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		if (progress != 0.0)
		{
			return Math.Pow(2.0, 10.0 * (progress - 1.0));
		}
		return progress;
	}
}
