using Avalonia.Metadata;

namespace Avalonia.Platform;

[Unstable]
public interface IMacOSTopLevelPlatformHandle
{
	nint NSView { get; }

	nint NSWindow { get; }

	nint GetNSViewRetained();

	nint GetNSWindowRetained();
}
