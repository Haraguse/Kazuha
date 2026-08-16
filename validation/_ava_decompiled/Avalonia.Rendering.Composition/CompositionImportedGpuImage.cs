using System;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition;

internal class CompositionImportedGpuImage : CompositionGpuImportedObjectBase, ICompositionImportedGpuImage, ICompositionGpuImportedObject, IAsyncDisposable
{
	private readonly Func<IPlatformRenderInterfaceImportedImage> _importer;

	private IPlatformRenderInterfaceImportedImage? _image;

	public IPlatformRenderInterfaceImportedImage Image => _image ?? throw new ObjectDisposedException("CompositionImportedGpuImage");

	public bool IsUsable
	{
		get
		{
			if (_image != null)
			{
				return base.Compositor.Server.RenderInterface.Value == base.Context;
			}
			return false;
		}
	}

	public CompositionImportedGpuImage(Compositor compositor, IPlatformRenderInterfaceContext context, IExternalObjectsRenderInterfaceContextFeature feature, Func<IPlatformRenderInterfaceImportedImage> importer, IPlatformHandle? handle)
		: base(compositor, context, feature, handle)
	{
		_importer = importer;
	}

	protected override void Import()
	{
		using (base.Compositor.Server.RenderInterface.EnsureCurrent())
		{
			if (base.Context != base.Compositor.Server.RenderInterface.Value)
			{
				throw new PlatformGraphicsContextLostException();
			}
			_image = _importer();
		}
	}

	public override void Dispose()
	{
		_image?.Dispose();
		_image = null;
	}
}
