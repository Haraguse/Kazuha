using Avalonia.Animation.Easings;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

public class VectorKeyFrameAnimation(Compositor compositor) : KeyFrameAnimation(compositor)
{
	private KeyFrames<Vector> _keyFrames = new KeyFrames<Vector>();

	private protected override IKeyFrames KeyFrames => _keyFrames;

	internal override IAnimationInstance CreateInstance(ServerObject targetObject, ExpressionVariant? finalValue)
	{
		return new KeyFrameAnimationInstance<Vector>(VectorInterpolator.Instance, _keyFrames.Snapshot(), CreateSnapshot(), finalValue?.CastOrDefault<Vector>(), targetObject, base.DelayBehavior, base.DelayTime, base.Direction, base.Duration, base.IterationBehavior, base.IterationCount, base.StopBehavior);
	}

	public void InsertKeyFrame(float normalizedProgressKey, Vector value, IEasing easingFunction)
	{
		_keyFrames.Insert(normalizedProgressKey, value, easingFunction);
	}

	public void InsertKeyFrame(float normalizedProgressKey, Vector value)
	{
		_keyFrames.Insert(normalizedProgressKey, value, base.Compositor.DefaultEasing);
	}
}
