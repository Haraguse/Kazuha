using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Reactive;

namespace Avalonia.Animation;

/// <summary>
/// Tracks the progress of an animation.
/// </summary>
public sealed class Animation : AvaloniaObject, IAnimation
{
	private static readonly List<(Func<AvaloniaProperty, bool> Condition, Type Animator, Func<IAnimator> Factory)> Animators;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Animation.Animation.Duration" /> property.
	/// </summary>
	public static readonly DirectProperty<Animation, TimeSpan> DurationProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Animation.Animation.IterationCount" /> property.
	/// </summary>
	public static readonly DirectProperty<Animation, IterationCount> IterationCountProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Animation.Animation.PlaybackDirection" /> property.
	/// </summary>
	public static readonly DirectProperty<Animation, PlaybackDirection> PlaybackDirectionProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Animation.Animation.PlaybackBehavior" /> property.
	/// </summary>
	public static readonly DirectProperty<Animation, PlaybackBehavior> PlaybackBehaviorProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Animation.Animation.FillMode" /> property.
	/// </summary>
	public static readonly DirectProperty<Animation, FillMode> FillModeProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Animation.Animation.Easing" /> property.
	/// </summary>
	public static readonly DirectProperty<Animation, Easing> EasingProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Animation.Animation.Delay" /> property.
	/// </summary>
	public static readonly DirectProperty<Animation, TimeSpan> DelayProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Animation.Animation.DelayBetweenIterations" /> property.
	/// </summary>
	public static readonly DirectProperty<Animation, TimeSpan> DelayBetweenIterationsProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Animation.Animation.SpeedRatio" /> property.
	/// </summary>
	public static readonly DirectProperty<Animation, double> SpeedRatioProperty;

	private TimeSpan _duration;

	private IterationCount _iterationCount = new IterationCount(1uL);

	private PlaybackDirection _playbackDirection;

	private PlaybackBehavior _playbackBehavior;

	private FillMode _fillMode;

	private Easing _easing = new LinearEasing();

	private TimeSpan _delay = TimeSpan.Zero;

	private TimeSpan _delayBetweenIterations = TimeSpan.Zero;

	private double _speedRatio = 1.0;

	private static readonly Dictionary<IAnimationSetter, (Type Type, Func<IAnimator> Factory)> s_animators;

	/// <summary>
	/// Gets or sets the active time of this animation.
	/// </summary>
	public TimeSpan Duration
	{
		get
		{
			return _duration;
		}
		set
		{
			SetAndRaise(DurationProperty, ref _duration, value);
		}
	}

	/// <summary>
	/// Gets or sets the repeat count for this animation.
	/// </summary>
	public IterationCount IterationCount
	{
		get
		{
			return _iterationCount;
		}
		set
		{
			SetAndRaise(IterationCountProperty, ref _iterationCount, value);
		}
	}

	/// <summary>
	/// Gets or sets the playback direction for this animation.
	/// </summary>
	public PlaybackDirection PlaybackDirection
	{
		get
		{
			return _playbackDirection;
		}
		set
		{
			SetAndRaise(PlaybackDirectionProperty, ref _playbackDirection, value);
		}
	}

	/// <summary>
	/// Gets or sets the playback behavior for this animation.
	/// When set to <see cref="F:Avalonia.Animation.PlaybackBehavior.Auto" />, manually started animations and
	/// animations targeting <see cref="F:Avalonia.Visual.IsVisibleProperty" /> always play,
	/// while style-applied animations pause when the control is not effectively visible
	/// (see <see cref="P:Avalonia.Visual.IsEffectivelyVisible" />).
	/// </summary>
	public PlaybackBehavior PlaybackBehavior
	{
		get
		{
			return _playbackBehavior;
		}
		set
		{
			SetAndRaise(PlaybackBehaviorProperty, ref _playbackBehavior, value);
		}
	}

	/// <summary>
	/// Gets or sets the value fill mode for this animation.
	/// </summary> 
	public FillMode FillMode
	{
		get
		{
			return _fillMode;
		}
		set
		{
			SetAndRaise(FillModeProperty, ref _fillMode, value);
		}
	}

