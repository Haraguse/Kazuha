namespace Avalonia.Rendering.Composition.Drawing;

internal struct PushOpacityPayload : IRenderDataPayload<PushOpacityPayload>
{
	public double Opacity;

	public static RenderDataOpcode Opcode => RenderDataOpcode.PushOpacity;
}
