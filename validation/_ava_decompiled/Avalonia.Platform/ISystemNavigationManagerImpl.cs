using System;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Avalonia.Platform;

[Unstable]
public interface ISystemNavigationManagerImpl
{
	event EventHandler<RoutedEventArgs>? BackRequested;
}
