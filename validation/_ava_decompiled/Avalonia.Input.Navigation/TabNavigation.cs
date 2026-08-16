using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Input.Navigation;

/// <summary>
/// The implementation for default tab navigation.
/// </summary>
internal static class TabNavigation
{
	public static IInputElement? GetNextTab(IInputElement e, bool goDownOnly)
	{
		return GetNextTab(e, GetGroupParent(e), goDownOnly);
	}

	public static IInputElement? GetNextTab(IInputElement? e, IInputElement container, bool goDownOnly)
	{
		KeyboardNavigationMode keyNavigationMode = GetKeyNavigationMode(container);
		if (e == null)
		{
			if (IsTabStop(container))
			{
				return container;
			}
			IInputElement activeElement = GetActiveElement(container);
			if (activeElement != null)
			{
				return GetNextTab(null, activeElement, goDownOnly: true);
			}
		}
		else if ((keyNavigationMode == KeyboardNavigationMode.Once || keyNavigationMode == KeyboardNavigationMode.None) && container != e)
		{
			if (goDownOnly)
			{
				return null;
			}
			IInputElement groupParent = GetGroupParent(container);
			return GetNextTab(container, groupParent, goDownOnly);
		}
		IInputElement inputElement = null;
		IInputElement inputElement2 = e;
		KeyboardNavigationMode keyboardNavigationMode = keyNavigationMode;
		while ((inputElement2 = GetNextTabInGroup(inputElement2, container, keyboardNavigationMode)) != null && inputElement != inputElement2)
		{
			if (inputElement == null)
			{
				inputElement = inputElement2;
			}
			IInputElement nextTab = GetNextTab(null, inputElement2, goDownOnly: true);
			if (nextTab != null)
			{
				return nextTab;
			}
			if (keyboardNavigationMode == KeyboardNavigationMode.Once)
			{
				keyboardNavigationMode = KeyboardNavigationMode.Contained;
			}
		}
		if (!goDownOnly && keyboardNavigationMode != KeyboardNavigationMode.Contained && GetParent(container) != null)
		{
			return GetNextTab(container, GetGroupParent(container), goDownOnly: false);
		}
		return null;
	}

	public static IInputElement? GetNextTabOutside(ICustomKeyboardNavigation e)
	{
		if (e is IInputElement container)
		{
			IInputElement lastInTree = GetLastInTree(container);
			if (lastInTree != null)
			{
				return GetNextTab(lastInTree, goDownOnly: false);
			}
		}
		return null;
	}

	public static IInputElement? GetPrevTab(IInputElement? e, IInputElement? container, bool goDownOnly)
	{
		if (container == null)
		{
			container = GetGroupParent(e ?? throw new InvalidOperationException("Either 'e' or 'container' must be non-null."));
		}
		KeyboardNavigationMode keyNavigationMode = GetKeyNavigationMode(container);
		if (e == null)
		{
			IInputElement activeElement = GetActiveElement(container);
			if (activeElement != null)
			{
				return GetPrevTab(null, activeElement, goDownOnly: true);
			}
			if (keyNavigationMode == KeyboardNavigationMode.Once)
			{
				IInputElement nextTabInGroup = GetNextTabInGroup(null, container, keyNavigationMode);
				if (nextTabInGroup == null)
				{
					if (IsTabStop(container))
					{
						return container;
					}
					if (goDownOnly)
					{
						return null;
					}
					return GetPrevTab(container, null, goDownOnly: false);
				}
				return GetPrevTab(null, nextTabInGroup, goDownOnly: true);
			}
		}
		else if (keyNavigationMode == KeyboardNavigationMode.Once || keyNavigationMode == KeyboardNavigationMode.None)
		{
			if (goDownOnly || container == e)
			{
				return null;
			}
			if (IsTabStop(container))
			{
				return container;
			}
			return GetPrevTab(container, null, goDownOnly: false);
		}
		IInputElement inputElement = null;
		IInputElement inputElement2 = e;
		while ((inputElement2 = GetPrevTabInGroup(inputElement2, container, keyNavigationMode)) != null && (inputElement2 != container || keyNavigationMode != KeyboardNavigationMode.Local))
		{
			if (IsTabStop(inputElement2) && !IsGroup(inputElement2))
			{
				return inputElement2;
			}
			if (inputElement == inputElement2)
			{
				break;
			}
			if (inputElement == null)
			{
				inputElement = inputElement2;
			}
			IInputElement prevTab = GetPrevTab(null, inputElement2, goDownOnly: true);
			if (prevTab != null)
			{
				return prevTab;
			}
		}
		if (keyNavigationMode == KeyboardNavigationMode.Contained)
		{
			return null;
		}
		if (e != container && IsTabStop(container))
		{
			return container;
		}
		if (!goDownOnly && GetParent(container) != null)
		{
			return GetPrevTab(container, null, goDownOnly: false);
		}
		return null;
	}

