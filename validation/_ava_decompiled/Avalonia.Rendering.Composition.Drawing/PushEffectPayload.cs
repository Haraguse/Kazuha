namespace Avalonia.Rendering.Composition.Drawing;

internal struct PushEffectPayload : IRenderDataPayload<PushEffectPayload>
{
	public int Effect;

	public Rect Bounds;

	public static RenderDataOpcode Opcode => RenderDataOpcode.PushEffect;
}
