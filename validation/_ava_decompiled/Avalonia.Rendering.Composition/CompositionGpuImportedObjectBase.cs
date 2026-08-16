using System;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition;

internal abstract class CompositionGpuImportedObjectBase : ICompositionGpuImportedObject, IAsyncDisposable
{
	protected Compositor Compositor { get; }

	public IPlatformRenderInterfaceContext Context { get; }

	public IExternalObjectsRenderInterfaceContextFeature Feature { get; }

	public Task ImportCompleted { get; }

	public bool IsLost => Context.IsLost;

	public CompositionGpuImportedObjectBase(Compositor compositor, IPlatformRenderInterfaceContext context, IExternalObjectsRenderInterfaceContextFeature feature, IPlatformHandle? handle)
	{
		CompositionGpuImportedObjectBase compositionGpuImportedObjectBase = this;
		Compositor = compositor;
		Context = context;
		Feature = feature;
		ImportCompleted = Compositor.InvokeServerJobAsync(delegate
		{
			using (handle as IExternalObjectsWrappedGpuHandle)
			{
				compositionGpuImportedObjectBase.Import();
			}
		});
	}

	protected abstract void Import();

	public abstract void Dispose();

	public ValueTask DisposeAsync()
	{
		return new ValueTask(Compositor.InvokeServerJobAsync(delegate
		{
			if (ImportCompleted.Status == TaskStatus.RanToCompletion)
			{
				Dispose();
			}
		}));
	}
}
