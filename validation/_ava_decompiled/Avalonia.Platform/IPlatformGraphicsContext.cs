using System;
using Avalonia.Metadata;

namespace Avalonia.Platform;

[Unstable]
public interface IPlatformGraphicsContext : IDisposable, IOptionalFeatureProvider
{
	bool IsLost { get; }

	IDisposable EnsureCurrent();
}
