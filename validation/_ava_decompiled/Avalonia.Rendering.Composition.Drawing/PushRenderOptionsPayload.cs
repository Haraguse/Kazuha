using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Drawing;

internal struct PushRenderOptionsPayload : IRenderDataPayload<PushRenderOptionsPayload>
{
	public RenderOptions Options;

	public static RenderDataOpcode Opcode => RenderDataOpcode.PushRenderOptions;
}
