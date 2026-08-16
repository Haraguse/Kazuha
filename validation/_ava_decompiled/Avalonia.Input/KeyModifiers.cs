using System;

namespace Avalonia.Input;

[Flags]
public enum KeyModifiers
{
	None = 0,
	Alt = 1,
	Control = 2,
	Shift = 4,
	Meta = 8
}
