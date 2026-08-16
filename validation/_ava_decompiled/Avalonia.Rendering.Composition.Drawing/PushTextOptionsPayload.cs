using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Drawing;

internal struct PushTextOptionsPayload : IRenderDataPayload<PushTextOptionsPayload>
{
	public TextOptions Options;

	public static RenderDataOpcode Opcode => RenderDataOpcode.PushTextOptions;
}
