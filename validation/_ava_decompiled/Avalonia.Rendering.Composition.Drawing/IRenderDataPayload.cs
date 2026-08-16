namespace Avalonia.Rendering.Composition.Drawing;

internal interface IRenderDataPayload<TSelf> where TSelf : unmanaged, IRenderDataPayload<TSelf>
{
	static abstract RenderDataOpcode Opcode { get; }
}
