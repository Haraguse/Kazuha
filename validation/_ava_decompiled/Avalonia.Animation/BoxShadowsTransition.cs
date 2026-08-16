using System;
using Avalonia.Animation.Animators;
using Avalonia.Media;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:Avalonia.Media.BoxShadows" /> type.
/// </summary>  
public class BoxShadowsTransition : Transition<BoxShadows>
{
	internal override IObservable<BoxShadows> DoTransition(IObservable<double> progress, BoxShadows oldValue, BoxShadows newValue)
	{
		return AnimatorDrivenTransition<BoxShadows, BoxShadowsAnimator>.Transition(base.Easing, progress, oldValue, newValue);
	}
}
