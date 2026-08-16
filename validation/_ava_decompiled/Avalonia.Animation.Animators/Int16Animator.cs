using System;

namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:System.Int16" /> properties.
/// </summary>
internal class Int16Animator : Animator<short>
{
	private const double maxVal = 32767.0;

	/// <inheritdoc />
	public override short Interpolate(double progress, short oldValue, short newValue)
	{
		double num = (double)oldValue / 32767.0;
		double num2 = (double)newValue / 32767.0 - num;
		return (short)Math.Round(32767.0 * (num2 * progress + num));
	}
}
