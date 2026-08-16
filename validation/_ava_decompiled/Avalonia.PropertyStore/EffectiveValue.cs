using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;

namespace Avalonia.PropertyStore;

/// <summary>
/// Represents the active value for a property in a <see cref="T:Avalonia.PropertyStore.ValueStore" />.
/// </summary>
/// <remarks>
/// This class is an abstract base for the generic <see cref="T:Avalonia.PropertyStore.EffectiveValue`1" />.
/// </remarks>
internal abstract class EffectiveValue
{
	/// <summary>
	/// Gets the property targeted by this value.
	/// </summary>
	public AvaloniaProperty Property { get; protected init; }

	/// <summary>
	/// Gets the current effective value as a boxed value.
	/// </summary>
	public object? Value => GetBoxedValue();

	/// <summary>
	/// Gets the priority of the current effective value.
	/// </summary>
	public BindingPriority Priority { get; protected set; }

	/// <summary>
	/// Gets the priority of the current base value.
	/// </summary>
	public BindingPriority BasePriority { get; protected set; }

	/// <summary>
	/// Gets the active value entry for the current effective value.
	/// </summary>
	public IValueEntry? ValueEntry { get; private set; }

	/// <summary>
	/// Gets the active value entry for the current base value.
	/// </summary>
	public IValueEntry? BaseValueEntry { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the property has a coercion function.
	/// </summary>
	public bool HasCoercion { get; protected set; }

	/// <summary>
	/// Gets a value indicating whether the <see cref="P:Avalonia.PropertyStore.EffectiveValue.Value" /> was overridden by a call to 
	/// <see cref="M:Avalonia.AvaloniaObject.SetCurrentValue``1(Avalonia.StyledProperty{``0},``0)" />.
	/// </summary>
	public bool IsOverridenCurrentValue { get; set; }

	/// <summary>
	/// Gets a value indicating whether the <see cref="P:Avalonia.PropertyStore.EffectiveValue.Value" /> is the result of the 
	///
	/// </summary>
	public bool IsCoercedDefaultValue { get; set; }

	/// <summary>
	/// Initializes a new instance of <see cref="T:Avalonia.PropertyStore.EffectiveValue" />.
	/// </summary>
	/// <param name="property">The property targeted by this value.</param>
	protected EffectiveValue(AvaloniaProperty property)
	{
		Property = property;
	}

	/// <summary>
	/// Begins a reevaluation pass on the effective value.
	/// </summary>
	/// <param name="clearLocalValue">
	/// Determines whether any current local value should be cleared.
	/// </param>
	/// <remarks>
	/// This method resets the <see cref="P:Avalonia.PropertyStore.EffectiveValue.Priority" /> and <see cref="P:Avalonia.PropertyStore.EffectiveValue.BasePriority" /> properties
	/// to Unset, pending reevaluation.
	/// </remarks>
	public void BeginReevaluation(bool clearLocalValue = false)
	{
		if (clearLocalValue || (Priority != BindingPriority.LocalValue && !IsOverridenCurrentValue))
		{
			Priority = BindingPriority.Unset;
		}
		if (clearLocalValue || (BasePriority != BindingPriority.LocalValue && !IsOverridenCurrentValue))
		{
			BasePriority = BindingPriority.Unset;
		}
	}

	/// <summary>
	/// Ends a reevaluation pass on the effective value.
	/// </summary>
	/// <param name="owner">The associated value store.</param>
	/// <param name="property">The property being reevaluated.</param>
	/// <remarks>
	/// Handles coercing the default value if necessary.
	/// </remarks>
	public void EndReevaluation(ValueStore owner, AvaloniaProperty property)
	{
		if (Priority == BindingPriority.Unset && HasCoercion)
		{
			CoerceDefaultValueAndRaise(owner, property);
		}
	}

	/// <summary>
	/// Gets a value indicating whether the effective value represents the default value of the
	/// property and can be removed.
	/// </summary>
	/// <returns>True if the effective value can be removed; otherwise false.</returns>
	public bool CanRemove()
	{
		if (Priority == BindingPriority.Unset && !IsOverridenCurrentValue)
		{
			return !IsCoercedDefaultValue;
		}
		return false;
	}

	/// <summary>
	/// Unsubscribes from any unused value entries.
	/// </summary>
	public void UnsubscribeIfNecessary()
	{
		if (Priority == BindingPriority.Unset)
		{
			ValueEntry?.Unsubscribe();
			ValueEntry = null;
		}
		if (BasePriority == BindingPriority.Unset)
		{
			BaseValueEntry?.Unsubscribe();
			BaseValueEntry = null;
		}
	}

	/// <summary>
	/// Sets the value and base value for a non-LocalValue priority, raising 
	/// <see cref="E:Avalonia.AvaloniaObject.PropertyChanged" /> where necessary.
	/// </summary>
	/// <param name="owner">The associated value store.</param>
	/// <param name="value">The new value of the property.</param>
	/// <param name="priority">The priority of the new value.</param>
	public abstract void SetAndRaise(ValueStore owner, IValueEntry value, BindingPriority priority);

	/// <summary>
	/// Sets the value and base value for a LocalValue priority, raising 
	/// <see cref="E:Avalonia.AvaloniaObject.PropertyChanged" /> where necessary.
	/// </summary>
	/// <param name="owner">The associated value store.</param>
	/// <param name="property">The property being changed.</param>
	/// <param name="value">The new value of the property.</param>
	public abstract void SetLocalValueAndRaise(ValueStore owner, AvaloniaProperty property, object? value);

	/// <summary>
	/// Raises <see cref="E:Avalonia.AvaloniaObject.PropertyChanged" /> in response to an inherited value
	/// change.
	/// </summary>
	/// <param name="owner">The owner object.</param>
	/// <param name="property">The property being changed.</param>
	/// <param name="oldValue">The old value of the property.</param>
	/// <param name="newValue">The new value of the property.</param>
	public abstract void RaiseInheritedValueChanged(AvaloniaObject owner, AvaloniaProperty property, EffectiveValue? oldValue, EffectiveValue? newValue);

	/// <summary>
	/// Removes the current animation value and reverts to the base value, raising
	/// <see cref="E:Avalonia.AvaloniaObject.PropertyChanged" /> where necessary.
	/// </summary>
	/// <param name="owner">The associated value store.</param>
	/// <param name="property">The property being changed.</param>
	public abstract void RemoveAnimationAndRaise(ValueStore owner, AvaloniaProperty property);

	/// <summary>
	/// Coerces the property value.
	/// </summary>
	/// <param name="owner">The associated value store.</param>
	/// <param name="property">The property to coerce.</param>
	public abstract void CoerceValue(ValueStore owner, AvaloniaProperty property);

	/// <summary>
	/// Disposes the effective value, raising <see cref="E:Avalonia.AvaloniaObject.PropertyChanged" />
	/// where necessary.
	/// </summary>
	/// <param name="owner">The associated value store.</param>
	/// <param name="property">The property being cleared.</param>
	public abstract void DisposeAndRaiseUnset(ValueStore owner, AvaloniaProperty property);

	/// <summary>
	/// Coerces the default value, raising <see cref="E:Avalonia.AvaloniaObject.PropertyChanged" />
	/// where necessary.
	/// </summary>
	/// <param name="owner">The associated value store.</param>
	/// <param name="property">The property being coerced.</param>
	protected abstract void CoerceDefaultValueAndRaise(ValueStore owner, AvaloniaProperty property);

	/// <summary>
	/// Gets the current effective value as a boxed value.
	/// </summary>
	protected abstract object? GetBoxedValue();

	protected void UpdateValueEntry(IValueEntry? entry, BindingPriority priority)
	{
		if (priority <= BindingPriority.Animation)
		{
			if (Priority > BindingPriority.LocalValue && Priority < BindingPriority.Inherited)
			{
				BaseValueEntry = ValueEntry;
				ValueEntry = null;
			}
			if (ValueEntry != entry)
			{
				ValueEntry?.Unsubscribe();
				ValueEntry = entry;
			}
		}
		else if (Priority <= BindingPriority.Animation)
		{
			if (BaseValueEntry != entry)
			{
				BaseValueEntry?.Unsubscribe();
				BaseValueEntry = entry;
			}
		}
		else if (ValueEntry != entry)
		{
			ValueEntry?.Unsubscribe();
			ValueEntry = entry;
		}
	}
}
/// <summary>
/// Represents the active value for a property in a <see cref="T:Avalonia.PropertyStore.ValueStore" />.
/// </summary>
/// <remarks>
/// Stores the active value in an <see cref="T:Avalonia.AvaloniaObject" />'s <see cref="T:Avalonia.PropertyStore.ValueStore" />
/// for a single property, when the value is not inherited or unset/default.
/// </remarks>
internal sealed class EffectiveValue<T> : EffectiveValue
{
	private sealed class UncommonFields
	{
		public Func<AvaloniaObject, T, T>? _coerce;

