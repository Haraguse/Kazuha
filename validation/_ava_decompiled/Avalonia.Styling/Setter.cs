using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Animation;
using Avalonia.Data;
using Avalonia.Metadata;
using Avalonia.PropertyStore;

namespace Avalonia.Styling;

/// <summary>
/// A setter for a <see cref="T:Avalonia.Styling.Style" />.
/// </summary>
/// <remarks>
/// A <see cref="T:Avalonia.Styling.Setter" /> is used to set a <see cref="T:Avalonia.AvaloniaProperty" /> value on a
/// <see cref="T:Avalonia.AvaloniaObject" /> depending on a condition.
/// </remarks>
public class Setter : SetterBase, IValueEntry, ISetterInstance, IAnimationSetter
{
	private object? _value;

	private DirectPropertySetterInstance? _direct;

	/// <summary>
	/// Gets or sets the property to set.
	/// </summary>
	public AvaloniaProperty? Property { get; set; }

	/// <summary>
	/// Gets or sets the property value.
	/// </summary>
	[Content]
	[AssignBinding]
	[DependsOn("Property")]
	public object? Value
	{
		get
		{
			return _value;
		}
		set
		{
			(value as ISetterValue)?.Initialize(this);
			_value = value;
		}
	}

	AvaloniaProperty IValueEntry.Property => EnsureProperty();

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.Setter" /> class.
	/// </summary>
	public Setter()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.Setter" /> class.
	/// </summary>
	/// <param name="property">The property to set.</param>
	/// <param name="value">The property value.</param>
	public Setter(AvaloniaProperty property, object? value)
	{
		Property = property;
		Value = value;
	}

	public override string ToString()
	{
		return $"Setter: {Property} = {Value}";
	}

	void IValueEntry.Unsubscribe()
	{
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Implicit conversion methods might be removed by the linker. We don't have a reliable way to prevent it, except converting everything in compile time when possible.")]
	internal override ISetterInstance Instance(IStyleInstance instance, StyledElement target)
	{
		if (target == null)
		{
			throw new InvalidOperationException("Don't know how to instance a style on this type.");
		}
		if ((object)Property == null)
		{
			throw new InvalidOperationException("Setter.Property must be set.");
		}
		if (Property.IsDirect && instance.HasActivator)
		{
			throw new InvalidOperationException($"Cannot set direct property '{Property}' in '{instance.Source}' because the style has an activator.");
		}
		if (Property.IsClassesBindingProperty(out string classPropertyName) && instance.HasActivator)
		{
			throw new InvalidOperationException($"Cannot set Class Binding property '(Classes.{classPropertyName})' in '{instance.Source}' because the style has an activator.");
		}
		if (Value is BindingBase binding)
		{
			return SetBinding((StyleInstance)instance, target, binding);
		}
		if (Value is ITemplate template && !typeof(ITemplate).IsAssignableFrom(Property.PropertyType))
		{
			return new PropertySetterTemplateInstance(Property, template);
		}
		if (!Property.IsValidValue(Value))
		{
			throw new InvalidCastException($"Setter value '{Value}' is not a valid value for property '{Property}'.");
		}
		if (Property.IsDirect)
		{
			return SetDirectValue(target);
		}
		return this;
	}

	bool IValueEntry.HasValue()
	{
		return true;
	}

	object? IValueEntry.GetValue()
	{
		return Value;
	}

	bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error)
	{
		state = BindingValueType.Value;
		error = null;
		return false;
	}

	private AvaloniaProperty EnsureProperty()
	{
		return Property ?? throw new InvalidOperationException("Setter.Property must be set.");
	}

	private ISetterInstance SetBinding(StyleInstance instance, AvaloniaObject target, BindingBase binding)
	{
		if (!Property.IsDirect)
		{
			BindingExpressionBase bindingExpressionBase = binding.CreateInstance(target, Property, null);
			bindingExpressionBase.Attach(target.GetValueStore(), null, target, Property, instance.Priority);
			return bindingExpressionBase;
		}
		target.Bind(Property, binding);
		return new DirectPropertySetterBindingInstance();
	}

	private ISetterInstance SetDirectValue(StyledElement target)
	{
		target.SetValue(Property, Value);
		return _direct ?? (_direct = new DirectPropertySetterInstance());
	}
}
