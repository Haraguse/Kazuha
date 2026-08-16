using System;

namespace Avalonia.Input;

[Flags]
public enum RawInputModifiers
{
	None = 0,
	Alt = 1,
	Control = 2,
	Shift = 4,
	Meta = 8,
	LeftMouseButton = 0x10,
	RightMouseButton = 0x20,
	MiddleMouseButton = 0x40,
	XButton1MouseButton = 0x80,
	XButton2MouseButton = 0x100,
	KeyboardMask = Alt | Control | Shift | Meta,
	PenInverted = 0x200,
	PenEraser = 0x400,
	PenBarrelButton = 0x800
}
