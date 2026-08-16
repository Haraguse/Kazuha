using System;
using Avalonia.Metadata;

namespace Avalonia.Platform;

[PrivateApi]
public interface IReadableBitmapImpl : IBitmapImpl, IDisposable
{
	PixelFormat? Format { get; }

	AlphaFormat? AlphaFormat { get; }

	ILockedFramebuffer Lock();
}
