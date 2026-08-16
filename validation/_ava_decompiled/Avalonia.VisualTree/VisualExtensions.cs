using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Utilities;

namespace Avalonia.VisualTree;

/// <summary>
/// Provides extension methods for working with the visual tree.
/// </summary>
public static class VisualExtensions
{
	private class ZOrderElement : IComparable<ZOrderElement>
	{
		private class ZOrderComparer : IComparer<ZOrderElement>
		{
			public int Compare(ZOrderElement? x, ZOrderElement? y)
			{
				if (x == y)
				{
					return 0;
				}
				if (y == null)
				{
					return 1;
				}
				return x?.CompareTo(y) ?? (-1);
			}
		}

		public Visual? Element { get; set; }

		public int Index { get; set; }

		public int ZIndex { get; set; }

		public static IComparer<ZOrderElement> Comparer { get; } = new ZOrderComparer();

		public int CompareTo(ZOrderElement? other)
		{
			if (other == null)
			{
				return 1;
			}
			int num = other.ZIndex - ZIndex;
			if (num != 0)
			{
				return num;
			}
			return other.Index - Index;
		}
	}

	/// <summary>
	/// Calculates the distance from a visual's ancestor.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <param name="ancestor">The ancestor visual.</param>
	/// <returns>
	/// The number of steps from the visual to the ancestor or -1 if
	/// <paramref name="visual" /> is not a descendent of <paramref name="ancestor" />.
	/// </returns>
	public static int CalculateDistanceFromAncestor(this Visual visual, Visual? ancestor)
	{
		Visual visual2 = visual ?? throw new ArgumentNullException("visual");
		int num = 0;
		while (visual2 != null && visual2 != ancestor)
		{
			visual2 = visual2.VisualParent;
			num++;
		}
		if (visual2 == null)
		{
			return -1;
		}
		return num;
	}

	/// <summary>
	/// Calculates the distance from a visual's root.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <returns>
	/// The number of steps from the visual to the root.
	/// </returns>
	public static int CalculateDistanceFromRoot(Visual visual)
	{
		Visual visual2 = visual ?? throw new ArgumentNullException("visual");
		int num = 0;
		visual2 = visual2.VisualParent;
		while (visual2 != null)
		{
			visual2 = visual2.VisualParent;
			num++;
		}
		return num;
	}

	/// <summary>
	/// Tries to get the first common ancestor of two visuals.
	/// </summary>
	/// <param name="visual">The first visual.</param>
	/// <param name="target">The second visual.</param>
	/// <returns>The common ancestor, or null if not found.</returns>
	public static Visual? FindCommonVisualAncestor(this Visual? visual, Visual? target)
	{
		if (visual == null || target == null)
		{
			return null;
		}
		Visual node = visual;
		Visual node2 = target;
		int num = CalculateDistanceFromRoot(node);
		int num2 = CalculateDistanceFromRoot(node2);
		if (num > num2)
		{
			GoUpwards(ref node, num - num2);
		}
		else
		{
			GoUpwards(ref node2, num2 - num);
		}
		if (node == node2)
		{
			return node;
		}
		while (node != null && node2 != null)
		{
			Visual visualParent = node.VisualParent;
			Visual visualParent2 = node2.VisualParent;
			if (visualParent == visualParent2)
			{
				return visualParent;
			}
			node = node.VisualParent;
			node2 = node2.VisualParent;
		}
		return null;
		static void GoUpwards(ref Visual? reference, int count)
		{
			for (int i = 0; i < count; i++)
			{
				reference = reference?.VisualParent;
			}
		}
	}

	/// <summary>
	/// Enumerates the ancestors of an <see cref="T:Avalonia.Visual" /> in the visual tree.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <returns>The visual's ancestors.</returns>
	public static IEnumerable<Visual> GetVisualAncestors(this Visual visual)
	{
		ThrowHelper.ThrowIfNull(visual, "visual");
		for (Visual v = visual.VisualParent; v != null; v = v.VisualParent)
		{
			yield return v;
		}
	}

