using System;

namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:System.Byte" /> properties.
/// </summary>
internal class ByteAnimator : Animator<byte>
{
	private const double maxVal = 255.0;

	/// <inheritdoc />
	public override byte Interpolate(double progress, byte oldValue, byte newValue)
	{
		double num = (double)(int)oldValue / 255.0;
		double num2 = (double)(int)newValue / 255.0 - num;
		return (byte)Math.Round(255.0 * (num2 * progress + num));
	}
}
