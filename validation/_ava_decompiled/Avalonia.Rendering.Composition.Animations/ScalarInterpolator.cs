namespace Avalonia.Rendering.Composition.Animations;

internal class ScalarInterpolator : IInterpolator<float>
{
	public static ScalarInterpolator Instance { get; } = new ScalarInterpolator();

	public float Interpolate(float from, float to, float progress)
	{
		return from + (to - from) * progress;
	}
}
