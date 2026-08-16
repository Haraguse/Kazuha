using Avalonia.Metadata;

namespace Avalonia.Platform.Surfaces;

[Unstable]
public interface IFramebufferPlatformSurface : IPlatformRenderSurface
{
	IFramebufferRenderTarget CreateFramebufferRenderTarget();
}
