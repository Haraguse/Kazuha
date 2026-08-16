using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Rendering;

/// <summary>
/// This class is used to render the visual tree into a DrawingContext by doing
/// a simple tree traversal.
/// It's currently used mostly for RenderTargetBitmap.Render and VisualBrush
/// </summary>
internal class ImmediateRenderer
{
	/// <summary>
	/// Renders a visual to a drawing context.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <param name="context">The drawing context.</param>
	public static void Render(DrawingContext context, Visual visual)
	{
		Render(context, visual, new Rect(visual.Bounds.Size));
	}

	public static void Render(DrawingContext context, Visual visual, Rect clipRect)
	{
		using (context.PushTransform(Matrix.CreateTranslation(0.0 - clipRect.Position.X, 0.0 - clipRect.Position.Y)))
		{
			using (context.PushClip(clipRect))
			{
				Render(context, visual, new Rect(visual.Bounds.Size), Matrix.Identity, new Rect(clipRect.Size));
			}
		}
	}

	private static void Render(DrawingContext context, Visual visual, Rect bounds, Matrix parentTransform, Rect clipRect)
	{
		if (!visual.IsVisible)
		{
			return;
		}
		double opacity = visual.Opacity;
		if (!(opacity > 0.0))
		{
			return;
		}
		Rect bounds2 = new Rect(bounds.Size);
		Matrix? matrix = visual.RenderTransform?.Value;
		Matrix matrix3;
		if (matrix.HasValue)
		{
			Matrix valueOrDefault = matrix.GetValueOrDefault();
			Matrix matrix2 = Matrix.CreateTranslation(visual.RenderTransformOrigin.ToPixels(visual.Bounds.Size));
			matrix3 = -matrix2 * valueOrDefault * matrix2 * Matrix.CreateTranslation(bounds.Position);
		}
		else
		{
			matrix3 = Matrix.CreateTranslation(bounds.Position);
		}
		using ((visual.TextOptions != default(TextOptions)) ? new DrawingContext.PushedState?(context.PushTextOptions(visual.TextOptions)) : ((DrawingContext.PushedState?)null))
		{
			using ((visual.RenderOptions != default(RenderOptions)) ? new DrawingContext.PushedState?(context.PushRenderOptions(visual.RenderOptions)) : ((DrawingContext.PushedState?)null))
			{
				using (context.PushTransform(matrix3))
				{
					using (visual.HasMirrorTransform ? new DrawingContext.PushedState?(context.PushTransform(new Matrix(-1.0, 0.0, 0.0, 1.0, visual.Bounds.Width, 0.0))) : ((DrawingContext.PushedState?)null))
					{
						using (context.PushOpacity(opacity))
						{
							DrawingContext.PushedState? pushedState = ((visual == null || !visual.ClipToBounds) ? ((DrawingContext.PushedState?)null) : ((!(visual is IVisualWithRoundRectClip visualWithRoundRectClip)) ? new DrawingContext.PushedState?(context.PushClip(bounds2)) : new DrawingContext.PushedState?(context.PushClip(new RoundedRect(in bounds2, visualWithRoundRectClip.ClipToBoundsRadius)))));
							using (pushedState)
							{
								Geometry clip = visual.Clip;
								using ((clip != null) ? new DrawingContext.PushedState?(context.PushGeometryClip(clip)) : ((DrawingContext.PushedState?)null))
								{
									IBrush opacityMask = visual.OpacityMask;
									using ((opacityMask != null) ? new DrawingContext.PushedState?(context.PushOpacityMask(opacityMask, bounds2)) : ((DrawingContext.PushedState?)null))
									{
										IEffect effect = visual.Effect;
										using ((effect != null) ? new DrawingContext.PushedState?(context.PushEffect(effect, bounds2)) : ((DrawingContext.PushedState?)null))
										{
											Matrix matrix4 = matrix3 * parentTransform;
											IEffect effect2 = visual.Effect;
											if (((effect2 != null) ? bounds2.Inflate(effect2.GetEffectOutputPadding()) : bounds2).TransformToAABB(matrix4).Intersects(clipRect))
											{
												visual.Render(context);
											}
											IEnumerable<Visual> enumerable;
											if (!visual.HasNonUniformZIndexChildren)
											{
												IEnumerable<Visual> visualChildren = visual.VisualChildren;
												enumerable = visualChildren;
											}
											else
											{
												IEnumerable<Visual> visualChildren = visual.VisualChildren.OrderBy((Visual x) => x, ZIndexComparer.Instance);
												enumerable = visualChildren;
											}
											if (visual.ClipToBounds)
											{
												matrix4 = Matrix.Identity;
												clipRect = bounds2;
											}
											foreach (Visual item in enumerable)
											{
												Render(context, item, item.Bounds, matrix4, clipRect);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
}
