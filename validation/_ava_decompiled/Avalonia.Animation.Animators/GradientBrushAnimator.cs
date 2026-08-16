using System;
using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Transformation;

namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:Avalonia.Media.SolidColorBrush" /> values. 
/// </summary>
internal class GradientBrushAnimator : Animator<IGradientBrush?>
{
	private static readonly RelativePointAnimator s_relativePointAnimator = new RelativePointAnimator();

	private static readonly RelativeScalarAnimator s_relativeScalarAnimator = new RelativeScalarAnimator();

	private static readonly DoubleAnimator s_doubleAnimator = new DoubleAnimator();

	public override IGradientBrush? Interpolate(double progress, IGradientBrush? oldValue, IGradientBrush? newValue)
	{
		if (oldValue == null || newValue == null)
		{
			if (!(progress >= 0.5))
			{
				return oldValue;
			}
			return newValue;
		}
		if (!(oldValue is IRadialGradientBrush radialGradientBrush) || !(newValue is IRadialGradientBrush radialGradientBrush2))
		{
			if (!(oldValue is IConicGradientBrush conicGradientBrush) || !(newValue is IConicGradientBrush conicGradientBrush2))
			{
				if (oldValue is ILinearGradientBrush linearGradientBrush && newValue is ILinearGradientBrush linearGradientBrush2)
				{
					return new ImmutableLinearGradientBrush(InterpolateStops(progress, oldValue.GradientStops, newValue.GradientStops), s_doubleAnimator.Interpolate(progress, oldValue.Opacity, newValue.Opacity), InterpolateTransform(progress, oldValue.Transform, newValue.Transform), s_relativePointAnimator.Interpolate(progress, oldValue.TransformOrigin, newValue.TransformOrigin), oldValue.SpreadMethod, s_relativePointAnimator.Interpolate(progress, linearGradientBrush.StartPoint, linearGradientBrush2.StartPoint), s_relativePointAnimator.Interpolate(progress, linearGradientBrush.EndPoint, linearGradientBrush2.EndPoint));
				}
				if (!(progress >= 0.5))
				{
					return oldValue;
				}
				return newValue;
			}
			return new ImmutableConicGradientBrush(InterpolateStops(progress, oldValue.GradientStops, newValue.GradientStops), s_doubleAnimator.Interpolate(progress, oldValue.Opacity, newValue.Opacity), InterpolateTransform(progress, oldValue.Transform, newValue.Transform), s_relativePointAnimator.Interpolate(progress, oldValue.TransformOrigin, newValue.TransformOrigin), oldValue.SpreadMethod, s_relativePointAnimator.Interpolate(progress, conicGradientBrush.Center, conicGradientBrush2.Center), s_doubleAnimator.Interpolate(progress, conicGradientBrush.Angle, conicGradientBrush2.Angle));
		}
		return new ImmutableRadialGradientBrush(InterpolateStops(progress, oldValue.GradientStops, newValue.GradientStops), s_doubleAnimator.Interpolate(progress, oldValue.Opacity, newValue.Opacity), InterpolateTransform(progress, oldValue.Transform, newValue.Transform), s_relativePointAnimator.Interpolate(progress, oldValue.TransformOrigin, newValue.TransformOrigin), oldValue.SpreadMethod, s_relativePointAnimator.Interpolate(progress, radialGradientBrush.Center, radialGradientBrush2.Center), s_relativePointAnimator.Interpolate(progress, radialGradientBrush.GradientOrigin, radialGradientBrush2.GradientOrigin), s_relativeScalarAnimator.Interpolate(progress, radialGradientBrush.RadiusX, radialGradientBrush2.RadiusX), s_relativeScalarAnimator.Interpolate(progress, radialGradientBrush.RadiusY, radialGradientBrush2.RadiusY));
	}

	public override IDisposable BindAnimation(Animatable control, IObservable<IGradientBrush?> instance)
	{
		if ((object)base.Property == null)
		{
			throw new InvalidOperationException("Animator has no property specified.");
		}
		return control.Bind((AvaloniaProperty<IBrush>)base.Property, instance, BindingPriority.Animation);
	}

