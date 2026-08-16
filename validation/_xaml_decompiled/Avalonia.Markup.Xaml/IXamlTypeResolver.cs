using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Markup.Xaml;

public interface IXamlTypeResolver
{
	[RequiresUnreferencedCode("XamlTypeResolver might require unreferenced code.")]
	Type Resolve(string qualifiedTypeName);
}
