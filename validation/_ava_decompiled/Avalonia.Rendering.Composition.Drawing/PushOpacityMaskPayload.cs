namespace Avalonia.Rendering.Composition.Drawing;

internal struct PushOpacityMaskPayload : IRenderDataPayload<PushOpacityMaskPayload>
{
	public int Brush;

	public Rect Bounds;

	public static RenderDataOpcode Opcode => RenderDataOpcode.PushOpacityMask;
}
