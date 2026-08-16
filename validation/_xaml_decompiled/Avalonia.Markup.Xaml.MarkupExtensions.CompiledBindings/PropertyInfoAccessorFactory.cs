using System;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

public static class PropertyInfoAccessorFactory
{
	public static IPropertyAccessor CreateInpcPropertyAccessor(WeakReference<object?> target, IPropertyInfo property)
	{
		return new InpcPropertyAccessor(target, property);
	}

	public static IPropertyAccessor CreateAvaloniaPropertyAccessor(WeakReference<object?> target, IPropertyInfo property)
	{
		object target2;
		return new AvaloniaPropertyAccessor(new WeakReference<AvaloniaObject>((AvaloniaObject)(target.TryGetTarget(out target2) ? target2 : null)), (AvaloniaProperty)property);
	}

	public static IPropertyAccessor CreateIndexerPropertyAccessor(WeakReference<object?> target, IPropertyInfo property, int argument)
	{
		return new IndexerAccessor(target, property, argument);
	}
}
