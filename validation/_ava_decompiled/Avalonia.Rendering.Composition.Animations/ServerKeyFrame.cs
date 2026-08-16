using Avalonia.Animation.Easings;
using Avalonia.Rendering.Composition.Expressions;

namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
/// Server-side composition animation key frame
/// </summary>
internal struct ServerKeyFrame<T>
{
	public T Value;

	public Expression? Expression;

	public IEasing EasingFunction;

	public float Key;
}
