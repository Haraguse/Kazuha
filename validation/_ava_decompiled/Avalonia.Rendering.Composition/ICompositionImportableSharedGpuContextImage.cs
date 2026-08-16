using System;
using Avalonia.Metadata;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// An GPU image descriptor obtained from a context from the same share group as one used by the compositor
/// </summary>
[NotClientImplementable]
public interface ICompositionImportableSharedGpuContextImage : IDisposable
{
}
