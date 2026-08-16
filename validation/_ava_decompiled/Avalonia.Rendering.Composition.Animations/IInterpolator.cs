namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
///  An interface to define interpolation logic for a particular type
/// </summary>
internal interface IInterpolator<T>
{
	T Interpolate(T from, T to, float progress);
}