		public T? _uncoercedValue;

		public T? _uncoercedBaseValue;
	}

	private readonly StyledPropertyMetadata<T> _metadata;

	private T? _baseValue;

	private readonly UncommonFields? _uncommon;

	/// <summary>
	/// Gets the current effective value.
	/// </summary>
	public new T Value { get; private set; }

	public EffectiveValue(AvaloniaObject owner, StyledProperty<T> property, EffectiveValue<T>? inherited)
		: base(property)
	{
		base.Priority = BindingPriority.Unset;
		base.BasePriority = BindingPriority.Unset;
		_metadata = property.GetMetadata(owner);
		T val = (T)((inherited == null) ? ((object)_metadata.DefaultValue) : ((object)inherited.Value));
		Func<AvaloniaObject, T, T> coerceValue = _metadata.CoerceValue;
		if (coerceValue != null)
		{
			base.HasCoercion = true;
			_uncommon = new UncommonFields
			{
				_coerce = coerceValue,
				_uncoercedValue = val,
				_uncoercedBaseValue = val
			};
		}
		Value = val;
	}

	public override void SetAndRaise(ValueStore owner, IValueEntry value, BindingPriority priority)
	{
		UpdateValueEntry(value, priority);
		SetAndRaiseCore(owner, (StyledProperty<T>)value.Property, GetValue(value), priority);
		if (priority > BindingPriority.LocalValue && value.GetDataValidationState(out BindingValueType state, out Exception error))
		{
			owner.Owner.OnUpdateDataValidation(value.Property, state, error);
		}
	}

