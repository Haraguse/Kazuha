using System;

namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:System.Int64" /> properties.
/// </summary>
internal class Int64Animator : Animator<long>
{
	private const double maxVal = 9.223372036854776E+18;

	/// <inheritdoc />
	public override long Interpolate(double progress, long oldValue, long newValue)
	{
		double num = (double)oldValue / 9.223372036854776E+18;
		double num2 = (double)newValue / 9.223372036854776E+18 - num;
		return (long)Math.Round(9.223372036854776E+18 * (num2 * progress + num));
	}
}
