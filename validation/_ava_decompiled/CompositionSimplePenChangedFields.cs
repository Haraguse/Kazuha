using System;

[Flags]
internal enum CompositionSimplePenChangedFields : byte
{
	Brush = 1,
	DashStyle = 2,
	LineCap = 4,
	LineJoin = 8,
	MiterLimit = 0x10,
	Thickness = 0x20
}