	/// <summary>
	/// Gets or sets the easing function to be used for this animation.
	/// </summary>
	public Easing Easing
	{
		get
		{
			return _easing;
		}
		set
		{
			SetAndRaise(EasingProperty, ref _easing, value);
		}
	}

	/// <summary> 
	/// Gets or sets the initial delay time for this animation. 
	/// </summary> 
	public TimeSpan Delay
	{
		get
		{
			return _delay;
		}
		set
		{
			SetAndRaise(DelayProperty, ref _delay, value);
		}
	}

	/// <summary> 
	/// Gets or sets the delay time in between iterations.
	/// </summary> 
	public TimeSpan DelayBetweenIterations
	{
		get
		{
			return _delayBetweenIterations;
		}
		set
		{
			SetAndRaise(DelayBetweenIterationsProperty, ref _delayBetweenIterations, value);
		}
	}

	/// <summary>
	/// Gets or sets the speed multiple for this animation.
	/// </summary> 
	public double SpeedRatio
	{
		get
		{
			return _speedRatio;
		}
		set
		{
			SetAndRaise(SpeedRatioProperty, ref _speedRatio, value);
		}
	}

	/// <summary>
	/// Gets the children of the <see cref="T:Avalonia.Animation.Animation" />.
	/// </summary>
	[Content]
	public KeyFrames Children { get; } = new KeyFrames();

	/// <summary>
	/// Sets the value of the Animator attached property for a setter.
	/// </summary>
	/// <param name="setter">The animation setter.</param>
	/// <param name="value">The property animator value.</param>
	public static void SetAnimator(IAnimationSetter setter, ICustomAnimator value)
	{
		s_animators[setter] = (value.WrapperType, value.CreateWrapper);
	}

