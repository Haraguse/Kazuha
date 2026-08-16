namespace Avalonia.Rendering.Composition.Drawing;

internal struct PushGeometryClipPayload : IRenderDataPayload<PushGeometryClipPayload>
{
	public int Geometry;

	public static RenderDataOpcode Opcode => RenderDataOpcode.PushGeometryClip;
}
