using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;
using Avalonia.Media;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:Avalonia.Media.IEffect" /> type.
/// </summary>
public class EffectTransition : Transition<IEffect?>
{
	private sealed class IncompatibleTransitionObservable : TransitionObservableBase<IEffect?>
	{
		private readonly IEffect? _from;

		private readonly IEffect? _to;

		public IncompatibleTransitionObservable(IObservable<double> progress, Easing easing, IEffect? from, IEffect? to)
			: base(progress, (IEasing)easing)
		{
			_from = from;
			_to = to;
		}

		protected override IEffect? ProduceValue(double progress)
		{
			if (!(progress >= 0.5))
			{
				return _from;
			}
			return _to;
		}
	}

	private static readonly BlurEffectAnimator s_blurEffectAnimator = new BlurEffectAnimator();

	private static readonly DropShadowEffectAnimator s_dropShadowEffectAnimator = new DropShadowEffectAnimator();

	private static readonly ImmutableBlurEffect s_DefaultBlur = new ImmutableBlurEffect(0.0);

	private static readonly ImmutableDropShadowDirectionEffect s_DefaultDropShadow = new ImmutableDropShadowDirectionEffect(0.0, 0.0, 0.0, default(Color), 0.0);

	private bool TryWithAnimator<TAnimator, TInterface>(IObservable<double> progress, TAnimator animator, IEffect? oldValue, IEffect? newValue, TInterface defaultValue, [MaybeNullWhen(false)] out IObservable<IEffect?> observable) where TAnimator : EffectAnimatorBase<TInterface> where TInterface : class, IEffect
	{
		observable = null;
		TInterface val = null;
		TInterface val2 = null;
		if (oldValue is TInterface val3)
		{
			val = val3;
			if (newValue is TInterface val4)
			{
				val2 = val4;
			}
			else
			{
				if (newValue != null)
				{
					return false;
				}
				val2 = defaultValue;
			}
		}
		else
		{
			if (!(newValue is TInterface val5))
			{
				return false;
			}
			val = defaultValue;
			val2 = val5;
		}
		observable = new AnimatorTransitionObservable<IEffect, Animator<IEffect>>(animator, progress, base.Easing, val, val2);
		return true;
	}

	internal override IObservable<IEffect?> DoTransition(IObservable<double> progress, IEffect? oldValue, IEffect? newValue)
	{
		if ((oldValue != null || newValue != null) && (TryWithAnimator(progress, s_blurEffectAnimator, oldValue, newValue, (IBlurEffect)s_DefaultBlur, out IObservable<IEffect> observable) || TryWithAnimator(progress, s_dropShadowEffectAnimator, oldValue, newValue, (IDropShadowEffect)s_DefaultDropShadow, out observable)))
		{
			return observable;
		}
		return new IncompatibleTransitionObservable(progress, base.Easing, oldValue, newValue);
	}
}
