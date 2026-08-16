using System;
using Avalonia.Metadata;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// An imported GPU semaphore object that's usable by composition APIs 
/// </summary>
[NotClientImplementable]
public interface ICompositionImportedGpuSemaphore : ICompositionGpuImportedObject, IAsyncDisposable
{
}
