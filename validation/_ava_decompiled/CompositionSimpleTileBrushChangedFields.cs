using System;

[Flags]
internal enum CompositionSimpleTileBrushChangedFields : byte
{
	AlignmentX = 1,
	AlignmentY = 2,
	DestinationRect = 4,
	SourceRect = 8,
	Stretch = 0x10,
	TileMode = 0x20
}
