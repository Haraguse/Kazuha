using System;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data;

internal class PropertyElement : ICompiledBindingPathElement
{
	private readonly bool _isFirstElement;

	public IPropertyInfo Property { get; }

	public Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> AccessorFactory { get; }

	public bool AcceptsNull { get; }

	public PropertyElement(IPropertyInfo property, Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> accessorFactory, bool isFirstElement, bool acceptsNull)
	{
		Property = property;
		AccessorFactory = accessorFactory;
		_isFirstElement = isFirstElement;
		AcceptsNull = acceptsNull;
	}

	public override string ToString()
	{
		if (!_isFirstElement)
		{
			return "." + Property.Name;
		}
		return Property.Name;
	}
}
