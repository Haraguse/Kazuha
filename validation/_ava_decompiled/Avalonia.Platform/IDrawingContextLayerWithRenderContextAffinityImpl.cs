using System;

namespace Avalonia.Platform;

public interface IDrawingContextLayerWithRenderContextAffinityImpl : IDrawingContextLayerImpl, IBitmapImpl, IDisposable
{
	bool HasRenderContextAffinity { get; }

	IBitmapImpl CreateNonAffinedSnapshot();
}
