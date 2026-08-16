using Avalonia.Animation.Utils;

namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases a <see cref="T:System.Double" /> value 
/// using a piecewise simulated bounce function.
/// </summary>
public class BounceEaseInOut : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		if (progress < 0.5)
		{
			return 0.5 * (1.0 - BounceEaseUtils.Bounce(1.0 - progress * 2.0));
		}
		return 0.5 * BounceEaseUtils.Bounce(progress * 2.0 - 1.0) + 0.5;
	}
}