	public static IInputElement? GetPrevTabOutside(ICustomKeyboardNavigation e)
	{
		if (e is IInputElement e2)
		{
			IInputElement firstChild = GetFirstChild(e2);
			if (firstChild != null)
			{
				return GetPrevTab(firstChild, null, goDownOnly: false);
			}
		}
		return null;
	}

	private static IInputElement? FocusedElement(IInputElement? e)
	{
		if (e != null && !e.IsKeyboardFocusWithin && e is IFocusScope scope)
		{
			IInputElement inputElement = FocusManager.GetFocusManager(e)?.GetFocusedElement(scope);
			if (inputElement != null && !IsFocusScope(e) && inputElement is Visual visual && e is Visual visual2 && visual != e && visual2.IsVisualAncestorOf(visual))
			{
				return inputElement;
			}
		}
		return null;
	}

	private static IInputElement? GetFirstChild(IInputElement e)
	{
		IInputElement inputElement = FocusedElement(e);
		if (inputElement != null)
		{
			return inputElement;
		}
		if ((!(e is InputElement e2) || IsVisibleAndEnabled(e2)) && e is Visual parent)
		{
			IInputElement[] array = FocusHelpers.GetInputElementChildren(parent).ToArray();
			int num = array.Length;
			for (int i = 0; i < num; i++)
			{
				if (array[i] is InputElement inputElement2)
				{
					if (IsVisibleAndEnabled(inputElement2))
					{
						return inputElement2;
					}
					IInputElement firstChild = GetFirstChild(inputElement2);
					if (firstChild != null)
					{
						return firstChild;
					}
				}
			}
		}
		return null;
	}

	private static IInputElement? GetLastChild(IInputElement e)
	{
		IInputElement inputElement = FocusedElement(e);
		if (inputElement != null)
		{
			return inputElement;
		}
		if ((!(e is InputElement e2) || IsVisibleAndEnabled(e2)) && e is Visual parent)
		{
			IInputElement[] array = FocusHelpers.GetInputElementChildren(parent).ToArray();
			for (int num = array.Length - 1; num >= 0; num--)
			{
				if (array[num] is InputElement inputElement2)
				{
					if (IsVisibleAndEnabled(inputElement2))
					{
						return inputElement2;
					}
					IInputElement lastChild = GetLastChild(inputElement2);
					if (lastChild != null)
					{
						return lastChild;
					}
				}
			}
		}
		return null;
	}

	internal static IInputElement? GetFirstTabInGroup(IInputElement container)
	{
		IInputElement inputElement = null;
		int num = int.MinValue;
		IInputElement inputElement2 = container;
		while ((inputElement2 = GetNextInTree(inputElement2, container)) != null)
		{
			if (IsTabStopOrGroup(inputElement2))
			{
				int tabIndex = KeyboardNavigation.GetTabIndex(inputElement2);
				if (tabIndex < num || inputElement == null)
				{
					num = tabIndex;
					inputElement = inputElement2;
				}
			}
		}
		return inputElement;
	}

	private static IInputElement GetLastInTree(IInputElement container)
	{
		IInputElement inputElement = container;
		IInputElement result;
		do
		{
			result = inputElement;
			inputElement = GetLastChild(inputElement);
		}
		while (inputElement != null && !IsGroup(inputElement));
		if (inputElement != null)
		{
			return inputElement;
		}
		return result;
	}

	private static IInputElement? GetLastTabInGroup(IInputElement container)
	{
		IInputElement inputElement = null;
		int num = int.MaxValue;
		IInputElement inputElement2 = GetLastInTree(container);
		while (inputElement2 != null && inputElement2 != container)
		{
			if (IsTabStopOrGroup(inputElement2))
			{
				int tabIndex = KeyboardNavigation.GetTabIndex(inputElement2);
				if (tabIndex > num || inputElement == null)
				{
					num = tabIndex;
					inputElement = inputElement2;
				}
			}
			inputElement2 = GetPreviousInTree(inputElement2, container);
		}
		return inputElement;
	}

