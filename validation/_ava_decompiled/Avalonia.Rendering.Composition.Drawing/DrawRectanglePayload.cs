namespace Avalonia.Rendering.Composition.Drawing;

internal struct DrawRectanglePayload : IRenderDataPayload<DrawRectanglePayload>
{
	public int ServerBrush;

	public int ServerPen;

	public int ClientPen;

	public RoundedRect Rect;

	public int BoxShadowCount;

	public static RenderDataOpcode Opcode => RenderDataOpcode.DrawRectangle;
}
