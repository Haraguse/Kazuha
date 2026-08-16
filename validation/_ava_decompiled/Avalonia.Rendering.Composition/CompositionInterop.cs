using System;
using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition;

internal class CompositionInterop : ICompositionGpuInterop
{
	private readonly Compositor _compositor;

	private readonly IPlatformRenderInterfaceContext _context;

	private readonly IExternalObjectsRenderInterfaceContextFeature _externalObjects;

	private readonly IExternalObjectsHandleWrapRenderInterfaceContextFeature? _externalObjectsWithHandleWrap;

	public IReadOnlyList<string> SupportedImageHandleTypes => _externalObjects.SupportedImageHandleTypes;

	public IReadOnlyList<string> SupportedSemaphoreTypes => _externalObjects.SupportedSemaphoreTypes;

	public bool IsLost => _context.IsLost;

	public byte[]? DeviceLuid { get; set; }

	public byte[]? DeviceUuid { get; set; }

	public CompositionInterop(Compositor compositor, IExternalObjectsRenderInterfaceContextFeature externalObjects)
	{
		_compositor = compositor;
		_context = compositor.Server.RenderInterface.Value;
		DeviceLuid = externalObjects.DeviceLuid;
		DeviceUuid = externalObjects.DeviceUuid;
		_externalObjects = externalObjects;
		_externalObjectsWithHandleWrap = _context.TryGetFeature<IExternalObjectsHandleWrapRenderInterfaceContextFeature>();
	}

	public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType)
	{
		return _externalObjects.GetSynchronizationCapabilities(imageHandleType);
	}

	public ICompositionImportedGpuImage ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties)
	{
		IPlatformHandle platformHandle = _externalObjectsWithHandleWrap?.WrapImageHandleOnAnyThread(handle, properties);
		handle = platformHandle ?? handle;
		return new CompositionImportedGpuImage(_compositor, _context, _externalObjects, () => _externalObjects.ImportImage(handle, properties), handle);
	}

	public ICompositionImportedGpuImage ImportImage(ICompositionImportableSharedGpuContextImage image)
	{
		return new CompositionImportedGpuImage(_compositor, _context, _externalObjects, () => _externalObjects.ImportImage(image), null);
	}

	public ICompositionImportedGpuSemaphore ImportSemaphore(IPlatformHandle handle)
	{
		IPlatformHandle platformHandle = _externalObjectsWithHandleWrap?.WrapSemaphoreHandleOnAnyThread(handle);
		handle = platformHandle ?? handle;
		return new CompositionImportedGpuSemaphore(handle, _compositor, _context, _externalObjects);
	}

	public ICompositionImportedGpuImage ImportSemaphore(ICompositionImportableSharedGpuContextSemaphore image)
	{
		throw new NotSupportedException();
	}
}
