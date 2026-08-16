using System;

namespace Avalonia.Data.Core.Plugins;

internal class PropertyInfoAccessorPlugin : IPropertyAccessorPlugin
{
	private readonly IPropertyInfo _propertyInfo;

	private readonly Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> _accessorFactory;

	public PropertyInfoAccessorPlugin(IPropertyInfo propertyInfo, Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> accessorFactory)
	{
		_propertyInfo = propertyInfo;
		_accessorFactory = accessorFactory;
	}

	public bool Match(object obj, string propertyName)
	{
		throw new InvalidOperationException("The PropertyInfoAccessorPlugin does not support dynamic matching");
	}

	public IPropertyAccessor Start(WeakReference<object?> reference, string propertyName)
	{
		return _accessorFactory(reference, _propertyInfo);
	}
}
