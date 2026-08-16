using System;

namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:System.UInt64" /> properties.
/// </summary>
internal class UInt64Animator : Animator<ulong>
{
	private const double maxVal = 1.8446744073709552E+19;

	/// <inheritdoc />
	public override ulong Interpolate(double progress, ulong oldValue, ulong newValue)
	{
		double num = (double)oldValue / 1.8446744073709552E+19;
		double num2 = (double)newValue / 1.8446744073709552E+19 - num;
		return (ulong)Math.Round(1.8446744073709552E+19 * (num2 * progress + num));
	}
}
