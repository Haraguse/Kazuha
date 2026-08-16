using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
/// Server-side counterpart of KeyFrameAnimation with values baked-in
/// </summary>
internal class KeyFrameAnimationInstance<T> : AnimationInstanceBase, IAnimationInstance, IServerClockItem where T : struct
{
	private readonly IInterpolator<T> _interpolator;

	private readonly ServerKeyFrame<T>[] _keyFrames;

	private readonly ExpressionVariant? _finalValue;

	private readonly AnimationDelayBehavior _delayBehavior;

	private readonly TimeSpan _delayTime;

	private readonly PlaybackDirection _direction;

	private readonly TimeSpan _duration;

	private readonly AnimationIterationBehavior _iterationBehavior;

	private readonly int _iterationCount;

	private readonly AnimationStopBehavior _stopBehavior;

	private TimeSpan _startedAt;

	private T _startingValue;

	private readonly TimeSpan _totalDuration;

	private bool _finished;

	public KeyFrameAnimationInstance(IInterpolator<T> interpolator, ServerKeyFrame<T>[] keyFrames, PropertySetSnapshot snapshot, ExpressionVariant? finalValue, ServerObject target, AnimationDelayBehavior delayBehavior, TimeSpan delayTime, PlaybackDirection direction, TimeSpan duration, AnimationIterationBehavior iterationBehavior, int iterationCount, AnimationStopBehavior stopBehavior)
		: base(target, snapshot)
	{
		_interpolator = interpolator;
		_keyFrames = keyFrames;
		_finalValue = finalValue;
		_delayBehavior = delayBehavior;
		_delayTime = delayTime;
		_direction = direction;
		_duration = duration;
		_iterationBehavior = iterationBehavior;
		_iterationCount = iterationCount;
		_stopBehavior = stopBehavior;
		if (_iterationBehavior == AnimationIterationBehavior.Count)
		{
			_totalDuration = delayTime.Add(TimeSpan.FromTicks(iterationCount * _duration.Ticks));
		}
		if (_keyFrames.Length == 0)
		{
			throw new InvalidOperationException("Animation has no key frames");
		}
		if (_duration.Ticks <= 0)
		{
			throw new InvalidOperationException("Invalid animation duration");
		}
	}

	protected override ExpressionVariant EvaluateCore(TimeSpan now, ExpressionVariant currentValue)
	{
		ExpressionVariant expressionVariant = ExpressionVariant.Create(_startingValue);
		ExpressionEvaluationContext ctx = new ExpressionEvaluationContext
		{
			Parameters = base.Parameters,
			Target = base.TargetObject,
			CurrentValue = currentValue,
			FinalValue = (_finalValue ?? expressionVariant),
			StartingValue = expressionVariant,
			ForeignFunctionInterface = BuiltInExpressionFfi.Instance
		};
		TimeSpan timeSpan = now - _startedAt;
		ExpressionVariant result = EvaluateImpl(timeSpan, currentValue, ref ctx);
		if (_iterationBehavior == AnimationIterationBehavior.Count && !_finished && timeSpan > _totalDuration)
		{
			base.TargetObject.Compositor.Animations.RemoveFromClock(this);
			_finished = true;
		}
		return result;
	}

	private ExpressionVariant EvaluateImpl(TimeSpan elapsed, ExpressionVariant currentValue, ref ExpressionEvaluationContext ctx)
	{
		if (elapsed < _delayTime)
		{
			if (_delayBehavior == AnimationDelayBehavior.SetInitialValueBeforeDelay)
			{
				return ExpressionVariant.Create(GetKeyFrame(ref ctx, _keyFrames[0]));
			}
			return currentValue;
		}
		elapsed -= _delayTime;
		long num = elapsed.Ticks / _duration.Ticks;
		if (_iterationBehavior == AnimationIterationBehavior.Count && num >= _iterationCount)
		{
			return ExpressionVariant.Create(GetKeyFrame(ref ctx, _keyFrames[_keyFrames.Length - 1]));
		}
		bool flag = num % 2 == 0;
		elapsed = TimeSpan.FromTicks(elapsed.Ticks % _duration.Ticks);
		bool num2 = ((_direction == PlaybackDirection.Alternate) ? (!flag) : ((_direction == PlaybackDirection.AlternateReverse) ? flag : (_direction == PlaybackDirection.Reverse)));
		double num3 = elapsed.TotalSeconds / _duration.TotalSeconds;
		if (num2)
		{
			num3 = 1.0 - num3;
		}
		ServerKeyFrame<T> f = new ServerKeyFrame<T>
		{
			Value = _startingValue
		};
		ServerKeyFrame<T> f2 = _keyFrames[_keyFrames.Length - 1];
		for (int i = 0; i < _keyFrames.Length; i++)
		{
			ServerKeyFrame<T> serverKeyFrame = _keyFrames[i];
			if ((double)serverKeyFrame.Key < num3)
			{
				if (i == _keyFrames.Length - 1)
				{
					return ExpressionVariant.Create(GetKeyFrame(ref ctx, serverKeyFrame));
				}
				f = serverKeyFrame;
				f2 = _keyFrames[i + 1];
				continue;
			}
			if (i == 0)
			{
				f2 = _keyFrames[i];
			}
			break;
		}
		double progress = Math.Max(0.0, Math.Min(1.0, (num3 - (double)f.Key) / (double)(f2.Key - f.Key)));
		float num4 = (float)f2.EasingFunction.Ease(progress);
		if (float.IsNaN(num4) || float.IsInfinity(num4))
		{
			return currentValue;
		}
		return ExpressionVariant.Create(_interpolator.Interpolate(GetKeyFrame(ref ctx, f), GetKeyFrame(ref ctx, f2), num4));
	}

	private static T GetKeyFrame(ref ExpressionEvaluationContext ctx, ServerKeyFrame<T> f)
	{
		if (f.Expression != null)
		{
			return f.Expression.Evaluate(ref ctx).CastOrDefault<T>();
		}
		return f.Value;
	}

	public override void Initialize(TimeSpan startedAt, ExpressionVariant startingValue, CompositionProperty property)
	{
		_startedAt = startedAt;
		_startingValue = startingValue.CastOrDefault<T>();
		HashSet<(string, string)> hashSet = new HashSet<(string, string)>();
		ServerKeyFrame<T>[] keyFrames = _keyFrames;
		for (int i = 0; i < keyFrames.Length; i++)
		{
			keyFrames[i].Expression?.CollectReferences(hashSet);
		}
		Initialize(property, hashSet);
	}

	public override void Activate()
	{
		if (!_finished)
		{
			base.TargetObject.Compositor.Animations.AddToClock(this);
			base.Activate();
		}
	}

	public override void Deactivate()
	{
		base.TargetObject.Compositor.Animations.RemoveFromClock(this);
		base.Deactivate();
	}
}
