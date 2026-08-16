using System.Numerics;
using Avalonia.Animation.Easings;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

public class QuaternionKeyFrameAnimation(Compositor compositor) : KeyFrameAnimation(compositor)
{
	private KeyFrames<Quaternion> _keyFrames = new KeyFrames<Quaternion>();

	private protected override IKeyFrames KeyFrames => _keyFrames;

	internal override IAnimationInstance CreateInstance(ServerObject targetObject, ExpressionVariant? finalValue)
	{
		return new KeyFrameAnimationInstance<Quaternion>(QuaternionInterpolator.Instance, _keyFrames.Snapshot(), CreateSnapshot(), finalValue?.CastOrDefault<Quaternion>(), targetObject, base.DelayBehavior, base.DelayTime, base.Direction, base.Duration, base.IterationBehavior, base.IterationCount, base.StopBehavior);
	}

	public void InsertKeyFrame(float normalizedProgressKey, Quaternion value, IEasing easingFunction)
	{
		_keyFrames.Insert(normalizedProgressKey, value, easingFunction);
	}

	public void InsertKeyFrame(float normalizedProgressKey, Quaternion value)
	{
		_keyFrames.Insert(normalizedProgressKey, value, base.Compositor.DefaultEasing);
	}
}
