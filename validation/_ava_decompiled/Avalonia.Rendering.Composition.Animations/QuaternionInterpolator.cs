using System.Numerics;

namespace Avalonia.Rendering.Composition.Animations;

internal class QuaternionInterpolator : IInterpolator<Quaternion>
{
	public static QuaternionInterpolator Instance { get; } = new QuaternionInterpolator();

	public Quaternion Interpolate(Quaternion from, Quaternion to, float progress)
	{
		return Quaternion.Lerp(from, to, progress);
	}
}
