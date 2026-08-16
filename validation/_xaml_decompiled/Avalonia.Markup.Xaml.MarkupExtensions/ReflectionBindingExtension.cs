using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Data;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

[RequiresUnreferencedCode("BindingExpression and ReflectionBinding heavily use reflection. Consider using CompiledBindings instead.")]
[RequiresDynamicCode("BindingExpression and ReflectionBinding require dynamic code. Consider using CompiledBindings instead.")]
public sealed class ReflectionBindingExtension : ReflectionBinding
{
	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.ReflectionBinding" /> class.
	/// </summary>
	public ReflectionBindingExtension()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.ReflectionBinding" /> class.
	/// </summary>
	/// <param name="path">The binding path.</param>
	public ReflectionBindingExtension(string path)
		: base(path)
	{
	}

	public ReflectionBinding ProvideValue(IServiceProvider serviceProvider)
	{
		return new ReflectionBinding
		{
			TypeResolver = serviceProvider.ResolveType,
			Converter = base.Converter,
			ConverterCulture = base.ConverterCulture,
			ConverterParameter = base.ConverterParameter,
			ElementName = base.ElementName,
			FallbackValue = base.FallbackValue,
			Mode = base.Mode,
			Path = base.Path,
			Priority = base.Priority,
			Delay = base.Delay,
			Source = base.Source,
			StringFormat = base.StringFormat,
			RelativeSource = base.RelativeSource,
			DefaultAnchor = new WeakReference(serviceProvider.GetDefaultAnchor()),
			TargetNullValue = base.TargetNullValue,
			NameScope = new WeakReference<INameScope>(serviceProvider.GetService<INameScope>()),
			UpdateSourceTrigger = base.UpdateSourceTrigger
		};
	}
}
