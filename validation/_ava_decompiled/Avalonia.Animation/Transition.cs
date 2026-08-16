using System;
using Avalonia.Data;

namespace Avalonia.Animation;

/// <summary>
/// Defines how a property should be animated using a transition.
/// </summary>
public abstract class Transition<T> : TransitionBase
{
	static Transition()
	{
		TransitionBase.PropertyProperty.Changed.AddClassHandler(delegate(Transition<T> x, AvaloniaPropertyChangedEventArgs e)
		{
			x.OnPropertyPropertyChanged(e);
		});
	}

	private void OnPropertyPropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		if (e.NewValue is AvaloniaProperty avaloniaProperty && !avaloniaProperty.PropertyType.IsAssignableFrom(typeof(T)))
		{
			throw new InvalidCastException($"Invalid property type \"{typeof(T).Name}\" for this transition: {GetType().Name}.");
		}
	}

	/// <summary>
	/// Apply interpolation to the property.
	/// </summary>
	internal abstract IObservable<T> DoTransition(IObservable<double> progress, T oldValue, T newValue);

	internal override IDisposable Apply(Animatable control, IClock clock, object? oldValue, object? newValue)
	{
		if ((object)base.Property == null)
		{
			throw new InvalidOperationException("Transition has no property specified.");
		}
		IObservable<T> source = DoTransition(new TransitionInstance(clock, base.Delay, base.Duration), (T)oldValue, (T)newValue);
		return control.Bind((AvaloniaProperty<T>)base.Property, source, BindingPriority.Animation);
	}
}
