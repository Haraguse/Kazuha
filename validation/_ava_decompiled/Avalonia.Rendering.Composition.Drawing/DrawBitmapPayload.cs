namespace Avalonia.Rendering.Composition.Drawing;

internal struct DrawBitmapPayload : IRenderDataPayload<DrawBitmapPayload>
{
	public int Bitmap;

	public double Opacity;

	public Rect SourceRect;

	public Rect DestRect;

	public static RenderDataOpcode Opcode => RenderDataOpcode.DrawBitmap;
}
