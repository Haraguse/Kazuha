using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:Avalonia.Point" /> type.
/// </summary>  
public class PointTransition : Transition<Point>
{
	internal override IObservable<Point> DoTransition(IObservable<double> progress, Point oldValue, Point newValue)
	{
		return AnimatorDrivenTransition<Point, PointAnimator>.Transition(base.Easing, progress, oldValue, newValue);
	}
}
