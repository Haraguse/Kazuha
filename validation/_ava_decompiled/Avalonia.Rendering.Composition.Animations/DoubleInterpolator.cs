namespace Avalonia.Rendering.Composition.Animations;

internal class DoubleInterpolator : IInterpolator<double>
{
	public static DoubleInterpolator Instance { get; } = new DoubleInterpolator();

	public double Interpolate(double from, double to, float progress)
	{
		return from + (to - from) * (double)progress;
	}
}
