using System;
using Avalonia.Metadata;

namespace Avalonia.Platform;

[Unstable]
public interface IPlatformRenderInterfaceImportedImage : IPlatformRenderInterfaceImportedObject, IDisposable
{
	IBitmapImpl SnapshotWithKeyedMutex(uint acquireIndex, uint releaseIndex);

	IBitmapImpl SnapshotWithSemaphores(IPlatformRenderInterfaceImportedSemaphore waitForSemaphore, IPlatformRenderInterfaceImportedSemaphore signalSemaphore);

	IBitmapImpl SnapshotWithTimelineSemaphores(IPlatformRenderInterfaceImportedSemaphore waitForSemaphore, ulong waitForValue, IPlatformRenderInterfaceImportedSemaphore signalSemaphore, ulong signalValue);

	IBitmapImpl SnapshotWithAutomaticSync();
}
