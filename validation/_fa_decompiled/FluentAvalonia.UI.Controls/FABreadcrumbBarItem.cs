using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using FluentAvalonia.Collections;

namespace FluentAvalonia.UI.Controls;

[TemplatePart(Name = "PART_LayoutRoot", Type = typeof(Grid))]
[TemplatePart(Name = "PART_EllipsisFlyout", Type = typeof(Flyout))]
[TemplatePart(Name = "PART_ItemButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_LastItemContentPresenter", Type = typeof(ContentPresenter), IsRequired = false)]
[TemplatePart(Name = "PART_ChevronTextBlock", Type = typeof(TextBlock), IsRequired = false)]
[TemplatePart(Name = "PART_EllipsisDropDownItemContentPresenter", Type = typeof(ContentPresenter), IsRequired = false)]
[PseudoClasses(new string[] { ":pressed", ":inline", ":ellipsis", ":inline", ":ellipsisDropDown" })]
public class FABreadcrumbBarItem : ContentControl
{
	private bool _childPreviewKeyDownToken;

	private bool _isEllipsisDropDownItem;

	private bool _isEllipsisItem;

	private bool _isLastItem;

	private bool _allowClickOnLastItem;

	private Flyout _ellipsisFlyout;

	private Button _button;

	private WeakReference<FABreadcrumbBar> _parentBreadcrumb;

	private FAItemsRepeater _ellipsisItemsRepeater;

	private IDataTemplate _ellipsisDropDownItemDataTemplate;

	private BreadcrumbElementFactory _ellipsisElementFactory;

	private FABreadcrumbBarItem _ellipsisItem;

	private int _index;

	private bool _isPressed;

	private int _trackedPointerId;

	private const string s_ellipsisItemsRepeaterPartName = "PART_EllipsisItemsRepeater";

	private const string s_itemButtonPartName = "PART_ItemButton";

	private const string s_itemEllipsisFlyoutPartName = "PART_EllipsisFlyout";

	private const string s_ellipsisItemsRepeaterAutomationName = "EllipsisItemsRepeater";

	private const string s_pcInline = ":inline";

	private const string s_pcEllipsis = ":ellipsis";

	private const string s_pcLastItem = ":lastItem";