	static Animation()
	{
		Animators = new List<(Func<AvaloniaProperty, bool>, Type, Func<IAnimator>)>
		{
			((AvaloniaProperty prop) => typeof(double).IsAssignableFrom(prop.PropertyType) && typeof(Transform).IsAssignableFrom(prop.OwnerType), typeof(TransformAnimator), () => new TransformAnimator()),
			((AvaloniaProperty prop) => typeof(bool).IsAssignableFrom(prop.PropertyType), typeof(BoolAnimator), () => new BoolAnimator()),
			((AvaloniaProperty prop) => typeof(byte).IsAssignableFrom(prop.PropertyType), typeof(ByteAnimator), () => new ByteAnimator()),
			((AvaloniaProperty prop) => typeof(short).IsAssignableFrom(prop.PropertyType), typeof(Int16Animator), () => new Int16Animator()),
			((AvaloniaProperty prop) => typeof(int).IsAssignableFrom(prop.PropertyType), typeof(Int32Animator), () => new Int32Animator()),
			((AvaloniaProperty prop) => typeof(long).IsAssignableFrom(prop.PropertyType), typeof(Int64Animator), () => new Int64Animator()),
			((AvaloniaProperty prop) => typeof(ushort).IsAssignableFrom(prop.PropertyType), typeof(UInt16Animator), () => new UInt16Animator()),
			((AvaloniaProperty prop) => typeof(uint).IsAssignableFrom(prop.PropertyType), typeof(UInt32Animator), () => new UInt32Animator()),
			((AvaloniaProperty prop) => typeof(ulong).IsAssignableFrom(prop.PropertyType), typeof(UInt64Animator), () => new UInt64Animator()),
			((AvaloniaProperty prop) => typeof(float).IsAssignableFrom(prop.PropertyType), typeof(FloatAnimator), () => new FloatAnimator()),
			((AvaloniaProperty prop) => typeof(double).IsAssignableFrom(prop.PropertyType), typeof(DoubleAnimator), () => new DoubleAnimator()),
			((AvaloniaProperty prop) => typeof(decimal).IsAssignableFrom(prop.PropertyType), typeof(DecimalAnimator), () => new DecimalAnimator())
		};
		DurationProperty = AvaloniaProperty.RegisterDirect("Duration", (Animation o) => o._duration, delegate(Animation o, TimeSpan v)
		{
			o._duration = v;
		});
		IterationCountProperty = AvaloniaProperty.RegisterDirect("IterationCount", (Animation o) => o._iterationCount, delegate(Animation o, IterationCount v)
		{
			o._iterationCount = v;
		});
		PlaybackDirectionProperty = AvaloniaProperty.RegisterDirect("PlaybackDirection", (Animation o) => o._playbackDirection, delegate(Animation o, PlaybackDirection v)
		{
			o._playbackDirection = v;
		}, PlaybackDirection.Normal);
		PlaybackBehaviorProperty = AvaloniaProperty.RegisterDirect("PlaybackBehavior", (Animation o) => o._playbackBehavior, delegate(Animation o, PlaybackBehavior v)
		{
			o._playbackBehavior = v;
		}, PlaybackBehavior.Auto);
		FillModeProperty = AvaloniaProperty.RegisterDirect("FillMode", (Animation o) => o._fillMode, delegate(Animation o, FillMode v)
		{
			o._fillMode = v;
		}, FillMode.None);
		EasingProperty = AvaloniaProperty.RegisterDirect("Easing", (Animation o) => o._easing, delegate(Animation o, Easing v)
		{
			o._easing = v;
		});
		DelayProperty = AvaloniaProperty.RegisterDirect("Delay", (Animation o) => o._delay, delegate(Animation o, TimeSpan v)
		{
			o._delay = v;
		});
		DelayBetweenIterationsProperty = AvaloniaProperty.RegisterDirect("DelayBetweenIterations", (Animation o) => o._delayBetweenIterations, delegate(Animation o, TimeSpan v)
		{
			o._delayBetweenIterations = v;
		});
		SpeedRatioProperty = AvaloniaProperty.RegisterDirect("SpeedRatio", (Animation o) => o._speedRatio, delegate(Animation o, double v)
		{
			o._speedRatio = v;
		}, 0.0, BindingMode.TwoWay);
		s_animators = new Dictionary<IAnimationSetter, (Type, Func<IAnimator>)>();
		RegisterAnimator<IEffect, EffectAnimator>();
		RegisterAnimator<BoxShadow, BoxShadowAnimator>();
		RegisterAnimator<BoxShadows, BoxShadowsAnimator>();
		RegisterAnimator<IBrush, BaseBrushAnimator>();
		RegisterAnimator<CornerRadius, CornerRadiusAnimator>();
		RegisterAnimator<Color, ColorAnimator>();
		RegisterAnimator<Vector, VectorAnimator>();
		RegisterAnimator<Point, PointAnimator>();
		RegisterAnimator<Rect, RectAnimator>();
		RegisterAnimator<RelativePoint, RelativePointAnimator>();
		RegisterAnimator<RelativeScalar, RelativeScalarAnimator>();
		RegisterAnimator<Size, SizeAnimator>();
		RegisterAnimator<Thickness, ThicknessAnimator>();
	}

	/// <summary>
	/// Registers a <see cref="T:Avalonia.Animation.Animators.Animator`1" /> that can handle
	/// a value type that matches the specified condition.
	/// </summary>
	private static void RegisterAnimator<T, TAnimator>() where TAnimator : Animator<T>, new()
	{
		Animators.Insert(0, ((AvaloniaProperty prop) => typeof(T).IsAssignableFrom(prop.PropertyType), typeof(TAnimator), () => new TAnimator()));
	}

	public static void RegisterCustomAnimator<T, TAnimator>() where TAnimator : InterpolatingAnimator<T>, new()
	{
		Animators.Insert(0, ((AvaloniaProperty prop) => typeof(T).IsAssignableFrom(prop.PropertyType), typeof(InterpolatingAnimator<T>.AnimatorWrapper), () => new TAnimator().CreateWrapper()));
	}

