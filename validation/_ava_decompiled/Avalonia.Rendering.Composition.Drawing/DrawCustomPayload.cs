namespace Avalonia.Rendering.Composition.Drawing;

internal struct DrawCustomPayload : IRenderDataPayload<DrawCustomPayload>
{
	public int Operation;

	public static RenderDataOpcode Opcode => RenderDataOpcode.DrawCustom;
}
