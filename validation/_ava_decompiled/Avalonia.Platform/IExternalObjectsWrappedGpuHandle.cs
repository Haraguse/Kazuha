using System;
using Avalonia.Metadata;

namespace Avalonia.Platform;

[Unstable]
[NotClientImplementable]
public interface IExternalObjectsWrappedGpuHandle : IPlatformHandle, IDisposable
{
}
