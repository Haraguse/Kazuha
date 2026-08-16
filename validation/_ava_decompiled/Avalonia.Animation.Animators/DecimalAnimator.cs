namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:System.Decimal" /> properties.
/// </summary>
internal class DecimalAnimator : Animator<decimal>
{
	/// <inheritdoc />
	public override decimal Interpolate(double progress, decimal oldValue, decimal newValue)
	{
		return (newValue - oldValue) * (decimal)progress + oldValue;
	}
}
