using System.Numerics;

namespace Avalonia.Rendering.Composition.Animations;

internal class Vector2Interpolator : IInterpolator<Vector2>
{
	public static Vector2Interpolator Instance { get; } = new Vector2Interpolator();

	public Vector2 Interpolate(Vector2 from, Vector2 to, float progress)
	{
		return Vector2.Lerp(from, to, progress);
	}
}