	public override void SetLocalValueAndRaise(ValueStore owner, AvaloniaProperty property, object? value)
	{
		SetLocalValueAndRaise(owner, (StyledProperty<T>)property, (T)value);
	}

	public void SetLocalValueAndRaise(ValueStore owner, StyledProperty<T> property, T value)
	{
		SetAndRaiseCore(owner, property, value, BindingPriority.LocalValue);
	}

	public void SetCurrentValueAndRaise(ValueStore owner, StyledProperty<T> property, T value)
	{
		SetAndRaiseCore(owner, property, value, base.Priority, isOverriddenCurrentValue: true);
	}

	public void SetCoercedDefaultValueAndRaise(ValueStore owner, StyledProperty<T> property, T value)
	{
		SetAndRaiseCore(owner, property, value, base.Priority, isOverriddenCurrentValue: false, isCoercedDefaultValue: true);
	}

	public bool TryGetBaseValue([MaybeNullWhen(false)] out T value)
	{
		value = _baseValue;
		return base.BasePriority != BindingPriority.Unset;
	}

	public override void RaiseInheritedValueChanged(AvaloniaObject owner, AvaloniaProperty property, EffectiveValue? oldValue, EffectiveValue? newValue)
	{
		StyledProperty<T> property2 = (StyledProperty<T>)property;
		T val = ((oldValue != null) ? ((EffectiveValue<T>)oldValue).Value : _metadata.DefaultValue);
		T val2 = ((newValue != null) ? ((EffectiveValue<T>)newValue).Value : _metadata.DefaultValue);
		BindingPriority priority = ((newValue != null) ? BindingPriority.Inherited : BindingPriority.Unset);
		if (!EqualityComparer<T>.Default.Equals(val, val2))
		{
			owner.RaisePropertyChanged(property2, val, val2, priority, isEffectiveValue: true);
		}
	}

	public override void RemoveAnimationAndRaise(ValueStore owner, AvaloniaProperty property)
	{
		UpdateValueEntry(null, BindingPriority.Animation);
		SetAndRaiseCore(owner, (StyledProperty<T>)property, _baseValue, base.BasePriority);
	}

	public override void CoerceValue(ValueStore owner, AvaloniaProperty property)
	{
		if (_uncommon != null)
		{
			SetAndRaiseCore(owner, (StyledProperty<T>)property, _uncommon._uncoercedValue, base.Priority, _uncommon._uncoercedBaseValue, base.BasePriority);
		}
	}

	public override void DisposeAndRaiseUnset(ValueStore owner, AvaloniaProperty property)
	{
		bool num = base.ValueEntry?.GetDataValidationState(out BindingValueType state, out Exception error) ?? base.BaseValueEntry?.GetDataValidationState(out state, out error) ?? false;
		base.ValueEntry?.Unsubscribe();
		base.BaseValueEntry?.Unsubscribe();
		StyledProperty<T> property2 = (StyledProperty<T>)property;
		T val;
		BindingPriority priority;
		if (property.Inherits && owner.TryGetInheritedValue(property, out EffectiveValue result))
		{
			val = ((EffectiveValue<T>)result).Value;
			priority = BindingPriority.Inherited;
		}
		else
		{
			val = _metadata.DefaultValue;
			priority = BindingPriority.Unset;
		}
		if (!EqualityComparer<T>.Default.Equals(val, Value))
		{
			owner.Owner.RaisePropertyChanged(property2, Value, val, priority, isEffectiveValue: true);
			if (property.Inherits)
			{
				owner.OnInheritedEffectiveValueDisposed(property2, Value, val);
			}
		}
		if (num)
		{
			owner.Owner.OnUpdateDataValidation(property2, BindingValueType.UnsetValue, null);
		}
	}

