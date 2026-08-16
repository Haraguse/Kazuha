using System;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;
using Avalonia.Media;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:Avalonia.Media.IBrush" /> type.
/// </summary>
public class BrushTransition : Transition<IBrush?>
{
	private sealed class IncompatibleTransitionObservable : TransitionObservableBase<IBrush?>
	{
		private readonly IBrush? _from;

		private readonly IBrush? _to;

		public IncompatibleTransitionObservable(IObservable<double> progress, Easing easing, IBrush? from, IBrush? to)
			: base(progress, (IEasing)easing)
		{
			_from = from;
			_to = to;
		}

		protected override IBrush? ProduceValue(double progress)
		{
			if (!(progress >= 0.5))
			{
				return _from;
			}
			return _to;
		}
	}

	private static readonly GradientBrushAnimator s_gradientAnimator = new GradientBrushAnimator();

	private static readonly ISolidColorBrushAnimator s_solidColorBrushAnimator = new ISolidColorBrushAnimator();

	internal override IObservable<IBrush?> DoTransition(IObservable<double> progress, IBrush? oldValue, IBrush? newValue)
	{
		if (oldValue == null || newValue == null)
		{
			return new IncompatibleTransitionObservable(progress, base.Easing, oldValue, newValue);
		}
		if (oldValue is IGradientBrush gradientBrush)
		{
			if (newValue is IGradientBrush newValue2)
			{
				return new AnimatorTransitionObservable<IGradientBrush, GradientBrushAnimator>(s_gradientAnimator, progress, base.Easing, gradientBrush, newValue2);
			}
			if (newValue is ISolidColorBrush solidColorBrush)
			{
				IGradientBrush newValue3 = GradientBrushAnimator.ConvertSolidColorBrushToGradient(gradientBrush, solidColorBrush);
				return new AnimatorTransitionObservable<IGradientBrush, GradientBrushAnimator>(s_gradientAnimator, progress, base.Easing, gradientBrush, newValue3);
			}
		}
		else if (newValue is IGradientBrush gradientBrush2 && oldValue is ISolidColorBrush solidColorBrush2)
		{
			IGradientBrush oldValue2 = GradientBrushAnimator.ConvertSolidColorBrushToGradient(gradientBrush2, solidColorBrush2);
			return new AnimatorTransitionObservable<IGradientBrush, GradientBrushAnimator>(s_gradientAnimator, progress, base.Easing, oldValue2, gradientBrush2);
		}
		if (oldValue is ISolidColorBrush oldValue3 && newValue is ISolidColorBrush newValue4)
		{
			return new AnimatorTransitionObservable<ISolidColorBrush, ISolidColorBrushAnimator>(s_solidColorBrushAnimator, progress, base.Easing, oldValue3, newValue4);
		}
		return new IncompatibleTransitionObservable(progress, base.Easing, oldValue, newValue);
	}
}
