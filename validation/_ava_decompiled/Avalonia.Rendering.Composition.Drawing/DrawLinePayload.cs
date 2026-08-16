namespace Avalonia.Rendering.Composition.Drawing;

internal struct DrawLinePayload : IRenderDataPayload<DrawLinePayload>
{
	public int ServerPen;

	public int ClientPen;

	public Point P1;

	public Point P2;

	public static RenderDataOpcode Opcode => RenderDataOpcode.DrawLine;
}
