using System;

namespace Avalonia.Data.Core;

public class ClrPropertyInfo : IPropertyInfo
{
	private readonly Func<object, object?>? _getter;

	private readonly Action<object, object?>? _setter;

	public string Name { get; }

	public Type PropertyType { get; }

	public bool CanSet => _setter != null;

	public bool CanGet => _getter != null;

	public ClrPropertyInfo(string name, Func<object, object?>? getter, Action<object, object?>? setter, Type propertyType)
	{
		_getter = getter;
		_setter = setter;
		PropertyType = propertyType;
		Name = name;
	}

	public object? Get(object target)
	{
		if (_getter == null)
		{
			throw new NotSupportedException("Property " + Name + " doesn't have a getter");
		}
		return _getter(target);
	}

	public void Set(object target, object? value)
	{
		if (_setter == null)
		{
			throw new NotSupportedException("Property " + Name + " doesn't have a setter");
		}
		_setter(target, value);
	}
}
