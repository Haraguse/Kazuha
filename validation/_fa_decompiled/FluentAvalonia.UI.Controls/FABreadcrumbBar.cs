using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FluentAvalonia.Collections;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// The BreadcrumbBar control provides the direct path of pages or folders to the current location.
/// </summary>
[TemplatePart(Name = "PART_ItemsRepeater", Type = typeof(FAItemsRepeater))]
public class FABreadcrumbBar : TemplatedControl
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FABreadcrumbBar.ItemsSource" /> property
	/// </summary>
	public static readonly StyledProperty<IEnumerable> ItemsSourceProperty = ItemsControl.ItemsSourceProperty.AddOwner<FABreadcrumbBar>((StyledPropertyMetadata<IEnumerable>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FABreadcrumbBar.ItemTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty = ItemsControl.ItemTemplateProperty.AddOwner<FABreadcrumbBar>((StyledPropertyMetadata<IDataTemplate>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FABreadcrumbBar.IsLastItemClickEnabled" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsLastItemClickEnabledProperty = AvaloniaProperty.Register<FABreadcrumbBar, bool>("IsLastItemClickEnabled", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	private FAItemsSourceView _breadcrumbItemsSourceView;

	private BreadcrumbIterable _itemsIterable;

	private FAItemsRepeater _itemsRepeater;

	private BreadcrumbElementFactory _itemsRepeaterElementFactory;

	private BreadcrumbLayout _itemsRepeaterLayout;

	private FABreadcrumbBarItem _ellipsisBreadcrumBarItem;

	private FABreadcrumbBarItem _lastBreadcrumbBarItem;

	private int _focusedIndex;

	private const string s_tpItemsRepeater = "PART_ItemsRepeater";

	private const string SR_AutomationNameEllipsisBreadcrumbBarItem = "AutomationNameEllipsisBreadcrumbBarItem";

	/// <summary>
	/// Gets or sets an object source used to generate the content of the BreadcrumbBar.
	/// </summary>
	public IEnumerable ItemsSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IEnumerable>(ItemsSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IEnumerable>(ItemsSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the data template for the BreadcrumbBarItem.
	/// </summary>
	public IDataTemplate ItemTemplate
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IDataTemplate>(ItemTemplateProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IDataTemplate>(ItemTemplateProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether the last item can be clicked
	/// </summary>
	public bool IsLastItemClickEnabled
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsLastItemClickEnabledProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsLastItemClickEnabledProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Occurs when an item is clicked in the BreadcrumbBar.
	/// </summary>
	public event TypedEventHandler<FABreadcrumbBar, FABreadcrumbBarItemClickedEventArgs> ItemClicked;

	public FABreadcrumbBar()
	{
		_itemsRepeaterElementFactory = new BreadcrumbElementFactory();
		_itemsRepeaterLayout = new BreadcrumbLayout(this);
		_itemsIterable = new BreadcrumbIterable(null);
		((Interactive)this).AddHandler<KeyEventArgs>(InputElement.KeyDownEvent, (EventHandler<KeyEventArgs>)OnChildPreviewKeyDown, (RoutingStrategies)2, false);
		((InputElement)this).GettingFocus += OnGettingFocus;
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		RevokeListeners();
		((TemplatedControl)this).OnApplyTemplate(e);
		FAItemsRepeater fAItemsRepeater = NameScopeExtensions.Get<FAItemsRepeater>(e.NameScope, "PART_ItemsRepeater");
		fAItemsRepeater.Layout = _itemsRepeaterLayout;
		fAItemsRepeater.ItemTemplate = (IDataTemplate)(object)_itemsRepeaterElementFactory;
		fAItemsRepeater.ElementPrepared += OnElementPreparedEvent;
		fAItemsRepeater.ElementIndexChanged += OnElementIndexChangedEvent;
		fAItemsRepeater.ElementClearing += OnElementClearingEvent;
		((Control)fAItemsRepeater).Loaded += OnBreadcrumbBarItemsRepeaterLoaded;
		_itemsRepeater = fAItemsRepeater;
		UpdateItemsRepeaterItemsSource();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((Control)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)ItemsSourceProperty)
		{
			UpdateItemsRepeaterItemsSource();
		}
		else if (change.Property == (AvaloniaProperty)(object)ItemTemplateProperty)
		{
			UpdateItemTemplate();
			UpdateEllipsisBreadcrumbBarItemDropDownItemTemplate();
		}
		else if (change.Property == (AvaloniaProperty)(object)IsLastItemClickEnabledProperty)
		{
			ForceUpdateLastElement();
		}
	}

	private void OnBreadcrumbBarItemsRepeaterLoaded(object sender, RoutedEventArgs e)
	{
		if (_itemsRepeater != null)
		{
			OnBreadcrumbBarItemsSourceCollectionChanged(null, null);
		}
	}

	private void UpdateItemTemplate()
	{
		IDataTemplate itemTemplate = ItemTemplate;
		_itemsRepeaterElementFactory.UserElementFactory(itemTemplate);
	}

	private void UpdateEllipsisBreadcrumbBarItemDropDownItemTemplate()
	{
		IDataTemplate itemTemplate = ItemTemplate;
		_ellipsisBreadcrumBarItem?.SetEllipsisDropDownItemDataTemplate(itemTemplate);
	}

	private void UpdateItemsRepeaterItemsSource()
	{
		if (_breadcrumbItemsSourceView != null)
		{
			_breadcrumbItemsSourceView.CollectionChanged -= OnBreadcrumbBarItemsSourceCollectionChanged;
		}
		_breadcrumbItemsSourceView = null;
		IEnumerable itemsSource = ItemsSource;
		if (itemsSource != null)
		{
			_breadcrumbItemsSourceView = new FAItemsSourceView(itemsSource);
			if (_itemsRepeater != null)
			{
				_itemsIterable = new BreadcrumbIterable(itemsSource);
				_itemsRepeater.ItemsSource = _itemsIterable;
			}
			if (_breadcrumbItemsSourceView != null)
			{
				_breadcrumbItemsSourceView.CollectionChanged += OnBreadcrumbBarItemsSourceCollectionChanged;
			}
		}
	}

	private void OnBreadcrumbBarItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (_itemsRepeater != null)
		{
			IEnumerable itemsSource = ItemsSource;
			_itemsIterable = new BreadcrumbIterable(itemsSource);
			_itemsRepeater.ItemsSource = _itemsIterable;
			ForceUpdateLastElement();
		}
	}

	private void ResetLastBreadcrumbBarItem()
	{
		_lastBreadcrumbBarItem?.ResetVisualProperties();
	}

	private void ForceUpdateLastElement()
	{
		FAItemsSourceView breadcrumbItemsSourceView = _breadcrumbItemsSourceView;
		if (breadcrumbItemsSourceView != null)
		{
			int count = breadcrumbItemsSourceView.Count;
			FAItemsRepeater itemsRepeater = _itemsRepeater;
			if (itemsRepeater != null)
			{
				Control val = itemsRepeater.TryGetElement(count);
				UpdateLastElement(val as FABreadcrumbBarItem);
			}
			if (count == 0)
			{
				ResetLastBreadcrumbBarItem();
			}
		}
		else
		{
			ResetLastBreadcrumbBarItem();
		}
	}

	private void UpdateLastElement(FABreadcrumbBarItem newLastItem)
	{
		ResetLastBreadcrumbBarItem();
		newLastItem?.SetPropertiesForLastItem();
		_lastBreadcrumbBarItem = newLastItem;
	}

	private void OnElementPreparedEvent(FAItemsRepeater sender, FAItemsRepeaterElementPreparedEventArgs args)
	{
		if (!(args.Element is FABreadcrumbBarItem fABreadcrumbBarItem))
		{
			return;
		}
		fABreadcrumbBarItem.SetIsEllipsisDropDownItem(isEllipsisDropDownItem: false);
		fABreadcrumbBarItem.SetParentBreadcrumb(this);
		int index = args.Index;
		fABreadcrumbBarItem.SetIndex(index);
		if (index == 0)
		{
			fABreadcrumbBarItem.SetPropertiesForEllipsisItem();
			_ellipsisBreadcrumBarItem = fABreadcrumbBarItem;
			UpdateEllipsisBreadcrumbBarItemDropDownItemTemplate();
			string localizedStringResource = FALocalizationHelper.Instance.GetLocalizedStringResource("AutomationNameEllipsisBreadcrumbBarItem");
			AutomationProperties.SetName((StyledElement)(object)fABreadcrumbBarItem, localizedStringResource);
			return;
		}
		FAItemsSourceView breadcrumbItemsSourceView = _breadcrumbItemsSourceView;
		if (breadcrumbItemsSourceView != null)
		{
			int count = breadcrumbItemsSourceView.Count;
			if (index == count)
			{
				UpdateLastElement(fABreadcrumbBarItem);
			}
			else
			{
				fABreadcrumbBarItem.ResetVisualProperties();
			}
		}
	}

	private void OnElementIndexChangedEvent(FAItemsRepeater sender, FAItemsRepeaterElementIndexChangedEventArgs args)
	{
		if (_focusedIndex == args.OldIndex)
		{
			int newIndex = args.NewIndex;
			if (args.Element is FABreadcrumbBarItem fABreadcrumbBarItem)
			{
				fABreadcrumbBarItem.SetIndex(newIndex);
			}
			FocusElementAt(newIndex);
		}
	}

	private void OnElementClearingEvent(FAItemsRepeater sender, FAItemsRepeaterElementClearingEventArgs args)
	{
		if (args.Element is FABreadcrumbBarItem fABreadcrumbBarItem)
		{
			fABreadcrumbBarItem.ResetVisualProperties();
		}
	}

	internal void RaiseItemClickedEvent(object content, in int index)
	{
		if (ItemClicked != null)
		{
			FABreadcrumbBarItemClickedEventArgs args = new FABreadcrumbBarItemClickedEventArgs(index, content);
			ItemClicked(this, args);
		}
	}

	internal IEnumerable<object> GetHiddenElementsList(int firstShownElement)
	{
		PooledList<object> pooledList = new PooledList<object>();
		if (_breadcrumbItemsSourceView != null)
		{
			for (int i = 0; i < firstShownElement - 1; i++)
			{
				pooledList.Add(_breadcrumbItemsSourceView.GetAt(i));
			}
		}
		return pooledList;
	}

	internal IEnumerable<object> HiddenElements()
	{
		if (_itemsRepeater != null && _itemsRepeaterLayout != null && _itemsRepeaterLayout.EllipsisIsRendered)
		{
			return GetHiddenElementsList(_itemsRepeaterLayout.FirstRenderedItemIndexAfterEllipsis);
		}
		return null;
	}

	internal void ReIndexVisibleElementsForAccessibility()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		FAItemsRepeater itemsRepeater = _itemsRepeater;
		if (itemsRepeater == null)
		{
			return;
		}
		int getVisibleItemsCount = _itemsRepeaterLayout.GetVisibleItemsCount;
		bool ellipsisIsRendered = _itemsRepeaterLayout.EllipsisIsRendered;
		int num = 1;
		if (ellipsisIsRendered)
		{
			num = _itemsRepeaterLayout.FirstRenderedItemIndexAfterEllipsis;
		}
		FABreadcrumbBarItem ellipsisBreadcrumBarItem = _ellipsisBreadcrumBarItem;
		if (ellipsisBreadcrumBarItem != null)
		{
			AccessibilityView val = (AccessibilityView)((!ellipsisIsRendered) ? 1 : 3);
			((AvaloniaObject)ellipsisBreadcrumBarItem).SetValue<AccessibilityView>((StyledProperty<AccessibilityView>)(object)AutomationProperties.AccessibilityViewProperty, val, (BindingPriority)0);
		}
		int num2 = 1;
		int num3 = num;
		while (num2 <= getVisibleItemsCount)
		{
			Control val2 = itemsRepeater.TryGetElement(num3);
			if (val2 != null)
			{
				((AvaloniaObject)val2).SetValue<int>((StyledProperty<int>)(object)AutomationProperties.PositionInSetProperty, num2, (BindingPriority)0);
				((AvaloniaObject)val2).SetValue<int>((StyledProperty<int>)(object)AutomationProperties.SizeOfSetProperty, getVisibleItemsCount, (BindingPriority)0);
			}
			num2++;
			num3++;
		}
	}

	private void OnGettingFocus(object sender, FocusChangingEventArgs args)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Invalid comparison between Unknown and I4
		FAItemsRepeater itemsRepeater = _itemsRepeater;
		if (itemsRepeater == null || ((int)args.NavigationMethod != 2 && (int)args.NavigationMethod != 1))
		{
			return;
		}
		KeyModifiers commandModifiers = Application.Current.PlatformSettings.HotkeyConfiguration.CommandModifiers;
		IInputElement oldFocusedElement = args.OldFocusedElement;
		if (oldFocusedElement != null)
		{
			IInputElement obj = ((oldFocusedElement is Visual) ? oldFocusedElement : null);
			if ((object)itemsRepeater == ((obj != null) ? VisualExtensions.GetVisualParent((Visual)(object)obj) : null))
			{
				if ((commandModifiers & args.KeyModifiers) != 2)
				{
					IInputElement newFocusedElement = args.NewFocusedElement;
					Control val = (Control)(object)((newFocusedElement is Control) ? newFocusedElement : null);
					if (val != null)
					{
						FocusElementAt(itemsRepeater.GetElementIndex(val));
						((RoutedEventArgs)args).Handled = true;
					}
				}
				return;
			}
		}
		if (_itemsRepeaterLayout != null)
		{
			if (_itemsRepeaterLayout.EllipsisIsRendered)
			{
				_focusedIndex = 0;
			}
			else
			{
				_focusedIndex = 1;
			}
			FocusElementAt(_focusedIndex);
		}
		Control val2 = itemsRepeater.TryGetElement(_focusedIndex);
		if (val2 != null && args.TrySetNewFocusedElement((IInputElement)(object)val2))
		{
			((RoutedEventArgs)args).Handled = true;
		}
	}

	private void FocusElementAt(int index)
	{
		if (index >= 0)
		{
			_focusedIndex = index;
		}
	}

	private bool MoveFocus(int indexIncrement)
	{
		FAItemsRepeater itemsRepeater = _itemsRepeater;
		if (itemsRepeater != null)
		{
			TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)this);
			object obj;
			if (topLevel == null)
			{
				obj = null;
			}
			else
			{
				IFocusManager focusManager = topLevel.FocusManager;
				obj = ((focusManager != null) ? focusManager.GetFocusedElement() : null);
			}
			Control val = (Control)((obj is Control) ? obj : null);
			if (val != null)
			{
				int elementIndex = itemsRepeater.GetElementIndex(val);
				if (elementIndex >= 0 && indexIncrement != 0)
				{
					elementIndex += indexIncrement;
					for (int count = itemsRepeater.ItemsSourceView.Count; elementIndex >= 0 && elementIndex < count; elementIndex += indexIncrement)
					{
						Control val2 = itemsRepeater.TryGetElement(elementIndex);
						if (val2 != null && ((InputElement)val2).Focus((NavigationMethod)0, (KeyModifiers)0))
						{
							FocusElementAt(elementIndex);
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	private bool MoveFocusPrevious()
	{
		int indexIncrement = -1;
		FAItemsRepeater itemsRepeater = _itemsRepeater;
		if (itemsRepeater != null && itemsRepeater.Layout is BreadcrumbLayout breadcrumbLayout)
		{
			if (_focusedIndex == 1)
			{
				indexIncrement = 0;
			}
			else if (breadcrumbLayout.EllipsisIsRendered && _focusedIndex == breadcrumbLayout.FirstRenderedItemIndexAfterEllipsis)
			{
				indexIncrement = -_focusedIndex;
			}
		}
		return MoveFocus(indexIncrement);
	}

	private bool MoveFocusNext()
	{
		int indexIncrement = 1;
		if (_focusedIndex == 0)
		{
			FAItemsRepeater itemsRepeater = _itemsRepeater;
			if (itemsRepeater != null)
			{
				indexIncrement = (itemsRepeater.Layout as BreadcrumbLayout).FirstRenderedItemIndexAfterEllipsis;
			}
		}
		return MoveFocus(indexIncrement);
	}

	private FindNextElementOptions GetFindNextElementOptions()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		FindNextElementOptions val = new FindNextElementOptions();
		val.set_SearchRoot((InputElement)(object)this);
		return val;
	}

	private void OnChildPreviewKeyDown(object sender, KeyEventArgs args)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		bool flag = (int)((Visual)this).FlowDirection == 0;
		bool flag2 = (int)args.Key == 23;
		bool flag3 = (int)args.Key == 25;
		if ((flag & flag3) || (!flag & flag2))
		{
			if (MoveFocusNext())
			{
				((RoutedEventArgs)args).Handled = true;
			}
		}
		else if (((flag & flag2) || (!flag & flag3)) && MoveFocusPrevious())
		{
			((RoutedEventArgs)args).Handled = true;
		}
	}

	private void RevokeListeners()
	{
		if (_itemsRepeater != null)
		{
			((Control)_itemsRepeater).Loaded -= OnBreadcrumbBarItemsRepeaterLoaded;
			_itemsRepeater.ElementPrepared -= OnElementPreparedEvent;
			_itemsRepeater.ElementIndexChanged -= OnElementIndexChangedEvent;
			_itemsRepeater.ElementClearing -= OnElementClearingEvent;
		}
		FAItemsSourceView breadcrumbItemsSourceView = _breadcrumbItemsSourceView;
		if (breadcrumbItemsSourceView != null)
		{
			breadcrumbItemsSourceView.CollectionChanged -= OnBreadcrumbBarItemsSourceCollectionChanged;
		}
	}
}
