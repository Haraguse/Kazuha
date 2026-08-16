using Avalonia.Metadata;

namespace Avalonia.Rendering;

/// <summary>
/// Provides factory methods for creating <see cref="T:Avalonia.Rendering.IRenderLoop" /> instances.
/// </summary>
[PrivateApi]
public static class RenderLoop
{
	/// <summary>
	/// Creates an <see cref="T:Avalonia.Rendering.IRenderLoop" /> from an <see cref="T:Avalonia.Rendering.IRenderTimer" />.
	/// </summary>
	public static IRenderLoop FromTimer(IRenderTimer timer)
	{
		return new DefaultRenderLoop(timer);
	}
}
