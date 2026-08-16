using Avalonia.Metadata;

namespace Avalonia.Platform;

[PrivateApi]
public interface IRuntimePlatform
{
	RuntimePlatformInfo GetRuntimeInfo();
}
