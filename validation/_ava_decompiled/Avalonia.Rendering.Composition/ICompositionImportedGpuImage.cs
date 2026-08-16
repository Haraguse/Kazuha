using System;
using Avalonia.Metadata;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// An imported GPU image object that's usable by composition APIs 
/// </summary>
[NotClientImplementable]
public interface ICompositionImportedGpuImage : ICompositionGpuImportedObject, IAsyncDisposable
{
}
