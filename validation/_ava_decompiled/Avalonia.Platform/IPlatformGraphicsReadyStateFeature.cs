using Avalonia.Metadata;

namespace Avalonia.Platform;

[Unstable]
public interface IPlatformGraphicsReadyStateFeature
{
	bool IsReady { get; }

	bool UsesContexts { get; }
}
