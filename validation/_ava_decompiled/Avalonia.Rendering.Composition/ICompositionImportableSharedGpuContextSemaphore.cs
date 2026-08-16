using System;
using Avalonia.Metadata;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// An GPU semaphore descriptor obtained from a context from the same share group as one used by the compositor
/// </summary>
[NotClientImplementable]
public interface ICompositionImportableSharedGpuContextSemaphore : IDisposable
{
}
