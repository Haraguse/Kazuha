using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Avalonia.Input;

[PrivateApi]
public class KeyboardDevice : IKeyboardDevice, IInputDevice, INotifyPropertyChanged
{
	private IInputElement? _focusedElement;

	private IInputRoot? _focusedRoot;

	private readonly TextInputMethodManager _textInputManager = new TextInputMethodManager();

	internal static KeyboardDevice? Instance => AvaloniaLocator.Current.GetService<IKeyboardDevice>() as KeyboardDevice;

	public IInputManager? InputManager => AvaloniaLocator.Current.GetService<IInputManager>();

	public IFocusManager? FocusManager => AvaloniaLocator.Current.GetService<IFocusManager>();

	public IInputElement? FocusedElement => _focusedElement;

	public event PropertyChangedEventHandler? PropertyChanged;

	private static void ClearFocusWithinAncestors(IInputElement? element)
	{
		for (IInputElement inputElement = element; inputElement != null; inputElement = (IInputElement)((inputElement as Visual)?.VisualParent))
		{
			if (inputElement is InputElement inputElement2)
			{
				inputElement2.IsKeyboardFocusWithin = false;
			}
		}
	}

	private void ClearFocusWithin(IInputElement element, bool clearRoot)
	{
		if (element is Visual visual)
		{
			foreach (Visual visualChild in visual.VisualChildren)
			{
				if (visualChild is IInputElement { IsKeyboardFocusWithin: not false } inputElement)
				{
					ClearFocusWithin(inputElement, clearRoot: true);
					break;
				}
			}
		}
		if (clearRoot && element is InputElement inputElement2)
		{
			inputElement2.IsKeyboardFocusWithin = false;
		}
	}

	private void SetIsFocusWithin(IInputElement? oldElement, IInputElement? newElement)
	{
		if (newElement == null && oldElement != null)
		{
			ClearFocusWithinAncestors(oldElement);
			return;
		}
		IInputElement inputElement = null;
		IInputElement inputElement2;
		for (inputElement2 = newElement; inputElement2 != null; inputElement2 = (inputElement2 as Visual)?.VisualParent as IInputElement)
		{
			if (inputElement2.IsKeyboardFocusWithin)
			{
				inputElement = inputElement2;
				break;
			}
		}
		inputElement2 = oldElement;
		if (inputElement2 != null && inputElement != null)
		{
			ClearFocusWithin(inputElement, clearRoot: false);
		}
		inputElement2 = newElement;
		while (inputElement2 != null && inputElement2 != inputElement)
		{
			if (inputElement2 is InputElement inputElement3)
			{
				inputElement3.IsKeyboardFocusWithin = true;
			}
			inputElement2 = (inputElement2 as Visual)?.VisualParent as IInputElement;
		}
	}

	private void ClearChildrenFocusWithin(IInputElement element, bool clearRoot)
	{
		if (element is Visual visual)
		{
			foreach (Visual visualChild in visual.VisualChildren)
			{
				if (visualChild is IInputElement { IsKeyboardFocusWithin: not false } inputElement)
				{
					ClearChildrenFocusWithin(inputElement, clearRoot: true);
					break;
				}
			}
		}
		if (clearRoot && element is InputElement inputElement2)
		{
			inputElement2.IsKeyboardFocusWithin = false;
		}
	}

	public void SetFocusedElement(IInputElement? element, NavigationMethod method, KeyModifiers keyModifiers)
	{
		SetFocusedElement(element, method, keyModifiers, isFocusChangeCancellable: true);
	}

