using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Drawing;

internal class CompositionRenderDataSceneBrushContent : ISceneBrushContent, IImmutableBrush, IBrush, IDisposable
{
	public record Properties(ServerCompositionRenderData RenderData, Rect? Rect, bool UseScalableRasterization);

	private readonly Rect? _rect;

	public ServerCompositionRenderData RenderData { get; }

	public ITileBrush Brush { get; }

	public Rect Rect => _rect ?? (RenderData?.Bounds?.ToRect()).GetValueOrDefault();

	public double Opacity => Brush.Opacity;

	public ITransform? Transform => Brush.Transform;

	public RelativePoint TransformOrigin => Brush.TransformOrigin;

	public bool UseScalableRasterization { get; }

	public CompositionRenderDataSceneBrushContent(ITileBrush brush, Properties properties)
	{
		Brush = brush;
		_rect = properties.Rect;
		UseScalableRasterization = properties.UseScalableRasterization;
		RenderData = properties.RenderData;
	}

	public void Dispose()
	{
	}

	public void Render(IDrawingContextImpl context, Matrix? transform)
	{
		if (transform.HasValue)
		{
			Matrix transform2 = context.Transform;
			context.Transform = transform.Value * transform2;
			RenderData.Render(context);
			context.Transform = transform2;
		}
		else
		{
			RenderData.Render(context);
		}
	}
}