	/// <summary>
	/// Finds first ancestor of given type.
	/// </summary>
	/// <typeparam name="T">Ancestor type.</typeparam>
	/// <param name="visual">The visual.</param>
	/// <param name="includeSelf">If given visual should be included in search.</param>
	/// <returns>First ancestor of given type.</returns>
	public static T? FindAncestorOfType<T>(this Visual? visual, bool includeSelf = false) where T : class
	{
		return visual.FindAncestorOfType<T>(includeSelf, null);
	}

	/// <summary>
	/// Finds first ancestor of given type that matches a predicate.
	/// </summary>
	/// <typeparam name="T">Ancestor type.</typeparam>
	/// <param name="visual">The visual.</param>
	/// <param name="includeSelf">If given visual should be included in search.</param>
	/// <param name="predicate">The predicate that the ancestor must match.</param>
	/// <returns>First ancestor of given type.</returns>
	public static T? FindAncestorOfType<T>(this Visual? visual, bool includeSelf, Predicate<T>? predicate) where T : class
	{
		if (visual == null)
		{
			return null;
		}
		for (Visual visual2 = (includeSelf ? visual : visual.VisualParent); visual2 != null; visual2 = visual2.VisualParent)
		{
			if (visual2 is T val && (predicate == null || predicate(val)))
			{
				return val;
			}
		}
		return null;
	}

	/// <summary>
	/// Finds first descendant of given type.
	/// </summary>
	/// <typeparam name="T">Descendant type.</typeparam>
	/// <param name="visual">The visual.</param>
	/// <param name="includeSelf">If given visual should be included in search.</param>
	/// <returns>First descendant of given type.</returns>
	public static T? FindDescendantOfType<T>(this Visual? visual, bool includeSelf = false) where T : class
	{
		return visual.FindDescendantOfType<T>(includeSelf, null);
	}

	/// <summary>
	/// Finds first descendant of given type that matches given predicate.
	/// </summary>
	/// <typeparam name="T">Descendant type.</typeparam>
	/// <param name="visual">The visual.</param>
	/// <param name="includeSelf">If given visual should be included in search.</param>
	/// <param name="predicate">The predicate that the descendant must match.</param>
	/// <returns>First descendant of given type that matches given predicate.</returns>
	public static T? FindDescendantOfType<T>(this Visual? visual, bool includeSelf, Predicate<T>? predicate) where T : class
	{
		if (visual == null)
		{
			return null;
		}
		if (includeSelf && visual is T val && (predicate == null || predicate(val)))
		{
			return val;
		}
		return FindDescendantOfTypeCore(visual, predicate);
	}

	/// <summary>
	/// Enumerates an <see cref="T:Avalonia.Visual" /> and its ancestors in the visual tree.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <returns>The visual and its ancestors.</returns>
	public static IEnumerable<Visual> GetSelfAndVisualAncestors(this Visual visual)
	{
		ThrowHelper.ThrowIfNull(visual, "visual");
		yield return visual;
		foreach (Visual visualAncestor in visual.GetVisualAncestors())
		{
			yield return visualAncestor;
		}
	}

	public static TransformedBounds? GetTransformedBounds(this Visual visual)
	{
		if (visual == null)
		{
			throw new ArgumentNullException("visual");
		}
		Rect clip = default(Rect);
		Matrix transform = Matrix.Identity;
		if (!Visit(visual))
		{
			return null;
		}
		return new TransformedBounds(new Rect(visual.Bounds.Size), clip, transform);
		bool Visit(Visual visual2)
		{
			if (!visual2.IsVisible)
			{
				return false;
			}
			Rect rect = new Rect(visual2.Bounds.Size);
			Visual visualParent = visual2.GetVisualParent();
			if (visualParent == null)
			{
				clip = rect;
				return true;
			}
			if (!Visit(visualParent))
			{
				return false;
			}
			Matrix identity = Matrix.Identity;
			if (visual2.HasMirrorTransform)
			{
				Matrix matrix = new Matrix(-1.0, 0.0, 0.0, 1.0, visual2.Bounds.Width, 0.0);
				identity *= matrix;
			}
			if (visual2.RenderTransform != null)
			{
				Matrix matrix2 = Matrix.CreateTranslation(visual2.RenderTransformOrigin.ToPixels(rect.Size));
				Matrix matrix3 = -matrix2 * visual2.RenderTransform.Value * matrix2;
				identity *= matrix3;
			}
			transform = identity * Matrix.CreateTranslation(visual2.Bounds.Position) * transform;
			if (visual2.ClipToBounds)
			{
				Rect rect2 = rect.TransformToAABB(transform);
				Rect rect3 = (visual2.ClipToBounds ? rect2.Intersect(clip) : clip);
				clip = clip.Intersect(rect3);
			}
			return true;
		}
	}

