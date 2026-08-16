using Avalonia.Animation.Easings;
using Avalonia.Rendering.Composition.Expressions;

namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
/// Composition animation key frame
/// </summary>
internal struct KeyFrame<T>
{
	public float NormalizedProgressKey;

	public T Value;

	public Expression Expression;

	public IEasing EasingFunction;
}
