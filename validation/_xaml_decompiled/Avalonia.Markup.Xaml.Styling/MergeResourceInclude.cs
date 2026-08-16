using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Markup.Xaml.Styling;

/// <summary>
/// Loads a resource dictionary from a specified URL.
/// </summary>
/// <remarks>
/// If used from the XAML code, it is merged into the parent dictionary in the compile time. 
/// When used in runtime, this type behaves like <see cref="T:Avalonia.Markup.Xaml.Styling.ResourceInclude" />.  
/// </remarks>
[RequiresUnreferencedCode("StyleInclude and ResourceInclude use AvaloniaXamlLoader.Load which dynamically loads referenced assembly with Avalonia resources. Note, StyleInclude and ResourceInclude defined in XAML are resolved compile time and are safe with trimming and AOT.")]
public class MergeResourceInclude : ResourceInclude
{
	public MergeResourceInclude(Uri? baseUri)
		: base(baseUri)
	{
	}

	public MergeResourceInclude(IServiceProvider serviceProvider)
		: base(serviceProvider)
	{
	}
}
