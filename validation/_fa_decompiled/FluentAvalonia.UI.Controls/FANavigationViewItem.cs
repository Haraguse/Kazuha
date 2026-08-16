using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls.Primitives;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the container for an item in a NavigationView control.
/// </summary>
[PseudoClasses(new string[] { ":leftnav", ":topnav", ":topoverflow" })]
[PseudoClasses(new string[] { ":iconleft", ":icononly", ":contentonly" })]
[PseudoClasses(new string[] { ":selected" })]
[PseudoClasses(new string[] { ":iconcollapsed" })]
[PseudoClasses(new string[] { ":chevronclosed", ":chevronopen", ":chevronhidden" })]
[PseudoClasses(new string[] { ":infobadge" })]
[TemplatePart("FlyoutContentGrid", typeof(Panel))]
[TemplatePart("NVIPresenter", typeof(FANavigationViewItemPresenter))]
[TemplatePart("NVIRootGrid", typeof(Grid))]
[TemplatePart("NVIMenuItemsHost", typeof(FAItemsRepeater))]
public class FANavigationViewItem : FANavigationViewItemBase
{
	private FACompositeDisposable _splitViewRevokers;

	private FANavigationViewItemPresenter _presenter;

	private object _suggestedToolTipContent;

	private FAItemsRepeater _repeater;

	private Panel _flyoutContentGrid;

	private Grid _rootGrid;

	private bool _isClosedCompact;

	private bool _appliedTemplate;

	private bool _isRepeaterParentedToFlyout;

