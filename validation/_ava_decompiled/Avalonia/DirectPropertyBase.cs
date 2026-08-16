using System;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.PropertyStore;

namespace Avalonia;

/// <summary>
/// Base class for direct properties.
/// </summary>
/// <typeparam name="TValue">The type of the property's value.</typeparam>
/// <remarks>
/// Whereas <see cref="T:Avalonia.DirectProperty`2" /> is typed on the owner type, this base
/// class provides a non-owner-typed interface to a direct property.
/// </remarks>
public abstract class DirectPropertyBase<TValue> : AvaloniaProperty<TValue>
{
	/// <summary>
	/// Gets the type that registered the property.
	/// </summary>
	public Type Owner { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.DirectPropertyBase`1" /> class.
	/// </summary>
	/// <param name="name">The name of the property.</param>
	/// <param name="ownerType">The type of the class that registers the property.</param>
	/// <param name="metadata">The property metadata.</param>
	private protected DirectPropertyBase(string name, Type ownerType, AvaloniaPropertyMetadata metadata)
		: base(name, ownerType, ownerType, metadata, (Action<AvaloniaObject, bool>?)null)
	{
		base.IsDirect = true;
		Owner = ownerType;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.DirectPropertyBase`1" /> class.
	/// </summary>
	/// <param name="source">The property to copy.</param>
	/// <param name="ownerType">The new owner type.</param>
	/// <param name="metadata">Optional overridden metadata.</param>
	private protected DirectPropertyBase(DirectPropertyBase<TValue> source, Type ownerType, AvaloniaPropertyMetadata metadata)
		: base((AvaloniaProperty<TValue>)source, ownerType, metadata)
	{
		base.IsDirect = true;
		Owner = ownerType;
	}

	/// <summary>
	/// Gets the value of the property on the instance.
	/// </summary>
	/// <param name="instance">The instance.</param>
	/// <returns>The property value.</returns>
	internal abstract TValue InvokeGetter(AvaloniaObject instance);

	/// <summary>
	/// Sets the value of the property on the instance.
	/// </summary>
	/// <param name="instance">The instance.</param>
	/// <param name="value">The value.</param>
	internal abstract void InvokeSetter(AvaloniaObject instance, BindingValue<TValue> value);

	/// <summary>
	/// Gets the unset value for the property on the specified type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>The unset value.</returns>
	public TValue GetUnsetValue(Type type)
	{
		return GetMetadata(type).UnsetValue;
	}

	/// <summary>
	/// Gets the unset value for the property on the specified object.
	/// </summary>
	/// <param name="owner">The object.</param>
	/// <returns>The unset value.</returns>
	public TValue GetUnsetValue(AvaloniaObject owner)
	{
		return GetMetadata(owner).UnsetValue;
	}

	/// <inheritdoc cref="M:Avalonia.AvaloniaProperty.GetMetadata(System.Type)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new DirectPropertyMetadata<TValue> GetMetadata(Type type)
	{
		return CastMetadata(base.GetMetadata(type));
	}

	/// <inheritdoc cref="M:Avalonia.AvaloniaProperty.GetMetadata(Avalonia.AvaloniaObject)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new DirectPropertyMetadata<TValue> GetMetadata(AvaloniaObject owner)
	{
		return CastMetadata(base.GetMetadata(owner));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static DirectPropertyMetadata<TValue> CastMetadata(AvaloniaPropertyMetadata metadata)
	{
		return Unsafe.As<DirectPropertyMetadata<TValue>>(metadata);
	}

	/// <summary>
	/// Overrides the metadata for the property on the specified type.
	/// </summary>
	/// <typeparam name="T">The type.</typeparam>
	/// <param name="metadata">The metadata.</param>
	public void OverrideMetadata<T>(DirectPropertyMetadata<TValue> metadata) where T : AvaloniaObject
	{
		OverrideMetadata(typeof(T), (AvaloniaPropertyMetadata)metadata);
	}

	/// <summary>
	/// Overrides the metadata for the property on the specified type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <param name="metadata">The metadata.</param>
	public void OverrideMetadata(Type type, DirectPropertyMetadata<TValue> metadata)
	{
		OverrideMetadata(type, (AvaloniaPropertyMetadata)metadata);
	}

	internal override EffectiveValue CreateEffectiveValue(AvaloniaObject o)
	{
		throw new InvalidOperationException("Cannot create EffectiveValue for direct property.");
	}

	/// <inheritdoc />
	internal override void RouteClearValue(AvaloniaObject o)
	{
		o.ClearValue(this);
	}

	internal override void RouteCoerceDefaultValue(AvaloniaObject o)
	{
	}

	/// <inheritdoc />
	internal override object? RouteGetValue(AvaloniaObject o)
	{
		return o.GetValue(this);
	}

	internal override object? RouteGetBaseValue(AvaloniaObject o)
	{
		return o.GetValue(this);
	}

	/// <inheritdoc />
	internal override IDisposable? RouteSetValue(AvaloniaObject o, object? value, BindingPriority priority)
	{
		BindingValue<object> bindingValue = TryConvert(value);
		if (bindingValue.HasValue)
		{
			o.SetValue(this, (TValue)bindingValue.Value);
		}
		else if (bindingValue.Type == BindingValueType.UnsetValue)
		{
			o.ClearValue(this);
		}
		else if (bindingValue.HasError)
		{
			throw bindingValue.Error;
		}
		return null;
	}

	internal override void RouteSetDirectValueUnchecked(AvaloniaObject o, object? value)
	{
		BindingValue<TValue> value2 = BindingValue<TValue>.FromUntypedStrict(value);
		o.SetDirectValueUnchecked(this, value2);
	}

	internal override void RouteSetCurrentValue(AvaloniaObject o, object? value)
	{
		RouteSetValue(o, value, BindingPriority.LocalValue);
	}

	/// <summary>
	/// Routes an untyped Bind call to a typed call.
	/// </summary>
	/// <param name="o">The object instance.</param>
	/// <param name="source">The binding source.</param>
	/// <param name="priority">The priority.</param>
	internal override IDisposable RouteBind(AvaloniaObject o, IObservable<object?> source, BindingPriority priority)
	{
		return o.Bind(this, source);
	}
}
