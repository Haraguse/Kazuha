using System;

namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases a <see cref="T:System.Double" /> value 
/// using a half sine wave function.
/// </summary>
public class SineEaseInOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		return 0.5 * (1.0 - Math.Cos(progress * Math.PI));
	}
}