	/// <summary>
	/// Gets the first visual in the visual tree whose bounds contain a point.
	/// </summary>
	/// <param name="visual">The root visual to test.</param>
	/// <param name="p">The point.</param>
	/// <returns>The visual at the requested point.</returns>
	public static Visual? GetVisualAt(this Visual visual, Point p)
	{
		ThrowHelper.ThrowIfNull(visual, "visual");
		return visual.GetVisualAt(p, (Visual x) => x.IsVisible);
	}

	/// <summary>
	/// Gets the first visual in the visual tree whose bounds contain a point.
	/// </summary>
	/// <param name="visual">The root visual to test.</param>
	/// <param name="p">The point.</param>
	/// <param name="filter">
	/// A filter predicate. If the predicate returns false then the visual and all its
	/// children will be excluded from the results.
	/// </param>
	/// <returns>The visual at the requested point.</returns>
	public static Visual? GetVisualAt(this Visual visual, Point p, Func<Visual, bool> filter)
	{
		ThrowHelper.ThrowIfNull(visual, "visual");
		return visual.GetPresentationSource()?.HitTester.HitTestFirst(p, visual, filter);
	}

	/// <summary>
	/// Enumerates the visible visuals in the visual tree whose bounds contain a point.
	/// </summary>
	/// <param name="visual">The root visual to test.</param>
	/// <param name="p">The point.</param>
	/// <returns>The visuals at the requested point.</returns>
	public static IEnumerable<Visual> GetVisualsAt(this Visual visual, Point p)
	{
		ThrowHelper.ThrowIfNull(visual, "visual");
		return visual.GetVisualsAt(p, (Visual x) => x.IsVisible);
	}

	/// <summary>
	/// Enumerates the visuals in the visual tree whose bounds contain a point.
	/// </summary>
	/// <param name="visual">The root visual to test.</param>
	/// <param name="p">The point.</param>
	/// <param name="filter">
	/// A filter predicate. If the predicate returns false then the visual and all its
	/// children will be excluded from the results.
	/// </param>
	/// <returns>The visuals at the requested point.</returns>
	public static IEnumerable<Visual> GetVisualsAt(this Visual visual, Point p, Func<Visual, bool> filter)
	{
		ThrowHelper.ThrowIfNull(visual, "visual");
		IPresentationSource presentationSource = visual.GetPresentationSource();
		if (presentationSource == null)
		{
			return Array.Empty<Visual>();
		}
		return presentationSource.HitTester.HitTest(p, visual, filter);
	}

	/// <summary>
	/// Enumerates the children of an <see cref="T:Avalonia.Visual" /> in the visual tree.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <returns>The visual children.</returns>
	public static IEnumerable<Visual> GetVisualChildren(this Visual visual)
	{
		return visual.VisualChildren;
	}

	/// <summary>
	/// Enumerates the descendants of an <see cref="T:Avalonia.Visual" /> in the visual tree.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <returns>The visual's ancestors.</returns>
	public static IEnumerable<Visual> GetVisualDescendants(this Visual visual)
	{
		foreach (Visual child in visual.VisualChildren)
		{
			yield return child;
			foreach (Visual visualDescendant in child.GetVisualDescendants())
			{
				yield return visualDescendant;
			}
		}
	}

