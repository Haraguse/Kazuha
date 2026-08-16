using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Input;

/// <summary>
/// Handles access keys for a window.
/// </summary>
internal class AccessKeyHandler : IAccessKeyHandler
{
	private enum ProcessKeyResult
	{
		NoMatch,
		MoreMatches,
		LastMatch
	}

	private struct AccessKeyInformation
	{
		public IInputElement? Target { get; set; }
	}

	/// <summary>
	/// Defines the AccessKey attached event.
	/// </summary>
	public static readonly RoutedEvent<AccessKeyEventArgs> AccessKeyEvent = RoutedEvent.Register<AccessKeyEventArgs>("AccessKey", RoutingStrategies.Bubble, typeof(AccessKeyHandler));

	/// <summary>
	/// Defines the AccessKeyPressed attached event.
	/// </summary>
	public static readonly RoutedEvent<AccessKeyPressedEventArgs> AccessKeyPressedEvent = RoutedEvent.Register<AccessKeyPressedEventArgs>("AccessKeyPressed", RoutingStrategies.Bubble, typeof(AccessKeyHandler));

	/// <summary>
	/// Defines the ShowAccessKey attached property.
	/// </summary>
	public static readonly AttachedProperty<bool> ShowAccessKeyProperty = AvaloniaProperty.RegisterAttached<AccessKeyHandler, Visual, bool>("ShowAccessKey", defaultValue: false, inherits: true);

	/// <summary>
	/// The registered access keys.
	/// </summary>
	private readonly List<AccessKeyRegistration> _registrations = new List<AccessKeyRegistration>();

	/// <summary>
	/// The window to which the handler belongs.
	/// </summary>
	private InputElement? _owner;

	/// <summary>
	/// Whether access keys are currently being shown;
	/// </summary>
	private bool _showingAccessKeys;

	/// <summary>
	/// Whether to ignore the Alt KeyUp event.
	/// </summary>
	private bool _ignoreAltUp;

	/// <summary>
	/// Whether the AltKey is down.
	/// </summary>
	private bool _altIsDown;

	/// <summary>
	/// Element to restore following AltKey taking focus.
	/// </summary>
	private WeakReference<IInputElement>? _restoreFocusElementRef;

	/// <summary>
	/// The window's main menu.
	/// </summary>
	private IMainMenu? _mainMenu;

	protected IReadOnlyList<AccessKeyRegistration> Registrations => _registrations;

	/// <summary>
	/// Gets or sets the window's main menu.
	/// </summary>
	public IMainMenu? MainMenu
	{
		get
		{
			return _mainMenu;
		}
		set
		{
			if (_mainMenu != null)
			{
				_mainMenu.Closed -= MainMenuClosed;
			}
			_mainMenu = value;
			if (_mainMenu != null)
			{
				_mainMenu.Closed += MainMenuClosed;
			}
		}
	}

