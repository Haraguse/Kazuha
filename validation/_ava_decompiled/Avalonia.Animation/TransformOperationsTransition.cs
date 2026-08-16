using System;
using Avalonia.Animation.Animators;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Avalonia.Animation;

public class TransformOperationsTransition : Transition<ITransform>
{
	private static readonly TransformOperationsAnimator s_operationsAnimator = new TransformOperationsAnimator();

	internal override IObservable<ITransform> DoTransition(IObservable<double> progress, ITransform oldValue, ITransform newValue)
	{
		TransformOperations oldValue2 = TransformOperationsAnimator.EnsureOperations(oldValue);
		TransformOperations newValue2 = TransformOperationsAnimator.EnsureOperations(newValue);
		return new AnimatorTransitionObservable<TransformOperations, TransformOperationsAnimator>(s_operationsAnimator, progress, base.Easing, oldValue2, newValue2);
	}
}
