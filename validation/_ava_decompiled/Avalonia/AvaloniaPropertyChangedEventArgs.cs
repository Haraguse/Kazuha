using System;
using Avalonia.Data;

namespace Avalonia;

/// <summary>
/// Provides information for a avalonia property change.
/// </summary>
public abstract class AvaloniaPropertyChangedEventArgs : EventArgs
{
	/// <summary>
	/// Gets the <see cref="T:Avalonia.AvaloniaObject" /> that the property changed on.
	/// </summary>
	/// <value>The sender object.</value>
	public AvaloniaObject Sender { get; private set; }

	/// <summary>
	/// Gets the property that changed.
	/// </summary>
	/// <value>
	/// The property that changed.
	/// </value>
	public AvaloniaProperty Property => GetProperty();

	/// <summary>
	/// Gets the old value of the property.
	/// </summary>
	public object? OldValue => GetOldValue();

	/// <summary>
	/// Gets the new value of the property.
	/// </summary>
	public object? NewValue => GetNewValue();

	/// <summary>
	/// Gets the priority of the binding that produced the value.
	/// </summary>
	/// <value>
	/// The priority of the new value.
	/// </value>
	public BindingPriority Priority { get; private set; }

	internal bool IsEffectiveValueChange { get; private set; }

	public AvaloniaPropertyChangedEventArgs(AvaloniaObject sender, BindingPriority priority)
	{
		Sender = sender;
		Priority = priority;
		IsEffectiveValueChange = true;
	}

	internal AvaloniaPropertyChangedEventArgs(AvaloniaObject sender, BindingPriority priority, bool isEffectiveValueChange)
	{
		Sender = sender;
		Priority = priority;
		IsEffectiveValueChange = isEffectiveValueChange;
	}

	/// <summary>
	/// Sets the Sender property.
	/// This is purely for reuse in some code paths where multiple allocations may occur.
	/// </summary>
	/// <param name="sender">The sender object.</param>
	internal void SetSender(AvaloniaObject sender)
	{
		Sender = sender;
	}

	protected abstract AvaloniaProperty GetProperty();

	protected abstract object? GetOldValue();

	protected abstract object? GetNewValue();
}
/// <summary>
/// Provides information for an Avalonia property change.
/// </summary>
public class AvaloniaPropertyChangedEventArgs<T> : AvaloniaPropertyChangedEventArgs
{
	/// <summary>
	/// Gets the property that changed.
	/// </summary>
	/// <value>
	/// The property that changed.
	/// </value>
	public new AvaloniaProperty<T> Property { get; }

	/// <summary>
	/// Gets the old value of the property.
	/// </summary>
	public new Optional<T> OldValue { get; private set; }

	/// <summary>
	/// Gets the new value of the property.
	/// </summary>
	public new BindingValue<T> NewValue { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.AvaloniaPropertyChangedEventArgs" /> class.
	/// </summary>
	/// <param name="sender">The object that the property changed on.</param>
	/// <param name="property">The property that changed.</param>
	/// <param name="oldValue">The old value of the property.</param>
	/// <param name="newValue">The new value of the property.</param>
	/// <param name="priority">The priority of the binding that produced the value.</param>
	public AvaloniaPropertyChangedEventArgs(AvaloniaObject sender, AvaloniaProperty<T> property, Optional<T> oldValue, BindingValue<T> newValue, BindingPriority priority)
		: this(sender, property, oldValue, newValue, priority, true)
	{
	}

	internal AvaloniaPropertyChangedEventArgs(AvaloniaObject sender, AvaloniaProperty<T> property, Optional<T> oldValue, BindingValue<T> newValue, BindingPriority priority, bool isEffectiveValueChange)
		: base(sender, priority, isEffectiveValueChange)
	{
		Property = property;
		OldValue = oldValue;
		NewValue = newValue;
	}

	protected override AvaloniaProperty GetProperty()
	{
		return Property;
	}

	protected override object? GetOldValue()
	{
		return OldValue.GetValueOrDefault(AvaloniaProperty.UnsetValue);
	}

	protected override object? GetNewValue()
	{
		return NewValue.GetValueOrDefault(AvaloniaProperty.UnsetValue);
	}
}
