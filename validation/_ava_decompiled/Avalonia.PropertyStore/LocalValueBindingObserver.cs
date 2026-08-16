using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;

namespace Avalonia.PropertyStore;

internal class LocalValueBindingObserver<T> : LocalValueBindingObserverBase<T>, IObserver<object?>
{
	public LocalValueBindingObserver(ValueStore owner, StyledProperty<T> property)
		: base(owner, property)
	{
	}

	public void Start(IObservable<object?> source)
	{
		_subscription = source.Subscribe(this);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Implicit conversion methods might be removed by the linker. We don't have a reliable way to prevent it, except converting everything in compile time when possible.")]
	public void OnNext(object? value)
	{
		if (value != BindingOperations.DoNothing)
		{
			base.OnNext(BindingValue<T>.FromUntyped(value, base.Property.PropertyType));
		}
	}
}
