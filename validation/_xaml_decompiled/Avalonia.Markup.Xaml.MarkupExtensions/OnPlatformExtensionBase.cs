using System;
using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public abstract class OnPlatformExtensionBase<TReturn, TOn> : IAddChild<TOn> where TOn : On<TReturn>
{
	[MarkupExtensionDefaultOption]
	public TReturn? Default { get; set; }

	[MarkupExtensionOption("WINDOWS")]
	public TReturn? Windows { get; set; }

	[MarkupExtensionOption("OSX")]
	public TReturn? macOS { get; set; }

	[MarkupExtensionOption("LINUX")]
	public TReturn? Linux { get; set; }

	[MarkupExtensionOption("ANDROID")]
	public TReturn? Android { get; set; }

	[MarkupExtensionOption("IOS")]
	public TReturn? iOS { get; set; }

	[MarkupExtensionOption("BROWSER")]
	public TReturn? Browser { get; set; }

	public object ProvideValue()
	{
		return this;
	}

	void IAddChild<TOn>.AddChild(TOn child)
	{
	}

	private protected static bool ShouldProvideOptionInternal(string option)
	{
		return option switch
		{
			"WINDOWS" => OperatingSystem.IsWindows(), 
			"OSX" => OperatingSystem.IsMacOS(), 
			"LINUX" => OperatingSystem.IsLinux(), 
			"ANDROID" => OperatingSystem.IsAndroid(), 
			"IOS" => OperatingSystem.IsIOS(), 
			"BROWSER" => OperatingSystem.IsBrowser(), 
			_ => OperatingSystem.IsOSPlatform(option), 
		};
	}
}
