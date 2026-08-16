using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:System.Single" /> types.
/// </summary>  
public class FloatTransition : Transition<float>
{
	internal override IObservable<float> DoTransition(IObservable<double> progress, float oldValue, float newValue)
	{
		return AnimatorDrivenTransition<float, FloatAnimator>.Transition(base.Easing, progress, oldValue, newValue);
	}
}
