using Avalonia.Metadata;

namespace Avalonia.Platform;

[Unstable]
public interface IPlatformGraphics
{
	bool UsesSharedContext { get; }

	IPlatformGraphicsContext CreateContext();

	IPlatformGraphicsContext GetSharedContext();
}
