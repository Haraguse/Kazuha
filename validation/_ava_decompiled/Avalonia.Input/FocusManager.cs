using System;
using System.Linq;
using Avalonia.Input.Navigation;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Input;

/// <summary>
/// Manages focus for the application.
/// </summary>
public class FocusManager : IFocusManager
{
	/// <summary>
	/// Private attached property for storing the currently focused element in a focus scope.
	/// </summary>
	/// <remarks>
	/// This property is set on the control which defines a focus scope and tracks the currently
	/// focused element within that scope.
	/// </remarks>
	private static readonly AttachedProperty<IInputElement> FocusedElementProperty;

	private StyledElement? _focusRoot;

	private readonly XYFocus _xyFocus = new XYFocus();

	private IInputElement? _contentRoot;

	private XYFocusOptions? _reusableFocusOptions;

	/// <summary>
	/// Gets or sets the content root for the focus management system.
	/// </summary>
	[PrivateApi]
	public IInputElement? ContentRoot
	{
		get
		{
			return _contentRoot;
		}
		set
		{
			_contentRoot = value;
		}
	}

	private IInputElement? Current => KeyboardDevice.Instance?.FocusedElement;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Input.FocusManager" /> class.
	/// </summary>
	static FocusManager()
	{
		FocusedElementProperty = AvaloniaProperty.RegisterAttached<FocusManager, StyledElement, IInputElement>("FocusedElement");
		InputElement.PointerPressedEvent.AddClassHandler(typeof(IInputElement), OnPreviewPointerEventHandler, RoutingStrategies.Tunnel);
		InputElement.PointerReleasedEvent.AddClassHandler(typeof(IInputElement), OnPreviewPointerEventHandler, RoutingStrategies.Tunnel);
	}

	[PrivateApi]
	public FocusManager()
	{
	}

	/// <inheritdoc />
	public IInputElement? GetFocusedElement()
	{
		return Current;
	}