	private static IInputElement? GetNextInTree(IInputElement e, IInputElement container)
	{
		IInputElement inputElement = null;
		if (e == container || !IsGroup(e))
		{
			inputElement = GetFirstChild(e);
		}
		if (inputElement != null || e == container)
		{
			return inputElement;
		}
		IInputElement inputElement2 = e;
		do
		{
			IInputElement nextSibling = GetNextSibling(inputElement2);
			if (nextSibling != null)
			{
				return nextSibling;
			}
			inputElement2 = GetParent(inputElement2);
		}
		while (inputElement2 != null && inputElement2 != container);
		return null;
	}

	private static IInputElement? GetNextSibling(IInputElement e)
	{
		if (GetParent(e) is Visual parent && e is Visual visual)
		{
			List<IInputElement> list = FocusHelpers.GetInputElementChildren(parent).ToList();
			int count = list.Count;
			int i;
			for (i = 0; i < count && list[i] != visual; i++)
			{
			}
			for (i++; i < count; i++)
			{
				IInputElement inputElement = list[i];
				if (inputElement != null)
				{
					return inputElement;
				}
			}
		}
		return null;
	}

	private static IInputElement? GetNextTabInGroup(IInputElement? e, IInputElement container, KeyboardNavigationMode tabbingType)
	{
		if (tabbingType == KeyboardNavigationMode.None)
		{
			return null;
		}
		if (e == null || e == container)
		{
			return GetFirstTabInGroup(container);
		}
		if (tabbingType == KeyboardNavigationMode.Once)
		{
			return null;
		}
		IInputElement nextTabWithSameIndex = GetNextTabWithSameIndex(e, container);
		if (nextTabWithSameIndex != null)
		{
			return nextTabWithSameIndex;
		}
		return GetNextTabWithNextIndex(e, container, tabbingType);
	}

	private static IInputElement? GetNextTabWithSameIndex(IInputElement e, IInputElement container)
	{
		int tabIndex = KeyboardNavigation.GetTabIndex(e);
		IInputElement inputElement = e;
		while ((inputElement = GetNextInTree(inputElement, container)) != null)
		{
			if (IsTabStopOrGroup(inputElement) && KeyboardNavigation.GetTabIndex(inputElement) == tabIndex)
			{
				return inputElement;
			}
		}
		return null;
	}

	private static IInputElement? GetNextTabWithNextIndex(IInputElement e, IInputElement container, KeyboardNavigationMode tabbingType)
	{
		IInputElement inputElement = null;
		IInputElement inputElement2 = null;
		int num = int.MinValue;
		int num2 = int.MinValue;
		int tabIndex = KeyboardNavigation.GetTabIndex(e);
		IInputElement inputElement3 = container;
		while ((inputElement3 = GetNextInTree(inputElement3, container)) != null)
		{
			if (IsTabStopOrGroup(inputElement3))
			{
				int tabIndex2 = KeyboardNavigation.GetTabIndex(inputElement3);
				if (tabIndex2 > tabIndex && (tabIndex2 < num2 || inputElement == null))
				{
					num2 = tabIndex2;
					inputElement = inputElement3;
				}
				if (tabIndex2 < num || inputElement2 == null)
				{
					num = tabIndex2;
					inputElement2 = inputElement3;
				}
			}
		}
		if (tabbingType == KeyboardNavigationMode.Cycle && inputElement == null)
		{
			inputElement = inputElement2;
		}
		return inputElement;
	}

	private static IInputElement? GetPrevTabInGroup(IInputElement? e, IInputElement container, KeyboardNavigationMode tabbingType)
	{
		if (tabbingType == KeyboardNavigationMode.None)
		{
			return null;
		}
		if (e == null)
		{
			return GetLastTabInGroup(container);
		}
		if (tabbingType == KeyboardNavigationMode.Once)
		{
			return null;
		}
		if (e == container)
		{
			return null;
		}
		IInputElement prevTabWithSameIndex = GetPrevTabWithSameIndex(e, container);
		if (prevTabWithSameIndex != null)
		{
			return prevTabWithSameIndex;
		}
		return GetPrevTabWithPrevIndex(e, container, tabbingType);
	}

	private static IInputElement? GetPrevTabWithSameIndex(IInputElement e, IInputElement container)
	{
		int tabIndex = KeyboardNavigation.GetTabIndex(e);
		for (IInputElement previousInTree = GetPreviousInTree(e, container); previousInTree != null; previousInTree = GetPreviousInTree(previousInTree, container))
		{
			if (IsTabStopOrGroup(previousInTree) && KeyboardNavigation.GetTabIndex(previousInTree) == tabIndex && previousInTree != container)
			{
				return previousInTree;
			}
		}
		return null;
	}

