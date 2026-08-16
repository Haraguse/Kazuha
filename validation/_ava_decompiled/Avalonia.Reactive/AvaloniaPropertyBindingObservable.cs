using System;
using System.Collections.Generic;
using Avalonia.Data;

namespace Avalonia.Reactive;

internal class AvaloniaPropertyBindingObservable<TSource, TResult> : LightweightObservableBase<BindingValue<TResult>>, IDescription
{
	private readonly WeakReference<AvaloniaObject> _target;

	private readonly AvaloniaProperty _property;

	private readonly Func<TSource, TResult>? _converter;

	private BindingValue<TResult> _value = BindingValue<TResult>.Unset;

	public string Description => _target.GetType().Name + "." + _property.Name;

	public AvaloniaPropertyBindingObservable(AvaloniaObject target, AvaloniaProperty property, Func<TSource, TResult>? converter = null)
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
	}

	protected override void Subscribed(IObserver<BindingValue<TResult>> observer, bool first)
	{
		if (_value.Type != BindingValueType.UnsetValue)
		{
			observer.OnNext(_value);
		}
	}

	private void PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == _property)
		{
			if (e is AvaloniaPropertyChangedEventArgs<TResult> e2)
			{
				PublishValue(AvaloniaObjectExtensions.GetValue(e.Sender, e2.Property));
			}
			else
			{
				PublishUntypedValue(e.Sender.GetValue(e.Property));
			}
		}
	}

	private void PropertyChanged_WithConversion(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (!(e.Property == _property))
		{
			return;
		}
		if (e is AvaloniaPropertyChangedEventArgs<TSource> e2)
		{
			TSource value = AvaloniaObjectExtensions.GetValue(e.Sender, e2.Property);
			TResult newValue = _converter(value);
			PublishValue(newValue);
			return;
		}
		object obj = e.Sender.GetValue(e.Property);
		if (obj is TSource arg)
		{
			obj = _converter(arg);
		}
		PublishUntypedValue(obj);
	}

	private void PublishValue(TResult newValue)
	{
		if (!_value.HasValue || !EqualityComparer<TResult>.Default.Equals(newValue, _value.Value))
		{
			_value = newValue;
			PublishNext(_value);
		}
	}

	private void PublishUntypedValue(object? newValue)
	{
		if (!object.Equals(newValue, _value))
		{
			_value = (TResult)newValue;
			PublishNext(_value);
		}
	}
}
