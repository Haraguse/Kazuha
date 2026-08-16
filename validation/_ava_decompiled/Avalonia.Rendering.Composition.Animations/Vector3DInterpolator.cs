namespace Avalonia.Rendering.Composition.Animations;

internal class Vector3DInterpolator : IInterpolator<Vector3D>
{
	public static Vector3DInterpolator Instance { get; } = new Vector3DInterpolator();

	public Vector3D Interpolate(Vector3D from, Vector3D to, float progress)
	{
		return new Vector3D(DoubleInterpolator.Instance.Interpolate(from.X, to.X, progress), DoubleInterpolator.Instance.Interpolate(from.Y, to.Y, progress), DoubleInterpolator.Instance.Interpolate(from.Z, to.Z, progress));
	}
}