	private static (Type Type, Func<IAnimator> Factory)? GetAnimatorType(AvaloniaProperty property)
	{
		foreach (var (func, item, item2) in Animators)
		{
			if (func(property))
			{
				return (item, item2);
			}
		}
		return null;
	}

	/// <summary>
	/// Gets the value of the Animator attached property for a setter.
	/// </summary>
	/// <param name="setter">The animation setter.</param>
	/// <returns>The property animator type.</returns>
	internal static (Type Type, Func<IAnimator> Factory)? GetAnimator(IAnimationSetter setter)
	{
		if (s_animators.TryGetValue(setter, out (Type, Func<IAnimator>) value))
		{
			return value;
		}
		return null;
	}

	private (IList<IAnimator> Animators, IList<IDisposable> subscriptions, bool animatesVisibility) InterpretKeyframes(Animatable control)
	{
		Dictionary<(Type, AvaloniaProperty), Func<IAnimator>> dictionary = new Dictionary<(Type, AvaloniaProperty), Func<IAnimator>>();
		List<AnimatorKeyFrame> list = new List<AnimatorKeyFrame>();
		List<IDisposable> list2 = new List<IDisposable>();
		bool item = false;
		foreach (KeyFrame child in Children)
		{
			foreach (IAnimationSetter setter in child.Setters)
			{
				if ((object)setter.Property == null)
				{
					throw new InvalidOperationException("No Setter property assigned.");
				}
				if (setter.Property == Visual.IsVisibleProperty)
				{
					item = true;
				}
				(Type, Func<IAnimator>)? tuple = GetAnimator(setter) ?? GetAnimatorType(setter.Property);
				if (!tuple.HasValue)
				{
					throw new InvalidOperationException($"No animator registered for the property {setter.Property}. Add an animator to the Animation.Animators collection that matches this property to animate it.");
				}
				var (type, func) = tuple.Value;
				if (!dictionary.ContainsKey((type, setter.Property)))
				{
					dictionary[(type, setter.Property)] = func;
				}
				Cue cue = child.Cue;
				if (child.TimingMode == KeyFrameTimingMode.TimeSpan)
				{
					cue = new Cue(child.KeyTime.TotalSeconds / Duration.TotalSeconds);
				}
				AnimatorKeyFrame animatorKeyFrame = new AnimatorKeyFrame(type, func, cue, child.KeySpline);
				list2.Add(animatorKeyFrame.BindSetter(setter, control));
				list.Add(animatorKeyFrame);
			}
		}
		list.Sort((AnimatorKeyFrame x, AnimatorKeyFrame y) => x.Cue.CueValue.CompareTo(y.Cue.CueValue));
		List<IAnimator> list3 = new List<IAnimator>();
		foreach (KeyValuePair<(Type, AvaloniaProperty), Func<IAnimator>> item2 in dictionary)
		{
			IAnimator animator = item2.Value();
			animator.Property = item2.Key.Item2;
			list3.Add(animator);
		}
		FillMode fillMode;
		foreach (AnimatorKeyFrame keyframe in list)
		{
			IAnimator animator2 = list3.First((IAnimator a) => a.GetType() == keyframe.AnimatorType && a.Property == keyframe.Property);
			bool flag = animator2.Count == 0;
			if (flag)
			{
				fillMode = FillMode;
				bool flag2 = (uint)(fillMode - 2) <= 1u;
				flag = flag2;
			}
			if (flag)
			{
				keyframe.FillBefore = true;
			}
			animator2.Add(keyframe);
		}
		fillMode = FillMode;
		if ((fillMode == FillMode.Forward || fillMode == FillMode.Both) ? true : false)
		{
			foreach (IAnimator item3 in list3)
			{
				if (item3.Count > 0)
				{
					item3[item3.Count - 1].FillAfter = true;
				}
			}
		}
		return (Animators: list3, subscriptions: list2, animatesVisibility: item);
	}

	IDisposable IAnimation.Apply(Animatable control, IClock? clock, IObservable<bool> match, Action? onComplete, bool isManuallyStarted)
	{
		return Apply(control, clock, match, onComplete, isManuallyStarted);
	}