	/// <summary>
	/// Enumerates an <see cref="T:Avalonia.Visual" /> and its descendants in the visual tree.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <returns>The visual and its ancestors.</returns>
	public static IEnumerable<Visual> GetSelfAndVisualDescendants(this Visual visual)
	{
		yield return visual;
		foreach (Visual visualDescendant in visual.GetVisualDescendants())
		{
			yield return visualDescendant;
		}
	}

	/// <summary>
	/// Gets the visual parent of an <see cref="T:Avalonia.Visual" />.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <returns>The parent, or null if the visual is unparented.</returns>
	public static Visual? GetVisualParent(this Visual visual)
	{
		return visual.VisualParent;
	}

	/// <summary>
	/// Gets the visual parent of an <see cref="T:Avalonia.Visual" />.
	/// </summary>
	/// <typeparam name="T">The type of the visual parent.</typeparam>
	/// <param name="visual">The visual.</param>
	/// <returns>
	/// The parent, or null if the visual is unparented or its parent is not of type <typeparamref name="T" />.
	/// </returns>
	public static T? GetVisualParent<T>(this Visual visual) where T : class
	{
		return visual.VisualParent as T;
	}

	public static IPresentationSource? GetPresentationSource(this Visual visual)
	{
		return visual.PresentationSource;
	}

	internal static Visual? GetVisualRoot(this Visual visual)
	{
		return visual.PresentationSource?.RootVisual;
	}

	internal static ILayoutRoot? GetLayoutRoot(this Visual visual)
	{
		return visual.PresentationSource?.LayoutRoot;
	}

	/// <summary>
	/// Gets the layout manager for the visual's presentation source, or null if the visual is not attached to a visual root.
	/// </summary>
	public static ILayoutManager? GetLayoutManager(this Visual visual)
	{
		return visual.PresentationSource?.LayoutRoot.LayoutManager;
	}

	/// <summary>
	/// Attempts to obtain platform settings from the visual's root.
	/// This will return null if the visual is not attached to a visual root.
	/// </summary>
	public static IPlatformSettings? GetPlatformSettings(this Visual visual)
	{
		return visual.GetPresentationSource()?.PlatformSettings;
	}

	/// <summary>
	/// Returns a value indicating whether this control is attached to a visual root.
	/// </summary>
	public static bool IsAttachedToVisualTree(this Visual visual)
	{
		return visual.IsAttachedToVisualTree;
	}

	/// <summary>
	/// Tests whether an <see cref="T:Avalonia.Visual" /> is an ancestor of another visual.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <param name="target">The potential descendant.</param>
	/// <returns>
	/// True if <paramref name="visual" /> is an ancestor of <paramref name="target" />;
	/// otherwise false.
	/// </returns>
	public static bool IsVisualAncestorOf(this Visual? visual, Visual? target)
	{
		for (Visual visual2 = target?.VisualParent; visual2 != null; visual2 = visual2.VisualParent)
		{
			if (visual2 == visual)
			{
				return true;
			}
		}
		return false;
	}

	public static IEnumerable<Visual> SortByZIndex(this IEnumerable<Visual> elements)
	{
		return from x in elements.Select((Visual element, int index) => new ZOrderElement
			{
				Element = element,
				Index = index,
				ZIndex = element.ZIndex
			}).OrderBy((ZOrderElement x) => x, ZOrderElement.Comparer)
			select x.Element;
	}

	private static T? FindDescendantOfTypeCore<T>(Visual visual, Predicate<T>? predicate) where T : class
	{
		IAvaloniaList<Visual> visualChildren = visual.VisualChildren;
		int count = visualChildren.Count;
		for (int i = 0; i < count; i++)
		{
			Visual visual2 = visualChildren[i];
			if (visual2 is T val && (predicate == null || predicate(val)))
			{
				return val;
			}
			T val2 = FindDescendantOfTypeCore(visual2, predicate);
			if (val2 != null)
			{
				return val2;
			}
		}
		return null;
	}
}
