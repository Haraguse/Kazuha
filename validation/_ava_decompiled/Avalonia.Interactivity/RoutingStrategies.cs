using System;

namespace Avalonia.Interactivity;

[Flags]
public enum RoutingStrategies
{
	Direct = 1,
	Tunnel = 2,
	Bubble = 4
}
