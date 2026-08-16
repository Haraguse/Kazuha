using System;

namespace Avalonia.Platform.Surfaces;

/// <summary>
/// For simple cases when framebuffer is always available
/// </summary>
public class FuncFramebufferRenderTarget : IFramebufferRenderTarget, IDisposable, IPlatformRenderSurfaceRenderTarget
{
	public delegate ILockedFramebuffer LockFramebufferDelegate(IRenderTarget.RenderTargetSceneInfo sceneInfo, out FramebufferLockProperties properties);

	private readonly LockFramebufferDelegate _lockFramebuffer;

	public bool RetainsFrameContents { get; }

	public FuncFramebufferRenderTarget(Func<ILockedFramebuffer> lockFramebuffer)
		: this(delegate(IRenderTarget.RenderTargetSceneInfo _, out FramebufferLockProperties properties)
		{
			properties = default(FramebufferLockProperties);
			return lockFramebuffer();
		})
	{
	}

	public FuncFramebufferRenderTarget(LockFramebufferDelegate lockFramebuffer, bool retainsFrameContents = false)
	{
		_lockFramebuffer = lockFramebuffer;
		RetainsFrameContents = retainsFrameContents;
	}

	public void Dispose()
	{
	}

	public ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo, out FramebufferLockProperties properties)
	{
		return _lockFramebuffer(sceneInfo, out properties);
	}
}
