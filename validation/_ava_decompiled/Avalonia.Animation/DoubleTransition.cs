using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:System.Double" /> types.
/// </summary>  
public class DoubleTransition : Transition<double>
{
	internal override IObservable<double> DoTransition(IObservable<double> progress, double oldValue, double newValue)
	{
		return AnimatorDrivenTransition<double, DoubleAnimator>.Transition(base.Easing, progress, oldValue, newValue);
	}
}