	private bool _restoreToExpandedState;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewItem.CompactPaneLength" /> property
	/// </summary>
	public static readonly StyledProperty<double> CompactPaneLengthProperty = AvaloniaProperty.Register<FANavigationViewItem, double>("CompactPaneLength", 48.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewItem.HasUnrealizedChildren" /> property
	/// </summary>
	public static readonly DirectProperty<FANavigationViewItem, bool> HasUnrealizedChildrenProperty = AvaloniaProperty.RegisterDirect<FANavigationViewItem, bool>("HasUnrealizedChildren", (Func<FANavigationViewItem, bool>)((FANavigationViewItem x) => x.HasUnrealizedChildren), (Action<FANavigationViewItem, bool>)delegate(FANavigationViewItem x, bool v)
	{
		x.HasUnrealizedChildren = v;
	}, false, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewItem.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = FASettingsExpander.IconSourceProperty.AddOwner<FANavigationViewItem>((StyledPropertyMetadata<FAIconSource>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewItem.IsChildSelected" /> property
	/// </summary>
	public static readonly DirectProperty<FANavigationViewItem, bool> IsChildSelectedProperty = AvaloniaProperty.RegisterDirect<FANavigationViewItem, bool>("IsChildSelectedProperty", (Func<FANavigationViewItem, bool>)((FANavigationViewItem x) => x.IsChildSelected), (Action<FANavigationViewItem, bool>)delegate(FANavigationViewItem x, bool v)
	{
		x.IsChildSelected = v;
	}, false, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewItem.IsExpanded" /> property
	/// </summary>
	public static readonly DirectProperty<FANavigationViewItem, bool> IsExpandedProperty = AvaloniaProperty.RegisterDirect<FANavigationViewItem, bool>("IsExpanded", (Func<FANavigationViewItem, bool>)((FANavigationViewItem x) => x.IsExpanded), (Action<FANavigationViewItem, bool>)delegate(FANavigationViewItem x, bool v)
	{
		x.IsExpanded = v;
	}, false, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewItem.MenuItems" /> property
	/// </summary>
	public static readonly DirectProperty<FANavigationViewItem, IList<object>> MenuItemsProperty = FANavigationView.MenuItemsProperty.AddOwner<FANavigationViewItem>((Func<FANavigationViewItem, IList<object>>)((FANavigationViewItem x) => x.MenuItems), (Action<FANavigationViewItem, IList<object>>)null, (IList<object>)null, (BindingMode)0, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewItem.MenuItemsSource" /> property
	/// </summary>
	public static readonly StyledProperty<IEnumerable> MenuItemsSourceProperty = FANavigationView.MenuItemsSourceProperty.AddOwner<FANavigationViewItem>((StyledPropertyMetadata<IEnumerable>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewItem.SelectsOnInvoked" /> property
	/// </summary>
	public static readonly DirectProperty<FANavigationViewItem, bool> SelectsOnInvokedProperty = AvaloniaProperty.RegisterDirect<FANavigationViewItem, bool>("SelectsOnInvoked", (Func<FANavigationViewItem, bool>)((FANavigationViewItem x) => x.SelectsOnInvoked), (Action<FANavigationViewItem, bool>)delegate(FANavigationViewItem x, bool v)
	{
		x.SelectsOnInvoked = v;
	}, false, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewItem.InfoBadge" /> property
	/// </summary>
	public static readonly StyledProperty<FAInfoBadge> InfoBadgeProperty = AvaloniaProperty.Register<FANavigationViewItem, FAInfoBadge>("InfoBadge", (FAInfoBadge)null, false, (BindingMode)1, (Func<FAInfoBadge, bool>)null, (Func<AvaloniaObject, FAInfoBadge, FAInfoBadge>)null, false);

	private bool _hasUnrealizedChildren;

	private bool _isChildSelected;

	private bool _isExpanded;

	private IList<object> _menuItems;

	private bool _selectsOnInvoked = true;

	private const string s_tpNVIPresenter = "NVIPresenter";

	private const string s_tpNVIRootGrid = "NVIRootGrid";

	private const string s_tpNVIMenuItemsHost = "NVIMenuItemsHost";

	private const string s_tpFlyoutContentGrid = "FlyoutContentGrid";

	private const string s_pcSelected = ":selected";

	private const string s_pcIconCollapsed = ":iconcollapsed";

	private const string s_pcInfoBadge = ":infobadge";

	/// <summary>
	/// Gets the CompactPaneLength of the NavigationView that hosts this item.
	/// </summary>
	public double CompactPaneLength
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(CompactPaneLengthProperty);
		}
		private set
		{
			((AvaloniaObject)this).SetValue<double>(CompactPaneLengthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the current item has child items that haven't been shown.
	/// </summary>
	public bool HasUnrealizedChildren
	{
		get
		{
			return _hasUnrealizedChildren;
		}
		set
		{
			if (((AvaloniaObject)this).SetAndRaise<bool>((DirectPropertyBase<bool>)(object)HasUnrealizedChildrenProperty, ref _hasUnrealizedChildren, value))
			{
				OnHasUnrealizedChildrenPropertyChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the icon to show next to the menu item text.
	/// </summary>
	public FAIconSource IconSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconSource>(IconSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAIconSource>(IconSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the value that indicates whether or not descendant item is selected.
	/// </summary>
	public bool IsChildSelected
	{
		get
		{
			return _isChildSelected;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<bool>((DirectPropertyBase<bool>)(object)IsChildSelectedProperty, ref _isChildSelected, value);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether a tree node is expanded. Ignored if there are no menu items.
	/// </summary>
	public bool IsExpanded
	{
		get
		{
			return _isExpanded;
		}
		set
		{
			if (((AvaloniaObject)this).SetAndRaise<bool>((DirectPropertyBase<bool>)(object)IsExpandedProperty, ref _isExpanded, value))
			{
				OnIsExpandedPropertyChanged();
			}
		}
	}

	/// <summary>
	/// Gets the collection of menu items displayed as children of the NavigationViewItem.
	/// </summary>
	public IList<object> MenuItems
	{
		get
		{
			return _menuItems;
		}
		private set
		{
			((AvaloniaObject)this).SetAndRaise<IList<object>>((DirectPropertyBase<IList<object>>)(object)MenuItemsProperty, ref _menuItems, value);
		}
	}

	/// <summary>
	/// Gets or sets an object source used to generate the content of the NavigationViewItem submenu.
	/// </summary>
	public IEnumerable MenuItemsSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IEnumerable>(MenuItemsSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IEnumerable>(MenuItemsSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether invoking a navigation menu item also selects it.
	/// </summary>
	public bool SelectsOnInvoked
	{
		get
		{
			return _selectsOnInvoked;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<bool>((DirectPropertyBase<bool>)(object)SelectsOnInvokedProperty, ref _selectsOnInvoked, value);
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewItem.InfoBadge" /> to display in the NavigationViewItem
	/// </summary>
	public FAInfoBadge InfoBadge
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAInfoBadge>(InfoBadgeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAInfoBadge>(InfoBadgeProperty, value, (BindingPriority)0);
		}
	}

	internal Control SelectionIndicator => _presenter?.SelectionIndicator;

	internal FANavigationViewItemPresenter NVIPresenter => _presenter;

	private bool HasChildren
	{
		get
		{
			if ((MenuItems == null || MenuItems.Count() <= 0) && (MenuItemsSource == null || _repeater == null || _repeater.ItemsSourceView == null || _repeater.ItemsSourceView.Count <= 0))
			{
				return HasUnrealizedChildren;
			}
			return true;
		}
	}

	private bool ShouldShowIcon => IconSource != null;

	private bool ShouldEnableToolTip
	{
		get
		{
			if (IsOnLeftNav)
			{
				return _isClosedCompact;
			}
			return false;
		}
	}

	private bool ShouldShowContent => ((ContentControl)this).Content != null;

	private bool IsOnLeftNav
	{
		get
		{
			if (base.Position != NavigationViewRepeaterPosition.LeftNav)
			{
				return base.Position == NavigationViewRepeaterPosition.LeftFooter;
			}
			return true;
		}
	}

	private bool IsOnTopPrimary
	{
		get
		{
			bool flag = true;
			FANavigationView getNavigationView = base.GetNavigationView;
			if (getNavigationView != null)
			{
				flag = getNavigationView.PaneDisplayMode == FANavigationViewPaneDisplayMode.Top;
			}
			return (base.Position == NavigationViewRepeaterPosition.TopPrimary) & flag;
		}
	}

	internal bool ShouldRepeaterShowInFlyout
	{
		get
		{
			if (!_isClosedCompact || !base.IsTopLevelItem)
			{
				return IsOnTopPrimary;
			}
			return true;
		}
	}

	internal bool IsRepeaterVisible
	{
		get
		{
			FAItemsRepeater repeater = _repeater;
			if (repeater == null)
			{
				return false;
			}
			return ((Visual)repeater).IsVisible;
		}
	}

	internal FAItemsRepeater GetRepeater => _repeater;

	/// <summary>
	/// Create instance of <see cref="T:FluentAvalonia.UI.Controls.FANavigationViewItem" />.
	/// </summary>
	public FANavigationViewItem()
	{
		MenuItems = (IList<object>)new AvaloniaList<object>();
	}

	/// <inheritdoc />
	protected override void OnNavigationViewItemBaseDepthChanged()
	{
		UpdateItemIndentation();
		PropagateDepthToChildren(base.Depth + 1);
	}

	/// <inheritdoc />
	protected override void OnNavigationViewItemBaseIsSelectedChanged()
	{
		UpdateVisualState();
	}

	/// <inheritdoc />
	protected override void OnNavigationViewItemBasePositionChanged()
	{
		UpdateVisualState();
		ReparentRepeater();
		if (_rootGrid != null)
		{
			FlyoutBase value = ((AvaloniaObject)_rootGrid).GetValue<FlyoutBase>((StyledProperty<FlyoutBase>)(object)FlyoutBase.AttachedFlyoutProperty);
			PopupFlyoutBase val = (PopupFlyoutBase)(object)((value is PopupFlyoutBase) ? value : null);
			if (val != null)
			{
				val.Placement = (PlacementMode)((base.Position == NavigationViewRepeaterPosition.TopPrimary || base.Position == NavigationViewRepeaterPosition.TopFooter) ? 9 : 13);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		_appliedTemplate = false;
		_restoreToExpandedState = false;
		UnhookEventsAndClearFields();
		((TemplatedControl)this).OnApplyTemplate(e);
		_presenter = NameScopeExtensions.Find<FANavigationViewItemPresenter>(e.NameScope, "NVIPresenter");
		_rootGrid = NameScopeExtensions.Find<Grid>(e.NameScope, "NVIRootGrid");
		if (_rootGrid != null)
		{
			FlyoutBase attachedFlyout = FlyoutBase.GetAttachedFlyout((Control)(object)_rootGrid);
			FlyoutBase obj = ((attachedFlyout is PopupFlyoutBase) ? attachedFlyout : null);
			if (obj != null)
			{
				((PopupFlyoutBase)obj).Closing += OnFlyoutClosing;
			}
		}
		FANavigationView fANavigationView = base.GetNavigationView;
		if (fANavigationView == null)
		{
			fANavigationView = VisualExtensions.FindAncestorOfType<FANavigationView>((Visual)(object)this, false);
			SetNavigationViewParent(fANavigationView);
		}
		SplitView getSplitView = base.GetSplitView;
		if (getSplitView != null)
		{
			PrepNavigationViewItem(getSplitView);
		}
		else
		{
			((Control)this).Loaded += HandleLoaded;
		}
		if (fANavigationView != null)
		{
			_repeater = NameScopeExtensions.Find<FAItemsRepeater>(e.NameScope, "NVIMenuItemsHost");
			if (_repeater != null)
			{
				(_repeater.Layout as FAStackLayout).DisableVirtualization = true;
				_repeater.ElementPrepared += fANavigationView.OnRepeaterElementPrepared;
				_repeater.ElementClearing += fANavigationView.OnRepeaterElementClearing;
				_repeater.ItemTemplate = (IDataTemplate)(object)fANavigationView.ItemsFactory;
			}
			UpdateRepeaterItemsSource();
		}
		_flyoutContentGrid = NameScopeExtensions.Find<Panel>(e.NameScope, "FlyoutContentGrid");
		_appliedTemplate = true;
		UpdateItemIndentation();
		UpdateVisualState();
		ReparentRepeater();
		if (!ShouldRepeaterShowInFlyout)
		{
			ShowHideChildren();
		}
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			OnIconPropertyChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)ContentControl.ContentProperty)
		{
			OnContentChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)InfoBadgeProperty)
		{
			UpdateVisualStateForInfoBadge();
		}
		else if (change.Property == (AvaloniaProperty)(object)MenuItemsProperty)
		{
			OnMenuItemsPropertyChanged();
		}
		else if (change.Property == (AvaloniaProperty)(object)MenuItemsSourceProperty)
		{
			OnMenuItemsSourcePropertyChanged();
		}
	}

	private void UpdateRepeaterItemsSource()
	{
		if (_repeater != null)
		{
			if (_repeater.ItemsSourceView != null)
			{
				_repeater.ItemsSourceView.CollectionChanged -= OnItemsSourceViewChanged;
			}
			IEnumerable menuItemsSource = MenuItemsSource;
			FAItemsRepeater repeater = _repeater;
			IEnumerable itemsSource;
			if (menuItemsSource == null)
			{
				IEnumerable menuItems = _menuItems;
				itemsSource = menuItems;
			}
			else
			{
				itemsSource = menuItemsSource;
			}
			repeater.ItemsSource = itemsSource;
			if (_repeater.ItemsSourceView != null)
			{
				_repeater.ItemsSourceView.CollectionChanged += OnItemsSourceViewChanged;
			}
		}
	}

	private void OnItemsSourceViewChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		UpdateVisualStateForChevron();
	}

	private void OnSplitViewPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		if (args.Property == (AvaloniaProperty)(object)SplitView.CompactPaneLengthProperty)
		{
			UpdateCompactPaneLength();
		}
		else if (args.Property == (AvaloniaProperty)(object)SplitView.IsPaneOpenProperty || args.Property == (AvaloniaProperty)(object)SplitView.DisplayModeProperty)
		{
			UpdateIsClosedCompact();
			ReparentRepeater();
			HandleExpansionStateMemory();
		}
	}

	private void UpdateCompactPaneLength()
	{
		SplitView getSplitView = base.GetSplitView;
		if (getSplitView != null)
		{
			double len = (CompactPaneLength = getSplitView.CompactPaneLength);
			if (_presenter != null)
			{
				_presenter.UpdateCompactPaneLength(len, IsOnLeftNav);
			}
		}
	}

	private void UpdateIsClosedCompact()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		SplitView getSplitView = base.GetSplitView;
		if (getSplitView != null)
		{
			_isClosedCompact = !getSplitView.IsPaneOpen && ((int)getSplitView.DisplayMode == 3 || (int)getSplitView.DisplayMode == 1);
			UpdateVisualState();
		}
	}

	private void UpdateVisualStateForClosedCompact()
	{
		if (_presenter != null)
		{
			_presenter.UpdateClosedCompactVisualState(base.IsTopLevelItem, _isClosedCompact);
		}
	}

	private void UpdateNavigationViewItemToolTip()
	{
		object tip = ToolTip.GetTip((Control)(object)this);
		if (tip != null && tip != _suggestedToolTipContent)
		{
			return;
		}
		if (ShouldEnableToolTip)
		{
			if (tip != _suggestedToolTipContent)
			{
				ToolTip.SetTip((Control)(object)this, _suggestedToolTipContent);
			}
		}
		else
		{
			ToolTip.SetTip((Control)(object)this, (object)null);
		}
	}

	private void SuggestedToolTipChanged(object newContent)
	{
		object suggestedToolTipContent = null;
		if (newContent is string text)
		{
			suggestedToolTipContent = text;
		}
		object tip = ToolTip.GetTip((Control)(object)this);
		if (_suggestedToolTipContent != null && tip == _suggestedToolTipContent)
		{
			ToolTip.SetTip((Control)(object)this, (object)null);
		}
		_suggestedToolTipContent = suggestedToolTipContent;
	}

	protected virtual void OnIsExpandedPropertyChanged()
	{
		_restoreToExpandedState = false;
		UpdateVisualStateForChevron();
	}

	protected virtual void OnIconPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		UpdateVisualState();
	}

	protected virtual void OnContentChanged(AvaloniaPropertyChangedEventArgs args)
	{
		SuggestedToolTipChanged(args.NewValue);
		UpdateVisualState();
		if (!IsOnLeftNav)
		{
			base.GetNavigationView?.TopNavigationViewItemContentChanged();
		}
	}

	protected virtual void OnMenuItemsPropertyChanged()
	{
		UpdateRepeaterItemsSource();
		UpdateVisualStateForChevron();
	}

	protected virtual void OnMenuItemsSourcePropertyChanged()
	{
		UpdateRepeaterItemsSource();
		UpdateVisualStateForChevron();
	}

	private void OnHasUnrealizedChildrenPropertyChanged()
	{
		UpdateVisualStateForChevron();
	}

	private void ShowSelectionIndicator(bool vis)
	{
		if (SelectionIndicator != null)
		{
			((Visual)SelectionIndicator).Opacity = (vis ? 1.0 : 0.0);
		}
	}

	private void UpdateVisualStateForIconAndContent(bool showIcon, bool showContent)
	{
		if (_presenter != null)
		{
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":iconleft", showIcon & showContent);
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":icononly", showIcon && !showContent);
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":contentonly", !showIcon);
		}
	}

	private void UpdateVisualStateForNavigationViewPositionChange()
	{
		switch (base.Position)
		{
		case NavigationViewRepeaterPosition.LeftNav:
		case NavigationViewRepeaterPosition.LeftFooter:
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftnav", true);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topnav", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topoverflow", false);
			if (_presenter != null)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":leftnav", true);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":topnav", false);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":topoverflow", false);
			}
			break;
		case NavigationViewRepeaterPosition.TopPrimary:
		case NavigationViewRepeaterPosition.TopFooter:
			_restoreToExpandedState = false;
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftnav", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topnav", true);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topoverflow", false);
			if (_presenter != null)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":leftnav", false);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":topnav", true);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":topoverflow", false);
			}
			break;
		case NavigationViewRepeaterPosition.TopOverflow:
			_restoreToExpandedState = false;
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftnav", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topnav", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topoverflow", true);
			if (_presenter != null)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":leftnav", false);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":topnav", false);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":topoverflow", true);
			}
			break;
		}
		UpdateVisualStateForClosedCompact();
	}