	/// <summary>
	/// Sets the owner of the access key handler.
	/// </summary>
	/// <param name="owner">The owner.</param>
	/// <remarks>
	/// This method can only be called once, typically by the owner itself on creation.
	/// </remarks>
	public void SetOwner(InputElement owner)
	{
		if (_owner != null)
		{
			throw new InvalidOperationException("AccessKeyHandler owner has already been set.");
		}
		_owner = owner ?? throw new ArgumentNullException("owner");
		_owner.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
		_owner.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
		_owner.AddHandler(InputElement.KeyUpEvent, OnPreviewKeyUp, RoutingStrategies.Tunnel);
		_owner.AddHandler(InputElement.PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
		OnSetOwner(owner);
	}

	protected virtual void OnSetOwner(InputElement owner)
	{
	}

	/// <summary>
	/// Registers an input element to be associated with an access key.
	/// </summary>
	/// <param name="accessKey">The access key.</param>
	/// <param name="element">The input element.</param>
	public void Register(string accessKey, IInputElement element)
	{
		ArgumentException.ThrowIfNullOrEmpty(accessKey, "accessKey");
		string text = NormalizeKey(accessKey);
		for (int num = _registrations.Count - 1; num >= 0; num--)
		{
			AccessKeyRegistration accessKeyRegistration = _registrations[num];
			if (accessKeyRegistration.Key == text && accessKeyRegistration.GetInputElement() == null)
			{
				_registrations.RemoveAt(num);
			}
		}
		_registrations.Add(new AccessKeyRegistration(text, new WeakReference<IInputElement>(element)));
	}

	/// <summary>
	/// Unregisters the access keys associated with the input element.
	/// </summary>
	/// <param name="element">The input element.</param>
	public void Unregister(IInputElement element)
	{
		for (int num = _registrations.Count - 1; num >= 0; num--)
		{
			IInputElement inputElement = _registrations[num].GetInputElement();
			if (inputElement == null || inputElement == element)
			{
				_registrations.RemoveAt(num);
			}
		}
	}

	private static void SetShowAccessKeys(AvaloniaObject target, bool value)
	{
		target.SetValue(ShowAccessKeyProperty, value);
	}

	/// <summary>
	/// Called when a key is pressed in the owner window.
	/// </summary>
	/// <param name="sender">The event sender.</param>
	/// <param name="e">The event args.</param>
	protected virtual void OnPreviewKeyDown(object? sender, KeyEventArgs e)
	{
		bool flag = IsFocusWithinOwner(_owner);
		if (!flag)
		{
			return;
		}
		Key key = e.Key;
		if ((uint)(key - 120) <= 1u)
		{
			_altIsDown = true;
			IMainMenu mainMenu = MainMenu;
			if (mainMenu == null || !mainMenu.IsOpen)
			{
				IInputElement inputElement = FocusManager.GetFocusManager(e.Source as IInputElement)?.GetFocusedElement();
				if (inputElement != null)
				{
					_restoreFocusElementRef = new WeakReference<IInputElement>(inputElement);
				}
				SetShowAccessKeys(_owner, _showingAccessKeys = flag);
				return;
			}
			CloseMenu();
			_ignoreAltUp = true;
			WeakReference<IInputElement>? restoreFocusElementRef = _restoreFocusElementRef;
			if (restoreFocusElementRef != null && restoreFocusElementRef.TryGetTarget(out IInputElement target))
			{
				target.Focus();
			}
			_restoreFocusElementRef = null;
		}
		else if (_altIsDown)
		{
			_ignoreAltUp = true;
		}
	}

	/// <summary>
	/// Called when a key is pressed in the owner window.
	/// </summary>
	/// <param name="sender">The event sender.</param>
	/// <param name="e">The event args.</param>
	protected virtual void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (!IsFocusWithinOwner(_owner))
		{
			return;
		}
		if (!e.KeyModifiers.HasAllFlags(KeyModifiers.Alt) || e.KeyModifiers.HasAllFlags(KeyModifiers.Control))
		{
			IMainMenu? mainMenu = MainMenu;
			if (mainMenu == null || !mainMenu.IsOpen)
			{
				return;
			}
		}
		e.Handled = ProcessKey(e.KeySymbol, e.Source as IInputElement);
	}

	/// <summary>
	/// Handles the Alt/F10 keys being released in the window.
	/// </summary>
	/// <param name="sender">The event sender.</param>
	/// <param name="e">The event args.</param>
	protected virtual void OnPreviewKeyUp(object? sender, KeyEventArgs e)
	{
		Key key = e.Key;
		if ((uint)(key - 120) <= 1u)
		{
			_altIsDown = false;
			if (_ignoreAltUp)
			{
				_ignoreAltUp = false;
			}
			else if (_showingAccessKeys && MainMenu != null)
			{
				MainMenu.Open();
			}
		}
	}

	/// <summary>
	/// Handles pointer presses in the window.
	/// </summary>
	/// <param name="sender">The event sender.</param>
	/// <param name="e">The event args.</param>
	protected virtual void OnPreviewPointerPressed(object? sender, PointerEventArgs e)
	{
		if (_showingAccessKeys)
		{
			SetShowAccessKeys(_owner, value: false);
		}
	}

	/// <summary>
	/// Closes the <see cref="P:Avalonia.Input.AccessKeyHandler.MainMenu" /> and performs other bookeeping.
	/// </summary>
	private void CloseMenu()
	{
		MainMenu.Close();
		SetShowAccessKeys(_owner, _showingAccessKeys = false);
	}

	private void MainMenuClosed(object? sender, EventArgs e)
	{
		SetShowAccessKeys(_owner, value: false);
	}

	/// <summary>
	/// Processes the given key for the element's targets 
	/// </summary>
	/// <param name="key">The access key to process.</param>
	/// <param name="element">The element to get the targets which are in scope.</param>
	/// <returns>If there matches <c>true</c>, otherwise <c>false</c>.</returns>
	protected bool ProcessKey(string? key, IInputElement? element)
	{
		if (string.IsNullOrEmpty(key))
		{
			return false;
		}
		key = NormalizeKey(key);
		AccessKeyInformation targetForElement = GetTargetForElement(element, key);
		List<IInputElement> targets = SortByHierarchy(GetTargetsForKey(key, element, targetForElement));
		return ProcessKey(key, targets) != ProcessKeyResult.NoMatch;
	}

	private static string NormalizeKey(string key)
	{
		return key.ToUpperInvariant();
	}

