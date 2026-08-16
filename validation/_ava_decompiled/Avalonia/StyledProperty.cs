using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Avalonia.Utilities;

namespace Avalonia;

/// <summary>
/// A styled avalonia property.
/// </summary>
public class StyledProperty<TValue> : AvaloniaProperty<TValue>, IStyledPropertyAccessor
{
	private Optional<TValue> _singleDefaultValue;

	/// <summary>
	/// A method which returns "false" for values that are never valid for this property.
	/// </summary>
	public Func<TValue, bool>? ValidateValue { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.StyledProperty`1" /> class.
	/// </summary>
	/// <param name="name">The name of the property.</param>
	/// <param name="ownerType">The type of the class that registers the property.</param>
	/// <param name="hostType">The class that the property being is registered on.</param>
	/// <param name="metadata">The property metadata.</param>
	/// <param name="inherits">Whether the property inherits its value.</param>
	/// <param name="validate">
	/// <para>A method which returns "false" for values that are never valid for this property.</para>
	/// <para>This method is not part of the property's metadata and so cannot be changed after registration.</para>
	/// </param>
	/// <param name="notifying">A <see cref="P:Avalonia.AvaloniaProperty.Notifying" /> callback.</param>
	internal StyledProperty(string name, Type ownerType, Type hostType, StyledPropertyMetadata<TValue> metadata, bool inherits = false, Func<TValue, bool>? validate = null, Action<AvaloniaObject, bool>? notifying = null)
		: base(name, ownerType, hostType, (AvaloniaPropertyMetadata)metadata, notifying)
	{
		base.Inherits = inherits;
		ValidateValue = validate;
		if (validate != null && !validate(metadata.DefaultValue))
		{
			StyledPropertyNonGenericHelper.ThrowInvalidDefaultValue(name, metadata.DefaultValue, name);
		}
		_singleDefaultValue = metadata.DefaultValue;
	}

	/// <summary>
	/// Registers the property on another type.
	/// </summary>
	/// <typeparam name="TOwner">The type of the additional owner.</typeparam>
	/// <returns>The property.</returns>        
	public StyledProperty<TValue> AddOwner<TOwner>(StyledPropertyMetadata<TValue>? metadata = null) where TOwner : AvaloniaObject
	{
		AvaloniaPropertyRegistry.Instance.Register(typeof(TOwner), this);
		if (metadata != null)
		{
			OverrideMetadata<TOwner>(metadata);
		}
		return this;
	}

	public TValue CoerceValue(AvaloniaObject instance, TValue baseValue)
	{
		StyledPropertyMetadata<TValue> metadata = GetMetadata(instance);
		if (metadata.CoerceValue != null)
		{
			return metadata.CoerceValue(instance, baseValue);
		}
		return baseValue;
	}

	/// <summary>
	/// Gets the default value for the property on the specified type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>The default value.</returns>
	/// <remarks>
	/// For performance, prefer the <see cref="M:Avalonia.StyledProperty`1.GetDefaultValue(Avalonia.AvaloniaObject)" /> overload when possible.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TValue GetDefaultValue(Type type)
	{
		if (!_singleDefaultValue.HasValue)
		{
			return GetMetadata(type).DefaultValue;
		}
		return _singleDefaultValue.GetValueOrDefault();
	}

	/// <summary>
	/// Gets the default value for the property on the specified object.
	/// </summary>
	/// <param name="owner">The object.</param>
	/// <returns>The default value.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TValue GetDefaultValue(AvaloniaObject owner)
	{
		if (!_singleDefaultValue.HasValue)
		{
			return GetMetadata(owner).DefaultValue;
		}
		return _singleDefaultValue.GetValueOrDefault();
	}

	/// <inheritdoc cref="M:Avalonia.AvaloniaProperty.GetMetadata(System.Type)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new StyledPropertyMetadata<TValue> GetMetadata(Type type)
	{
		return CastMetadata(base.GetMetadata(type));
	}

	/// <inheritdoc cref="M:Avalonia.AvaloniaProperty.GetMetadata(Avalonia.AvaloniaObject)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new StyledPropertyMetadata<TValue> GetMetadata(AvaloniaObject owner)
	{
		return CastMetadata(base.GetMetadata(owner));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static StyledPropertyMetadata<TValue> CastMetadata(AvaloniaPropertyMetadata metadata)
	{
		return Unsafe.As<StyledPropertyMetadata<TValue>>(metadata);
	}

	/// <summary>
	/// Overrides the default value for the property on the specified type.
	/// </summary>
	/// <typeparam name="T">The type.</typeparam>
	/// <param name="defaultValue">The default value.</param>
	public void OverrideDefaultValue<T>(TValue defaultValue) where T : AvaloniaObject
	{
		OverrideDefaultValue(typeof(T), defaultValue);
	}

	/// <summary>
	/// Overrides the default value for the property on the specified type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <param name="defaultValue">The default value.</param>
	public void OverrideDefaultValue(Type type, TValue defaultValue)
	{
		OverrideMetadata(type, new StyledPropertyMetadata<TValue>(defaultValue));
	}

	/// <summary>
	/// Overrides the metadata for the property on the specified type.
	/// </summary>
	/// <typeparam name="T">The type.</typeparam>
	/// <param name="metadata">The metadata.</param>
	public void OverrideMetadata<T>(StyledPropertyMetadata<TValue> metadata) where T : AvaloniaObject
	{
		OverrideMetadata(typeof(T), metadata);
	}

	/// <summary>
	/// Overrides the metadata for the property on the specified type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <param name="metadata">The metadata.</param>
	public void OverrideMetadata(Type type, StyledPropertyMetadata<TValue> metadata)
	{
		if (ValidateValue != null && !ValidateValue(metadata.DefaultValue))
		{
			StyledPropertyNonGenericHelper.ThrowInvalidDefaultValue(base.Name, metadata.DefaultValue, "metadata");
		}
		OverrideMetadata(type, (AvaloniaPropertyMetadata)metadata);
		if (_singleDefaultValue != metadata.DefaultValue)
		{
			_singleDefaultValue = default(Optional<TValue>);
		}
	}

	/// <summary>
	/// Gets the string representation of the property.
	/// </summary>
	/// <returns>The property's string representation.</returns>
	public override string ToString()
	{
		return base.Name;
	}

	object? IStyledPropertyAccessor.GetDefaultValue(Type type)
	{
		return GetDefaultValue(type);
	}

	object? IStyledPropertyAccessor.GetDefaultValue(AvaloniaObject owner)
	{
		return GetDefaultValue(owner);
	}

	bool IStyledPropertyAccessor.ValidateValue(object? value)
	{
		if (value == null)
		{
			if (!typeof(TValue).IsValueType || Nullable.GetUnderlyingType(typeof(TValue)) != null)
			{
				return ValidateValue?.Invoke(default(TValue)) ?? true;
			}
		}
		else if (value is TValue arg)
		{
			return ValidateValue?.Invoke(arg) ?? true;
		}
		return false;
	}

	internal override EffectiveValue CreateEffectiveValue(AvaloniaObject o)
	{
		return o.GetValueStore().CreateEffectiveValue(this);
	}

	/// <inheritdoc />
	internal override void RouteClearValue(AvaloniaObject o)
	{
		o.ClearValue(this);
	}

	internal override void RouteCoerceDefaultValue(AvaloniaObject o)
	{
		o.GetValueStore().CoerceDefaultValue(this);
	}

	/// <inheritdoc />
	internal override object? RouteGetValue(AvaloniaObject o)
	{
		return o.GetValue(this);
	}

	/// <inheritdoc />
	internal override object? RouteGetBaseValue(AvaloniaObject o)
	{
		Optional<TValue> baseValue = o.GetBaseValue(this);
		if (!baseValue.HasValue)
		{
			return AvaloniaProperty.UnsetValue;
		}
		return baseValue.Value;
	}

	/// <inheritdoc />
	internal override IDisposable? RouteSetValue(AvaloniaObject target, object? value, BindingPriority priority)
	{
		if (ShouldSetValue(target, value, out TValue converted))
		{
			return target.SetValue(this, converted, priority);
		}
		return null;
	}

	internal override void RouteSetCurrentValue(AvaloniaObject target, object? value)
	{
		if (ShouldSetValue(target, value, out TValue converted))
		{
			target.SetCurrentValue(this, converted);
		}
	}

	internal override IDisposable RouteBind(AvaloniaObject target, IObservable<object?> source, BindingPriority priority)
	{
		return target.Bind(this, source, priority);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Implicit conversion methods might be removed by the linker. We don't have a reliable way to prevent it, except converting everything in compile time when possible.")]
	private bool ShouldSetValue(AvaloniaObject target, object? value, [NotNullWhen(true)] out TValue? converted)
	{
		if (value != BindingOperations.DoNothing)
		{
			if (value == AvaloniaProperty.UnsetValue)
			{
				target.ClearValue(this);
			}
			else
			{
				if (TypeUtilities.TryConvertImplicit(base.PropertyType, value, out object result))
				{
					converted = (TValue)result;
					return true;
				}
				StyledPropertyNonGenericHelper.ThrowInvalidValue(base.Name, value, "value");
			}
		}
		converted = default(TValue);
		return false;
	}
}
