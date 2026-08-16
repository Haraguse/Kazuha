using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Collections;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles all animations on properties
/// with <see cref="T:Avalonia.Media.IBrush" /> as their type and
/// redirect them to the properly registered
/// animators in this class.
/// </summary>
internal class BaseBrushAnimator : Animator<IBrush?>
{
	private static readonly List<(Func<Type, bool> Match, Type AnimatorType, Func<IAnimator> AnimatorFactory)> _brushAnimators = new List<(Func<Type, bool>, Type, Func<IAnimator>)>();

	/// <summary>
	/// Register an <see cref="T:Avalonia.Animation.Animators.Animator`1" /> that handles a specific
	/// <see cref="T:Avalonia.Media.IBrush" />'s descendant value type.
	/// </summary>
	/// <param name="condition">
	/// The condition to which the <see cref="T:Avalonia.Animation.Animators.Animator`1" />
	/// is to be activated and used.
	/// </param>
	/// <typeparam name="TAnimator">
	/// The type of the animator to instantiate.
	/// </typeparam>
	public static void RegisterBrushAnimator<TAnimator>(Func<Type, bool> condition) where TAnimator : IAnimator, new()
	{
		_brushAnimators.Insert(0, (condition, typeof(TAnimator), () => new TAnimator()));
	}

	/// <inheritdoc />
	public override IDisposable? Apply(Animation animation, Animatable control, IClock? clock, IObservable<bool> match, Action? onComplete, bool shouldPauseOnInvisible)
	{
		if (TryCreateCustomRegisteredAnimator(out IAnimator animator) || TryCreateGradientAnimator(out animator) || TryCreateSolidColorBrushAnimator(out animator))
		{
			return animator.Apply(animation, control, clock, match, onComplete, shouldPauseOnInvisible);
		}
		Logger.TryGet(LogEventLevel.Error, "Animations")?.Log(this, "The animation's keyframe value types set is not supported.");
		return base.Apply(animation, control, clock, match, onComplete, shouldPauseOnInvisible);
	}

	/// <summary>
	/// Fallback implementation of <see cref="T:Avalonia.Media.IBrush" /> animation.
	/// </summary>
	public override IBrush? Interpolate(double progress, IBrush? oldValue, IBrush? newValue)
	{
		if (!(progress >= 0.5))
		{
			return oldValue;
		}
		return newValue;
	}

	private bool TryCreateGradientAnimator([NotNullWhen(true)] out IAnimator? animator)
	{
		IGradientBrush gradientBrush = null;
		using (AvaloniaList<AnimatorKeyFrame>.Enumerator enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.Value is IGradientBrush gradientBrush2)
				{
					gradientBrush = gradientBrush2;
					break;
				}
			}
		}
		if (gradientBrush == null)
		{
			animator = null;
			return false;
		}
		GradientBrushAnimator gradientBrushAnimator = new GradientBrushAnimator();
		gradientBrushAnimator.Property = base.Property;
		using (AvaloniaList<AnimatorKeyFrame>.Enumerator enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				AnimatorKeyFrame current = enumerator.Current;
				if (current.Value is ISolidColorBrush solidColorBrush)
				{
					gradientBrushAnimator.Add(new AnimatorKeyFrame(typeof(GradientBrushAnimator), () => new GradientBrushAnimator(), current.Cue, current.KeySpline)
					{
						Value = GradientBrushAnimator.ConvertSolidColorBrushToGradient(gradientBrush, solidColorBrush),
						FillBefore = current.FillBefore,
						FillAfter = current.FillAfter
					});
					continue;
				}
				if (current.Value is IGradientBrush)
				{
					gradientBrushAnimator.Add(new AnimatorKeyFrame(typeof(GradientBrushAnimator), () => new GradientBrushAnimator(), current.Cue, current.KeySpline)
					{
						Value = current.Value,
						FillBefore = current.FillBefore,
						FillAfter = current.FillAfter
					});
					continue;
				}
				animator = null;
				return false;
			}
		}
		animator = gradientBrushAnimator;
		return true;
	}

	private bool TryCreateSolidColorBrushAnimator([NotNullWhen(true)] out IAnimator? animator)
	{
		ISolidColorBrushAnimator solidColorBrushAnimator = new ISolidColorBrushAnimator();
		solidColorBrushAnimator.Property = base.Property;
		using (AvaloniaList<AnimatorKeyFrame>.Enumerator enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				AnimatorKeyFrame current = enumerator.Current;
				if (current.Value is ISolidColorBrush)
				{
					solidColorBrushAnimator.Add(new AnimatorKeyFrame(typeof(ISolidColorBrushAnimator), () => new ISolidColorBrushAnimator(), current.Cue, current.KeySpline)
					{
						Value = current.Value,
						FillBefore = current.FillBefore,
						FillAfter = current.FillAfter
					});
					continue;
				}
				animator = null;
				return false;
			}
		}
		animator = solidColorBrushAnimator;
		return true;
	}

	private bool TryCreateCustomRegisteredAnimator([NotNullWhen(true)] out IAnimator? animator)
	{
		if (_brushAnimators.Count > 0)
		{
			Type type = base[0].Value?.GetType();
			if ((object)type != null)
			{
				foreach (var (func, animatorType, func2) in _brushAnimators)
				{
					if (!func(type))
					{
						continue;
					}
					animator = func2();
					if (animator == null)
					{
						continue;
					}
					animator.Property = base.Property;
					using (AvaloniaList<AnimatorKeyFrame>.Enumerator enumerator2 = GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							AnimatorKeyFrame current = enumerator2.Current;
							animator.Add(new AnimatorKeyFrame(animatorType, func2, current.Cue, current.KeySpline)
							{
								Value = current.Value,
								FillBefore = current.FillBefore,
								FillAfter = current.FillAfter
							});
						}
					}
					return true;
				}
			}
		}
		animator = null;
		return false;
	}
}
