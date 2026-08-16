namespace Avalonia.Animation.Easings;

/// <summary>
/// Linearly eases a <see cref="T:System.Double" /> value.
/// </summary>
public class LinearEasing : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		return progress;
	}
}
