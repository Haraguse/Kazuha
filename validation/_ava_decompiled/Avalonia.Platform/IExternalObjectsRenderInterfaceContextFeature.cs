using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition;

namespace Avalonia.Platform;

[Unstable]
public interface IExternalObjectsRenderInterfaceContextFeature
{
	/// <summary>
	/// Returns the list of image handle types supported by the current GPU backend, see <see cref="T:Avalonia.Platform.KnownPlatformGraphicsExternalImageHandleTypes" />
	/// </summary>
	IReadOnlyList<string> SupportedImageHandleTypes { get; }

	/// <summary>
	/// Returns the list of semaphore types supported by the current GPU backend, see <see cref="T:Avalonia.Platform.KnownPlatformGraphicsExternalSemaphoreHandleTypes" />
	/// </summary>
	IReadOnlyList<string> SupportedSemaphoreTypes { get; }

	byte[]? DeviceUuid { get; }

	byte[]? DeviceLuid { get; }

	IPlatformRenderInterfaceImportedImage ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties);

	IPlatformRenderInterfaceImportedImage ImportImage(ICompositionImportableSharedGpuContextImage image);

	IPlatformRenderInterfaceImportedSemaphore ImportSemaphore(IPlatformHandle handle);

	CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType);
}
