using System;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

/// <inheritdoc />
public sealed class OnFormFactorExtension : OnFormFactorExtensionBase<object, On>
{
	public OnFormFactorExtension()
	{
	}

	public OnFormFactorExtension(object defaultValue)
	{
		base.Default = defaultValue;
	}

	public static bool ShouldProvideOption(IServiceProvider serviceProvider, FormFactorType option)
	{
		IRuntimePlatform? service = serviceProvider.GetService<IRuntimePlatform>();
		if (service == null)
		{
			return false;
		}
		return service.GetRuntimeInfo().FormFactor == option;
	}
}
/// <inheritdoc />
public sealed class OnFormFactorExtension<TReturn> : OnFormFactorExtensionBase<TReturn, On<TReturn>>
{
	public OnFormFactorExtension()
	{
	}

	public OnFormFactorExtension(TReturn defaultValue)
	{
		base.Default = defaultValue;
	}

	public static bool ShouldProvideOption(IServiceProvider serviceProvider, FormFactorType option)
	{
		IRuntimePlatform? service = serviceProvider.GetService<IRuntimePlatform>();
		if (service == null)
		{
			return false;
		}
		return service.GetRuntimeInfo().FormFactor == option;
	}
}
