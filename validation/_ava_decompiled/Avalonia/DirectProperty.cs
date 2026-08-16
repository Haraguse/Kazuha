using System;
using Avalonia.Data;

namespace Avalonia;

/// <summary>
/// A direct avalonia property.
/// </summary>
/// <typeparam name="TOwner">The class that registered the property.</typeparam>
/// <typeparam name="TValue">The type of the property's value.</typeparam>
/// <remarks>
/// Direct avalonia properties are backed by a field on the object, but exposed via the
/// <see cref="T:Avalonia.AvaloniaProperty" /> system. They hold a getter and an optional setter which
/// allows the avalonia property system to read and write the current value.
/// </remarks>
public class DirectProperty<TOwner, TValue> : DirectPropertyBase<TValue>, IDirectPropertyAccessor where TOwner : AvaloniaObject
{
	/// <summary>
	/// Gets the getter function.
	/// </summary>
	public Func<TOwner, TValue> Getter { get; }

	/// <summary>
	/// Gets the setter function.
	/// </summary>
	public Action<TOwner, TValue>? Setter { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.DirectProperty`2" /> class.
	/// </summary>
	/// <param name="name">The name of the property.</param>
	/// <param name="getter">Gets the current value of the property.</param>
	/// <param name="setter">Sets the value of the property. May be null.</param>
	/// <param name="metadata">The property metadata.</param>
	internal DirectProperty(string name, Func<TOwner, TValue> getter, Action<TOwner, TValue>? setter, DirectPropertyMetadata<TValue> metadata)
		: base(name, typeof(TOwner), (AvaloniaPropertyMetadata)metadata)
	{
		Getter = getter ?? throw new ArgumentNullException("getter");
		Setter = setter;
		base.IsReadOnly = setter == null;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.AvaloniaProperty" /> class.
	/// </summary>
	/// <param name="source">The property to copy.</param>
	/// <param name="getter">Gets the current value of the property.</param>
	/// <param name="setter">Sets the value of the property. May be null.</param>
	/// <param name="metadata">Optional overridden metadata.</param>
	private DirectProperty(DirectPropertyBase<TValue> source, Func<TOwner, TValue> getter, Action<TOwner, TValue>? setter, DirectPropertyMetadata<TValue> metadata)
		: base(source, typeof(TOwner), (AvaloniaPropertyMetadata)metadata)
	{
		Getter = getter ?? throw new ArgumentNullException("getter");
		Setter = setter;
		base.IsReadOnly = setter == null;
	}

	/// <summary>
	/// Registers the direct property on another type.
	/// </summary>
	/// <typeparam name="TNewOwner">The type of the additional owner.</typeparam>
	/// <param name="getter">Gets the current value of the property.</param>
	/// <param name="setter">Sets the value of the property.</param>
	/// <param name="unsetValue">
	/// The value to use when the property is set to <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />
	/// </param>
	/// <param name="defaultBindingMode">The default binding mode for the property.</param>
	/// <param name="enableDataValidation">
	/// Whether the property is interested in data validation.
	/// </param>
	/// <returns>The property.</returns>
	public DirectProperty<TNewOwner, TValue> AddOwner<TNewOwner>(Func<TNewOwner, TValue> getter, Action<TNewOwner, TValue>? setter = null, TValue unsetValue = default(TValue), BindingMode defaultBindingMode = BindingMode.Default, bool enableDataValidation = false) where TNewOwner : AvaloniaObject
	{
		DirectPropertyMetadata<TValue> directPropertyMetadata = new DirectPropertyMetadata<TValue>(unsetValue, defaultBindingMode, enableDataValidation);
		directPropertyMetadata.Merge(GetMetadata<TOwner>(), this);
		directPropertyMetadata.Freeze();
		DirectProperty<TNewOwner, TValue> directProperty = new DirectProperty<TNewOwner, TValue>(this, getter, setter, directPropertyMetadata);
		AvaloniaPropertyRegistry.Instance.Register(typeof(TNewOwner), directProperty);
		return directProperty;
	}

	/// <inheritdoc />
	internal override TValue InvokeGetter(AvaloniaObject instance)
	{
		return Getter((TOwner)instance);
	}

	/// <inheritdoc />
	internal override void InvokeSetter(AvaloniaObject instance, BindingValue<TValue> value)
	{
		if (Setter == null)
		{
			throw new ArgumentException("The property " + base.Name + " is readonly.");
		}
		if (value.HasValue)
		{
			Setter((TOwner)instance, value.Value);
		}
	}

	/// <inheritdoc />
	object? IDirectPropertyAccessor.GetValue(AvaloniaObject instance)
	{
		return Getter((TOwner)instance);
	}

	/// <inheritdoc />
	void IDirectPropertyAccessor.SetValue(AvaloniaObject instance, object? value)
	{
		if (Setter == null)
		{
			throw new ArgumentException("The property " + base.Name + " is readonly.");
		}
		Setter((TOwner)instance, (TValue)value);
	}

	object? IDirectPropertyAccessor.GetUnsetValue(Type type)
	{
		return GetMetadata(type).UnsetValue;
	}

	object? IDirectPropertyAccessor.GetUnsetValue(AvaloniaObject owner)
	{
		return GetMetadata(owner).UnsetValue;
	}
}
