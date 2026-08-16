namespace Avalonia.Markup.Xaml.MarkupExtensions;

public sealed class OnPlatformExtension : OnPlatformExtensionBase<object, On>
{
	public OnPlatformExtension()
	{
	}

	public OnPlatformExtension(object defaultValue)
	{
		base.Default = defaultValue;
	}

	public static bool ShouldProvideOption(string option)
	{
		return OnPlatformExtensionBase<object, On>.ShouldProvideOptionInternal(option);
	}
}
public sealed class OnPlatformExtension<TReturn> : OnPlatformExtensionBase<TReturn, On<TReturn>>
{
	public OnPlatformExtension()
	{
	}

	public OnPlatformExtension(TReturn defaultValue)
	{
		base.Default = defaultValue;
	}

	public static bool ShouldProvideOption(string option)
	{
		return OnPlatformExtensionBase<TReturn, On<TReturn>>.ShouldProvideOptionInternal(option);
	}
}
