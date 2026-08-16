using System;

namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases out a <see cref="T:System.Double" /> value 
/// using the quarter-wave of sine function
/// with shifted phase.
/// </summary>
public class SineEaseOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		return Math.Sin(progress * (Math.PI / 2.0));
	}
}
