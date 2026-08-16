using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Collections.Pooled;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input.Navigation;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Input;

public class XYFocus
{
	private XYFocusAlgorithms.XYFocusManifolds _manifolds = new XYFocusAlgorithms.XYFocusManifolds();

	private PooledList<XYFocusParams> _pooledCandidates = new PooledList<XYFocusParams>();

	private static readonly XYFocus _instance;

	public static readonly AttachedProperty<InputElement> DownProperty;

	public static readonly AttachedProperty<InputElement> LeftProperty;

	public static readonly AttachedProperty<InputElement> RightProperty;

	public static readonly AttachedProperty<InputElement> UpProperty;

	public static readonly AttachedProperty<XYFocusNavigationStrategy> DownNavigationStrategyProperty;

	public static readonly AttachedProperty<XYFocusNavigationStrategy> UpNavigationStrategyProperty;

	public static readonly AttachedProperty<XYFocusNavigationStrategy> LeftNavigationStrategyProperty;

	public static readonly AttachedProperty<XYFocusNavigationStrategy> RightNavigationStrategyProperty;

	public static readonly AttachedProperty<XYFocusNavigationModes> NavigationModesProperty;

	internal static readonly AttachedProperty<bool> IsFocusEngagementEnabledProperty;

	internal static readonly AttachedProperty<bool> IsFocusEngagedProperty;

	private static InputElement? GetDirectionOverride(InputElement element, InputElement? searchRoot, NavigationDirection direction, bool ignoreFocusabililty = false)
	{
		AvaloniaProperty xYFocusPropertyIndex = GetXYFocusPropertyIndex(element, direction);
		if (xYFocusPropertyIndex != null && element.GetValue(xYFocusPropertyIndex) is InputElement inputElement)
		{
			if (!ignoreFocusabililty && !FocusManager.CanFocus(inputElement))
			{
				return null;
			}
			if (searchRoot != null && !searchRoot.IsVisualAncestorOf(inputElement))
			{
				return null;
			}
			return inputElement;
		}
		return null;
	}

	private static InputElement? TryXYFocusBubble(InputElement element, InputElement? candidate, InputElement? searchRoot, NavigationDirection direction)
	{
		if (candidate == null)
		{
			return null;
		}
		InputElement inputElement = candidate;
		InputElement directionOverrideRoot = GetDirectionOverrideRoot(element, searchRoot, direction);
		if (directionOverrideRoot != null && !directionOverrideRoot.IsVisualAncestorOf(candidate))
		{
			inputElement = GetDirectionOverride(directionOverrideRoot, searchRoot, direction) ?? inputElement;
		}
		return inputElement;
	}

	private static InputElement? GetDirectionOverrideRoot(InputElement element, InputElement? searchRoot, NavigationDirection direction)
	{
		InputElement inputElement = element;
		while (inputElement != null && GetDirectionOverride(inputElement, searchRoot, direction) == null)
		{
			inputElement = inputElement.GetVisualParent() as InputElement;
		}
		return inputElement;
	}

	private static XYFocusNavigationStrategy GetStrategy(InputElement element, NavigationDirection direction, XYFocusNavigationStrategy? navigationStrategyOverride)
	{
		bool flag = navigationStrategyOverride == XYFocusNavigationStrategy.Auto;
		if (navigationStrategyOverride.HasValue && !flag)
		{
			return navigationStrategyOverride.Value - 1;
		}
		if (flag && element.GetVisualParent() is InputElement inputElement)
		{
			element = inputElement;
		}
		AvaloniaProperty xYFocusNavigationStrategyPropertyIndex = GetXYFocusNavigationStrategyPropertyIndex(element, direction);
		if ((object)xYFocusNavigationStrategyPropertyIndex != null)
		{
			InputElement inputElement2 = element;
			while (inputElement2 != null && inputElement2.GetValue(xYFocusNavigationStrategyPropertyIndex) is XYFocusNavigationStrategy xYFocusNavigationStrategy)
			{
				if (xYFocusNavigationStrategy != XYFocusNavigationStrategy.Auto)
				{
					return xYFocusNavigationStrategy;
				}
				inputElement2 = inputElement2.GetVisualParent() as InputElement;
			}
		}
		return XYFocusNavigationStrategy.Projection;
	}

