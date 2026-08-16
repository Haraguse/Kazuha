using System;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// An imported GPU object that's usable by composition APIs 
/// </summary>
[NotClientImplementable]
public interface ICompositionGpuImportedObject : IAsyncDisposable
{
	/// <summary>
	/// Tracks the import status of the object. Once the task is completed,
	/// the user code is allowed to free the resource owner in case when a non-owning
	/// sharing handle was used.
	/// </summary>
	Task ImportCompleted { get; }

	/// <summary>
	/// Indicates if the device context this instance is associated with is no longer available
	/// </summary>
	bool IsLost { get; }
}
