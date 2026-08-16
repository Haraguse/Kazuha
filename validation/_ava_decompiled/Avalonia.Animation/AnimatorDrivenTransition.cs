using System;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;

namespace Avalonia.Animation;

/// <summary>
/// <see cref="T:Avalonia.Animation.Transition`1" /> using an <see cref="T:Avalonia.Animation.Animators.Animator`1" /> to transition between values.
/// </summary>
/// <typeparam name="T">Type of the transitioned value.</typeparam>
/// <typeparam name="TAnimator">Type of the animator.</typeparam>
internal static class AnimatorDrivenTransition<T, TAnimator> where TAnimator : Animator<T>, new()
{
	private static readonly TAnimator s_animator = new TAnimator();

	public static IObservable<T> Transition(IEasing easing, IObservable<double> progress, T oldValue, T newValue)
	{
		return new AnimatorTransitionObservable<T, TAnimator>(s_animator, progress, easing, oldValue, newValue);
	}
}
