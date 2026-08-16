using System;
using Avalonia.Metadata;

namespace Avalonia.Platform;

/// <summary>
/// Defines the platform-specific interface for a <see cref="T:Avalonia.Media.Imaging.WriteableBitmap" />.
/// </summary>
[PrivateApi]
public interface IWriteableBitmapImpl : IBitmapImpl, IDisposable, IReadableBitmapImpl
{
}
