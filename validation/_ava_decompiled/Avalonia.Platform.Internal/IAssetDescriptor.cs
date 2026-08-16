using System.IO;
using System.Reflection;

namespace Avalonia.Platform.Internal;

internal interface IAssetDescriptor
{
	Assembly Assembly { get; }

	Stream GetStream();
}