	private const string s_pcEllipsisDropDown = ":ellipsisDropDown";

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		((TemplatedControl)this).OnApplyTemplate(e);
		RevokePartsListeners();
		if (_isEllipsisItem)
		{
			Grid val = NameScopeExtensions.Get<Grid>(e.NameScope, "PART_LayoutRoot");
			object obj = ((IDictionary<object, object>)((StyledElement)val).Resources)[(object)"PART_EllipsisFlyout"];
			_ellipsisFlyout = (Flyout)((obj is Flyout) ? obj : null);
			if (_ellipsisFlyout == null)
			{
				throw new InvalidOperationException("PART_LayoutRoot on BreadcrumbBarItem is missing Flyout in resources");
			}
		}
		_button = NameScopeExtensions.Find<Button>(e.NameScope, "PART_ItemButton");
		if (_button != null)
		{
			((Control)_button).Loaded += OnButtonLoadedEvent;
		}
		UpdateButtonCommonVisualState();
		UpdateInlineItemTypeVisualState();
		UpdateItemTypeVisualState();
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return (AutomationPeer)(object)new FABreadcrumbBarItemAutomationPeer((Control)(object)this);
	}

	protected override void OnPointerEntered(PointerEventArgs e)
	{
		((InputElement)this).OnPointerEntered(e);
		if (_isEllipsisDropDownItem)
		{
			ProcessPointerOver(e);
		}
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		((InputElement)this).OnPointerMoved(e);
		if (_isEllipsisDropDownItem)
		{
			ProcessPointerOver(e);
		}
	}

	protected override void OnPointerExited(PointerEventArgs e)
	{
		((InputElement)this).OnPointerExited(e);
		if (_isEllipsisDropDownItem)
		{
			ProcessPointerCanceled(e);
		}
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		((InputElement)this).OnPointerPressed(e);
		if (_isEllipsisDropDownItem && !IgnorePointerId(((PointerEventArgs)e).Pointer))
		{
			if ((int)((PointerEventArgs)e).Pointer.Type == 0)
			{
				PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)(object)this);
				PointerPointProperties properties = ((PointerPoint)(ref currentPoint)).Properties;
				_isPressed = ((PointerPointProperties)(ref properties)).IsLeftButtonPressed;
			}
			else
			{
				_isPressed = true;
			}
			if (_isPressed)
			{
				UpdateEllipsisDropDownItemCommonVisualState();
			}
		}
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		((Control)this).OnPointerReleased(e);
		if (_isEllipsisDropDownItem && !IgnorePointerId(((PointerEventArgs)e).Pointer) && _isPressed)
		{
			_isPressed = false;
			UpdateEllipsisDropDownItemCommonVisualState();
			OnClickEvent(null, null);
		}
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		((InputElement)this).OnPointerCaptureLost(e);
		if (_isEllipsisDropDownItem)
		{
			ProcessPointerCanceled(null, e.Pointer);
		}
	}

	private void ProcessPointerOver(PointerEventArgs args)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (IgnorePointerId(args.Pointer))
		{
			return;
		}
		bool isPressed = _isPressed;
		if (isPressed)
		{
			bool isPointerOver = ((InputElement)this).IsPointerOver;
			Rect bounds = ((Visual)this).Bounds;
			Rect val = default(Rect);
			((Rect)(ref val))._002Ector(((Rect)(ref bounds)).Size);
			Point position = args.GetPosition((Visual)(object)this);
			bool flag = ((Rect)(ref val)).Contains(position);
			if (!flag)
			{
				ProcessPointerCanceled(args);
			}
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pointerover", isPointerOver & flag);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", isPressed & flag);
		}
	}

	private void ProcessPointerCanceled(PointerEventArgs args, IPointer p = null)
	{
		if (!IgnorePointerId(((args != null) ? args.Pointer : null) ?? p))
		{
			_isPressed = false;
			ResetTrackedPointerId();
			UpdateEllipsisDropDownItemCommonVisualState();
		}
	}

	private void ResetTrackedPointerId()
	{
		_trackedPointerId = 0;
	}

	private void OnButtonLoadedEvent(object sender, RoutedEventArgs e)
	{
		((Control)_button).Loaded -= OnButtonLoadedEvent;
		if (_isEllipsisItem)
		{
			_button.Click += OnEllipsisItemClick;
		}
		else
		{
			_button.Click += OnBreadcrumbBarItemClick;
		}
		if (_isEllipsisItem)
		{
			SetPropertiesForEllipsisItem();
		}
		else if (_isLastItem)
		{
			SetPropertiesForLastItem();
		}
		else
		{
			ResetVisualProperties();
		}
	}

	internal void SetParentBreadcrumb(FABreadcrumbBar parent)
	{
		_parentBreadcrumb = new WeakReference<FABreadcrumbBar>(parent);
	}

	internal void SetEllipsisDropDownItemDataTemplate(object newDataTemplate)
	{
		IDataTemplate val = (IDataTemplate)((newDataTemplate is IDataTemplate) ? newDataTemplate : null);
		if (val != null)
		{
			_ellipsisDropDownItemDataTemplate = val;
		}
		else if (newDataTemplate == null)
		{
			_ellipsisDropDownItemDataTemplate = null;
		}
	}

	internal void SetIndex(int index)
	{
		_index = index;
	}

	internal void SetIsEllipsisDropDownItem(bool isEllipsisDropDownItem)
	{
		_isEllipsisDropDownItem = isEllipsisDropDownItem;
		HookListeners(_isEllipsisDropDownItem);
		UpdateItemTypeVisualState();
	}

	internal void RaiseItemClickedEvent(object content, int index)
	{
		if (_parentBreadcrumb.TryGetTarget(out var target))
		{
			target.RaiseItemClickedEvent(content, in index);
		}
	}

	private void OnBreadcrumbBarItemClick(object sender, RoutedEventArgs e)
	{
		RaiseItemClickedEvent(((ContentControl)this).Content, _index - 1);
	}

	private void OnFlyoutElementPreparedEvent(FAItemsRepeater sender, FAItemsRepeaterElementPreparedEventArgs args)
	{
		if (args.Element is FABreadcrumbBarItem fABreadcrumbBarItem)
		{
			fABreadcrumbBarItem.SetIsEllipsisDropDownItem(isEllipsisDropDownItem: true);
		}
		UpdateFlyoutIndex(args.Element, args.Index);
	}

	private void OnFlyoutElementIndexChangedEvent(FAItemsRepeater repeater, FAItemsRepeaterElementIndexChangedEventArgs args)
	{
		UpdateFlyoutIndex(args.Element, args.NewIndex);
	}

	private void OnChildPreviewKeyDown(object sender, KeyEventArgs args)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		if (_isEllipsisDropDownItem)
		{
			if ((int)args.Key == 6 || (int)args.Key == 18)
			{
				OnClickEvent(sender, null);
				((RoutedEventArgs)args).Handled = true;
			}
		}
		else if ((int)args.Key == 6 || (int)args.Key == 18)
		{
			if (_isEllipsisItem)
			{
				OnEllipsisItemClick(null, null);
			}
			else
			{
				OnBreadcrumbBarItemClick(null, null);
			}
			((RoutedEventArgs)args).Handled = true;
		}
	}

	private void UpdateFlyoutIndex(Control element, int index)
	{
		if (_ellipsisItemsRepeater != null)
		{
			int count = _ellipsisItemsRepeater.ItemsSourceView.Count;
			if (element is FABreadcrumbBarItem fABreadcrumbBarItem)
			{
				fABreadcrumbBarItem.SetEllipsisItem(this);
				fABreadcrumbBarItem.SetIndex(count - index);
			}
			((AvaloniaObject)element).SetValue<int>((StyledProperty<int>)(object)AutomationProperties.PositionInSetProperty, index + 1, (BindingPriority)0);
			((AvaloniaObject)element).SetValue<int>((StyledProperty<int>)(object)AutomationProperties.SizeOfSetProperty, count, (BindingPriority)0);
		}
	}

	private IList<object> CloneEllipsisItemSource(IEnumerable<object> ellipsisItemsSource)
	{
		int num = ellipsisItemsSource.Count();
		List<object> list = new List<object>(num);
		if (num > 0)
		{
			for (int num2 = num - 1; num2 >= 0; num2--)
			{
				list.Add(ellipsisItemsSource.ElementAt(num2));
			}
		}
		return list;
	}

	private void OpenFlyout()
	{
		Flyout ellipsisFlyout = _ellipsisFlyout;
		if (ellipsisFlyout != null)
		{
			((FlyoutBase)ellipsisFlyout).ShowAt((Control)(object)this);
		}
	}

	private void CloseFlyout()
	{
		Flyout ellipsisFlyout = _ellipsisFlyout;
		if (ellipsisFlyout != null)
		{
			((FlyoutBase)ellipsisFlyout).Hide();
		}
	}

	private void UpdateItemTypeVisualState()
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":inline", !_isEllipsisDropDownItem);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":ellipsisDropDown", _isEllipsisDropDownItem);
	}

	private void UpdateEllipsisDropDownItemCommonVisualState()
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", _isPressed);
	}

	private void UpdateInlineItemTypeVisualState()
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":ellipsis", _isEllipsisItem);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":lastItem", _isLastItem);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":allowClick", _allowClickOnLastItem);
	}

	private void UpdateButtonCommonVisualState()
	{
		if (_button != null)
		{
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_button).Classes, ":lastItem", _isLastItem && !_allowClickOnLastItem);
		}
	}

	private void OnEllipsisItemClick(object sender, RoutedEventArgs e)
	{
		if (_parentBreadcrumb.TryGetTarget(out var target))
		{
			IEnumerable<object> enumerable = target.HiddenElements();
			IList<object> itemsSource = CloneEllipsisItemSource(enumerable);
			if (enumerable is PooledList<object> pooledList)
			{
				pooledList.Dispose();
			}
			if (_ellipsisDropDownItemDataTemplate != null)
			{
				_ellipsisElementFactory.UserElementFactory(_ellipsisDropDownItemDataTemplate);
			}
			if (_ellipsisItemsRepeater != null)
			{
				_ellipsisItemsRepeater.ItemsSource = itemsSource;
			}
			OpenFlyout();
		}
	}

	internal void SetPropertiesForLastItem()
	{
		_isEllipsisItem = false;
		_isLastItem = true;
		if (_parentBreadcrumb.TryGetTarget(out var target))
		{
			_allowClickOnLastItem = target.IsLastItemClickEnabled;
		}
		UpdateButtonCommonVisualState();
		UpdateInlineItemTypeVisualState();
	}

	internal void ResetVisualProperties()
	{
		if (_isEllipsisDropDownItem)
		{
			UpdateEllipsisDropDownItemCommonVisualState();
			return;
		}
		_isEllipsisItem = false;
		_isLastItem = false;
		if (_button != null)
		{
			_button.Flyout = null;
		}
		_ellipsisFlyout = null;
		_ellipsisItemsRepeater = null;
		_ellipsisElementFactory = null;
		UpdateButtonCommonVisualState();
		UpdateInlineItemTypeVisualState();
	}

	private void InstantiateFlyout()
	{
		if (_button != null && _ellipsisFlyout != null)
		{
			FAItemsRepeater fAItemsRepeater = new FAItemsRepeater();
			((StyledElement)fAItemsRepeater).Name = "PART_EllipsisItemsRepeater";
			AvaloniaProperty nameProperty = (AvaloniaProperty)(object)AutomationProperties.NameProperty;
			((AvaloniaObject)fAItemsRepeater)[nameProperty] = "EllipsisItemsRepeater";
			((Layoutable)fAItemsRepeater).HorizontalAlignment = (HorizontalAlignment)0;
			fAItemsRepeater.Layout = new FAStackLayout();
			FAItemsRepeater fAItemsRepeater2 = fAItemsRepeater;
			_ellipsisElementFactory = new BreadcrumbElementFactory();
			fAItemsRepeater2.ItemTemplate = (IDataTemplate)(object)_ellipsisElementFactory;
			if (_ellipsisDropDownItemDataTemplate != null)
			{
				_ellipsisElementFactory.UserElementFactory(_ellipsisDropDownItemDataTemplate);
			}
			fAItemsRepeater2.ElementPrepared += OnFlyoutElementPreparedEvent;
			fAItemsRepeater2.ElementIndexChanged += OnFlyoutElementIndexChangedEvent;
			_ellipsisItemsRepeater = fAItemsRepeater2;
			_ellipsisFlyout.Content = fAItemsRepeater2;
			((PopupFlyoutBase)_ellipsisFlyout).Placement = (PlacementMode)9;
		}
	}

	internal void SetPropertiesForEllipsisItem()
	{
		_isEllipsisItem = true;
		_isLastItem = false;
		InstantiateFlyout();
		UpdateButtonCommonVisualState();
		UpdateInlineItemTypeVisualState();
	}

	private void SetEllipsisItem(FABreadcrumbBarItem ellipsisItem)
	{
		_ellipsisItem = ellipsisItem;
	}

	internal void OnClickEvent(object sender, RoutedEventArgs args)
	{
		if (_isEllipsisDropDownItem)
		{
			FABreadcrumbBarItem ellipsisItem = _ellipsisItem;
			if (ellipsisItem != null)
			{
				ellipsisItem.CloseFlyout();
				ellipsisItem.RaiseItemClickedEvent(((ContentControl)this).Content, _index - 1);
			}
		}
		else if (_isEllipsisItem)
		{
			OnEllipsisItemClick(null, null);
		}
		else
		{
			OnBreadcrumbBarItemClick(null, null);
		}
	}

	private void HookListeners(bool forEllipsisDropDownItem)
	{
		if (!_childPreviewKeyDownToken)
		{
			((Interactive)this).AddHandler<KeyEventArgs>(InputElement.KeyDownEvent, (EventHandler<KeyEventArgs>)OnChildPreviewKeyDown, (RoutingStrategies)2, false);
			_childPreviewKeyDownToken = true;
		}
	}

	private void RevokeListeners()
	{
		if (_childPreviewKeyDownToken)
		{
			((Interactive)this).RemoveHandler<KeyEventArgs>(InputElement.KeyDownEvent, (EventHandler<KeyEventArgs>)OnChildPreviewKeyDown);
			_childPreviewKeyDownToken = false;
		}
	}

	private void RevokePartsListeners()
	{
		if (_button != null)
		{
			((Control)_button).Loaded -= OnButtonLoadedEvent;
			if (_isEllipsisItem)
			{
				_button.Click -= OnEllipsisItemClick;
			}
			else
			{
				_button.Click -= OnBreadcrumbBarItemClick;
			}
		}
		if (_ellipsisItemsRepeater != null)
		{
			_ellipsisItemsRepeater.ElementPrepared -= OnFlyoutElementPreparedEvent;
			_ellipsisItemsRepeater.ElementIndexChanged -= OnFlyoutElementIndexChangedEvent;
		}
	}

	internal bool IsEllipsisDropDownItem()
	{
		return _isEllipsisDropDownItem;
	}

	private bool IgnorePointerId(IPointer pointer)
	{
		int id = pointer.Id;
		if (_trackedPointerId == 0)
		{
			_trackedPointerId = id;
		}
		else if (_trackedPointerId != id)
		{
			return true;
		}
		return false;
	}
}
