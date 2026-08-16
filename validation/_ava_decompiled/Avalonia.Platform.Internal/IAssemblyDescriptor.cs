using System.Collections.Generic;
using System.Reflection;

namespace Avalonia.Platform.Internal;

internal interface IAssemblyDescriptor
{
	Assembly Assembly { get; }

	Dictionary<string, IAssetDescriptor>? Resources { get; }

	Dictionary<string, IAssetDescriptor>? AvaloniaResources { get; }

	string? Name { get; }
}
