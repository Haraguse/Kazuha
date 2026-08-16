using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:System.Boolean" /> types.
/// </summary>  
public class BoolTransition : Transition<bool>
{
	internal override IObservable<bool> DoTransition(IObservable<double> progress, bool oldValue, bool newValue)
	{
		return AnimatorDrivenTransition<bool, BoolAnimator>.Transition(base.Easing, progress, oldValue, newValue);
	}
}