	protected override void CoerceDefaultValueAndRaise(ValueStore owner, AvaloniaProperty property)
	{
		T val = _uncommon._coerce(owner.Owner, _metadata.DefaultValue);
		if (!EqualityComparer<T>.Default.Equals(_metadata.DefaultValue, val))
		{
			SetCoercedDefaultValueAndRaise(owner, (StyledProperty<T>)property, val);
		}
	}

	protected override object? GetBoxedValue()
	{
		return Value;
	}

	private static T GetValue(IValueEntry entry)
	{
		if (entry is IValueEntry<T> valueEntry)
		{
			return valueEntry.GetValue();
		}
		return (T)entry.GetValue();
	}

	private void SetAndRaiseCore(ValueStore owner, StyledProperty<T> property, T value, BindingPriority priority, bool isOverriddenCurrentValue = false, bool isCoercedDefaultValue = false)
	{
		T value2 = Value;
		bool flag = false;
		bool flag2 = false;
		T val = value;
		base.IsOverridenCurrentValue = isOverriddenCurrentValue;
		base.IsCoercedDefaultValue = isCoercedDefaultValue;
		if (!isCoercedDefaultValue)
		{
			Func<AvaloniaObject, T, T> func = _uncommon?._coerce;
			if (func != null)
			{
				val = func(owner.Owner, value);
			}
		}
		if (priority <= base.Priority)
		{
			flag = !EqualityComparer<T>.Default.Equals(Value, val);
			Value = val;
			base.Priority = priority;
			if (!isCoercedDefaultValue && _uncommon != null)
			{
				_uncommon._uncoercedValue = value;
			}
		}
		if (priority <= base.BasePriority && priority >= BindingPriority.LocalValue)
		{
			flag2 = !EqualityComparer<T>.Default.Equals(_baseValue, val);
			_baseValue = val;
			base.BasePriority = priority;
			if (!isCoercedDefaultValue && _uncommon != null)
			{
				_uncommon._uncoercedBaseValue = value;
			}
		}
		if (flag)
		{
			NotifyValueChanged(owner, property, value2);
		}
		else if (flag2)
		{
			NotifyBaseValueChanged(owner, property);
		}
	}

	private void SetAndRaiseCore(ValueStore owner, StyledProperty<T> property, T value, BindingPriority priority, T baseValue, BindingPriority basePriority)
	{
		T value2 = Value;
		bool flag = false;
		bool flag2 = false;
		T val = value;
		T y = baseValue;
		Func<AvaloniaObject, T, T> func = _uncommon?._coerce;
		if (func != null)
		{
			val = func(owner.Owner, value);
			if (priority != basePriority)
			{
				y = func(owner.Owner, baseValue);
			}
		}
		if (!EqualityComparer<T>.Default.Equals(Value, val))
		{
			Value = val;
			flag = true;
			if (_uncommon != null)
			{
				_uncommon._uncoercedValue = value;
			}
		}
		if (!EqualityComparer<T>.Default.Equals(_baseValue, y))
		{
			_baseValue = val;
			flag2 = true;
			if (_uncommon != null)
			{
				_uncommon._uncoercedValue = baseValue;
			}
		}
		base.Priority = priority;
		base.BasePriority = basePriority;
		if (flag)
		{
			NotifyValueChanged(owner, property, value2);
		}
		if (flag2)
		{
			NotifyBaseValueChanged(owner, property);
		}
	}

	private void NotifyValueChanged(ValueStore owner, StyledProperty<T> property, T oldValue)
	{
		using (PropertyNotifying.Start(owner.Owner, property))
		{
			owner.Owner.RaisePropertyChanged(property, oldValue, Value, base.Priority, isEffectiveValue: true);
			if (property.Inherits)
			{
				owner.OnInheritedEffectiveValueChanged(property, oldValue, this);
			}
		}
	}

	private void NotifyBaseValueChanged(ValueStore owner, StyledProperty<T> property)
	{
		owner.Owner.RaisePropertyChanged(property, default(Optional<T>), _baseValue, base.BasePriority, isEffectiveValue: false);
	}
}
