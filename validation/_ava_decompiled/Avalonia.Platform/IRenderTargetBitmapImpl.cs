using System;
using Avalonia.Metadata;

namespace Avalonia.Platform;

/// <summary>
/// Defines the platform-specific interface for a
/// <see cref="T:Avalonia.Media.Imaging.RenderTargetBitmap" />.
/// </summary>
[Unstable]
public interface IRenderTargetBitmapImpl : IReadableBitmapImpl, IBitmapImpl, IDisposable
{
	IDrawingContextImpl CreateDrawingContext();
}
