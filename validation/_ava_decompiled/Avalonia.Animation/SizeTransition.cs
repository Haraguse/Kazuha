using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:Avalonia.Size" /> type.
/// </summary>  
public class SizeTransition : Transition<Size>
{
	internal override IObservable<Size> DoTransition(IObservable<double> progress, Size oldValue, Size newValue)
	{
		return AnimatorDrivenTransition<Size, SizeAnimator>.Transition(base.Easing, progress, oldValue, newValue);
	}
}
