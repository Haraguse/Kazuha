namespace Avalonia.Rendering.Composition.Drawing;

internal struct DrawGlyphRunPayload : IRenderDataPayload<DrawGlyphRunPayload>
{
	public int ServerBrush;

	public int GlyphRun;

	public static RenderDataOpcode Opcode => RenderDataOpcode.DrawGlyphRun;
}