	private static AvaloniaProperty? GetXYFocusPropertyIndex(InputElement element, NavigationDirection direction)
	{
		if (element.FlowDirection == FlowDirection.RightToLeft)
		{
			switch (direction)
			{
			case NavigationDirection.Left:
				direction = NavigationDirection.Right;
				break;
			case NavigationDirection.Right:
				direction = NavigationDirection.Left;
				break;
			}
		}
		return direction switch
		{
			NavigationDirection.Left => LeftProperty, 
			NavigationDirection.Right => RightProperty, 
			NavigationDirection.Up => UpProperty, 
			NavigationDirection.Down => DownProperty, 
			_ => null, 
		};
	}

	private static AvaloniaProperty? GetXYFocusNavigationStrategyPropertyIndex(InputElement element, NavigationDirection direction)
	{
		if (element.FlowDirection == FlowDirection.RightToLeft)
		{
			switch (direction)
			{
			case NavigationDirection.Left:
				direction = NavigationDirection.Right;
				break;
			case NavigationDirection.Right:
				direction = NavigationDirection.Left;
				break;
			}
		}
		return direction switch
		{
			NavigationDirection.Left => LeftNavigationStrategyProperty, 
			NavigationDirection.Right => RightNavigationStrategyProperty, 
			NavigationDirection.Up => UpNavigationStrategyProperty, 
			NavigationDirection.Down => DownNavigationStrategyProperty, 
			_ => null, 
		};
	}

	private static void FindElements(PooledList<XYFocusParams> focusList, InputElement startRoot, InputElement? currentElement, InputElement? activeScroller, bool ignoreClipping, KeyDeviceType? inputKeyDeviceType)
	{
		bool flag = activeScroller != null;
		IAvaloniaList<Visual> visualChildren = startRoot.VisualChildren;
		int count = visualChildren.Count;
		for (int i = 0; i < count; i++)
		{
			if (!(visualChildren[i] is InputElement inputElement))
			{
				continue;
			}
			bool flag2 = GetIsFocusEngagementEnabled(inputElement) && !GetIsFocusEngaged(inputElement);
			if (inputElement != currentElement && IsValidCandidate(inputElement, inputKeyDeviceType))
			{
				Rect? boundsForRanking = GetBoundsForRanking(inputElement, ignoreClipping);
				if (boundsForRanking.HasValue)
				{
					Rect valueOrDefault = boundsForRanking.GetValueOrDefault();
					if (flag)
					{
						if (IsCandidateParticipatingInScroll(inputElement, activeScroller) || !IsOccluded(inputElement, valueOrDefault) || IsCandidateChildOfAncestorScroller(inputElement, activeScroller))
						{
							focusList.Add(new XYFocusParams(inputElement, valueOrDefault));
						}
					}
					else
					{
						focusList.Add(new XYFocusParams(inputElement, valueOrDefault));
					}
				}
			}
			if (IsValidFocusSubtree(inputElement) && !flag2)
			{
				FindElements(focusList, inputElement, currentElement, activeScroller, ignoreClipping, inputKeyDeviceType);
			}
		}
	}

	private static bool IsValidFocusSubtree(InputElement candidate)
	{
		if (candidate.IsVisible)
		{
			return candidate.IsEnabled;
		}
		return false;
	}

	private static bool IsValidCandidate(InputElement candidate, KeyDeviceType? inputKeyDeviceType)
	{
		if (candidate.Focusable && candidate.IsEffectivelyEnabled && candidate.IsEffectivelyVisible)
		{
			return candidate.IsAllowedXYNavigationMode(inputKeyDeviceType);
		}
		return false;
	}

	/// Check if candidate's direct scroller is the same as active focused scroller.
	private static bool IsCandidateParticipatingInScroll(InputElement candidate, InputElement? activeScroller)
	{
		if (activeScroller == null)
		{
			return false;
		}
		return candidate.FindAncestorOfType<IScrollable>(includeSelf: true) == activeScroller;
	}

