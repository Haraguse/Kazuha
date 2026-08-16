using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// Provides extension methods for Avalonia media.
/// </summary>
public static class MediaExtensions
{
	/// <summary>
	/// Calculates scaling based on a <see cref="T:Avalonia.Media.Stretch" /> value.
	/// </summary>
	/// <param name="stretch">The stretch mode.</param>
	/// <param name="destinationSize">The size of the destination viewport.</param>
	/// <param name="sourceSize">The size of the source.</param>
	/// <param name="stretchDirection">The stretch direction.</param>
	/// <returns>A vector with the X and Y scaling factors.</returns>
	public static Vector CalculateScaling(this Stretch stretch, Size destinationSize, Size sourceSize, StretchDirection stretchDirection = StretchDirection.Both)
	{
		double num = 1.0;
		double num2 = 1.0;
		bool flag = !double.IsPositiveInfinity(destinationSize.Width);
		bool flag2 = !double.IsPositiveInfinity(destinationSize.Height);
		if ((stretch == Stretch.Uniform || stretch == Stretch.UniformToFill || stretch == Stretch.Fill) && (flag | flag2))
		{
			num = (MathUtilities.IsZero(sourceSize.Width) ? 0.0 : (destinationSize.Width / sourceSize.Width));
			num2 = (MathUtilities.IsZero(sourceSize.Height) ? 0.0 : (destinationSize.Height / sourceSize.Height));
			if (!flag)
			{
				num = num2;
			}
			else if (!flag2)
			{
				num2 = num;
			}
			else
			{
				switch (stretch)
				{
				case Stretch.Uniform:
					num = (num2 = ((num < num2) ? num : num2));
					break;
				case Stretch.UniformToFill:
					num = (num2 = ((num > num2) ? num : num2));
					break;
				}
			}
			switch (stretchDirection)
			{
			case StretchDirection.UpOnly:
				if (num < 1.0)
				{
					num = 1.0;
				}
				if (num2 < 1.0)
				{
					num2 = 1.0;
				}
				break;
			case StretchDirection.DownOnly:
				if (num > 1.0)
				{
					num = 1.0;
				}
				if (num2 > 1.0)
				{
					num2 = 1.0;
				}
				break;
			}
		}
		return new Vector(num, num2);
	}

	/// <summary>
	/// Calculates a scaled size based on a <see cref="T:Avalonia.Media.Stretch" /> value.
	/// </summary>
	/// <param name="stretch">The stretch mode.</param>
	/// <param name="destinationSize">The size of the destination viewport.</param>
	/// <param name="sourceSize">The size of the source.</param>
	/// <param name="stretchDirection">The stretch direction.</param>
	/// <returns>The size of the stretched source.</returns>
	public static Size CalculateSize(this Stretch stretch, Size destinationSize, Size sourceSize, StretchDirection stretchDirection = StretchDirection.Both)
	{
		return sourceSize * stretch.CalculateScaling(destinationSize, sourceSize, stretchDirection);
	}
}
