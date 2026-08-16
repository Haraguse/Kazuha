using System;

namespace Avalonia.Input;

[Flags]
public enum KeyStates
{
	None = 0,
	Down = 1,
	Toggled = 2
}
