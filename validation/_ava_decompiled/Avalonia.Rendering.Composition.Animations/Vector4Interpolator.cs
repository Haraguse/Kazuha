using System.Numerics;

namespace Avalonia.Rendering.Composition.Animations;

internal class Vector4Interpolator : IInterpolator<Vector4>
{
	public static Vector4Interpolator Instance { get; } = new Vector4Interpolator();

	public Vector4 Interpolate(Vector4 from, Vector4 to, float progress)
	{
		return Vector4.Lerp(from, to, progress);
	}
}
