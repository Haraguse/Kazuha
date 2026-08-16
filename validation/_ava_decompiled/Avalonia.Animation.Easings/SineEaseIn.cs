using System;

namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases in a <see cref="T:System.Double" /> value 
/// using the quarter-wave of sine function.
/// </summary>
public class SineEaseIn : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		return Math.Sin((progress - 1.0) * (Math.PI / 2.0)) + 1.0;
	}
}
