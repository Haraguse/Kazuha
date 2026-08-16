using System;
using Avalonia.Data;
using Avalonia.PropertyStore;

namespace Avalonia.Styling;

internal class PropertySetterTemplateInstance : IValueEntry, ISetterInstance
{
	private readonly ITemplate _template;

	private object? _value;

	public AvaloniaProperty Property { get; }

	public PropertySetterTemplateInstance(AvaloniaProperty property, ITemplate template)
	{
		_template = template;
		Property = property;
	}

	public bool HasValue()
	{
		return true;
	}

	public object? GetValue()
	{
		return _value ?? (_value = _template.Build());
	}

	bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error)
	{
		state = BindingValueType.Value;
		error = null;
		return false;
	}

	void IValueEntry.Unsubscribe()
	{
	}
}
