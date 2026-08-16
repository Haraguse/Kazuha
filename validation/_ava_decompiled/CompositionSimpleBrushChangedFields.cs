using System;

[Flags]
internal enum CompositionSimpleBrushChangedFields : byte
{
	Opacity = 1,
	TransformOrigin = 2,
	Transform = 4
}
