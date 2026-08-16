namespace Avalonia.Rendering.Composition.Drawing;

internal struct DrawEllipsePayload : IRenderDataPayload<DrawEllipsePayload>
{
	public int ServerBrush;

	public int ServerPen;

	public int ClientPen;

	public Rect Rect;

	public static RenderDataOpcode Opcode => RenderDataOpcode.DrawEllipse;
}
