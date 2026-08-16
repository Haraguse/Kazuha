namespace Avalonia.Rendering.Composition.Animations;

internal class VectorInterpolator : IInterpolator<Vector>
{
	public static VectorInterpolator Instance { get; } = new VectorInterpolator();

	public Vector Interpolate(Vector from, Vector to, float progress)
	{
		return new Vector(DoubleInterpolator.Instance.Interpolate(from.X, to.X, progress), DoubleInterpolator.Instance.Interpolate(from.Y, to.Y, progress));
	}
}
