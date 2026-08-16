using System;
using Avalonia.Animation.Animators;
using Avalonia.Media;

namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="T:Avalonia.AvaloniaProperty" /> with <see cref="T:Avalonia.Media.Color" /> type.
/// </summary>
public class ColorTransition : Transition<Color>
{
	internal override IObservable<Color> DoTransition(IObservable<double> progress, Color oldValue, Color newValue)
	{
		return AnimatorDrivenTransition<Color, ColorAnimator>.Transition(base.Easing, progress, oldValue, newValue);
	}
}
