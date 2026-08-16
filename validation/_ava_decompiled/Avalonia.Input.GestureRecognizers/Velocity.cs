namespace Avalonia.Input.GestureRecognizers;

internal readonly record struct Velocity(Vector PixelsPerSecond)
{
	public Velocity ClampMagnitude(double minValue, double maxValue)
	{
		double squaredLength = PixelsPerSecond.SquaredLength;
		if (squaredLength > maxValue * maxValue)
		{
			double length = PixelsPerSecond.Length;
			return new Velocity((length != 0.0) ? (PixelsPerSecond / length * maxValue) : Vector.Zero);
		}
		if (squaredLength < minValue * minValue)
		{
			double length2 = PixelsPerSecond.Length;
			return new Velocity((length2 != 0.0) ? (PixelsPerSecond / length2 * minValue) : Vector.Zero);
		}
		return this;
	}
}