	/// <inheritdoc cref="M:Avalonia.Animation.IAnimation.Apply(Avalonia.Animation.Animatable,Avalonia.Animation.IClock,System.IObservable{System.Boolean},System.Action,System.Boolean)" />
	internal IDisposable Apply(Animatable control, IClock? clock, IObservable<bool> match, Action? onComplete, bool isManuallyStarted = false)
	{
		(IList<IAnimator> Animators, IList<IDisposable> subscriptions, bool animatesVisibility) tuple = InterpretKeyframes(control);
		IList<IAnimator> item = tuple.Animators;
		IList<IDisposable> item2 = tuple.subscriptions;
		bool item3 = tuple.animatesVisibility;
		bool shouldPauseOnInvisible = _playbackBehavior switch
		{
			PlaybackBehavior.Auto => !(item3 | isManuallyStarted), 
			PlaybackBehavior.Always => false, 
			PlaybackBehavior.OnlyIfVisible => true, 
			_ => throw new InvalidOperationException($"Unknown PlaybackBehavior value: {_playbackBehavior}"), 
		};
		if (item.Count == 1)
		{
			IDisposable disposable = item[0].Apply(this, control, clock, match, onComplete, shouldPauseOnInvisible);
			if (disposable != null)
			{
				item2.Add(disposable);
			}
		}
		else
		{
			List<Task> list = ((onComplete != null) ? new List<Task>() : null);
			foreach (IAnimator item4 in item)
			{
				Action onComplete2 = null;
				if (onComplete != null)
				{
					TaskCompletionSource<object?> tcs = new TaskCompletionSource<object>();
					onComplete2 = delegate
					{
						tcs.SetResult(null);
					};
					list.Add(tcs.Task);
				}
				IDisposable disposable2 = item4.Apply(this, control, clock, match, onComplete2, shouldPauseOnInvisible);
				if (disposable2 != null)
				{
					item2.Add(disposable2);
				}
			}
			if (onComplete != null)
			{
				Task.WhenAll(list).ContinueWith(delegate(Task _, object? state)
				{
					((Action)state)();
				}, onComplete, TaskScheduler.FromCurrentSynchronizationContext());
			}
		}
		return new CompositeDisposable(item2);
	}

	public Task RunAsync(Animatable control, CancellationToken cancellationToken = default(CancellationToken))
	{
		return RunAsync(control, null, cancellationToken);
	}

	/// <inheritdoc cref="M:Avalonia.Animation.IAnimation.RunAsync(Avalonia.Animation.Animatable,Avalonia.Animation.IClock,System.Threading.CancellationToken)" />
	internal Task RunAsync(Animatable control, IClock? clock)
	{
		return RunAsync(control, clock, default(CancellationToken));
	}

	Task IAnimation.RunAsync(Animatable control, IClock? clock, CancellationToken cancellationToken)
	{
		return RunAsync(control, clock, cancellationToken);
	}

	/// <inheritdoc cref="M:Avalonia.Animation.IAnimation.RunAsync(Avalonia.Animation.Animatable,Avalonia.Animation.IClock,System.Threading.CancellationToken)" />
	internal Task RunAsync(Animatable control, IClock? clock, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.CompletedTask;
		}
		TaskCompletionSource<object?> run = new TaskCompletionSource<object>();
		if (IterationCount == IterationCount.Infinite)
		{
			run.SetException(new InvalidOperationException("Looping animations must not use the Run method."));
		}
		IDisposable subscriptions = null;
		IDisposable cancellation = null;
		subscriptions = Apply(control, clock, Observable.Return(value: true), delegate
		{
			run.TrySetResult(null);
			subscriptions?.Dispose();
			cancellation?.Dispose();
		}, isManuallyStarted: true);
		cancellation = cancellationToken.Register(delegate
		{
			run.TrySetResult(null);
			subscriptions?.Dispose();
			cancellation?.Dispose();
		});
		return run.Task;
	}
}
