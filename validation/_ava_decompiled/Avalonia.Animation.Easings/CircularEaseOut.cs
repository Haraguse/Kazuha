using System;

namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases out a <see cref="T:System.Double" /> value 
/// using the shifted second quadrant of
/// the unit circle.
/// </summary>
public class CircularEaseOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		return Math.Sqrt((2.0 - progress) * progress);
	}
}
