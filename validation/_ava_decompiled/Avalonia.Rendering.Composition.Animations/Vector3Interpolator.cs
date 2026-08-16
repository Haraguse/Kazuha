using System.Numerics;

namespace Avalonia.Rendering.Composition.Animations;

internal class Vector3Interpolator : IInterpolator<Vector3>
{
	public static Vector3Interpolator Instance { get; } = new Vector3Interpolator();

	public Vector3 Interpolate(Vector3 from, Vector3 to, float progress)
	{
		return Vector3.Lerp(from, to, progress);
	}
}
