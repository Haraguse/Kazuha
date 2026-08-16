using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Input;

internal static class FocusHelpers
{
	public static IEnumerable<IInputElement> GetInputElementChildren(AvaloniaObject? parent)
	{
		if (parent is Visual visual)
		{
			return visual.VisualChildren.OfType<IInputElement>();
		}
		return Array.Empty<IInputElement>();
	}

	public static bool CanHaveFocusableChildren(AvaloniaObject? parent)
	{
		if (parent == null)
		{
			return false;
		}
		IEnumerable<IInputElement> inputElementChildren = GetInputElementChildren(parent);
		bool flag = false;
		foreach (IInputElement item in inputElementChildren)
		{
			if (IsVisible(item))
			{
				if (item.Focusable)
				{
					flag = true;
				}
				else if (CanHaveFocusableChildren(item as AvaloniaObject))
				{
					flag = true;
				}
			}
			if (flag)
			{
				break;
			}
		}
		return flag;
	}

	public static IInputElement? GetFocusParent(IInputElement? inputElement)
	{
		if (inputElement == null)
		{
			return null;
		}
		if (inputElement is Visual visual)
		{
			Visual visualRoot = visual.VisualRoot;
			if (inputElement != visualRoot)
			{
				return visual.Parent as IInputElement;
			}
		}
		return null;
	}

	public static bool IsPotentialTabStop(IInputElement? element)
	{
		if (element is InputElement inputElement)
		{
			return inputElement.IsTabStop;
		}
		return false;
	}

	internal static bool IsVisible(IInputElement? element)
	{
		if (element is Visual visual)
		{
			return visual.IsEffectivelyVisible;
		}
		return false;
	}

	internal static bool IsFocusable(IInputElement? element)
	{
		return element?.Focusable ?? false;
	}

	internal static bool CanHaveChildren(IInputElement? element)
	{
		return element is Visual;
	}
}
