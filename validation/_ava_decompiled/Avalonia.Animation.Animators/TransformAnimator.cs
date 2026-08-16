using System;
using Avalonia.Collections;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Reactive;

namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:Avalonia.Media.Transform" /> properties.
/// </summary>
internal class TransformAnimator : Animator<double>
{
	private DoubleAnimator? _doubleAnimator;

	/// <inheritdoc />
	public override IDisposable? Apply(Animation animation, Animatable control, IClock? clock, IObservable<bool> obsMatch, Action? onComplete, bool shouldPauseOnInvisible)
	{
		Visual visual = (Visual)control;
		if ((object)base.Property == null)
		{
			throw new InvalidOperationException("Animator has no property specified.");
		}
		if (typeof(Transform).IsAssignableFrom(base.Property.OwnerType))
		{
			if (visual.RenderTransform is TransformOperations)
			{
				return Disposable.Empty;
			}
			if (visual.RenderTransform == null)
			{
				TransformGroup transformGroup = new TransformGroup();
				transformGroup.Children.Add(new ScaleTransform());
				transformGroup.Children.Add(new SkewTransform());
				transformGroup.Children.Add(new RotateTransform());
				transformGroup.Children.Add(new TranslateTransform());
				transformGroup.Children.Add(new Rotate3DTransform());
				visual.RenderTransform = transformGroup;
			}
			Type type = visual.RenderTransform.GetType();
			if (_doubleAnimator == null)
			{
				_doubleAnimator = new DoubleAnimator();
				using (AvaloniaList<AnimatorKeyFrame>.Enumerator enumerator = GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						AnimatorKeyFrame current = enumerator.Current;
						_doubleAnimator.Add(current);
					}
				}
				_doubleAnimator.Property = base.Property;
			}
			if (type == base.Property.OwnerType)
			{
				return _doubleAnimator.Apply(animation, (Transform)visual.RenderTransform, clock ?? control.Clock, obsMatch, onComplete, shouldPauseOnInvisible);
			}
			if (type == typeof(TransformGroup))
			{
				foreach (Transform child in ((TransformGroup)visual.RenderTransform).Children)
				{
					if (child.GetType() == base.Property.OwnerType)
					{
						return _doubleAnimator.Apply(animation, child, clock ?? control.Clock, obsMatch, onComplete, shouldPauseOnInvisible);
					}
				}
			}
			Logger.TryGet(LogEventLevel.Warning, "Animations")?.Log(control, $"Cannot find the appropriate transform: \"{base.Property.OwnerType}\" in {control}.");
		}
		else
		{
			Logger.TryGet(LogEventLevel.Error, "Animations")?.Log(control, $"Cannot apply animation: Target property owner {base.Property.OwnerType} is not a Transform object.");
		}
		return null;
	}

	/// <inheritdoc /> 
	public override double Interpolate(double p, double o, double n)
	{
		return 0.0;
	}
}
