using System;

namespace Avalonia.Rendering.Composition;

[Flags]
public enum CompositionGpuImportedImageSynchronizationCapabilities
{
	/// <summary>
	/// Pre-render and after-render semaphores must be provided alongside with the image
	/// </summary>
	Semaphores = 1,
	/// <summary>
	/// Image must be created with D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX or in other compatible way
	/// </summary>
	KeyedMutex = 2,
	/// <summary>
	/// Synchronization and ordering is somehow handled by the underlying platform
	/// </summary>
	Automatic = 4,
	/// <summary>
	/// Pre-render and after-render timeline semaphores must be provided alongside with the image
	/// </summary>
	TimelineSemaphores = 8
}
