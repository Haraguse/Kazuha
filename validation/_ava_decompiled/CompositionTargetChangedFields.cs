using System;

[Flags]
internal enum CompositionTargetChangedFields : byte
{
	Root = 1,
	IsEnabled = 2,
	DebugOverlays = 4,
	LastLayoutPassTiming = 8,
	Scaling = 0x10,
	Size = 0x20,
	TransparencyLevel = 0x40
}
