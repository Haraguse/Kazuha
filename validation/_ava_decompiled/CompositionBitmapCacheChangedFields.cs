using System;

[Flags]
internal enum CompositionBitmapCacheChangedFields : byte
{
	RenderAtScale = 1,
	RenderAtScaleAnimated = 2,
	SnapsToDevicePixels = 4,
	SnapsToDevicePixelsAnimated = 8,
	EnableClearType = 0x10,
	EnableClearTypeAnimated = 0x20
}
