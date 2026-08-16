using System;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionDrawingSurface : ServerCompositionSurface, IDisposable
{
	private IRef<IBitmapImpl>? _bitmap;

	private IPlatformRenderInterfaceContext? _createdWithContext;

	public override IRef<IBitmapImpl>? Bitmap
	{
		get
		{
			if (base.Compositor.RenderInterface.Value != _createdWithContext)
			{
				return null;
			}
			return _bitmap;
		}
	}

	public ServerCompositionDrawingSurface(ServerCompositor compositor)
		: base(compositor)
	{
	}

	private void PerformSanityChecks(CompositionImportedGpuImage image)
	{
		if (!image.IsUsable)
		{
			throw new PlatformGraphicsContextLostException();
		}
		if (!image.ImportCompleted.IsCompleted)
		{
			throw new InvalidOperationException("The import operation is not completed yet");
		}
		if (image.ImportCompleted.IsFaulted)
		{
			image.ImportCompleted.GetAwaiter().GetResult();
		}
	}

	private void Update(IBitmapImpl newImage, IPlatformRenderInterfaceContext context)
	{
		_bitmap?.Dispose();
		_bitmap = RefCountable.Create(newImage);
		_createdWithContext = context;
		base.Changed?.Invoke();
	}

	public void UpdateWithAutomaticSync(CompositionImportedGpuImage image)
	{
		using (base.Compositor.RenderInterface.EnsureCurrent())
		{
			PerformSanityChecks(image);
			Update(image.Image.SnapshotWithAutomaticSync(), image.Context);
		}
	}

	public void UpdateWithKeyedMutex(CompositionImportedGpuImage image, uint acquireIndex, uint releaseIndex)
	{
		using (base.Compositor.RenderInterface.EnsureCurrent())
		{
			PerformSanityChecks(image);
			Update(image.Image.SnapshotWithKeyedMutex(acquireIndex, releaseIndex), image.Context);
		}
	}

	public void UpdateWithSemaphores(CompositionImportedGpuImage image, CompositionImportedGpuSemaphore wait, CompositionImportedGpuSemaphore signal)
	{
		using (base.Compositor.RenderInterface.EnsureCurrent())
		{
			PerformSanityChecks(image);
			if (!wait.IsUsable || !signal.IsUsable)
			{
				throw new PlatformGraphicsContextLostException();
			}
			Update(image.Image.SnapshotWithSemaphores(wait.Semaphore, signal.Semaphore), image.Context);
		}
	}

	public void UpdateWithTimelineSemaphores(CompositionImportedGpuImage image, CompositionImportedGpuSemaphore wait, ulong waitForValue, CompositionImportedGpuSemaphore signal, ulong signalValue)
	{
		using (base.Compositor.RenderInterface.EnsureCurrent())
		{
			PerformSanityChecks(image);
			if (!wait.IsUsable || !signal.IsUsable)
			{
				throw new PlatformGraphicsContextLostException();
			}
			Update(image.Image.SnapshotWithTimelineSemaphores(wait.Semaphore, waitForValue, signal.Semaphore, signalValue), image.Context);
		}
	}

	public void Dispose()
	{
		_bitmap?.Dispose();
	}
}