	private void UpdateVisualStateForToolTip()
	{
		UpdateNavigationViewItemToolTip();
	}

	internal void UpdateVisualState()
	{
		if (!_appliedTemplate)
		{
			return;
		}
		if (_presenter != null)
		{
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":selected", ((ListBoxItem)this).IsSelected);
		}
		UpdateVisualStateForNavigationViewPositionChange();
		bool shouldShowIcon = ShouldShowIcon;
		bool shouldShowContent = ShouldShowContent;
		if (IsOnLeftNav)
		{
			if (_presenter != null)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":iconcollapsed", !shouldShowIcon);
			}
		}
		else if (_presenter != null)
		{
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":iconcollapsed", false);
		}
		UpdateVisualStateForToolTip();
		UpdateVisualStateForIconAndContent(shouldShowIcon, shouldShowContent);
		UpdateVisualStateForInfoBadge();
		UpdateVisualStateForChevron();
	}

	private void UpdateVisualStateForChevron()
	{
		if (_presenter != null)
		{
			bool flag = HasChildren && (!_isClosedCompact || !ShouldRepeaterShowInFlyout);
			bool isExpanded = IsExpanded;
			if (_presenter != null)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":chevronopen", flag & isExpanded);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":chevronclosed", flag & !isExpanded);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":chevronhidden", !flag);
			}
		}
	}

	internal void ShowHideChildren()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if (_repeater == null)
		{
			return;
		}
		bool isExpanded = IsExpanded;
		((Visual)_repeater).IsVisible = isExpanded;
		if (!ShouldRepeaterShowInFlyout)
		{
			return;
		}
		if (isExpanded)
		{
			if (!_isRepeaterParentedToFlyout)
			{
				ReparentRepeater();
			}
			Dispatcher.UIThread.Post((Action)delegate
			{
				FlyoutBase.ShowAttachedFlyout((Control)(object)_rootGrid);
			}, default(DispatcherPriority));
		}
		else
		{
			FlyoutBase attachedFlyout = FlyoutBase.GetAttachedFlyout((Control)(object)_rootGrid);
			if (attachedFlyout != null)
			{
				attachedFlyout.Hide();
			}
		}
	}

	private void ReparentRepeater()
	{
		if (HasChildren && _repeater != null)
		{
			if (ShouldRepeaterShowInFlyout && !_isRepeaterParentedToFlyout)
			{
				((AvaloniaList<Control>)(object)((Panel)_rootGrid).Children).Remove((Control)(object)_repeater);
				((AvaloniaList<Control>)(object)_flyoutContentGrid.Children).Add((Control)(object)_repeater);
				_isRepeaterParentedToFlyout = true;
				PropagateDepthToChildren(0);
			}
			else if (!ShouldRepeaterShowInFlyout && _isRepeaterParentedToFlyout)
			{
				((AvaloniaList<Control>)(object)_flyoutContentGrid.Children).Remove((Control)(object)_repeater);
				((AvaloniaList<Control>)(object)((Panel)_rootGrid).Children).Add((Control)(object)_repeater);
				_isRepeaterParentedToFlyout = false;
				PropagateDepthToChildren(1);
			}
		}
	}

	private void UpdateItemIndentation()
	{
		if (_presenter != null)
		{
			int num = base.Depth * _itemIndentation;
			_presenter.UpdateContentLeftIndentation(num);
		}
	}

	internal void PropagateDepthToChildren(int depth)
	{
		if (_repeater == null || _repeater.ItemsSourceView == null)
		{
			return;
		}
		int count = _repeater.ItemsSourceView.Count;
		for (int i = 0; i < count; i++)
		{
			if (_repeater.TryGetElement(i) is FANavigationViewItemBase fANavigationViewItemBase)
			{
				fANavigationViewItemBase.Depth = depth;
			}
		}
	}

	internal void OnExpandCollapseChevronTapped(object sender, RoutedEventArgs args)
	{
		IsExpanded = !IsExpanded;
		args.Handled = true;
	}

	private void OnFlyoutClosing(object sender, CancelEventArgs args)
	{
		IsExpanded = false;
	}

	internal void RotateExpandCollapseChevron(bool isExpanded)
	{
		if (_presenter != null)
		{
			_presenter.RotateExpandCollapseChevron(isExpanded);
		}
	}

	private void UnhookEventsAndClearFields()
	{
		if (_rootGrid != null)
		{
			FlyoutBase attachedFlyout = FlyoutBase.GetAttachedFlyout((Control)(object)_rootGrid);
			PopupFlyoutBase val = (PopupFlyoutBase)(object)((attachedFlyout is PopupFlyoutBase) ? attachedFlyout : null);
			if (val != null)
			{
				val.Closing -= OnFlyoutClosing;
			}
			_rootGrid = null;
		}
		_splitViewRevokers?.Dispose();
		_splitViewRevokers = null;
		FANavigationView getNavigationView = base.GetNavigationView;
		if (getNavigationView != null && _repeater != null)
		{
			_repeater.ElementPrepared -= getNavigationView.OnRepeaterElementPrepared;
			_repeater.ElementClearing -= getNavigationView.OnRepeaterElementClearing;
			if (_repeater.ItemsSourceView != null)
			{
				_repeater.ItemsSourceView.CollectionChanged -= OnItemsSourceViewChanged;
			}
			_repeater.ItemsSource = null;
			_repeater = null;
		}
		_presenter = null;
		_flyoutContentGrid = null;
	}

	private void UpdateVisualStateForInfoBadge()
	{
		if (_presenter != null)
		{
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_presenter).Classes, ":infobadge", InfoBadge != null);
		}
	}

	public override string ToString()
	{
		return ((ContentControl)this).Content?.ToString() ?? "NavigationViewItem";
	}

	private void PrepNavigationViewItem(SplitView splitView)
	{
		_splitViewRevokers = new FACompositeDisposable(AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)splitView, (AvaloniaProperty)(object)SplitView.IsPaneOpenProperty).Subscribe(OnSplitViewPropertyChanged), AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)splitView, (AvaloniaProperty)(object)SplitView.DisplayModeProperty).Subscribe(OnSplitViewPropertyChanged), AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)splitView, (AvaloniaProperty)(object)SplitView.CompactPaneLengthProperty).Subscribe(OnSplitViewPropertyChanged));
		UpdateCompactPaneLength();
		UpdateIsClosedCompact();
	}

	private void HandleLoaded(object sender, RoutedEventArgs args)
	{
		SplitView getSplitView = base.GetSplitView;
		if (getSplitView != null)
		{
			PrepNavigationViewItem(getSplitView);
		}
		UpdateVisualStateForChevron();
		((Control)this).Loaded -= HandleLoaded;
	}

	private void HandleExpansionStateMemory()
	{
		if (!base.IsTopLevelItem)
		{
			return;
		}
		SplitView getSplitView = base.GetSplitView;
		if (getSplitView != null)
		{
			if (getSplitView.IsPaneOpen)
			{
				RestoreExpandedState();
			}
			else
			{
				ForceCollapse();
			}
		}
	}

	private void ForceCollapse()
	{
		if (IsExpanded)
		{
			IsExpanded = false;
			_restoreToExpandedState = true;
		}
	}

	private void RestoreExpandedState()
	{
		if (_restoreToExpandedState)
		{
			IsExpanded = true;
			_restoreToExpandedState = false;
		}
	}
}
