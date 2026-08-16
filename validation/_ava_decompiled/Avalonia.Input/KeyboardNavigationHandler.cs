using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Input.Navigation;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Input;

/// <summary>
/// Handles keyboard navigation for a window.
/// </summary>
internal sealed class KeyboardNavigationHandler : IKeyboardNavigationHandler
{
	/// <summary>
	/// The window to which the handler belongs.
	/// </summary>
	private InputElement? _owner;

	/// <summary>
	/// Sets the owner of the keyboard navigation handler.
	/// </summary>
	/// <param name="owner">The owner.</param>
	/// <remarks>
	/// This method can only be called once, typically by the owner itself on creation.
	/// </remarks>
	[PrivateApi]
	public void SetOwner(InputElement owner)
	{
		if (_owner != null)
		{
			throw new InvalidOperationException("KeyboardNavigationHandler owner has already been set.");
		}
		_owner = owner ?? throw new ArgumentNullException("owner");
		_owner.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
	}

	/// <summary>
	/// Gets the next control in the specified navigation direction.
	/// </summary>
	/// <param name="element">The element.</param>
	/// <param name="direction">The navigation direction.</param>
	/// <returns>
	/// The next element in the specified direction, or null if <paramref name="element" />
	/// was the last in the requested direction.
	/// </returns>
	public static IInputElement? GetNext(IInputElement element, NavigationDirection direction)
	{
		element = element ?? throw new ArgumentNullException("element");
		return GetNextPrivate(element, null, direction, null);
	}

	private static IInputElement? GetNextPrivate(IInputElement? element, InputElement? owner, NavigationDirection direction, KeyDeviceType? keyDeviceType)
	{
		IInputElement inputElement = element ?? owner ?? throw new ArgumentNullException("owner");
		ICustomKeyboardNavigation customKeyboardNavigation = (element as Visual)?.FindAncestorOfType<ICustomKeyboardNavigation>(includeSelf: true);
		if (customKeyboardNavigation != null && HandlePreCustomNavigation(customKeyboardNavigation, inputElement, direction, out IInputElement result))
		{
			return result;
		}
		IInputElement inputElement2;
		if (direction == NavigationDirection.Next)
		{
			inputElement2 = TabNavigation.GetNextTab(inputElement, goDownOnly: false);
		}
		else if (direction == NavigationDirection.Previous)
		{
			inputElement2 = TabNavigation.GetPrevTab(inputElement, null, goDownOnly: false);
		}
		else
		{
			if ((uint)(direction - 4) > 3u)
			{
				throw new ArgumentOutOfRangeException("direction", direction, null);
			}
			IInputElement? inputElement4;
			if (element != null)
			{
				IInputElement inputElement3 = XYFocus.TryDirectionalFocus(direction, element, owner, null, keyDeviceType);
				inputElement4 = inputElement3;
			}
			else
			{
				inputElement4 = TabNavigation.GetNextTab(inputElement, goDownOnly: true);
			}
			inputElement2 = inputElement4;
		}
		if (customKeyboardNavigation == null && HandlePostCustomNavigation(inputElement, inputElement2, direction, out result))
		{
			return result;
		}
		return inputElement2;
	}

	/// <inheritdoc />
	public bool Move(IInputElement? element, NavigationDirection direction, KeyModifiers keyModifiers = KeyModifiers.None, KeyDeviceType? deviceType = null)
	{
		IInputElement nextPrivate = GetNextPrivate(element, _owner, direction, deviceType);
		if (nextPrivate != null)
		{
			NavigationMethod method = ((direction == NavigationDirection.Next || direction == NavigationDirection.Previous) ? NavigationMethod.Tab : NavigationMethod.Directional);
			return nextPrivate.Focus(method, keyModifiers);
		}
		return false;
	}

	/// <summary>
	/// Handles the Tab key being pressed in the window.
	/// </summary>
	/// <param name="sender">The event sender.</param>
	/// <param name="e">The event args.</param>
	private void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Tab)
		{
			IInputElement element = FocusManager.GetFocusManager(e.Source as IInputElement)?.GetFocusedElement();
			NavigationDirection direction = (((e.KeyModifiers & KeyModifiers.Shift) != KeyModifiers.None) ? NavigationDirection.Previous : NavigationDirection.Next);
			e.Handled = Move(element, direction, e.KeyModifiers, e.KeyDeviceType);
			return;
		}
		Key key = e.Key;
		if ((uint)(key - 23) <= 3u)
		{
			IInputElement element2 = FocusManager.GetFocusManager(e.Source as IInputElement)?.GetFocusedElement();
			e.Handled = Move(element2, e.Key switch
			{
				Key.Left => NavigationDirection.Left, 
				Key.Right => NavigationDirection.Right, 
				Key.Up => NavigationDirection.Up, 
				Key.Down => NavigationDirection.Down, 
				_ => throw new ArgumentOutOfRangeException(), 
			}, e.KeyModifiers, e.KeyDeviceType);
		}
	}

	private static bool HandlePreCustomNavigation(ICustomKeyboardNavigation customHandler, IInputElement element, NavigationDirection direction, [NotNullWhen(true)] out IInputElement? result)
	{
		var (flag, inputElement) = customHandler.GetNext(element, direction);
		if (flag)
		{
			if (inputElement != null)
			{
				result = inputElement;
				return true;
			}
			IInputElement inputElement2 = direction switch
			{
				NavigationDirection.Next => TabNavigation.GetNextTabOutside(customHandler), 
				NavigationDirection.Previous => TabNavigation.GetPrevTabOutside(customHandler), 
				_ => null, 
			};
			if (inputElement2 != null)
			{
				result = inputElement2;
				return true;
			}
		}
		result = null;
		return false;
	}

	private static bool HandlePostCustomNavigation(IInputElement element, IInputElement? newElement, NavigationDirection direction, [NotNullWhen(true)] out IInputElement? result)
	{
		if (newElement is Visual visual)
		{
			ICustomKeyboardNavigation customKeyboardNavigation = visual.FindAncestorOfType<ICustomKeyboardNavigation>(includeSelf: true);
			if (customKeyboardNavigation != null)
			{
				var (flag, inputElement) = customKeyboardNavigation.GetNext(element, direction);
				if (flag && inputElement != null)
				{
					result = inputElement;
					return true;
				}
			}
		}
		result = null;
		return false;
	}
}
