using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Drawing;

internal class ImmediateRenderDataSceneBrushContent : ISceneBrushContent, IImmutableBrush, IBrush, IDisposable
{
	private RenderDataStream? _stream;

	public ITileBrush Brush { get; }

	public Rect Rect { get; }

	public double Opacity => Brush.Opacity;

	public ITransform? Transform => Brush.Transform;

	public RelativePoint TransformOrigin => Brush.TransformOrigin;

	public bool UseScalableRasterization { get; }

	public ImmediateRenderDataSceneBrushContent(ITileBrush brush, RenderDataStream stream, Rect? rect, bool useScalableRasterization)
	{
		Brush = brush;
		_stream = stream;
		UseScalableRasterization = useScalableRasterization;
		Rect = rect ?? ServerCompositionRenderData.ApplyRenderBoundsRounding(stream.CalculateBounds()).GetValueOrDefault();
	}

	public void Dispose()
	{
		if (_stream != null)
		{
			_stream.DisposeResources();
			_stream.Dispose();
			_stream = null;
		}
	}

	private void Render(IDrawingContextImpl context)
	{
		_stream?.Replay(context);
	}

	public void Render(IDrawingContextImpl context, Matrix? transform)
	{
		if (transform.HasValue)
		{
			Matrix transform2 = context.Transform;
			context.Transform = transform.Value * transform2;
			Render(context);
			context.Transform = transform2;
		}
		else
		{
			Render(context);
		}
	}
}
