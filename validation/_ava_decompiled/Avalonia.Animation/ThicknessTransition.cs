using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:Avalonia.Thickness" /> type.
/// </summary>  
public class ThicknessTransition : Transition<Thickness>
{
	internal override IObservable<Thickness> DoTransition(IObservable<double> progress, Thickness oldValue, Thickness newValue)
	{
		return AnimatorDrivenTransition<Thickness, ThicknessAnimator>.Transition(base.Easing, progress, oldValue, newValue);
	}
}
