namespace Avalonia.Rendering.Composition.Animations;

internal class BooleanInterpolator : IInterpolator<bool>
{
	public static BooleanInterpolator Instance { get; } = new BooleanInterpolator();

	public bool Interpolate(bool from, bool to, float progress)
	{
		if (!(progress >= 1f))
		{
			return from;
		}
		return to;
	}
}
