using Avalonia.Animation.Easings;

namespace Avalonia.Rendering.Composition.Animations;

internal interface IKeyFrames
{
	void InsertExpressionKeyFrame(float normalizedProgressKey, string value, IEasing easingFunction);
}