	public void SetFocusedElement(IInputElement? element, NavigationMethod method, KeyModifiers keyModifiers, bool isFocusChangeCancellable)
	{
		if (element == FocusedElement)
		{
			return;
		}
		Interactive interactive = FocusedElement as Interactive;
		bool flag = true;
		FocusChangingEventArgs e = new FocusChangingEventArgs(InputElement.LosingFocusEvent)
		{
			OldFocusedElement = FocusedElement,
			NewFocusedElement = element,
			NavigationMethod = method,
			KeyModifiers = keyModifiers,
			CanCancelOrRedirectFocus = isFocusChangeCancellable
		};
		interactive?.RaiseEvent(e);
		if (e.Canceled)
		{
			flag = false;
		}
		if (flag && e.NewFocusedElement is Interactive interactive2)
		{
			FocusChangingEventArgs e2 = new FocusChangingEventArgs(InputElement.GettingFocusEvent)
			{
				OldFocusedElement = FocusedElement,
				NewFocusedElement = e.NewFocusedElement,
				NavigationMethod = method,
				KeyModifiers = keyModifiers,
				CanCancelOrRedirectFocus = isFocusChangeCancellable
			};
			interactive2.RaiseEvent(e2);
			if (e2.Canceled)
			{
				flag = false;
			}
			element = e2.NewFocusedElement;
		}
		if (flag)
		{
			IInputElement focusedElement = FocusedElement;
			if (focusedElement != null && (!((Visual)focusedElement).IsAttachedToVisualTree || _focusedRoot != ((Visual)element)?.GetInputRoot()) && _focusedRoot != null)
			{
				ClearChildrenFocusWithin(_focusedRoot.RootElement, clearRoot: true);
			}
			SetIsFocusWithin(focusedElement, element);
			_focusedElement = element;
			_focusedRoot = (_focusedElement as Visual)?.GetInputRoot();
			interactive?.RaiseEvent(new FocusChangedEventArgs(InputElement.LostFocusEvent)
			{
				OldFocusedElement = focusedElement,
				NewFocusedElement = element,
				NavigationMethod = method,
				KeyModifiers = keyModifiers
			});
			(element as Interactive)?.RaiseEvent(new FocusChangedEventArgs(InputElement.GotFocusEvent)
			{
				OldFocusedElement = focusedElement,
				NewFocusedElement = element,
				NavigationMethod = method,
				KeyModifiers = keyModifiers
			});
			_textInputManager.SetFocusedElement(element);
			RaisePropertyChanged("FocusedElement");
		}
	}

	protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public void ProcessRawEvent(RawInputEventArgs e)
	{
		if (e.Handled)
		{
			return;
		}
		IInputElement inputElement = FocusedElement ?? e.Root.FocusRoot;
		if (e is RawKeyEventArgs { Type: var type } e2 && (uint)type <= 1u)
		{
			RoutedEvent<KeyEventArgs> routedEvent = ((e2.Type == RawKeyEventType.KeyDown) ? InputElement.KeyDownEvent : InputElement.KeyUpEvent);
			KeyEventArgs e3 = new KeyEventArgs
			{
				RoutedEvent = routedEvent,
				Key = e2.Key,
				KeyModifiers = e2.Modifiers.ToKeyModifiers(),
				PhysicalKey = e2.PhysicalKey,
				KeySymbol = e2.KeySymbol,
				KeyDeviceType = e2.KeyDeviceType,
				Source = inputElement
			};
			Visual visual = inputElement as Visual;
			while (visual != null && !e3.Handled && e2.Type == RawKeyEventType.KeyDown)
			{
				List<KeyBinding> list = (visual as IInputElement)?.KeyBindings;
				if (list != null)
				{
					KeyBinding[] array = null;
					foreach (KeyBinding item in list)
					{
						KeyGesture gesture = item.Gesture;
						if ((object)gesture != null && gesture.Matches(e3))
						{
							array = list.ToArray();
							break;
						}
					}
					if (array != null)
					{
						KeyBinding[] array2 = array;
						foreach (KeyBinding keyBinding in array2)
						{
							if (e3.Handled)
							{
								break;
							}
							keyBinding.TryHandle(e3);
						}
					}
				}
				visual = visual.VisualParent;
			}
			inputElement.RaiseEvent(e3);
			e.Handled = e3.Handled;
		}
		if (e is RawTextInputEventArgs e4)
		{
			TextInputEventArgs e5 = new TextInputEventArgs
			{
				Text = e4.Text,
				Source = inputElement,
				RoutedEvent = InputElement.TextInputEvent
			};
			inputElement.RaiseEvent(e5);
			e.Handled = e5.Handled;
		}
	}
}
