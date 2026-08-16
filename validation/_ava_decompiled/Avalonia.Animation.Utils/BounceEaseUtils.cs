namespace Avalonia.Animation.Utils;

/// <summary>
/// Helper static class for BounceEase classes.
/// </summary>
internal static class BounceEaseUtils
{
	/// <summary>
	/// Returns the consequent <see cref="T:System.Double" /> value of
	/// a simulated bounce function.
	/// </summary>
	/// <param name="progress">The amount of progress from 0 to 1.</param>
	/// <returns>The result of the easing function</returns>
	internal static double Bounce(double progress)
	{
		if (progress < 0.36363636363636365)
		{
			return 121.0 * progress * progress / 16.0;
		}
		if (progress < 0.7272727272727273)
		{
			return 9.075 * progress * progress - 9.9 * progress + 3.4;
		}
		if (progress < 0.9)
		{
			return 12.066481994459833 * progress * progress - 19.63545706371191 * progress + 8.898060941828255;
		}
		return 10.8 * progress * progress - 20.52 * progress + 10.72;
	}
}