	private static ProcessKeyResult ProcessKey(string key, List<IInputElement> targets)
	{
		if (!targets.Any())
		{
			return ProcessKeyResult.NoMatch;
		}
		bool flag = true;
		bool flag2 = false;
		IInputElement inputElement = null;
		int num = 0;
		for (int i = 0; i < targets.Count; i++)
		{
			IInputElement inputElement2 = targets[i];
			if (!IsTargetable(inputElement2))
			{
				continue;
			}
			if (inputElement == null)
			{
				inputElement = inputElement2;
				num = i;
			}
			else
			{
				if (flag2)
				{
					inputElement = inputElement2;
					num = i;
				}
				flag = false;
			}
			flag2 = inputElement2.IsFocused;
		}
		if (inputElement == null)
		{
			return ProcessKeyResult.NoMatch;
		}
		AccessKeyEventArgs e = new AccessKeyEventArgs(key, !flag);
		inputElement.RaiseEvent(e);
		if (num != targets.Count - 1)
		{
			return ProcessKeyResult.MoreMatches;
		}
		return ProcessKeyResult.LastMatch;
	}

	private List<IInputElement> GetTargetsForKey(string key, IInputElement? sender, AccessKeyInformation senderInfo)
	{
		List<IInputElement> list = CopyMatchingAndPurgeDead(key);
		if (!list.Any())
		{
			return list;
		}
		List<IInputElement> list2 = new List<IInputElement>(1);
		foreach (IInputElement item in list)
		{
			if (item != sender)
			{
				if (IsTargetable(item))
				{
					AccessKeyInformation targetForElement = GetTargetForElement(item, key);
					if (targetForElement.Target != null)
					{
						list2.Add(targetForElement.Target);
					}
				}
			}
			else if (senderInfo.Target != null)
			{
				list2.Add(senderInfo.Target);
			}
		}
		return list2;
	}

	private static bool IsTargetable(IInputElement element)
	{
		if (element != null && element.IsEffectivelyEnabled)
		{
			return element.IsEffectivelyVisible;
		}
		return false;
	}

	private List<IInputElement> CopyMatchingAndPurgeDead(string key)
	{
		List<IInputElement> list = new List<IInputElement>(_registrations.Count);
		for (int num = _registrations.Count - 1; num >= 0; num--)
		{
			AccessKeyRegistration accessKeyRegistration = _registrations[num];
			IInputElement inputElement = accessKeyRegistration.GetInputElement();
			if (inputElement != null)
			{
				if (accessKeyRegistration.Key == key)
				{
					list.Add(inputElement);
				}
			}
			else
			{
				_registrations.RemoveAt(num);
			}
		}
		list.Reverse();
		return list;
	}

	/// <summary>
	/// Returns targeting information for the given element.
	/// </summary>
	/// <param name="element"></param>
	/// <param name="key"></param>
	/// <returns>AccessKeyInformation with target for the access key.</returns>
	private static AccessKeyInformation GetTargetForElement(IInputElement? element, string key)
	{
		AccessKeyInformation result = default(AccessKeyInformation);
		if (element == null)
		{
			return result;
		}
		AccessKeyPressedEventArgs e = new AccessKeyPressedEventArgs(key);
		element.RaiseEvent(e);
		result.Target = e.Target;
		return result;
	}

	/// <summary>
	/// Checks if the focused element is a descendent of the owner.
	/// </summary>
	/// <param name="owner">The owner to check.</param>
	/// <returns>If focused element is decendant of owner <c>true</c>, otherwise <c>false</c>. </returns>
	private static bool IsFocusWithinOwner(IInputElement owner)
	{
		if (!(KeyboardDevice.Instance?.FocusedElement is InputElement target))
		{
			return false;
		}
		if (owner is Visual visual)
		{
			return visual.IsVisualAncestorOf(target);
		}
		return false;
	}

	/// <summary>
	/// Sorts the list of targets according to logical ancestors in the hierarchy
	/// so that child elements, for example within in the content of a tab,
	/// are processed before the next parent item i.e. the next tab item.
	/// </summary>
	private static List<IInputElement> SortByHierarchy(List<IInputElement> targets)
	{
		if (targets.Count <= 1)
		{
			return targets;
		}
		List<IInputElement> list = new List<IInputElement>(targets.Count);
		Queue<IInputElement> queue = new Queue<IInputElement>(targets);
		while (queue.Count > 0)
		{
			IInputElement inputElement = queue.Dequeue();
			if (list.Contains(inputElement))
			{
				continue;
			}
			list.Add(inputElement);
			ILogical parentElement = inputElement as ILogical;
			if (parentElement != null)
			{
				list.AddRange(queue.Where((IInputElement child) => parentElement.IsLogicalAncestorOf(child as ILogical)));
			}
		}
		return list;
	}
}
