using System;
using Avalonia.Data;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public sealed class CompiledBindingExtension : CompiledBinding
{
	public Type? DataType { get; set; }

	public CompiledBindingExtension()
	{
	}

	public CompiledBindingExtension(CompiledBindingPath path)
	{
		base.Path = path;
	}

	public CompiledBinding ProvideValue(IServiceProvider? provider)
	{
		return new CompiledBinding
		{
			Path = base.Path,
			Delay = base.Delay,
			Converter = base.Converter,
			ConverterCulture = base.ConverterCulture,
			ConverterParameter = base.ConverterParameter,
			TargetNullValue = base.TargetNullValue,
			FallbackValue = base.FallbackValue,
			Mode = base.Mode,
			Priority = base.Priority,
			StringFormat = base.StringFormat,
			Source = base.Source,
			DefaultAnchor = new WeakReference(provider?.GetDefaultAnchor()),
			UpdateSourceTrigger = base.UpdateSourceTrigger
		};
	}
}
