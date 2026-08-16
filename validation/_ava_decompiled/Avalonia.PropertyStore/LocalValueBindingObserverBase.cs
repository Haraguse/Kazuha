using System;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.PropertyStore;

internal class LocalValueBindingObserverBase<T> : IObserver<T>, IObserver<BindingValue<T>>, IDisposable
{
	private readonly ValueStore _owner;

	private readonly bool _hasDataValidation;

	protected IDisposable? _subscription;

	private T? _defaultValue;

	private bool _isDefaultValueInitialized;

	public StyledProperty<T> Property { get; }

	protected LocalValueBindingObserverBase(ValueStore owner, StyledProperty<T> property)
	{
		_owner = owner;
		Property = property;
		_hasDataValidation = property.GetMetadata(owner.Owner).EnableDataValidation == true;
	}

	public void Start(IObservable<T> source)
	{
		_subscription = source.Subscribe(this);
	}

	public void Start(IObservable<BindingValue<T>> source)
	{
		_subscription = source.Subscribe(this);
	}

	public void Dispose()
	{
		_subscription?.Dispose();
		_subscription = null;
		OnCompleted();
	}

	public void OnCompleted()
	{
		if (_hasDataValidation)
		{
			_owner.Owner.OnUpdateDataValidation(Property, BindingValueType.UnsetValue, null);
		}
		_owner.OnLocalValueBindingCompleted(Property, this);
	}

	public void OnError(Exception error)
	{
		OnCompleted();
	}

	public void OnNext(T value)
	{
		if (Dispatcher.UIThread.CheckAccess())
		{
			Execute(this, value);
			return;
		}
		T newValue = value;
		Dispatcher.UIThread.Post(delegate
		{
			Execute(this, newValue);
		});
		static void Execute(LocalValueBindingObserverBase<T> instance, T cachedDefaultValue)
		{
			ValueStore owner = instance._owner;
			StyledProperty<T> property = instance.Property;
			Func<T, bool>? validateValue = property.ValidateValue;
			if (validateValue != null && !validateValue(cachedDefaultValue))
			{
				cachedDefaultValue = instance.GetCachedDefaultValue();
			}
			owner.SetLocalValue(property, cachedDefaultValue);
			if (instance._hasDataValidation)
			{
				owner.Owner.OnUpdateDataValidation(property, BindingValueType.Value, null);
			}
		}
	}

	public void OnNext(BindingValue<T> value)
	{
		if (value.Type == BindingValueType.DoNothing)
		{
			return;
		}
		if (Dispatcher.UIThread.CheckAccess())
		{
			Execute(this, value);
			return;
		}
		BindingValue<T> newValue = value;
		Dispatcher.UIThread.Post(delegate
		{
			Execute(this, newValue);
		});
		static void Execute(LocalValueBindingObserverBase<T> instance, BindingValue<T> bindingValue)
		{
			ValueStore owner = instance._owner;
			StyledProperty<T> property = instance.Property;
			BindingValueType type = bindingValue.Type;
			if (bindingValue.HasValue)
			{
				Func<T, bool>? validateValue = property.ValidateValue;
				if (validateValue != null && !validateValue(bindingValue.Value))
				{
					goto IL_0054;
				}
			}
			if (!bindingValue.HasValue && bindingValue.Type != BindingValueType.DataValidationError)
			{
				goto IL_0054;
			}
			goto IL_0063;
			IL_0063:
			if (bindingValue.HasValue)
			{
				owner.SetLocalValue(property, bindingValue.Value);
			}
			if (instance._hasDataValidation)
			{
				owner.Owner.OnUpdateDataValidation(property, type, bindingValue.Error);
			}
			return;
			IL_0054:
			bindingValue = bindingValue.WithValue(instance.GetCachedDefaultValue());
			goto IL_0063;
		}
	}

	private T GetCachedDefaultValue()
	{
		if (!_isDefaultValueInitialized)
		{
			_defaultValue = Property.GetDefaultValue(_owner.Owner);
			_isDefaultValueInitialized = true;
		}
		return _defaultValue;
	}
}
