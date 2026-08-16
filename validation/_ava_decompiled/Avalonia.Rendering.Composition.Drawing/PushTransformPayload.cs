namespace Avalonia.Rendering.Composition.Drawing;

internal struct PushTransformPayload : IRenderDataPayload<PushTransformPayload>
{
	public Matrix Matrix;

	public static RenderDataOpcode Opcode => RenderDataOpcode.PushTransform;
}
