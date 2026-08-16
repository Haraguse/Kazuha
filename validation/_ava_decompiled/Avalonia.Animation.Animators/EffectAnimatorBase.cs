using System;
using Avalonia.Data;
using Avalonia.Media;

namespace Avalonia.Animation.Animators;

internal abstract class EffectAnimatorBase<T> : Animator<IEffect?> where T : class, IEffect?
{
	public override IDisposable BindAnimation(Animatable control, IObservable<IEffect?> instance)
	{
		if ((object)base.Property == null)
		{
			throw new InvalidOperationException("Animator has no property specified.");
		}
		return control.Bind((AvaloniaProperty<IEffect>)base.Property, instance, BindingPriority.Animation);
	}

	protected abstract T Interpolate(double progress, T oldValue, T newValue);

	public override IEffect? Interpolate(double progress, IEffect? oldValue, IEffect? newValue)
	{
		T val = oldValue as T;
		T val2 = newValue as T;
		if (val == null || val2 == null)
		{
			if (!(progress >= 0.5))
			{
				return oldValue;
			}
			return newValue;
		}
		return Interpolate(progress, val, val2);
	}
}
