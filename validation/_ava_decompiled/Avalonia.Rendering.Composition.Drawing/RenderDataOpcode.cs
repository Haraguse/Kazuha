namespace Avalonia.Rendering.Composition.Drawing;

internal enum RenderDataOpcode : byte
{
	Invalid,
	DrawLine,
	DrawRectangle,
	DrawEllipse,
	DrawGeometry,
	DrawGlyphRun,
	DrawBitmap,
	DrawCustom,
	PushClip,
	PushGeometryClip,
	PushOpacity,
	PushOpacityMask,
	PushTransform,
	PushRenderOptions,
	PushTextOptions,
	PushEffect,
	Pop
}
