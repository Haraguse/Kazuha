using System;
using Avalonia.Data;

namespace Avalonia.PropertyStore;

internal class ImmediateValueEntry<T> : IValueEntry<T>, IValueEntry, IDisposable
{
	private readonly ImmediateValueFrame _owner;

	private readonly T _value;

	public StyledProperty<T> Property { get; }

	AvaloniaProperty IValueEntry.Property => Property;

	public ImmediateValueEntry(ImmediateValueFrame owner, StyledProperty<T> property, T value)
	{
		_owner = owner;
		_value = value;
		Property = property;
	}

	public void Unsubscribe()
	{
	}

	public void Dispose()
	{
		_owner.OnEntryDisposed(this);
	}

	bool IValueEntry.HasValue()
	{
		return true;
	}

	object? IValueEntry.GetValue()
	{
		return _value;
	}

	T IValueEntry<T>.GetValue()
	{
		return _value;
	}

	bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error)
	{
		state = BindingValueType.Value;
		error = null;
		return false;
	}
}
