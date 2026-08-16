using System;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition;

internal class CompositionImportedGpuSemaphore : CompositionGpuImportedObjectBase, ICompositionImportedGpuSemaphore, ICompositionGpuImportedObject, IAsyncDisposable
{
	private readonly IPlatformHandle _handle;

	private IPlatformRenderInterfaceImportedSemaphore? _semaphore;

	public IPlatformRenderInterfaceImportedSemaphore Semaphore => _semaphore ?? throw new ObjectDisposedException("CompositionImportedGpuSemaphore");

	public bool IsUsable
	{
		get
		{
			if (_semaphore != null)
			{
				return base.Compositor.Server.RenderInterface.Value == base.Context;
			}
			return false;
		}
	}

	public CompositionImportedGpuSemaphore(IPlatformHandle handle, Compositor compositor, IPlatformRenderInterfaceContext context, IExternalObjectsRenderInterfaceContextFeature feature)
		: base(compositor, context, feature, handle)
	{
		_handle = handle;
	}

	protected override void Import()
	{
		_semaphore = base.Feature.ImportSemaphore(_handle);
	}

	public override void Dispose()
	{
		_semaphore?.Dispose();
		_semaphore = null;
	}
}
