using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:Avalonia.Vector" /> type.
/// </summary>  
public class VectorTransition : Transition<Vector>
{
	internal override IObservable<Vector> DoTransition(IObservable<double> progress, Vector oldValue, Vector newValue)
	{
		return AnimatorDrivenTransition<Vector, VectorAnimator>.Transition(base.Easing, progress, oldValue, newValue);
	}
}
