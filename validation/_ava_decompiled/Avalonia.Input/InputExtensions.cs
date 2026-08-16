using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Input;

/// <summary>
/// Defines extensions for the <see cref="T:Avalonia.Input.IInputElement" /> interface.
/// </summary>
public static class InputExtensions
{
	private static readonly Func<Visual, bool> s_hitTestDelegate = IsHitTestVisible;

	private static readonly Func<Visual, bool> s_hitTestEnabledOnlyDelegate = IsHitTestVisible_EnabledOnly;

	/// <summary>
	/// Returns the active input elements at a point on an <see cref="T:Avalonia.Input.IInputElement" />.
	/// </summary>
	/// <param name="element">The element to test.</param>
	/// <param name="p">The point on <paramref name="element" />.</param>
	/// <param name="enabledElementsOnly">Whether to only return elements for which <see cref="P:Avalonia.Input.IInputElement.IsEffectivelyEnabled" /> is true.</param>
	/// <returns>
	/// The active input elements found at the point, ordered topmost first.
	/// </returns>
	public static IEnumerable<IInputElement> GetInputElementsAt(this IInputElement element, Point p, bool enabledElementsOnly = true)
	{
		element = element ?? throw new ArgumentNullException("element");
		return (element as Visual)?.GetVisualsAt(p, enabledElementsOnly ? s_hitTestEnabledOnlyDelegate : s_hitTestDelegate).Cast<IInputElement>() ?? Enumerable.Empty<IInputElement>();
	}

	/// <inheritdoc cref="M:Avalonia.Input.InputExtensions.GetInputElementsAt(Avalonia.Input.IInputElement,Avalonia.Point,System.Boolean)" />
	public static IEnumerable<IInputElement> GetInputElementsAt(this IInputElement element, Point p)
	{
		return element.GetInputElementsAt(p, true);
	}

	/// <summary>
	/// Returns the topmost active input element at a point on an <see cref="T:Avalonia.Input.IInputElement" />.
	/// </summary>
	/// <param name="element">The element to test.</param>
	/// <param name="p">The point on <paramref name="element" />.</param>
	/// <param name="enabledElementsOnly">Whether to only return elements for which <see cref="P:Avalonia.Input.IInputElement.IsEffectivelyEnabled" /> is true.</param>
	/// <returns>The topmost <see cref="T:Avalonia.Input.IInputElement" /> at the specified position.</returns>
	public static IInputElement? InputHitTest(this IInputElement element, Point p, bool enabledElementsOnly = true)
	{
		element = element ?? throw new ArgumentNullException("element");
		return (element as Visual)?.GetVisualAt(p, enabledElementsOnly ? s_hitTestEnabledOnlyDelegate : s_hitTestDelegate) as IInputElement;
	}

	/// <inheritdoc cref="M:Avalonia.Input.InputExtensions.InputHitTest(Avalonia.Input.IInputElement,Avalonia.Point,System.Boolean)" />
	public static IInputElement? InputHitTest(this IInputElement element, Point p)
	{
		return element.InputHitTest(p, true);
	}

	/// <summary>
	/// Returns the topmost active input element at a point on an <see cref="T:Avalonia.Input.IInputElement" />.
	/// </summary>
	/// <param name="element">The element to test.</param>
	/// <param name="p">The point on <paramref name="element" />.</param>
	/// <param name="filter">
	/// A filter predicate. If the predicate returns false then the visual and all its
	/// children will be excluded from the results.
	/// </param>
	/// <param name="enabledElementsOnly">Whether to only return elements for which <see cref="P:Avalonia.Input.IInputElement.IsEffectivelyEnabled" /> is true.</param>
	/// <returns>The topmost <see cref="T:Avalonia.Input.IInputElement" /> at the specified position.</returns>
	public static IInputElement? InputHitTest(this IInputElement element, Point p, Func<Visual, bool> filter, bool enabledElementsOnly = true)
	{
		element = element ?? throw new ArgumentNullException("element");
		filter = filter ?? throw new ArgumentNullException("filter");
		Func<Visual, bool> hitTestDelegate = (enabledElementsOnly ? s_hitTestEnabledOnlyDelegate : s_hitTestDelegate);
		return (element as Visual)?.GetVisualAt(p, (Visual x) => hitTestDelegate(x) && filter(x)) as IInputElement;
	}

	/// <inheritdoc cref="M:Avalonia.Input.InputExtensions.InputHitTest(Avalonia.Input.IInputElement,Avalonia.Point,System.Func{Avalonia.Visual,System.Boolean},System.Boolean)" />
	public static IInputElement? InputHitTest(this IInputElement element, Point p, Func<Visual, bool> filter)
	{
		return element.InputHitTest(p, filter, true);
	}

	private static bool IsHitTestVisible(Visual visual)
	{
		if (visual != null && visual.IsVisible && visual.IsAttachedToVisualTree && visual is IInputElement inputElement)
		{
			return inputElement.IsHitTestVisible;
		}
		return false;
	}

	private static bool IsHitTestVisible_EnabledOnly(Visual visual)
	{
		if (IsHitTestVisible(visual))
		{
			if (visual is IInputElement inputElement)
			{
				return inputElement.IsEffectivelyEnabled;
			}
			return false;
		}
		return false;
	}
}