	private static ImmutableTransform? InterpolateTransform(double progress, ITransform? oldTransform, ITransform? newTransform)
	{
		if (oldTransform is TransformOperations transformOperations && newTransform is TransformOperations to)
		{
			return new ImmutableTransform(TransformOperations.Interpolate(transformOperations, to, progress).Value);
		}
		if (oldTransform != null)
		{
			return new ImmutableTransform(oldTransform.Value);
		}
		return null;
	}

	private static IReadOnlyList<ImmutableGradientStop> InterpolateStops(double progress, IReadOnlyList<IGradientStop> oldValue, IReadOnlyList<IGradientStop> newValue)
	{
		int num = Math.Max(oldValue.Count, newValue.Count);
		ImmutableGradientStop[] array = new ImmutableGradientStop[num];
		int i = 0;
		int num2 = 0;
		int num3 = 0;
		for (; i < num; i++)
		{
			array[i] = new ImmutableGradientStop(s_doubleAnimator.Interpolate(progress, oldValue[num2].Offset, newValue[num3].Offset), ColorAnimator.InterpolateCore(progress, oldValue[num2].Color, newValue[num3].Color));
			if (num2 < oldValue.Count - 1)
			{
				num2++;
			}
			if (num3 < newValue.Count - 1)
			{
				num3++;
			}
		}
		return array;
	}

	internal static IGradientBrush ConvertSolidColorBrushToGradient(IGradientBrush gradientBrush, ISolidColorBrush solidColorBrush)
	{
		if (!(gradientBrush is IRadialGradientBrush radialGradientBrush))
		{
			if (!(gradientBrush is IConicGradientBrush conicGradientBrush))
			{
				if (gradientBrush is ILinearGradientBrush linearGradientBrush)
				{
					return new ImmutableLinearGradientBrush(CreateStopsFromSolidColorBrush(solidColorBrush, linearGradientBrush.GradientStops), solidColorBrush.Opacity, (linearGradientBrush.Transform != null) ? new ImmutableTransform(linearGradientBrush.Transform.Value) : null, linearGradientBrush.TransformOrigin, linearGradientBrush.SpreadMethod, linearGradientBrush.StartPoint, linearGradientBrush.EndPoint);
				}
				throw new NotSupportedException($"Gradient of type {gradientBrush?.GetType()} is not supported");
			}
			return new ImmutableConicGradientBrush(CreateStopsFromSolidColorBrush(solidColorBrush, conicGradientBrush.GradientStops), solidColorBrush.Opacity, (conicGradientBrush.Transform != null) ? new ImmutableTransform(conicGradientBrush.Transform.Value) : null, conicGradientBrush.TransformOrigin, conicGradientBrush.SpreadMethod, conicGradientBrush.Center, conicGradientBrush.Angle);
		}
		return new ImmutableRadialGradientBrush(CreateStopsFromSolidColorBrush(solidColorBrush, radialGradientBrush.GradientStops), solidColorBrush.Opacity, (radialGradientBrush.Transform != null) ? new ImmutableTransform(radialGradientBrush.Transform.Value) : null, radialGradientBrush.TransformOrigin, radialGradientBrush.SpreadMethod, radialGradientBrush.Center, radialGradientBrush.GradientOrigin, radialGradientBrush.RadiusX, radialGradientBrush.RadiusY);
		static IReadOnlyList<ImmutableGradientStop> CreateStopsFromSolidColorBrush(ISolidColorBrush solidColorBrush2, IReadOnlyList<IGradientStop> baseStops)
		{
			ImmutableGradientStop[] array = new ImmutableGradientStop[baseStops.Count];
			for (int i = 0; i < baseStops.Count; i++)
			{
				array[i] = new ImmutableGradientStop(baseStops[i].Offset, solidColorBrush2.Color);
			}
			return array;
		}
	}
}
