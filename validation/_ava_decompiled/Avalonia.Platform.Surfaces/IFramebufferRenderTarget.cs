using System;
using Avalonia.Metadata;

namespace Avalonia.Platform.Surfaces;

[PrivateApi]
public interface IFramebufferRenderTarget : IDisposable, IPlatformRenderSurfaceRenderTarget
{
	bool RetainsFrameContents => false;

	/// <summary>
	/// Provides a framebuffer descriptor for drawing.
	/// </summary>
	/// <remarks>
	/// Contents should be drawn on actual window after disposing
	/// </remarks>
	ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo, out FramebufferLockProperties properties);
}
