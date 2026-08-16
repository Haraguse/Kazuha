using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia.Animation.Animators;

/// <summary>
/// Base class for <see cref="T:Avalonia.Animation.Animators.Animator`1" /> objects
/// </summary>
internal abstract class Animator<T> : AvaloniaList<AnimatorKeyFrame>, IAnimator, IList<AnimatorKeyFrame>, ICollection<AnimatorKeyFrame>, IEnumerable<AnimatorKeyFrame>, IEnumerable
{
	private readonly struct KeyFrameInfo(double time, T value, KeySpline? keySpline)
	{
		public readonly double Time = time;

		public readonly T Value = value;

		public readonly KeySpline? KeySpline = keySpline;

		public static KeyFrameInfo FromKeyFrame(AnimatorKeyFrame source, T neutralValue)
		{
			return new KeyFrameInfo(source.Cue.CueValue, Animator<T>.GetTypedValue(source.Value, neutralValue), source.KeySpline);
		}
	}

	/// <summary>
	/// Gets or sets the target property for the keyframe.
	/// </summary>
	public AvaloniaProperty? Property { get; set; }

	/// <inheritdoc />
	public virtual IDisposable? Apply(Animation animation, Animatable control, IClock? clock, IObservable<bool> match, Action? onComplete, bool shouldPauseOnInvisible)
	{
		DisposeAnimationInstanceSubject<T> disposeAnimationInstanceSubject = new DisposeAnimationInstanceSubject<T>(this, animation, control, clock, onComplete, shouldPauseOnInvisible);
		return new CompositeDisposable(match.Subscribe(disposeAnimationInstanceSubject), disposeAnimationInstanceSubject);
	}

	protected T InterpolationHandler(double animationTime, T neutralValue)
	{
		if (base.Count == 0)
		{
			return neutralValue;
		}
		(KeyFrameInfo From, KeyFrameInfo To) keyFrames = GetKeyFrames(animationTime, neutralValue);
		KeyFrameInfo item = keyFrames.From;
		KeyFrameInfo item2 = keyFrames.To;
		double num = (animationTime - item.Time) / (item2.Time - item.Time);
		KeySpline keySpline = item2.KeySpline;
		if (keySpline != null)
		{
			num = keySpline.GetSplineProgress(num);
		}
		return Interpolate(num, item.Value, item2.Value);
	}

	private (KeyFrameInfo From, KeyFrameInfo To) GetKeyFrames(double time, T neutralValue)
	{
		AnimatorKeyFrame animatorKeyFrame = base[0];
		double cueValue = animatorKeyFrame.Cue.CueValue;
		if (time <= cueValue && cueValue > 0.0)
		{
			T value = (T)(animatorKeyFrame.FillBefore ? ((object)GetTypedValue(animatorKeyFrame.Value, neutralValue)) : ((object)neutralValue));
			return (From: new KeyFrameInfo(0.0, value, animatorKeyFrame.KeySpline), To: KeyFrameInfo.FromKeyFrame(animatorKeyFrame, neutralValue));
		}
		for (int i = 1; i < base.Count; i++)
		{
			AnimatorKeyFrame animatorKeyFrame2 = base[i];
			if (time <= animatorKeyFrame2.Cue.CueValue)
			{
				return (From: KeyFrameInfo.FromKeyFrame(base[i - 1], neutralValue), To: KeyFrameInfo.FromKeyFrame(base[i], neutralValue));
			}
		}
		AnimatorKeyFrame animatorKeyFrame3 = base[base.Count - 1];
		if (animatorKeyFrame3.Cue.CueValue >= 1.0)
		{
			if (base.Count == 1)
			{
				T value2 = (T)(animatorKeyFrame3.FillBefore ? ((object)GetTypedValue(animatorKeyFrame3.Value, neutralValue)) : ((object)neutralValue));
				return (From: new KeyFrameInfo(0.0, value2, animatorKeyFrame3.KeySpline), To: KeyFrameInfo.FromKeyFrame(animatorKeyFrame3, neutralValue));
			}
			return (From: KeyFrameInfo.FromKeyFrame(base[base.Count - 2], neutralValue), To: KeyFrameInfo.FromKeyFrame(animatorKeyFrame3, neutralValue));
		}
		T value3 = (T)(animatorKeyFrame3.FillAfter ? ((object)GetTypedValue(animatorKeyFrame3.Value, neutralValue)) : ((object)neutralValue));
		return (From: KeyFrameInfo.FromKeyFrame(animatorKeyFrame3, neutralValue), To: new KeyFrameInfo(1.0, value3, animatorKeyFrame3.KeySpline));
	}

	private static T GetTypedValue(object? untypedValue, T neutralValue)
	{
		if (untypedValue is T)
		{
			return (T)untypedValue;
		}
		return neutralValue;
	}

	public virtual IDisposable BindAnimation(Animatable control, IObservable<T> instance)
	{
		if ((object)Property == null)
		{
			throw new InvalidOperationException("Animator has no property specified.");
		}
		return control.Bind((AvaloniaProperty<T>)Property, instance, BindingPriority.Animation);
	}

	/// <summary>
	/// Runs the KeyFrames Animation.
	/// </summary>
	internal IDisposable Run(Animation animation, Animatable control, IClock? clock, Action? onComplete, bool shouldPauseOnInvisible)
	{
		AnimationInstance<T> instance = new AnimationInstance<T>(animation, control, this, clock ?? control.Clock ?? Clock.GlobalClock, onComplete, InterpolationHandler, shouldPauseOnInvisible);
		return BindAnimation(control, instance);
	}

	/// <summary>
	/// Interpolates in-between two key values given the desired progress time.
	/// </summary>
	public abstract T Interpolate(double progress, T oldValue, T newValue);
}
