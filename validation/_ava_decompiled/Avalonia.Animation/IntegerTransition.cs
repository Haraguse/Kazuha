using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:System.Int32" /> types.
/// </summary>  
public class IntegerTransition : Transition<int>
{
	internal override IObservable<int> DoTransition(IObservable<double> progress, int oldValue, int newValue)
	{
		return AnimatorDrivenTransition<int, Int32Animator>.Transition(base.Easing, progress, oldValue, newValue);
	}
}
