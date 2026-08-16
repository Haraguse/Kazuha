using System;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Media;

[NotClientImplementable]
public interface ISceneBrushContent : IImmutableBrush, IBrush, IDisposable
{
	ITileBrush Brush { get; }

	Rect Rect { get; }

	internal bool UseScalableRasterization { get; }

	void Render(IDrawingContextImpl context, Matrix? transform);
}
