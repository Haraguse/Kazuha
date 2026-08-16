using System;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia;

/// <summary>
/// Extension methods for <see cref="T:Avalonia.Visual" />.
/// </summary>
public static class VisualExtensions
{
	/// <summary>
	/// Converts a point from screen to client coordinates.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <param name="point">The point in screen coordinates.</param>
	/// <returns>The point in client coordinates.</returns>
	/// <exception cref="T:System.ArgumentException">Thown when <paramref name="visual" /> does not belong to a visual tree.</exception>
	public static Point PointToClient(this Visual visual, PixelPoint point)
	{
		IPresentationSource presentationSource = visual.PresentationSource;
		Visual? visual2 = presentationSource?.RootVisual ?? throw new ArgumentException("Visual does not belong to a visual tree.", "visual");
		Point valueOrDefault = presentationSource.PointToClient(point).GetValueOrDefault();
		return visual2.TranslatePoint(valueOrDefault, visual).Value;
	}

	/// <summary>
	/// Converts a point from client to screen coordinates.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <param name="point">The point in client coordinates.</param>
	/// <returns>The point in screen coordinates.</returns>
	/// <exception cref="T:System.ArgumentException">Thown when <paramref name="visual" /> does not belong to a visual tree.</exception>
	public static PixelPoint PointToScreen(this Visual visual, Point point)
	{
		IPresentationSource? presentationSource = visual.PresentationSource;
		Visual relativeTo = presentationSource?.RootVisual ?? throw new ArgumentException("Visual does not belong to a visual tree.", "visual");
		return presentationSource.PointToScreen(visual.TranslatePoint(point, relativeTo).Value).GetValueOrDefault();
	}

	/// <summary>
	/// Returns a transform that transforms the visual's coordinates into the coordinates
	/// of the specified <paramref name="to" />.
	/// </summary>
	/// <param name="from">The visual whose coordinates are to be transformed.</param>
	/// <param name="to">The visual to translate the coordinates to.</param>
	/// <returns>
	/// A <see cref="T:Avalonia.Matrix" /> containing the transform or null if the visuals don't share a
	/// common ancestor.
	/// </returns>
	public static Matrix? TransformToVisual(this Visual from, Visual to)
	{
		Visual visual = from.FindCommonVisualAncestor(to);
		if (visual != null)
		{
			Matrix offsetFrom = GetOffsetFrom(visual, from);
			if (!GetOffsetFrom(visual, to).TryInvert(out var inverted))
			{
				return null;
			}
			return inverted * offsetFrom;
		}
		return null;
	}

	/// <summary>
	/// Translates a point relative to this visual to coordinates that are relative to the specified visual.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <param name="point">The point value, as relative to this visual.</param>
	/// <param name="relativeTo">The visual to translate the given point into.</param>
	/// <returns>
	/// A point value, now relative to the target visual rather than this source element, or null if the
	/// two elements have no common ancestor.
	/// </returns>
	public static Point? TranslatePoint(this Visual visual, Point point, Visual relativeTo)
	{
		Matrix? matrix = visual.TransformToVisual(relativeTo);
		if (matrix.HasValue)
		{
			return point.Transform(matrix.Value);
		}
		return null;
	}

	/// <summary>
	/// Gets a transform from an ancestor to a descendent.
	/// </summary>
	/// <param name="ancestor">The ancestor visual.</param>
	/// <param name="visual">The visual.</param>
	/// <returns>The transform.</returns>
	private static Matrix GetOffsetFrom(Visual ancestor, Visual visual)
	{
		Matrix identity = Matrix.Identity;
		Visual visual2 = visual;
		while (visual2 != ancestor)
		{
			if (visual2.HasMirrorTransform)
			{
				Matrix matrix = new Matrix(-1.0, 0.0, 0.0, 1.0, visual2.Bounds.Width, 0.0);
				identity *= matrix;
			}
			ITransform? renderTransform = visual2.RenderTransform;
			if (renderTransform != null)
			{
				_ = renderTransform.Value;
				if (true)
				{
					Matrix matrix2 = Matrix.CreateTranslation(visual2.RenderTransformOrigin.ToPixels(visual2.Bounds.Size));
					Matrix matrix3 = -matrix2 * visual2.RenderTransform.Value * matrix2;
					identity *= matrix3;
				}
			}
			Point topLeft = visual2.Bounds.TopLeft;
			if (topLeft != default(Point))
			{
				identity *= Matrix.CreateTranslation(topLeft);
			}
			visual2 = visual2.VisualParent;
			if (visual2 == null)
			{
				throw new ArgumentException("'visual' is not a descendant of 'ancestor'.");
			}
		}
		return identity;
	}
}