	private static IInputElement? GetPrevTabWithPrevIndex(IInputElement e, IInputElement container, KeyboardNavigationMode tabbingType)
	{
		IInputElement inputElement = null;
		IInputElement inputElement2 = null;
		int tabIndex = KeyboardNavigation.GetTabIndex(e);
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		for (IInputElement inputElement3 = GetLastInTree(container); inputElement3 != null; inputElement3 = GetPreviousInTree(inputElement3, container))
		{
			if (IsTabStopOrGroup(inputElement3) && inputElement3 != container)
			{
				int tabIndex2 = KeyboardNavigation.GetTabIndex(inputElement3);
				if (tabIndex2 < tabIndex && (tabIndex2 > num2 || inputElement2 == null))
				{
					num2 = tabIndex2;
					inputElement2 = inputElement3;
				}
				if (tabIndex2 > num || inputElement == null)
				{
					num = tabIndex2;
					inputElement = inputElement3;
				}
			}
		}
		if (tabbingType == KeyboardNavigationMode.Cycle && inputElement2 == null)
		{
			inputElement2 = inputElement;
		}
		return inputElement2;
	}

	private static IInputElement? GetPreviousInTree(IInputElement e, IInputElement container)
	{
		if (e == container)
		{
			return null;
		}
		IInputElement previousSibling = GetPreviousSibling(e);
		if (previousSibling != null)
		{
			if (IsGroup(previousSibling))
			{
				return previousSibling;
			}
			return GetLastInTree(previousSibling);
		}
		return GetParent(e);
	}

	private static IInputElement? GetPreviousSibling(IInputElement e)
	{
		if (GetParent(e) is Visual parent && e is Visual visual)
		{
			List<IInputElement> list = FocusHelpers.GetInputElementChildren(parent).ToList();
			int count = list.Count;
			IInputElement result = null;
			for (int i = 0; i < count; i++)
			{
				IInputElement inputElement = list[i];
				if (inputElement == visual)
				{
					break;
				}
				if (inputElement != null)
				{
					IInputElement inputElement2 = inputElement;
					if (IsVisibleAndEnabled(inputElement2))
					{
						result = inputElement2;
					}
				}
			}
			return result;
		}
		return null;
	}

	private static IInputElement? GetActiveElement(IInputElement e)
	{
		return ((AvaloniaObject)e).GetValue(KeyboardNavigation.TabOnceActiveElementProperty);
	}

	private static IInputElement GetGroupParent(IInputElement e)
	{
		return GetGroupParent(e, includeCurrent: false);
	}

	private static IInputElement GetGroupParent(IInputElement element, bool includeCurrent)
	{
		IInputElement result = element;
		IInputElement inputElement = element;
		if (!includeCurrent)
		{
			result = inputElement;
			inputElement = GetParent(inputElement);
			if (inputElement == null)
			{
				return result;
			}
		}
		while (inputElement != null)
		{
			if (IsGroup(inputElement))
			{
				return inputElement;
			}
			result = inputElement;
			inputElement = GetParent(inputElement);
		}
		return result;
	}

	private static IInputElement? GetParent(IInputElement e)
	{
		if (e is Visual visual)
		{
			return visual.FindAncestorOfType<IInputElement>();
		}
		throw new NotSupportedException();
	}

	private static KeyboardNavigationMode GetKeyNavigationMode(IInputElement e)
	{
		return ((AvaloniaObject)e).GetValue(KeyboardNavigation.TabNavigationProperty);
	}

	private static bool IsFocusScope(IInputElement e)
	{
		if (!FocusManager.GetIsFocusScope(e))
		{
			return GetParent(e) == null;
		}
		return true;
	}

	private static bool IsGroup(IInputElement e)
	{
		return GetKeyNavigationMode(e) != KeyboardNavigationMode.Continue;
	}

	internal static bool IsTabStop(IInputElement e)
	{
		if (e is InputElement inputElement)
		{
			if (inputElement.Focusable && KeyboardNavigation.GetIsTabStop(inputElement) && inputElement.IsVisible)
			{
				return inputElement.IsEffectivelyEnabled;
			}
			return false;
		}
		return false;
	}

	private static bool IsTabStopOrGroup(IInputElement e)
	{
		if (!IsTabStop(e))
		{
			return IsGroup(e);
		}
		return true;
	}

	private static bool IsVisible(IInputElement e)
	{
		return (e as Visual)?.IsVisible ?? true;
	}

	private static bool IsVisibleAndEnabled(IInputElement e)
	{
		if (IsVisible(e))
		{
			return e.IsEffectivelyEnabled;
		}
		return false;
	}
}
