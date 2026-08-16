using System;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.PropertyStore;

internal class DirectBindingObserver<T> : IObserver<T>, IObserver<BindingValue<T>>, IDisposable
{
	private readonly ValueStore _owner;

	private readonly bool _hasDataValidation;

	private IDisposable? _subscription;

	public DirectPropertyBase<T> Property { get; }

	public DirectBindingObserver(ValueStore owner, DirectPropertyBase<T> property)
	{
		_owner = owner;
		_hasDataValidation = property.GetMetadata(owner.Owner).EnableDataValidation == true;
		Property = property;
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
		_owner.OnLocalValueBindingCompleted(Property, this);
		if (_hasDataValidation)
		{
			_owner.Owner.OnUpdateDataValidation(Property, BindingValueType.UnsetValue, null);
		}
	}

	public void OnError(Exception error)
	{
		OnCompleted();
	}

	public void OnNext(T value)
	{
		if (Dispatcher.UIThread.CheckAccess())
		{
			_owner.Owner.SetDirectValueUnchecked(Property, value);
			return;
		}
		AvaloniaObject instance = _owner.Owner;
		DirectPropertyBase<T> property = Property;
		T newValue = value;
		Dispatcher.UIThread.Post(delegate
		{
			instance.SetDirectValueUnchecked(property, newValue);
		});
	}

	public void OnNext(BindingValue<T> value)
	{
		if (Dispatcher.UIThread.CheckAccess())
		{
			_owner.Owner.SetDirectValueUnchecked(Property, value);
			return;
		}
		AvaloniaObject instance = _owner.Owner;
		DirectPropertyBase<T> property = Property;
		BindingValue<T> newValue = value;
		Dispatcher.UIThread.Post(delegate
		{
			instance.SetDirectValueUnchecked(property, newValue);
		});
	}
}
