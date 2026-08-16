using Avalonia.Metadata;

namespace Avalonia.Platform;

[PrivateApi]
public struct RenderTargetDrawingContextProperties
{
	/// <summary>
	/// Indicates that the drawing context targets a surface that preserved its contents since the previous frame
	/// </summary>
	public bool PreviousFrameIsRetained { get; init; }
}
