using System;

namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases out a <see cref="T:System.Double" /> value 
/// using a overshooting cubic function.
/// </summary>
public class BackEaseOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		double num = 1.0 - progress;
		return 1.0 - num * (num * num - Math.Sin(num * Math.PI));
	}
}
