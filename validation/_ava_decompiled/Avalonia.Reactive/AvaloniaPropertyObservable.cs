using System;
using System.Collections.Generic;
using Avalonia.Data;

namespace Avalonia.Reactive;

internal class AvaloniaPropertyObservable<TSource, TResult> : LightweightObservableBase<TResult>, IDescription
{
	private readonly WeakReference<AvaloniaObject> _target;

	private readonly AvaloniaProperty _property;

	private readonly Func<TSource, TResult>? _converter;

	private Optional<TResult> _value;

	public string Description => _target.GetType().Name + "." + _property.Name;

	public AvaloniaPropertyObservable(AvaloniaObject target, AvaloniaProperty property, Func<TSource, TResult>? converter = null)
	{
		_target = new WeakReference<AvaloniaObject>(target);
		_property = property;
		_converter = converter;
	}

	protected override void Initialize()
	{
		if (_target.TryGetTarget(out AvaloniaObject target))
		{
			Func<TSource, TResult> converter = _converter;
			if (converter != null)
			{
				TSource arg = (TSource)target.GetValue(_property);
				_value = converter(arg);
				target.PropertyChanged += PropertyChanged_WithConversion;
			}
			else
			{
				_value = (TResult)target.GetValue(_property);
				target.PropertyChanged += PropertyChanged;
			}
		}
	}

	protected override void Deinitialize()
	{
		if (_target.TryGetTarget(out AvaloniaObject target))
		{
			if (_converter != null)
			{
				target.PropertyChanged -= PropertyChanged_WithConversion;
			}
			else
			{
				target.PropertyChanged -= PropertyChanged;
			}
		}
		_value = default(Optional<TResult>);
	}

	protected override void Subscribed(IObserver<TResult> observer, bool first)
	{
		if (_value.HasValue)
		{
			observer.OnNext(_value.Value);
		}
	}

	private void PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == _property)
		{
			TResult newValue = ((!(e is AvaloniaPropertyChangedEventArgs<TResult> e2)) ? ((TResult)e.Sender.GetValue(e.Property)) : AvaloniaObjectExtensions.GetValue(e.Sender, e2.Property));
			PublishNewValue(newValue);
		}
	}

	private void PropertyChanged_WithConversion(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == _property)
		{
			TSource arg = ((!(e is AvaloniaPropertyChangedEventArgs<TSource> e2)) ? ((TSource)e.Sender.GetValue(e.Property)) : AvaloniaObjectExtensions.GetValue(e.Sender, e2.Property));
			TResult newValue = _converter(arg);
			PublishNewValue(newValue);
		}
	}

	private void PublishNewValue(TResult newValue)
	{
		if (!_value.HasValue || !EqualityComparer<TResult>.Default.Equals(newValue, _value.Value))
		{
			_value = newValue;
			PublishNext(_value.Value);
		}
	}
}
