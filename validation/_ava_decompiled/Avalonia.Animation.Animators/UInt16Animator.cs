using System;

namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:System.UInt16" /> properties.
/// </summary>
internal class UInt16Animator : Animator<ushort>
{
	private const double maxVal = 65535.0;

	/// <inheritdoc />
	public override ushort Interpolate(double progress, ushort oldValue, ushort newValue)
	{
		double num = (double)(int)oldValue / 65535.0;
		double num2 = (double)(int)newValue / 65535.0 - num;
		return (ushort)Math.Round(65535.0 * (num2 * progress + num));
	}
}
