using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.PropertyStore;

[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Implicit conversion methods might be removed by the linker. We don't have a reliable way to prevent it, except converting everything in compile time when possible.")]
internal class DirectUntypedBindingObserver<T> : IObserver<object?>, IDisposable
{
	private readonly ValueStore _owner;

	private readonly bool _hasDataValidation;

	private IDisposable? _subscription;

	public DirectPropertyBase<T> Property { get; }

	public DirectUntypedBindingObserver(ValueStore owner, DirectPropertyBase<T> property)
	{
		_owner = owner;
		_hasDataValidation = property.GetMetadata(owner.Owner).EnableDataValidation == true;
		Property = property;
	}

	public void Start(IObservable<object?> source)
	{
		_subscription = source.Subscribe(this);
	}

	public void Dispose()
	{
		_subscription?.Dispose();
		_subscription = null;
		_owner.OnLocalValueBindingCompleted(Property, this);
		if (_hasDataValidation)
		{
			_owner.Owner.OnUpdateDataValidation(Property, BindingValueType.UnsetValue, null);
		}
	}

	public void OnCompleted()
	{
		_owner.OnLocalValueBindingCompleted(Property, this);
	}

	public void OnError(Exception error)
	{
		OnCompleted();
	}

	public void OnNext(object? value)
	{
		BindingValue<T> typed = BindingValue<T>.FromUntyped(value);
		if (Dispatcher.UIThread.CheckAccess())
		{
			_owner.Owner.SetDirectValueUnchecked(Property, typed);
			return;
		}
		AvaloniaObject instance = _owner.Owner;
		DirectPropertyBase<T> property = Property;
		Dispatcher.UIThread.Post(delegate
		{
			instance.SetDirectValueUnchecked(property, typed);
		});
	}
}
