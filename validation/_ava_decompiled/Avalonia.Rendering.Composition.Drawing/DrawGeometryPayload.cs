namespace Avalonia.Rendering.Composition.Drawing;

internal struct DrawGeometryPayload : IRenderDataPayload<DrawGeometryPayload>
{
	public int ServerBrush;

	public int ServerPen;

	public int ClientPen;

	public int Geometry;

	public static RenderDataOpcode Opcode => RenderDataOpcode.DrawGeometry;
}
