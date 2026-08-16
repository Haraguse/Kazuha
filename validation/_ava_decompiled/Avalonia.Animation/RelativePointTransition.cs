using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:Avalonia.RelativePoint" /> type.
/// </summary>  
public class RelativePointTransition : Transition<RelativePoint>
{
	internal override IObservable<RelativePoint> DoTransition(IObservable<double> progress, RelativePoint oldValue, RelativePoint newValue)
	{
		return AnimatorDrivenTransition<RelativePoint, RelativePointAnimator>.Transition(base.Easing, progress, oldValue, newValue);
	}
}
