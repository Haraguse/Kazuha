using Avalonia.Metadata;

namespace Avalonia.Platform.Surfaces;

[PrivateApi]
public record struct FramebufferLockProperties(bool PreviousFrameIsRetained);
