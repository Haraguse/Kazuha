using System;
using System.ComponentModel;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

internal class InpcPropertyAccessor : PropertyAccessorBase, IWeakEventSubscriber<PropertyChangedEventArgs>
{
	protected readonly WeakReference<object?> _reference;

	private readonly IPropertyInfo _property;

	public override Type PropertyType => _property.PropertyType;

	public override object? Value
	{
		get
		{
			if (!_reference.TryGetTarget(out object target))
			{
				return null;
			}
			return _property.Get(target);
		}
	}

	public InpcPropertyAccessor(WeakReference<object?> reference, IPropertyInfo property)
	{
		_reference = reference ?? throw new ArgumentNullException("reference");
		_property = property ?? throw new ArgumentNullException("property");
	}

	public override bool SetValue(object? value, BindingPriority priority)
	{
		if (_property.CanSet && _reference.TryGetTarget(out object target))
		{
			_property.Set(target, value);
			SendCurrentValue();
			return true;
		}
		return false;
	}

	public void OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == _property.Name || string.IsNullOrEmpty(e.PropertyName))
		{
			SendCurrentValue();
		}
	}

	protected override void SubscribeCore()
	{
		SendCurrentValue();
		SubscribeToChanges();
	}

	protected override void UnsubscribeCore()
	{
		if (_reference.TryGetTarget(out object target) && target is INotifyPropertyChanged target2)
		{
			WeakEvents.ThreadSafePropertyChanged.Unsubscribe(target2, this);
		}
	}

	protected void SendCurrentValue()
	{
		try
		{
			object value = Value;
			PublishValue(value);
		}
		catch (Exception error)
		{
			PublishValue(new BindingNotification(error, BindingErrorType.Error));
		}
	}

	private void SubscribeToChanges()
	{
		if (_reference.TryGetTarget(out object target) && target is INotifyPropertyChanged target2)
		{
			WeakEvents.ThreadSafePropertyChanged.Subscribe(target2, this);
		}
	}
}