	/// <inheritdoc />
	public bool Focus(IInputElement? element, NavigationMethod method = NavigationMethod.Unspecified, KeyModifiers keyModifiers = KeyModifiers.None)
	{
		KeyboardDevice instance = KeyboardDevice.Instance;
		if (instance == null)
		{
			return false;
		}
		if (element != null)
		{
			return FocusCore(instance, element, method, keyModifiers);
		}
		IInputElement inputElement = _focusRoot?.GetValue(FocusedElementProperty);
		if (inputElement != null && inputElement != Current)
		{
			return FocusCore(instance, inputElement, method, keyModifiers);
		}
		_focusRoot = null;
		instance.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None, isFocusChangeCancellable: false);
		return false;
	}

	private bool FocusCore(KeyboardDevice keyboardDevice, IInputElement element, NavigationMethod method, KeyModifiers keyModifiers)
	{
		if (!CanFocus(element))
		{
			return false;
		}
		keyboardDevice.SetFocusedElement(element, method, keyModifiers);
		IInputElement focusedElement = keyboardDevice.FocusedElement;
		if (focusedElement != null)
		{
			StyledElement focusScope = GetFocusScope(focusedElement);
			if (focusScope != null)
			{
				focusScope.SetValue(FocusedElementProperty, focusedElement);
				_focusRoot = GetFocusRoot(focusScope);
			}
			return focusedElement == element;
		}
		_focusRoot = null;
		keyboardDevice.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None, isFocusChangeCancellable: false);
		return false;
	}

	internal void ClearFocusOnElementRemoved(IInputElement removedElement, Visual oldParent)
	{
		if (oldParent is IInputElement control)
		{
			StyledElement focusScope = GetFocusScope(control);
			if (focusScope != null)
			{
				IInputElement value = focusScope.GetValue(FocusedElementProperty);
				if (value != null && value == removedElement)
				{
					focusScope.ClearValue(FocusedElementProperty);
				}
			}
		}
		if (Current == removedElement)
		{
			Focus(null);
		}
	}

	[PrivateApi]
	public IInputElement? GetFocusedElement(IFocusScope scope)
	{
		return (scope as StyledElement)?.GetValue(FocusedElementProperty);
	}

	/// <summary>
	/// Notifies the focus manager of a change in focus scope.
	/// </summary>
	/// <param name="scope">The new focus scope.</param>
	[PrivateApi]
	public void SetFocusScope(IFocusScope scope)
	{
		KeyboardDevice instance = KeyboardDevice.Instance;
		if (instance != null)
		{
			IInputElement focusedElement = GetFocusedElement(scope);
			if (focusedElement != null)
			{
				Focus(focusedElement);
				return;
			}
			if (scope is IInputElement inputElement && CanFocus(inputElement))
			{
				Focus(inputElement);
				return;
			}
			_focusRoot = scope as StyledElement;
			instance.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None, isFocusChangeCancellable: false);
		}
	}

	[PrivateApi]
	public void RemoveFocusRoot(IFocusScope scope)
	{
		if (scope == _focusRoot)
		{
			Focus(null);
		}
	}

	[PrivateApi]
	public static bool GetIsFocusScope(IInputElement e)
	{
		return e is IFocusScope;
	}

	/// <summary>
	/// Public API customers should use TopLevel.GetTopLevel(control).FocusManager.
	/// But since we have split projects, we can't access TopLevel from Avalonia.Base.
	/// That's why we need this helper method instead.
	/// </summary>
	internal static FocusManager? GetFocusManager(IInputElement? element)
	{
		return ((FocusManager)((element as Visual)?.GetInputRoot()?.FocusManager)) ?? ((FocusManager)AvaloniaLocator.Current.GetService<IFocusManager>());
	}

	/// <inheritdoc />
	public bool TryMoveFocus(NavigationDirection direction, FindNextElementOptions? options = null)
	{
		ValidateDirection(direction);
		XYFocusOptions xYFocusOptions = ToFocusOptions(options, updateManifold: true);
		bool result = FindAndSetNextFocus(options?.FocusedElement ?? Current, direction, xYFocusOptions);
		_reusableFocusOptions = xYFocusOptions;
		return result;
	}

	/// <summary>
	/// Checks if the specified element can be focused.
	/// </summary>
	/// <param name="e">The element.</param>
	/// <returns>True if the element can be focused.</returns>
	internal static bool CanFocus(IInputElement e)
	{
		if (e.Focusable && e.IsEffectivelyEnabled)
		{
			return IsVisible(e);
		}
		return false;
	}

	private static bool CanPointerFocus(IInputElement e, PointerEventArgs ev)
	{
		if (CanFocus(e))
		{
			if (!(ev is PointerReleasedEventArgs e2))
			{
				if (ev is PointerPressedEventArgs e3 && e3.Pointer.Type == PointerType.Mouse)
				{
					return true;
				}
			}
			else if (e2.Pointer.Type != PointerType.Mouse)
			{
				return true;
			}
			return false;
		}
		return false;
	}

	/// <summary>
	/// Gets the focus scope of the specified control, traversing popups.
	/// </summary>
	/// <param name="control">The control.</param>
	/// <returns>The focus scope.</returns>
	private static StyledElement? GetFocusScope(IInputElement control)
	{
		for (IInputElement inputElement = control; inputElement != null; inputElement = (inputElement as Visual)?.GetVisualParent<IInputElement>() ?? ((inputElement as IHostedVisualTreeRoot)?.Host as IInputElement))
		{
			if (inputElement is IFocusScope && inputElement is Visual visual)
			{
				Visual visualRoot = visual.VisualRoot;
				if (visualRoot != null && visualRoot.IsVisible)
				{
					return visual;
				}
			}
		}
		return null;
	}

	private static StyledElement? GetFocusRoot(StyledElement scope)
	{
		if (!(scope is Visual visual))
		{
			return null;
		}
		Visual visual2 = visual.PresentationSource?.InputRoot.FocusRoot;
		while (visual2 is IHostedVisualTreeRoot hostedVisualTreeRoot)
		{
			InputElement inputElement = hostedVisualTreeRoot.Host?.PresentationSource?.InputRoot.FocusRoot;
			if (inputElement == null)
			{
				break;
			}
			visual2 = inputElement;
		}
		return visual2;
	}

	/// <summary>
	/// Global handler for pointer pressed events.
	/// </summary>
	/// <param name="sender">The event sender.</param>
	/// <param name="e">The event args.</param>
	private static void OnPreviewPointerEventHandler(object? sender, RoutedEventArgs e)
	{
		if (sender == null)
		{
			return;
		}
		PointerEventArgs e2 = (PointerEventArgs)e;
		Visual relativeTo = (Visual)sender;
		if (sender != e.Source)
		{
			return;
		}
		if (!e2.GetCurrentPoint(relativeTo).Properties.IsLeftButtonPressed)
		{
			PointerReleasedEventArgs obj = e as PointerReleasedEventArgs;
			if (obj == null || obj.InitialPressMouseButton != MouseButton.Left)
			{
				return;
			}
		}
		for (Visual visual = (e2.Pointer?.Captured as Visual) ?? (e.Source as Visual); visual != null; visual = visual.VisualParent)
		{
			if (visual is IInputElement inputElement && CanPointerFocus(inputElement, e2))
			{
				inputElement.Focus(NavigationMethod.Pointer, e2.KeyModifiers);
				break;
			}
		}
	}

	private static bool IsVisible(IInputElement e)
	{
		if (e is Visual visual)
		{
			if (visual.IsAttachedToVisualTree)
			{
				return e.IsEffectivelyVisible;
			}
			return false;
		}
		return true;
	}

	/// <inheritdoc />
	public IInputElement? FindFirstFocusableElement()
	{
		if (!((_contentRoot as Visual)?.GetSelfAndVisualDescendants().FirstOrDefault((Visual x) => x is IInputElement) is IInputElement))
		{
			return null;
		}
		return GetFirstFocusableElementFromRoot(isReverse: false);
	}

	/// <summary>
	/// Retrieves the first element that can receive focus based on the specified scope.
	/// </summary>
	/// <param name="searchScope">The root element from which to search.</param>
	/// <returns>The first focusable element.</returns>
	public static IInputElement? FindFirstFocusableElement(IInputElement searchScope)
	{
		return GetFirstFocusableElement(searchScope);
	}

	/// <inheritdoc />
	public IInputElement? FindLastFocusableElement()
	{
		if (!((_contentRoot as Visual)?.GetSelfAndVisualDescendants().FirstOrDefault((Visual x) => x is IInputElement) is IInputElement))
		{
			return null;
		}
		return GetFirstFocusableElementFromRoot(isReverse: true);
	}

	/// <summary>
	/// Retrieves the last element that can receive focus based on the specified scope.
	/// </summary>
	/// <param name="searchScope">The root element from which to search.</param>
	/// <returns>The last focusable object.</returns>
	public static IInputElement? FindLastFocusableElement(IInputElement searchScope)
	{
		return GetFocusManager(searchScope)?.GetLastFocusableElement(searchScope);
	}

	/// <inheritdoc />
	public IInputElement? FindNextElement(NavigationDirection direction, FindNextElementOptions? options = null)
	{
		ValidateDirection(direction);
		XYFocusOptions xYFocusOptions = ToFocusOptions(options, updateManifold: false);
		IInputElement? result = FindNextFocus(options?.FocusedElement ?? Current, direction, xYFocusOptions);
		_reusableFocusOptions = xYFocusOptions;
		return result;
	}

	private static void ValidateDirection(NavigationDirection direction)
	{
		if (((uint)direction > 1u && (uint)(direction - 4) > 3u) || 1 == 0)
		{
			throw new ArgumentOutOfRangeException("direction", direction, "Only Next, Previous, Up, Down, Left and Right directions are supported");
		}
	}

	private XYFocusOptions ToFocusOptions(FindNextElementOptions? options, bool updateManifold)
	{
		XYFocusOptions xYFocusOptions = _reusableFocusOptions;
		_reusableFocusOptions = null;
		if (xYFocusOptions == null)
		{
			xYFocusOptions = new XYFocusOptions();
		}
		else
		{
			xYFocusOptions.Reset();
		}
		if (options != null)
		{
			xYFocusOptions.SearchRoot = options.SearchRoot;
			xYFocusOptions.ExclusionRect = options.ExclusionRect;
			xYFocusOptions.FocusHintRectangle = options.FocusHintRectangle;
			xYFocusOptions.NavigationStrategyOverride = options.NavigationStrategyOverride;
			xYFocusOptions.IgnoreOcclusivity = options.IgnoreOcclusivity;
		}
		xYFocusOptions.UpdateManifold = updateManifold;
		return xYFocusOptions;
	}

	private IInputElement? FindNextFocus(IInputElement? focusedElement, NavigationDirection direction, XYFocusOptions focusOptions, bool updateManifolds = true)
	{
		bool flag = (uint)direction <= 1u;
		if (flag || focusedElement == null)
		{
			bool isReverse = direction == NavigationDirection.Previous;
			return ProcessTabStopInternal(focusedElement, isReverse, queryOnly: true);
		}
		if (focusedElement is InputElement element)
		{
			Rect? boundsForRanking = XYFocus.GetBoundsForRanking(element, focusOptions.IgnoreClipping);
			if (boundsForRanking.HasValue)
			{
				Rect valueOrDefault = boundsForRanking.GetValueOrDefault();
				focusOptions.FocusedElementBounds = valueOrDefault;
			}
		}
		return _xyFocus.GetNextFocusableElement(direction, focusedElement as InputElement, null, updateManifolds, focusOptions);
	}

	internal static IInputElement? GetFirstFocusableElementInternal(IInputElement searchStart, IInputElement? focusCandidate = null)
	{
		IInputElement inputElement = null;
		bool flag = false;
		if (searchStart is InputElement inputElement2)
		{
			inputElement = inputElement2.GetFirstFocusableElementOverride();
			if (inputElement != null)
			{
				flag = FocusHelpers.IsFocusable(inputElement) || FocusHelpers.CanHaveFocusableChildren(inputElement as AvaloniaObject);
			}
		}
		if (flag)
		{
			if (focusCandidate == null || GetTabIndex(inputElement) < GetTabIndex(focusCandidate))
			{
				focusCandidate = inputElement;
			}
		}
		else
		{
			foreach (IInputElement inputElementChild in FocusHelpers.GetInputElementChildren(searchStart as AvaloniaObject))
			{
				if (!FocusHelpers.IsVisible(inputElementChild))
				{
					continue;
				}
				bool flag2 = FocusHelpers.CanHaveFocusableChildren(inputElementChild as AvaloniaObject);
				if (FocusHelpers.IsPotentialTabStop(inputElementChild))
				{
					if (focusCandidate == null && (FocusHelpers.IsFocusable(inputElementChild) | flag2))
					{
						focusCandidate = inputElementChild;
					}
					if ((FocusHelpers.IsFocusable(inputElementChild) | flag2) && (focusCandidate == null || GetTabIndex(inputElementChild) < GetTabIndex(focusCandidate)))
					{
						focusCandidate = inputElementChild;
					}
				}
				else if (flag2)
				{
					focusCandidate = GetFirstFocusableElementInternal(inputElementChild, focusCandidate);
				}
			}
		}
		return focusCandidate;
	}

	internal static IInputElement? GetLastFocusableElementInternal(IInputElement searchStart, IInputElement? lastFocus = null)
	{
		IInputElement inputElement = null;
		bool flag = false;
		if (searchStart is InputElement inputElement2)
		{
			inputElement = inputElement2.GetLastFocusableElementOverride();
			if (inputElement != null)
			{
				flag = FocusHelpers.IsFocusable(inputElement) || FocusHelpers.CanHaveFocusableChildren(inputElement as AvaloniaObject);
			}
		}
		if (flag)
		{
			if (lastFocus == null || GetTabIndex(inputElement) > GetTabIndex(lastFocus))
			{
				lastFocus = inputElement;
			}
		}
		else
		{
			foreach (IInputElement inputElementChild in FocusHelpers.GetInputElementChildren(searchStart as AvaloniaObject))
			{
				if (!FocusHelpers.IsVisible(inputElementChild))
				{
					continue;
				}
				bool flag2 = FocusHelpers.CanHaveFocusableChildren(inputElementChild as AvaloniaObject);
				if (FocusHelpers.IsPotentialTabStop(inputElementChild))
				{
					if (lastFocus == null && (FocusHelpers.IsFocusable(inputElementChild) | flag2))
					{
						lastFocus = inputElementChild;
					}
					if ((FocusHelpers.IsFocusable(inputElementChild) | flag2) && (lastFocus == null || GetTabIndex(inputElementChild) >= GetTabIndex(lastFocus)))
					{
						lastFocus = inputElementChild;
					}
				}
				else if (flag2)
				{
					lastFocus = GetLastFocusableElementInternal(inputElementChild, lastFocus);
				}
			}
		}
		return lastFocus;
	}

	private IInputElement? ProcessTabStopInternal(IInputElement? focusedElement, bool isReverse, bool queryOnly)
	{
		IInputElement inputElement = null;
		IInputElement tabStopCandidateElement = GetTabStopCandidateElement(focusedElement, isReverse, queryOnly, out var didCycleFocusAtRootVisualScope);
		bool num = InputElement.ProcessTabStop(_contentRoot, focusedElement, tabStopCandidateElement, isReverse, didCycleFocusAtRootVisualScope, out IInputElement newTabStop);
		if (num)
		{
			inputElement = newTabStop;
		}
		if (!num && inputElement == null && tabStopCandidateElement != null)
		{
			inputElement = tabStopCandidateElement;
		}
		return inputElement;
	}

	private IInputElement? GetTabStopCandidateElement(IInputElement? focusedElement, bool isReverse, bool queryOnly, out bool didCycleFocusAtRootVisualScope)
	{
		didCycleFocusAtRootVisualScope = false;
		IInputElement contentRoot = _contentRoot;
		if (contentRoot == null)
		{
			return null;
		}
		bool flag = false;
		if (focusedElement != null)
		{
			flag = CanProcessTabStop(focusedElement, isReverse);
		}
		IInputElement inputElement;
		if (focusedElement == null)
		{
			inputElement = (isReverse ? GetLastFocusableElement(contentRoot) : GetFirstFocusableElement(contentRoot));
			didCycleFocusAtRootVisualScope = true;
		}
		else if (!isReverse)
		{
			inputElement = GetNextTabStop(focusedElement);
			if (inputElement == null && (flag | queryOnly))
			{
				inputElement = GetFirstFocusableElement(contentRoot);
				didCycleFocusAtRootVisualScope = true;
			}
		}
		else
		{
			inputElement = GetPreviousTabStop(focusedElement);
			if (inputElement == null && (flag | queryOnly))
			{
				inputElement = GetLastFocusableElement(contentRoot);
				didCycleFocusAtRootVisualScope = true;
			}
		}
		return inputElement;
	}

	private IInputElement? GetNextTabStop(IInputElement? currentTabStop, bool ignoreCurrentTabStop = false)
	{
		if (currentTabStop == null || _contentRoot == null)
		{
			return null;
		}
		IInputElement currentCompare = currentTabStop;
		IInputElement inputElement = (currentTabStop as InputElement)?.GetNextTabStopOverride();
		if (inputElement == null && !ignoreCurrentTabStop && FocusHelpers.IsVisible(currentTabStop) && (FocusHelpers.CanHaveFocusableChildren(currentTabStop as AvaloniaObject) || FocusHelpers.CanHaveChildren(currentTabStop)))
		{
			inputElement = GetFirstFocusableElement(currentTabStop, inputElement);
		}
		if (inputElement == null)
		{
			bool currentPassed = false;
			IInputElement inputElement2 = currentTabStop;
			IInputElement inputElement3 = FocusHelpers.GetFocusParent(currentTabStop);
			bool flag = inputElement3 == (_contentRoot as Visual)?.VisualRoot;
			while (inputElement3 != null && !flag && inputElement == null)
			{
				if (IsValidTabStopSearchCandidate(inputElement2) && inputElement2 is InputElement element && KeyboardNavigation.GetTabNavigation(element) == KeyboardNavigationMode.Cycle)
				{
					inputElement = ((inputElement2 != GetParentTabStopElement(currentTabStop)) ? GetFirstFocusableElement(inputElement2, inputElement2) : GetFirstFocusableElement(currentTabStop));
					break;
				}
				if (IsValidTabStopSearchCandidate(inputElement3) && inputElement3 is InputElement element2 && KeyboardNavigation.GetTabNavigation(element2) == KeyboardNavigationMode.Once)
				{
					inputElement2 = inputElement3;
					inputElement3 = FocusHelpers.GetFocusParent(inputElement2);
					if (inputElement3 == null)
					{
						break;
					}
				}
				else if (!IsValidTabStopSearchCandidate(inputElement3))
				{
					AvaloniaObject parentTabStopElement = GetParentTabStopElement(inputElement3);
					if (parentTabStopElement == null)
					{
						if (GetRootOfPopupSubTree(inputElement2) is IInputElement inputElement4)
						{
							inputElement = GetNextOrPreviousTabStopInternal(inputElement4, inputElement2, inputElement, findNext: true, ref currentPassed, ref currentCompare);
							if (inputElement != null && !FocusHelpers.IsFocusable(inputElement))
							{
								inputElement = GetFirstFocusableElement(inputElement);
							}
							if (inputElement == null)
							{
								inputElement = GetFirstFocusableElement(inputElement4);
							}
							break;
						}
						inputElement3 = (_contentRoot as Visual)?.VisualRoot as IInputElement;
					}
					else if (parentTabStopElement is InputElement inputElement5 && KeyboardNavigation.GetTabNavigation(inputElement5) == KeyboardNavigationMode.None)
					{
						inputElement2 = inputElement5;
						inputElement3 = FocusHelpers.GetFocusParent(inputElement2);
						if (inputElement3 == null)
						{
							break;
						}
					}
					else
					{
						inputElement3 = parentTabStopElement as IInputElement;
					}
				}
				inputElement = GetNextOrPreviousTabStopInternal(inputElement3, inputElement2, inputElement, findNext: true, ref currentPassed, ref currentCompare);
				if (inputElement != null && !FocusHelpers.IsFocusable(inputElement) && FocusHelpers.CanHaveFocusableChildren(inputElement as AvaloniaObject))
				{
					inputElement = GetFirstFocusableElement(inputElement);
				}
				if (inputElement != null)
				{
					break;
				}
				if (IsValidTabStopSearchCandidate(inputElement3))
				{
					inputElement2 = inputElement3;
				}
				inputElement3 = FocusHelpers.GetFocusParent(inputElement3);
				currentPassed = false;
				flag = inputElement3 == (_contentRoot as Visual)?.VisualRoot;
			}
		}
		return inputElement;
	}

	private IInputElement? GetPreviousTabStop(IInputElement? currentTabStop, bool ignoreCurrentTabStop = false)
	{
		if (currentTabStop == null || _contentRoot == null)
		{
			return null;
		}
		IInputElement inputElement = (currentTabStop as InputElement)?.GetPreviousTabStopOverride();
		IInputElement currentCompare = currentTabStop;
		if (inputElement == null)
		{
			bool currentPassed = false;
			IInputElement inputElement2 = currentTabStop;
			IInputElement inputElement3 = FocusHelpers.GetFocusParent(currentTabStop);
			bool flag = inputElement3 == (_contentRoot as Visual)?.VisualRoot;
			while (inputElement3 != null && !flag && inputElement == null)
			{
				if (IsValidTabStopSearchCandidate(inputElement2) && inputElement2 is InputElement element && KeyboardNavigation.GetTabNavigation(element) == KeyboardNavigationMode.Cycle)
				{
					inputElement = GetLastFocusableElement(inputElement2, inputElement2);
					break;
				}
				if (IsValidTabStopSearchCandidate(inputElement3) && inputElement3 is InputElement element2 && KeyboardNavigation.GetTabNavigation(element2) == KeyboardNavigationMode.Once)
				{
					if (FocusHelpers.IsFocusable(inputElement3))
					{
						inputElement = inputElement3;
					}
					else
					{
						inputElement2 = inputElement3;
						inputElement3 = FocusHelpers.GetFocusParent(inputElement2);
						if (inputElement3 == null)
						{
							break;
						}
					}
				}
				else if (!IsValidTabStopSearchCandidate(inputElement3))
				{
					AvaloniaObject parentTabStopElement = GetParentTabStopElement(inputElement3);
					if (parentTabStopElement == null)
					{
						if (GetRootOfPopupSubTree(inputElement2) is IInputElement inputElement4)
						{
							inputElement = GetNextOrPreviousTabStopInternal(inputElement4, inputElement2, inputElement, findNext: false, ref currentPassed, ref currentCompare);
							if (inputElement != null && !FocusHelpers.IsFocusable(inputElement))
							{
								inputElement = GetLastFocusableElement(inputElement);
							}
							if (inputElement == null)
							{
								inputElement = GetLastFocusableElement(inputElement4);
							}
							break;
						}
						inputElement3 = (_contentRoot as Visual)?.VisualRoot as IInputElement;
					}
					else if (parentTabStopElement is InputElement element3 && KeyboardNavigation.GetTabNavigation(element3) == KeyboardNavigationMode.None)
					{
						if (FocusHelpers.IsFocusable(inputElement3))
						{
							inputElement = inputElement3;
						}
						else
						{
							inputElement2 = inputElement3;
							inputElement3 = FocusHelpers.GetFocusParent(inputElement2);
							if (inputElement3 == null)
							{
								break;
							}
						}
					}
					else
					{
						inputElement3 = parentTabStopElement as IInputElement;
					}
				}
				inputElement = GetNextOrPreviousTabStopInternal(inputElement3, inputElement2, inputElement, findNext: false, ref currentPassed, ref currentCompare);
				if (inputElement == null && FocusHelpers.IsPotentialTabStop(inputElement3) && FocusHelpers.IsFocusable(inputElement3))
				{
					inputElement = ((!(inputElement3 is InputElement element4) || KeyboardNavigation.GetTabNavigation(element4) != KeyboardNavigationMode.Cycle) ? inputElement3 : GetLastFocusableElement(inputElement3));
				}
				else if (inputElement != null && FocusHelpers.CanHaveFocusableChildren(inputElement as AvaloniaObject))
				{
					inputElement = GetLastFocusableElement(inputElement);
				}
				if (inputElement != null)
				{
					break;
				}
				if (IsValidTabStopSearchCandidate(inputElement3))
				{
					inputElement2 = inputElement3;
				}
				inputElement3 = FocusHelpers.GetFocusParent(inputElement3);
				currentPassed = false;
			}
		}
		return inputElement;
	}

	private IInputElement? GetNextOrPreviousTabStopInternal(IInputElement? parent, IInputElement? current, IInputElement? candidate, bool findNext, ref bool currentPassed, ref IInputElement? currentCompare)
	{
		IInputElement inputElement = candidate;
		IInputElement inputElement2 = null;
		int num = 0;
		bool flag = false;
		if (IsValidTabStopSearchCandidate(current))
		{
			currentCompare = current;
		}
		if (parent != null)
		{
			bool flag2 = false;
			foreach (IInputElement inputElementChild in FocusHelpers.GetInputElementChildren(parent as AvaloniaObject))
			{
				inputElement2 = null;
				flag = false;
				if (inputElementChild == current)
				{
					flag2 = true;
					currentPassed = true;
					continue;
				}
				if (FocusHelpers.IsVisible(inputElementChild))
				{
					if (inputElementChild == current)
					{
						flag2 = true;
						currentPassed = true;
						continue;
					}
					if (IsValidTabStopSearchCandidate(inputElementChild))
					{
						if (!FocusHelpers.IsPotentialTabStop(inputElementChild))
						{
							inputElement2 = GetNextOrPreviousTabStopInternal(inputElement2, current, inputElement, findNext, ref currentPassed, ref currentCompare);
							flag = true;
						}
						else
						{
							inputElement2 = inputElementChild;
						}
					}
					else if (FocusHelpers.CanHaveFocusableChildren(inputElementChild as AvaloniaObject))
					{
						inputElement2 = GetNextOrPreviousTabStopInternal(inputElementChild, current, inputElement, findNext, ref currentPassed, ref currentCompare);
						flag = true;
					}
				}
				if (inputElement2 == null || (!FocusHelpers.IsFocusable(inputElement2) && !FocusHelpers.CanHaveFocusableChildren(inputElement2 as AvaloniaObject)))
				{
					continue;
				}
				num = CompareTabIndex(inputElement2, currentCompare);
				if (findNext)
				{
					if (num <= 0 && (!(flag2 | currentPassed) || num != 0))
					{
						continue;
					}
					if (inputElement != null)
					{
						if (CompareTabIndex(inputElement2, inputElement) < 0)
						{
							inputElement = inputElement2;
						}
					}
					else
					{
						inputElement = inputElement2;
					}
				}
				else
				{
					if (num >= 0 && (!((!flag2 && !currentPassed) | flag) || num != 0))
					{
						continue;
					}
					if (inputElement != null)
					{
						if (CompareTabIndex(inputElement2, inputElement) >= 0)
						{
							inputElement = inputElement2;
						}
					}
					else
					{
						inputElement = inputElement2;
					}
				}
			}
		}
		return inputElement;
	}

	private static int CompareTabIndex(IInputElement? control1, IInputElement? control2)
	{
		return GetTabIndex(control1).CompareTo(GetTabIndex(control2));
	}

	private static int GetTabIndex(IInputElement? element)
	{
		if (element is InputElement inputElement)
		{
			return inputElement.TabIndex;
		}
		return int.MaxValue;
	}

	private bool CanProcessTabStop(IInputElement? focusedElement, bool isReverse)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = true;
		if (IsFocusedElementInPopup(focusedElement))
		{
			return true;
		}
		if (isReverse)
		{
			flag = IsFocusOnFirstTabStop(focusedElement);
		}
		else
		{
			flag2 = IsFocusOnLastTabStop(focusedElement);
		}
		if (flag | flag2)
		{
			flag3 = false;
		}
		if (flag3)
		{
			IInputElement firstFocusableElementFromRoot = GetFirstFocusableElementFromRoot(!isReverse);
			if (firstFocusableElementFromRoot != null)
			{
				AvaloniaObject parentTabStopElement = GetParentTabStopElement(firstFocusableElementFromRoot);
				if (parentTabStopElement is InputElement element && KeyboardNavigation.GetTabNavigation(element) == KeyboardNavigationMode.Once && parentTabStopElement == GetParentTabStopElement(focusedElement))
				{
					flag3 = false;
				}
			}
			else
			{
				flag3 = false;
			}
		}
		else if (flag2 | flag)
		{
			if (focusedElement is InputElement element2 && KeyboardNavigation.GetTabNavigation(element2) == KeyboardNavigationMode.Cycle)
			{
				flag3 = true;
			}
			else
			{
				for (AvaloniaObject parentTabStopElement2 = GetParentTabStopElement(focusedElement); parentTabStopElement2 != null; parentTabStopElement2 = GetParentTabStopElement(parentTabStopElement2 as IInputElement))
				{
					if (parentTabStopElement2 is InputElement element3 && KeyboardNavigation.GetTabNavigation(element3) == KeyboardNavigationMode.Cycle)
					{
						flag3 = true;
						break;
					}
				}
			}
		}
		return flag3;
	}

	private AvaloniaObject? GetParentTabStopElement(IInputElement? current)
	{
		if (current != null)
		{
			for (IInputElement focusParent = FocusHelpers.GetFocusParent(current); focusParent != null; focusParent = FocusHelpers.GetFocusParent(focusParent))
			{
				if (IsValidTabStopSearchCandidate(focusParent) && focusParent is InputElement result)
				{
					return result;
				}
			}
		}
		return null;
	}

	private bool IsValidTabStopSearchCandidate(IInputElement? element)
	{
		bool flag = FocusHelpers.IsPotentialTabStop(element);
		if (!flag)
		{
			flag = (element as InputElement)?.IsSet(KeyboardNavigation.TabNavigationProperty) ?? false;
		}
		return flag;
	}

	private IInputElement? GetFirstFocusableElementFromRoot(bool isReverse)
	{
		if ((_contentRoot as Visual)?.VisualRoot is IInputElement searchStart)
		{
			if (isReverse)
			{
				return GetLastFocusableElement(searchStart);
			}
			return GetFirstFocusableElement(searchStart);
		}
		return null;
	}

	private bool IsFocusOnLastTabStop(IInputElement? focusedElement)
	{
		if (focusedElement == null || !(_contentRoot is Visual visual))
		{
			return false;
		}
		IInputElement searchStart = visual.VisualRoot as IInputElement;
		return GetLastFocusableElement(searchStart) == focusedElement;
	}

	private bool IsFocusOnFirstTabStop(IInputElement? focusedElement)
	{
		if (focusedElement == null || !(_contentRoot is Visual visual))
		{
			return false;
		}
		return GetFirstFocusableElement(visual.VisualRoot as IInputElement) == focusedElement;
	}

	private static IInputElement? GetFirstFocusableElement(IInputElement searchStart, IInputElement? firstFocus = null)
	{
		firstFocus = GetFirstFocusableElementInternal(searchStart, firstFocus);
		if (firstFocus != null && !firstFocus.Focusable && FocusHelpers.CanHaveFocusableChildren(firstFocus as AvaloniaObject))
		{
			firstFocus = GetFirstFocusableElement(firstFocus);
		}
		return firstFocus;
	}

	private IInputElement? GetLastFocusableElement(IInputElement searchStart, IInputElement? lastFocus = null)
	{
		lastFocus = GetLastFocusableElementInternal(searchStart, lastFocus);
		if (lastFocus != null && !lastFocus.Focusable && FocusHelpers.CanHaveFocusableChildren(lastFocus as AvaloniaObject))
		{
			lastFocus = GetLastFocusableElement(lastFocus);
		}
		return lastFocus;
	}

	private bool IsFocusedElementInPopup(IInputElement? focusedElement)
	{
		if (focusedElement != null)
		{
			return GetRootOfPopupSubTree(focusedElement) != null;
		}
		return false;
	}

	private Visual? GetRootOfPopupSubTree(IInputElement? current)
	{
		return null;
	}

	private bool FindAndSetNextFocus(IInputElement? focusedElement, NavigationDirection direction, XYFocusOptions xYFocusOptions)
	{
		bool flag = false;
		if (xYFocusOptions.UpdateManifoldsFromFocusHintRect && xYFocusOptions.FocusHintRectangle.HasValue)
		{
			_xyFocus.SetManifoldsFromBounds(xYFocusOptions.FocusHintRectangle.GetValueOrDefault());
		}
		IInputElement inputElement = FindNextFocus(focusedElement, direction, xYFocusOptions, updateManifolds: false);
		if (inputElement != null)
		{
			flag = inputElement.Focus();
			if (flag && xYFocusOptions.UpdateManifold && inputElement is InputElement candidate)
			{
				Rect elementBounds = xYFocusOptions.FocusHintRectangle ?? xYFocusOptions.FocusedElementBounds.GetValueOrDefault();
				_xyFocus.UpdateManifolds(direction, elementBounds, candidate, xYFocusOptions.IgnoreClipping);
			}
		}
		return flag;
	}
}