	/// Check if there is a common parent scroller for both candidate and active scroller.
	private static bool IsCandidateChildOfAncestorScroller(InputElement candidate, InputElement? activeScroller)
	{
		if (activeScroller == null)
		{
			return false;
		}
		for (StyledElement parent = activeScroller.Parent; parent != null; parent = parent.Parent)
		{
			if (parent is IScrollable scrollable && scrollable is Visual visual && visual.IsVisualAncestorOf(candidate))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsOccluded(InputElement element, Rect elementBounds)
	{
		InputElement inputElement = (InputElement)element.VisualRoot;
		if (inputElement == null)
		{
			return true;
		}
		Rect rect = new Rect(0.0, 0.0, inputElement.Bounds.Width, inputElement.Bounds.Height);
		return !rect.Intersects(elementBounds);
	}

	internal static Rect? GetBoundsForRanking(InputElement element, bool ignoreClipping)
	{
		TransformedBounds? transformedBounds = element.GetTransformedBounds();
		if (transformedBounds.HasValue)
		{
			TransformedBounds valueOrDefault = transformedBounds.GetValueOrDefault();
			return ignoreClipping ? valueOrDefault.Bounds.TransformToAABB(valueOrDefault.Transform) : valueOrDefault.Clip;
		}
		return null;
	}

	internal XYFocus()
	{
	}

	internal XYFocusAlgorithms.XYFocusManifolds ResetManifolds()
	{
		_manifolds.Reset();
		return _manifolds;
	}

	internal void SetManifoldsFromBounds(Rect bounds)
	{
		_manifolds.VManifold = (Left: bounds.Left, Right: bounds.Right);
		_manifolds.HManifold = (Top: bounds.Top, Bottom: bounds.Bottom);
	}

	internal void UpdateManifolds(NavigationDirection direction, Rect elementBounds, InputElement candidate, bool ignoreClipping)
	{
		Rect value = GetBoundsForRanking(candidate, ignoreClipping).Value;
		XYFocusAlgorithms.UpdateManifolds(direction, elementBounds, value, _manifolds);
	}

	internal static InputElement? TryDirectionalFocus(NavigationDirection direction, IInputElement element, IInputElement? owner, InputElement? engagedControl, KeyDeviceType? keyDeviceType)
	{
		if (!(element is InputElement inputElement))
		{
			return null;
		}
		if (!inputElement.IsAllowedXYNavigationMode(keyDeviceType))
		{
			return null;
		}
		Rect? boundsForRanking = GetBoundsForRanking(inputElement, ignoreClipping: true);
		if (boundsForRanking.HasValue)
		{
			Rect valueOrDefault = boundsForRanking.GetValueOrDefault();
			InputElement inputElement2 = inputElement.FindXYSearchRoot(keyDeviceType);
			if (inputElement2 == null)
			{
				return null;
			}
			_instance.SetManifoldsFromBounds(valueOrDefault);
			return _instance.GetNextFocusableElement(direction, inputElement, engagedControl, updateManifolds: true, new XYFocusOptions
			{
				KeyDeviceType = keyDeviceType,
				FocusedElementBounds = valueOrDefault,
				UpdateManifold = true,
				SearchRoot = inputElement2
			});
		}
		return null;
	}

	internal InputElement? GetNextFocusableElement(NavigationDirection direction, InputElement? element, InputElement? engagedControl, bool updateManifolds, XYFocusOptions xyFocusOptions)
	{
		if (element == null)
		{
			return null;
		}
		InputElement inputElement = (InputElement)element.VisualRoot;
		if (inputElement == null)
		{
			return null;
		}
		bool isRightToLeft = element.FlowDirection == FlowDirection.RightToLeft;
		XYFocusNavigationStrategy strategy = GetStrategy(element, direction, xyFocusOptions.NavigationStrategyOverride);
		Rect bounds = xyFocusOptions.FocusedElementBounds ?? throw new InvalidOperationException("FocusedElementBounds needs to be set");
		InputElement inputElement2 = GetDirectionOverride(element, xyFocusOptions.SearchRoot, direction, ignoreFocusabililty: true);
		if (inputElement2 != null)
		{
			return inputElement2;
		}
		InputElement activeScrollerForScroll = GetActiveScrollerForScroll(direction, element);
		bool flag = activeScrollerForScroll != null;
		if (xyFocusOptions.FocusHintRectangle.HasValue)
		{
			bounds = xyFocusOptions.FocusHintRectangle.Value;
			element = null;
		}
		Rect rect = ((engagedControl != null) ? (GetBoundsForRanking(engagedControl, xyFocusOptions.IgnoreClipping) ?? inputElement.Bounds) : ((xyFocusOptions.SearchRoot == null) ? (GetBoundsForRanking(inputElement, xyFocusOptions.IgnoreClipping) ?? inputElement.Bounds) : (GetBoundsForRanking(xyFocusOptions.SearchRoot, xyFocusOptions.IgnoreClipping) ?? inputElement.Bounds)));
		PooledList<XYFocusParams> pooledCandidates = _pooledCandidates;
		try
		{
			GetAllValidFocusableChildren(pooledCandidates, inputElement, direction, element, engagedControl, xyFocusOptions.SearchRoot, activeScrollerForScroll, xyFocusOptions.IgnoreClipping, xyFocusOptions.KeyDeviceType);
			if (pooledCandidates.Count > 0)
			{
				double val = Math.Max(rect.Right - rect.Left, rect.Bottom - rect.Top);
				val = Math.Max(val, GetMaxRootBoundsDistance(pooledCandidates, bounds, direction, xyFocusOptions.IgnoreClipping));
				RankElements(pooledCandidates, direction, bounds, val, strategy, xyFocusOptions.ExclusionRect, xyFocusOptions.IgnoreClipping, xyFocusOptions.IgnoreCone);
				bool ignoreOcclusivity = xyFocusOptions.IgnoreOcclusivity | flag;
				inputElement2 = ChooseBestFocusableElementFromList(pooledCandidates, direction, bounds, xyFocusOptions.IgnoreClipping, ignoreOcclusivity, isRightToLeft, xyFocusOptions.UpdateManifold & updateManifolds);
				if (element != null)
				{
					inputElement2 = TryXYFocusBubble(element, inputElement2, xyFocusOptions.SearchRoot, direction);
				}
			}
		}
		finally
		{
			_pooledCandidates.Clear();
		}
		return inputElement2;
	}

	private InputElement? ChooseBestFocusableElementFromList(PooledList<XYFocusParams> scoreList, NavigationDirection direction, Rect bounds, bool ignoreClipping, bool ignoreOcclusivity, bool isRightToLeft, bool updateManifolds)
	{
		InputElement result = null;
		scoreList.Sort(delegate(XYFocusParams? elementA, XYFocusParams? elementB)
		{
			if (elementA.Element == elementB.Element)
			{
				return 0;
			}
			int num = elementB.Score.CompareTo(elementA.Score);
			if (num == 0)
			{
				Rect bounds2 = elementA.Bounds;
				Rect bounds3 = elementB.Bounds;
				if (bounds2 == bounds3)
				{
					return 0;
				}
				if (direction == NavigationDirection.Up || direction == NavigationDirection.Down)
				{
					if (isRightToLeft)
					{
						return bounds3.Left.CompareTo(bounds2.Left);
					}
					return bounds2.Left.CompareTo(bounds3.Left);
				}
				return bounds2.Top.CompareTo(bounds3.Top);
			}
			return num;
		});
		foreach (XYFocusParams score in scoreList)
		{
			if (score.Score <= 0.0)
			{
				break;
			}
			Rect elementBounds = (ignoreClipping ? GetBoundsForRanking(score.Element, ignoreClipping: false).Value : score.Bounds);
			if (Math.Abs(score.Bounds.X - double.MaxValue) > 2.220446049250313E-16 && (ignoreOcclusivity || !IsOccluded(score.Element, elementBounds)))
			{
				result = score.Element;
				if (updateManifolds)
				{
					XYFocusAlgorithms.UpdateManifolds(direction, bounds, score.Bounds, _manifolds);
				}
				break;
			}
		}
		return result;
	}

	private void GetAllValidFocusableChildren(PooledList<XYFocusParams> candidateList, InputElement startRoot, NavigationDirection direction, InputElement? currentElement, InputElement? engagedControl, InputElement? searchScope, InputElement? activeScroller, bool ignoreClipping, KeyDeviceType? inputKeyDeviceType)
	{
		InputElement startRoot2 = startRoot;
		if (searchScope != null)
		{
			startRoot2 = searchScope;
		}
		if (engagedControl == null)
		{
			FindElements(candidateList, startRoot2, currentElement, activeScroller, ignoreClipping, inputKeyDeviceType);
			return;
		}
		FindElements(candidateList, engagedControl, currentElement, activeScroller, ignoreClipping, inputKeyDeviceType);
		if (currentElement != engagedControl)
		{
			Rect? boundsForRanking = GetBoundsForRanking(engagedControl, ignoreClipping);
			if (boundsForRanking.HasValue)
			{
				Rect valueOrDefault = boundsForRanking.GetValueOrDefault();
				candidateList.Add(new XYFocusParams(engagedControl, valueOrDefault));
			}
		}
	}

	private void RankElements(IList<XYFocusParams> candidateList, NavigationDirection direction, Rect bounds, double maxRootBoundsDistance, XYFocusNavigationStrategy mode, Rect? exclusionRect, bool ignoreClipping, bool ignoreCone)
	{
		Rect exclusionRect2 = default(Rect);
		if (exclusionRect.HasValue)
		{
			exclusionRect2 = exclusionRect.Value;
		}
		foreach (XYFocusParams candidate in candidateList)
		{
			Rect bounds2 = candidate.Bounds;
			if (!exclusionRect2.Intersects(bounds2) && !exclusionRect2.Contains(bounds2))
			{
				if (mode == XYFocusNavigationStrategy.Projection && XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(bounds, bounds2, maxRootBoundsDistance, direction, exclusionRect2, ignoreCone))
				{
					candidate.Score = XYFocusAlgorithms.GetScoreProjection(direction, bounds, bounds2, _manifolds, maxRootBoundsDistance);
				}
				else if (mode == XYFocusNavigationStrategy.NavigationDirectionDistance || mode == XYFocusNavigationStrategy.RectilinearDistance)
				{
					candidate.Score = XYFocusAlgorithms.GetScoreProximity(direction, bounds, bounds2, maxRootBoundsDistance, mode == XYFocusNavigationStrategy.RectilinearDistance);
				}
			}
		}
	}

	private double GetMaxRootBoundsDistance(IList<XYFocusParams> list, Rect bounds, NavigationDirection direction, bool ignoreClipping)
	{
		XYFocusParams xYFocusParams = list[0];
		double num = double.MinValue;
		foreach (XYFocusParams item in list)
		{
			Rect bounds2 = item.Bounds;
			double num2 = direction switch
			{
				NavigationDirection.Left => bounds2.Left, 
				NavigationDirection.Right => bounds2.Right, 
				NavigationDirection.Up => bounds2.Top, 
				NavigationDirection.Down => bounds2.Bottom, 
				_ => 0.0, 
			};
			if (num2 > num)
			{
				num = num2;
				xYFocusParams = item;
			}
		}
		Rect bounds3 = xYFocusParams.Bounds;
		return direction switch
		{
			NavigationDirection.Left => Math.Abs(bounds3.Right - bounds.Left), 
			NavigationDirection.Right => Math.Abs(bounds.Right - bounds3.Left), 
			NavigationDirection.Up => Math.Abs(bounds.Bottom - bounds3.Top), 
			NavigationDirection.Down => Math.Abs(bounds3.Bottom - bounds.Top), 
			_ => 0.0, 
		};
	}

	private InputElement? GetActiveScrollerForScroll(NavigationDirection direction, InputElement focusedElement)
	{
		InputElement inputElement = null;
		for (inputElement = focusedElement; inputElement != null; inputElement = inputElement.VisualParent as InputElement)
		{
			InputElement inputElement2 = inputElement;
			if (inputElement2 is IScrollable { CanHorizontallyScroll: var canHorizontallyScroll, CanVerticallyScroll: var canVerticallyScroll })
			{
				bool flag = (uint)(direction - 4) <= 1u;
				bool flag2 = flag & canHorizontallyScroll;
				flag = (uint)(direction - 6) <= 1u;
				bool flag3 = flag & canVerticallyScroll;
				if (flag2 | flag3)
				{
					return inputElement2;
				}
			}
		}
		return null;
	}

	public static void SetDown(InputElement obj, InputElement value)
	{
		obj.SetValue(DownProperty, value);
	}

	public static InputElement GetDown(InputElement obj)
	{
		return obj.GetValue(DownProperty);
	}

	public static void SetLeft(InputElement obj, InputElement value)
	{
		obj.SetValue(LeftProperty, value);
	}

	public static InputElement GetLeft(InputElement obj)
	{
		return obj.GetValue(LeftProperty);
	}

	public static void SetRight(InputElement obj, InputElement value)
	{
		obj.SetValue(RightProperty, value);
	}

	public static InputElement GetRight(InputElement obj)
	{
		return obj.GetValue(RightProperty);
	}

	public static void SetUp(InputElement obj, InputElement value)
	{
		obj.SetValue(UpProperty, value);
	}

	public static InputElement GetUp(InputElement obj)
	{
		return obj.GetValue(UpProperty);
	}

	public static void SetDownNavigationStrategy(InputElement obj, XYFocusNavigationStrategy value)
	{
		obj.SetValue(DownNavigationStrategyProperty, value);
	}

	public static XYFocusNavigationStrategy GetDownNavigationStrategy(InputElement obj)
	{
		return obj.GetValue(DownNavigationStrategyProperty);
	}

	public static void SetUpNavigationStrategy(InputElement obj, XYFocusNavigationStrategy value)
	{
		obj.SetValue(UpNavigationStrategyProperty, value);
	}

	public static XYFocusNavigationStrategy GetUpNavigationStrategy(InputElement obj)
	{
		return obj.GetValue(UpNavigationStrategyProperty);
	}

	public static void SetLeftNavigationStrategy(InputElement obj, XYFocusNavigationStrategy value)
	{
		obj.SetValue(LeftNavigationStrategyProperty, value);
	}

	public static XYFocusNavigationStrategy GetLeftNavigationStrategy(InputElement obj)
	{
		return obj.GetValue(LeftNavigationStrategyProperty);
	}

	public static void SetRightNavigationStrategy(InputElement obj, XYFocusNavigationStrategy value)
	{
		obj.SetValue(RightNavigationStrategyProperty, value);
	}

	public static XYFocusNavigationStrategy GetRightNavigationStrategy(InputElement obj)
	{
		return obj.GetValue(RightNavigationStrategyProperty);
	}

	public static void SetNavigationModes(InputElement obj, XYFocusNavigationModes value)
	{
		obj.SetValue(NavigationModesProperty, value);
	}

	public static XYFocusNavigationModes GetNavigationModes(InputElement obj)
	{
		return obj.GetValue(NavigationModesProperty);
	}

	internal static void SetIsFocusEngagementEnabled(InputElement obj, bool value)
	{
		obj.SetValue(IsFocusEngagementEnabledProperty, value);
	}

	internal static bool GetIsFocusEngagementEnabled(InputElement obj)
	{
		return obj.GetValue(IsFocusEngagementEnabledProperty);
	}

	private static bool IsFocusEngagedCoerce(AvaloniaObject sender, bool value)
	{
		if (value && sender is InputElement obj)
		{
			return GetIsFocusEngagementEnabled(obj);
		}
		return false;
	}

	internal static void SetIsFocusEngaged(Visual obj, bool value)
	{
		obj.SetValue(IsFocusEngagedProperty, value);
	}

	internal static bool GetIsFocusEngaged(Visual obj)
	{
		return obj.GetValue(IsFocusEngagedProperty);
	}

	static XYFocus()
	{
		_instance = new XYFocus();
		DownProperty = AvaloniaProperty.RegisterAttached<XYFocus, InputElement, InputElement>("Down");
		LeftProperty = AvaloniaProperty.RegisterAttached<XYFocus, InputElement, InputElement>("Left");
		RightProperty = AvaloniaProperty.RegisterAttached<XYFocus, InputElement, InputElement>("Right");
		UpProperty = AvaloniaProperty.RegisterAttached<XYFocus, InputElement, InputElement>("Up");
		DownNavigationStrategyProperty = AvaloniaProperty.RegisterAttached<XYFocus, InputElement, XYFocusNavigationStrategy>("DownNavigationStrategy", XYFocusNavigationStrategy.Auto, inherits: true);
		UpNavigationStrategyProperty = AvaloniaProperty.RegisterAttached<XYFocus, InputElement, XYFocusNavigationStrategy>("UpNavigationStrategy", XYFocusNavigationStrategy.Auto, inherits: true);
		LeftNavigationStrategyProperty = AvaloniaProperty.RegisterAttached<XYFocus, InputElement, XYFocusNavigationStrategy>("LeftNavigationStrategy", XYFocusNavigationStrategy.Auto, inherits: true);
		RightNavigationStrategyProperty = AvaloniaProperty.RegisterAttached<XYFocus, InputElement, XYFocusNavigationStrategy>("RightNavigationStrategy", XYFocusNavigationStrategy.Auto, inherits: true);
		NavigationModesProperty = AvaloniaProperty.RegisterAttached<XYFocus, InputElement, XYFocusNavigationModes>("NavigationModes", XYFocusNavigationModes.Gamepad | XYFocusNavigationModes.Remote, inherits: true);
		IsFocusEngagementEnabledProperty = AvaloniaProperty.RegisterAttached<XYFocus, InputElement, bool>("IsFocusEngagementEnabled", defaultValue: false);
		IsFocusEngagedProperty = AvaloniaProperty.RegisterAttached<XYFocus, Visual, bool>("IsFocusEngaged", defaultValue: false, inherits: false, BindingMode.OneWay, null, IsFocusEngagedCoerce);
		IsFocusEngagedProperty.Changed.AddClassHandler<Visual>(delegate
		{
		});
	}
}
