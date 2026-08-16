namespace Avalonia.Rendering.Composition.Drawing;

internal struct PushClipPayload : IRenderDataPayload<PushClipPayload>
{
	public RoundedRect Clip;

	public static RenderDataOpcode Opcode => RenderDataOpcode.PushClip;
}
