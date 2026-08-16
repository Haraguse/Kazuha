using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public sealed class DynamicResourceExtension : BindingBase
{
	private object? _anchor;

	private BindingPriority _priority;

	private ThemeVariant? _themeVariant;

	[ConstructorArgument("resourceKey")]
	public object? ResourceKey { get; set; }

	public DynamicResourceExtension()
	{
	}

	public DynamicResourceExtension(object resourceKey)
	{
		ResourceKey = resourceKey;
	}

	public BindingBase ProvideValue(IServiceProvider serviceProvider)
	{
		if (serviceProvider.IsInControlTemplate())
		{
			_priority = BindingPriority.Template;
		}
		if (!(serviceProvider.GetService<IProvideValueTarget>()?.TargetObject is StyledElement))
		{
			_anchor = serviceProvider.GetFirstParent<StyledElement>() ?? ((object)serviceProvider.GetFirstParent<IResourceProvider>()) ?? ((object)serviceProvider.GetFirstParent<IResourceHost>());
		}
		_themeVariant = StaticResourceExtension.GetDictionaryVariant(serviceProvider.GetService<IAvaloniaXamlIlParentStackProvider>());
		return this;
	}

	internal override BindingExpressionBase CreateInstance(AvaloniaObject target, AvaloniaProperty? targetProperty, object? anchor)
	{
		if (ResourceKey == null)
		{
			throw new InvalidOperationException("DynamicResource must have a ResourceKey.");
		}
		return new DynamicResourceExpression(ResourceKey, _anchor, _themeVariant, _priority);
	}
}
