using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Media.Animation;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a container that enables navigation of app content. It has a header, 
/// a view for the main content, and a menu pane for navigation commands.
/// </summary>
[PseudoClasses(new string[] { ":separator" })]
[PseudoClasses(new string[] { ":listsizecompact", ":closedcompact" })]
[PseudoClasses(new string[] { ":backbuttoncollapsed", ":panecollapsed", ":headercollapsed" })]
[PseudoClasses(new string[] { ":minimalwithback", ":minimal", ":topnavminimal", ":compact", ":expanded" })]
[PseudoClasses(new string[] { ":autosuggestcollapsed", ":settingscollapsed", ":panetogglecollapsed", ":panenotoverlaying" })]
[TemplatePart("TogglePaneButton", typeof(Button))]
[TemplatePart("PaneHeaderContentBorder", typeof(ContentControl))]
[TemplatePart("PaneCustomContentBorder", typeof(ContentControl))]
[TemplatePart("FooterContentBorder", typeof(ContentControl))]
[TemplatePart("PaneHeaderOnTopPane", typeof(ContentControl))]
[TemplatePart("PaneTitleOnTopPane", typeof(ContentControl))]
[TemplatePart("PaneCustomContentOnTopPane", typeof(ContentControl))]
[TemplatePart("PaneFooterOnTopPane", typeof(ContentControl))]
[TemplatePart("RootSplitView", typeof(SplitView))]
[TemplatePart("TopNavGrid", typeof(Grid))]
[TemplatePart("MenuItemsHost", typeof(FAItemsRepeater))]
[TemplatePart("TopNavMenuItemsHost", typeof(FAItemsRepeater))]
[TemplatePart("TopNavMenuItemsOverflowHost", typeof(FAItemsRepeater))]
[TemplatePart("TopNavOverflowButton", typeof(Button))]
[TemplatePart("FooterMenuItemsHost", typeof(FAItemsRepeater))]
[TemplatePart("TopFooterMenuItemsHost", typeof(FAItemsRepeater))]
[TemplatePart("TopNavContentOverlayAreaGrid", typeof(Border))]
[TemplatePart("PaneAutoSuggestBoxPresenter", typeof(ContentControl))]
[TemplatePart("TopPaneAutoSuggestBoxPresenter", typeof(ContentControl))]
[TemplatePart("PaneContentGrid", typeof(Grid))]
[TemplatePart("ContentLeftPadding", typeof(Rectangle))]
[TemplatePart("PlaceholderGrid", typeof(Grid))]
[TemplatePart("PaneTitleTextBlock", typeof(Control))]
[TemplatePart("PaneTitlePresenter", typeof(ContentControl))]
[TemplatePart("PaneTitleHolder", typeof(Control))]
[TemplatePart("PaneAutoSuggestButton", typeof(Button))]
[TemplatePart("NavigationViewBackButton", typeof(Button))]
[TemplatePart("NavigationViewCloseButton", typeof(Button))]
[TemplatePart("MenuItemsScrollViewer", typeof(ScrollViewer))]
[TemplatePart("FooterItemsScrollViewer", typeof(ScrollViewer))]
[TemplatePart("ItemsContainerGrid", typeof(Control))]
public class FANavigationView : HeaderedContentControl
{
	private class StepEasingFunction : Easing
	{
		public int Steps { get; set; }

		public override double Ease(double progress)
		{
			return Math.Round(progress * (double)Steps) * (double)(1 / Steps);
		}
	}

	private NavigationViewItemsFactory _itemsFactory;

	private Button _paneToggleButton;

	private SplitView _splitView;

	private RowDefinition _itemsContainerRow;

	private ScrollViewer _menuItemsScrollViewer;

	private ScrollViewer _footerItemsScrollViewer;

	private Grid _paneContentGrid;

	private Control _paneTitleHolderFrameworkElement;

	private Control _paneTitleFrameworkElement;

	private Button _paneSearchButton;

	private Button _backButton;

	private Button _closeButton;

	private FAItemsRepeater _leftNavRepeater;

	private FAItemsRepeater _topNavRepeater;

	private FAItemsRepeater _leftNavFooterMenuRepeater;

	private FAItemsRepeater _topNavFooterMenuRepeater;

	private Button _topNavOverflowButton;

	private FAItemsRepeater _topNavRepeaterOverflowView;

	private Grid _topNavGrid;

	private Border _topNavContentOverlayAreaGrid;

	private Control _itemsContainer;

	private Control _prevIndicator;

	private Control _nextIndicator;

	private Control _activeIndicator;

	private object _lastSelectedItemPendingAnimationInTopNav;

	private Control _contentLeftPadding;

	private ContentControl _leftNavAutoSuggestBoxPresenter;

	private ContentControl _topNavAutoSuggestBoxPresenter;

	private ContentControl _leftNavPaneHeaderContentBorder;

	private ContentControl _leftNavPaneCustomContentBorder;

	private ContentControl _leftNavFooterContentBorder;

	private ContentControl _paneHeaderOnTopPane;

	private ContentControl _paneTitleOnTopPane;

	private ContentControl _paneCustomContentOnTopPane;

	private ContentControl _paneFooterOnTopPane;

	private ContentControl _paneTitlePresenter;

	private ColumnDefinition _paneHeaderCloseButtonColumn;

	private ColumnDefinition _paneHeaderToggleButtonColumn;

	private RowDefinition _paneHeaderContentBorderRow;

	private FANavigationViewItem _lastItemExpandedIntoFlyout;

	private IDisposable _splitViewRevokers;

	private IDisposable _sizeChangedRevoker;

	private IDisposable _paneTitleHolderRevoker;

	private IDisposable _itemsContainerSizeRevoker;

	private bool _wasForceClosed;

	private bool _isClosedCompact;

	private bool _blockNextClosingEvent;

	private bool _isLeftPaneTitleEmpty;

	private TopNavigationViewDataProvider _topDataProvider;

	private SelectionModel _selectionModel;

	private AvaloniaList<IEnumerable> _selectionModelSource;

	private ItemsSourceView _menuItemsSource;

	private ItemsSourceView _footerItemsSource;

	private bool _appliedTemplate;

	private bool _fromOnApplyTemplate;

	private bool _updateVisualStateForDisplayModeFromOnLoaded;

	private bool _shouldIgnoreNextSelectionChange;

	private bool _selectionChangeFromOverflowMenu;

	private bool _shouldRaiseItemInvokedAfterSelection;

	private TopNavigationViewLayoutState _topNavigationMode;

	private readonly float _topNavigationRecoveryGracePeriodWidth = 5f;

	private bool _isOpenPaneForInteraction;

	private bool _moveTopNavOverflowItemOnFlyoutClose;

	private bool _shouldIgnoreUIASelectionRaiseAsExpandCollapseWillRaise;

	private bool _orientationChangedPendingAnimation;

	private bool _tabKeyPrecedesFocusChange;

	private bool _initialNonForcedModeUpdate = true;

	private static readonly FASymbolIconSource _settingsIconSource;

	private const int _backButtonHeight = 40;

	private const int _backButtonWidth = 40;

	private const int _paneToggleButtonHeight = 40;

	private const int _paneToggleButtonWidth = 40;

	private const int _backButtonRowDefinition = 1;

	private const float paneElevationTranslationZ = 32f;

	private const int c_toggleButtonHeightWithNoBackButton = 56;

	private const int _mainMenuBlockIndex = 0;

	private const int _footerMenuBlockIndex = 1;

	private const int _itemNotFound = -1;

	private double _openPaneWidth = 320.0;

	private bool _isSelectionChangedPending;

	private object _pendingSelectionChangedItem;

	private NavigationRecommendedTransitionDirection _pendingSelectionChangedDirection;

	private const string SR_SettingsButtonName = "SettingsButtonName";

	private const string SR_NavigationOverflowButtonToolTip = "NavigationOverflowButtonToolTip";

	private const string SR_NavigationViewSearchButtonName = "NavigationViewSearchButtonName";

	private const string SR_NavigationBackButtonToolTip = "NavigationBackButtonToolTip";

	private const string SR_NavigationButtonOpenName = "NavigationButtonOpenName";

	private const string SR_NavigationButtonClosedName = "NavigationButtonClosedName";

	private const string SR_NavigationOverflowButtonName = "NavigationOverflowButtonName";

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.AlwaysShowHeader" /> property
	/// </summary>
	public static readonly StyledProperty<bool> AlwaysShowHeaderProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.AutoCompleteBox" /> property
	/// </summary>
	public static readonly StyledProperty<AutoCompleteBox> AutoCompleteBoxProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.CompactModeThresholdWidth" /> property
	/// </summary>
	public static readonly StyledProperty<double> CompactModeThresholdWidthProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.CompactPaneLength" /> property
	/// </summary>
	public static readonly StyledProperty<double> CompactPaneLengthProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.ContentOverlay" /> property
	/// </summary>
	public static readonly StyledProperty<Control> ContentOverlayProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.DisplayMode" /> property
	/// </summary>
	public static readonly DirectProperty<FANavigationView, FANavigationViewDisplayMode> DisplayModeProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.ExpandedModeThresholdWidth" /> property
	/// </summary>
	public static readonly StyledProperty<double> ExpandedModeThresholdWidthProperty;

	/// <summary>
	/// Defines the <see cref="F:FluentAvalonia.UI.Controls.FANavigationView.FooterMenuItemsProperty" />
	/// </summary>
	public static readonly DirectProperty<FANavigationView, IList<object>> FooterMenuItemsProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.FooterMenuItems" /> property
	/// </summary>
	public static readonly StyledProperty<IEnumerable> FooterMenuItemsSourceProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.IsBackButtonVisible" /> property
	/// </summary>
	/// <remarks>
	/// In WinUI, this is an enum NavigationViewBackButtonVisible with values
	/// Visible, Collapsed, and Auto (depends on form factor). For our purposes,
	/// bool works just fine for now
	/// </remarks>
	public static readonly StyledProperty<bool> IsBackButtonVisibleProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.IsBackEnabled" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsBackEnabledProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.IsPaneOpen" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsPaneOpenProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.IsPaneToggleButtonVisible" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsPaneToggleButtonVisibleProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.IsPaneVisible" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsPaneVisibleProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.IsSettingsVisible" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsSettingsVisibleProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.MenuItems" /> property
	/// </summary>
	public static readonly DirectProperty<FANavigationView, IList<object>> MenuItemsProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.MenuItemsSource" /> property
	/// </summary>
	public static readonly StyledProperty<IEnumerable> MenuItemsSourceProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.MenuItemTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> MenuItemTemplateProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.MenuItemTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<FADataTemplateSelector> MenuItemTemplateSelectorProperty;

	public static readonly StyledProperty<ControlTheme> MenuItemContainerThemeProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.OpenPaneLength" /> property
	/// </summary>
	public static readonly StyledProperty<double> OpenPaneLengthProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.PaneCustomContent" /> property
	/// </summary>
	public static readonly StyledProperty<Control> PaneCustomContentProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.PaneDisplayMode" /> property
	/// </summary>
	public static readonly StyledProperty<FANavigationViewPaneDisplayMode> PaneDisplayModeProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.PaneFooter" /> property
	/// </summary>
	public static readonly StyledProperty<Control> PaneFooterProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.PaneHeader" /> property
	/// </summary>
	public static readonly StyledProperty<Control> PaneHeaderProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.PaneTitle" /> property
	/// </summary>
	public static readonly StyledProperty<string> PaneTitleProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.SelectedItem" /> property
	/// </summary>
	public static readonly DirectProperty<FANavigationView, object> SelectedItemProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.SelectionFollowsFocus" /> property
	/// </summary>
	/// <remarks>
	/// WinUI uses an enum here, but only has Disabled/Enabled, so just use bool
	/// </remarks>
	public static readonly StyledProperty<bool> SelectionFollowsFocusProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.SettingsItem" /> property
	/// </summary>
	public static readonly DirectProperty<FANavigationView, FANavigationViewItem> SettingsItemProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationView.TemplateSettings" /> property
	/// </summary>
	public static readonly StyledProperty<FANavigationViewTemplateSettings> TemplateSettingsProperty;

	/// <summary>
	/// Property that stores disposables to each NavigationViewItem when their created in the ItemsRepeater,
	/// so they can be disposed when the item is removed
	/// </summary>
	internal static readonly AttachedProperty<FACompositeDisposable> NavigationViewItemBaseRevokersProperty;

	private object _selectedItem;

	private IList<object> _menuItems;

	private IList<object> _footerMenuItems;

	private FANavigationViewDisplayMode _displayMode;

	private FANavigationViewItem _settingsItem;

	private const string s_tpTogglePaneButton = "TogglePaneButton";

	private const string s_tpPaneHeaderContentBorder = "PaneHeaderContentBorder";

	private const string s_tpPaneCustomContentBorder = "PaneCustomContentBorder";

	private const string s_tpFooterContentBorder = "FooterContentBorder";

	private const string s_tpPaneHeaderOnTopPane = "PaneHeaderOnTopPane";

	private const string s_tpPaneTitleOnTopPane = "PaneTitleOnTopPane";

	private const string s_tpPaneCustomContentOnTopPane = "PaneCustomContentOnTopPane";

	private const string s_tpPaneFooterOnTopPane = "PaneFooterOnTopPane";

	private const string s_tpRootSplitView = "RootSplitView";

	private const string s_tpTopNavGrid = "TopNavGrid";

	private const string s_tpMenuItemsHost = "MenuItemsHost";

	private const string s_tpTopNavMenuItemsHost = "TopNavMenuItemsHost";

	private const string s_tpTopNavMenuItemsOverflowHost = "TopNavMenuItemsOverflowHost";

	private const string s_tpTopNavOverflowButton = "TopNavOverflowButton";

	private const string s_tpFooterMenuItemsHost = "FooterMenuItemsHost";

	private const string s_tpTopFooterMenuItemsHost = "TopFooterMenuItemsHost";

	private const string s_tpTopNavContentOverlayAreaGrid = "TopNavContentOverlayAreaGrid";

	private const string s_tpPaneAutoSuggestBoxPresenter = "PaneAutoSuggestBoxPresenter";

	private const string s_tpTopPaneAutoSuggestBoxPresenter = "TopPaneAutoSuggestBoxPresenter";

	private const string s_tpPaneContentGrid = "PaneContentGrid";

	private const string s_tpContentLeftPadding = "ContentLeftPadding";

	private const string s_tpPlaceholderGrid = "PlaceholderGrid";

	private const string s_tpPaneTitleTextBlock = "PaneTitleTextBlock";

	private const string s_tpPaneTitlePresenter = "PaneTitlePresenter";

	private const string s_tpPaneTitleHolder = "PaneTitleHolder";

	private const string s_tpPaneAutoSuggestButton = "PaneAutoSuggestButton";

	private const string s_tpNavigationViewBackButton = "NavigationViewBackButton";

	private const string s_tpNavigationViewCloseButton = "NavigationViewCloseButton";

	private const string s_tpMenuItemsScrollViewer = "MenuItemsScrollViewer";

	private const string s_tpFooterItemsScrollViewer = "FooterItemsScrollViewer";

	private const string s_tpItemsContainerGrid = "ItemsContainerGrid";

	private const string s_pcSeparator = ":separator";

	private const string s_pcListSizeCompact = ":listsizecompact";

	private const string s_pcBackButtonCollapsed = ":backbuttoncollapsed";

	private const string s_pcMinimalWithBack = ":minimalwithback";

	private const string s_pcMinimal = ":minimal";

	private const string s_pcTopNavMinimal = ":topnavminimal";

	private const string s_pcCompact = ":compact";

	private const string s_pcExpanded = ":expanded";

	private const string s_pcAutoSuggestCollapsed = ":autosuggestcollapsed";

	private const string s_pcSettingsCollapsed = ":settingscollapsed";

	private const string s_pcPaneToggleCollapsed = ":panetogglecollapsed";

	private const string s_pcPaneNotOverlaying = ":panenotoverlaying";

	private const string s_pcClosedCompact = ":closedcompact";

	private const string s_pcPaneCollapsed = ":panecollapsed";

	private const string s_pcHeaderCollapsed = ":headercollapsed";

	private const string s_resPaneToggleButtonWidth = "PaneToggleButtonWidth";

	private const string s_resPaneToggleButtonHeight = "PaneToggleButtonHeight";

	private int SelectedItemIndex => _topDataProvider.IndexOf(SelectedItem);

	internal bool IsTopNavigationView => PaneDisplayMode == FANavigationViewPaneDisplayMode.Top;

	private bool IsTopPrimaryListVisible
	{
		get
		{
			if (_topNavRepeater != null)
			{
				return TemplateSettings.TopPaneVisibility;
			}
			return false;
		}
	}

	internal bool IsOverlay
	{
		get
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Invalid comparison between Unknown and I4
			if (_splitView != null)
			{
				return (int)_splitView.DisplayMode == 2;
			}
			return false;
		}
	}

	private bool IsLightDismissable
	{
		get
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Invalid comparison between Unknown and I4
			if (_splitView != null)
			{
				if ((int)_splitView.DisplayMode != 0)
				{
					return (int)_splitView.DisplayMode != 1;
				}
				return false;
			}
			return false;
		}
	}

	internal bool ShouldShowBackButton
	{
		get
		{
			if (DisplayMode == FANavigationViewDisplayMode.Minimal && IsPaneOpen)
			{
				return false;
			}
			return ShouldShowBackOrCloseButton;
		}
	}

	internal bool ShouldShowCloseButton
	{
		get
		{
			if (_backButton != null && _closeButton != null)
			{
				if (!IsPaneOpen)
				{
					return false;
				}
				FANavigationViewPaneDisplayMode paneDisplayMode = PaneDisplayMode;
				if (paneDisplayMode != FANavigationViewPaneDisplayMode.LeftMinimal && (paneDisplayMode != FANavigationViewPaneDisplayMode.Auto || DisplayMode != FANavigationViewDisplayMode.Minimal))
				{
					return false;
				}
				return ShouldShowBackOrCloseButton;
			}
			return false;
		}
	}

	internal bool ShouldShowBackOrCloseButton => IsBackButtonVisible;

	private int GetNavigationViewItemCountInPrimaryList => _topDataProvider?.NavigationViewItemCountInPrimaryList ?? 0;

	private int GetNavigationViewItemCountInTopNav => _topDataProvider?.NavigationViewItemCountInTopNav ?? 0;

	private double GetTopNavigationViewActualWidth
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			Rect bounds = ((Visual)_topNavGrid).Bounds;
			return ((Rect)(ref bounds)).Width;
		}
	}

	internal NavigationViewItemsFactory ItemsFactory => _itemsFactory;

	internal SplitView GetSplitView => _splitView;

	/// <summary>
	/// Gets or sets a value that indicates whether the header is always visible.
	/// </summary>
	public bool AlwaysShowHeader
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(AlwaysShowHeaderProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(AlwaysShowHeaderProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets an <see cref="T:Avalonia.Controls.AutoCompleteBox" /> to be displayed in the NavigationView.
	/// </summary>
	public AutoCompleteBox AutoCompleteBox
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<AutoCompleteBox>(AutoCompleteBoxProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<AutoCompleteBox>(AutoCompleteBoxProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the minimum window width at which the NavigationView enters Compact display mode.
	/// </summary>
	public double CompactModeThresholdWidth
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(CompactModeThresholdWidthProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(CompactModeThresholdWidthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the width of the NavigationView pane in its compact display mode.
	/// </summary>
	public double CompactPaneLength
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(CompactPaneLengthProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(CompactPaneLengthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a UI element that is shown at the top of the control, below the pane 
	/// if PaneDisplayMode is Top.
	/// </summary>
	public Control ContentOverlay
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<Control>(ContentOverlayProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<Control>(ContentOverlayProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets a value that specifies how the pane and content areas of a NavigationView are being shown.
	/// </summary>
	public FANavigationViewDisplayMode DisplayMode
	{
		get
		{
			return _displayMode;
		}
		private set
		{
			((AvaloniaObject)this).SetAndRaise<FANavigationViewDisplayMode>((DirectPropertyBase<FANavigationViewDisplayMode>)(object)DisplayModeProperty, ref _displayMode, value);
		}
	}

	/// <summary>
	/// Gets or sets the minimum window width at which the NavigationView enters Expanded display mode.
	/// </summary>
	public double ExpandedModeThresholdWidth
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(ExpandedModeThresholdWidthProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(ExpandedModeThresholdWidthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the list of objects to be used as navigation items in the footer menu.
	/// </summary>
	public IList<object> FooterMenuItems
	{
		get
		{
			return _footerMenuItems;
		}
		private set
		{
			((AvaloniaObject)this).SetAndRaise<IList<object>>((DirectPropertyBase<IList<object>>)(object)FooterMenuItemsProperty, ref _footerMenuItems, value);
		}
	}

	/// <summary>
	/// Gets or sets the object that represents the navigation items to be used in the footer menu.
	/// </summary>
	public IEnumerable FooterMenuItemsSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IEnumerable>(FooterMenuItemsSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IEnumerable>(FooterMenuItemsSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the back button is visible or not.
	/// </summary>
	public bool IsBackButtonVisible
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsBackButtonVisibleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsBackButtonVisibleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the back button is enabled or disabled.
	/// </summary>
	public bool IsBackEnabled
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsBackEnabledProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsBackEnabledProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that specifies whether the NavigationView pane is expanded to its full width.
	/// </summary>
	public bool IsPaneOpen
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsPaneOpenProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsPaneOpenProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the menu toggle button is shown.
	/// </summary>
	public bool IsPaneToggleButtonVisible
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsPaneToggleButtonVisibleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsPaneToggleButtonVisibleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that determines whether the pane is shown.
	/// </summary>
	public bool IsPaneVisible
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsPaneVisibleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsPaneVisibleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the settings button is shown.
	/// </summary>
	public bool IsSettingsVisible
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsSettingsVisibleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsSettingsVisibleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the DataTemplate used to display each menu item.
	/// </summary>
	public IDataTemplate MenuItemTemplate
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IDataTemplate>(MenuItemTemplateProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IDataTemplate>(MenuItemTemplateProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a reference to a custom DataTemplateSelector logic class. The DataTemplateSelector 
	/// referenced by this property returns a template to apply to items.
	/// </summary>
	/// <remarks>
	/// This property should generally not be used but was added to support different containers for different
	/// data types. Should a more "Avalonia-like" solution arise, this property will be removed
	/// </remarks>
	public FADataTemplateSelector MenuItemTemplateSelector
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FADataTemplateSelector>(MenuItemTemplateSelectorProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FADataTemplateSelector>(MenuItemTemplateSelectorProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the ControlTheme applied to MenuItems
	/// </summary>
	public ControlTheme MenuItemContainerTheme
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ControlTheme>(MenuItemContainerThemeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ControlTheme>(MenuItemContainerThemeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the collection of menu items displayed in the NavigationView.
	/// </summary>
	public IList<object> MenuItems
	{
		get
		{
			return _menuItems;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<IList<object>>((DirectPropertyBase<IList<object>>)(object)MenuItemsProperty, ref _menuItems, value);
		}
	}

	/// <summary>
	/// Gets or sets an object source used to generate the content of the NavigationView menu.
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
	/// Gets or sets the width of the NavigationView pane when it's fully expanded.
	/// </summary>
	public double OpenPaneLength
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(OpenPaneLengthProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(OpenPaneLengthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a UI element that is shown in the NavigationView pane.
	/// </summary>
	public Control PaneCustomContent
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<Control>(PaneCustomContentProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<Control>(PaneCustomContentProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates how and where the NavigationView pane is shown.
	/// </summary>
	public FANavigationViewPaneDisplayMode PaneDisplayMode
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FANavigationViewPaneDisplayMode>(PaneDisplayModeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FANavigationViewPaneDisplayMode>(PaneDisplayModeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the content for the pane footer.
	/// </summary>
	public Control PaneFooter
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<Control>(PaneFooterProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<Control>(PaneFooterProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the content for the pane header.
	/// </summary>
	public Control PaneHeader
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<Control>(PaneHeaderProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<Control>(PaneHeaderProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the label adjacent to the menu icon when the NavigationView pane is open.
	/// </summary>
	public string PaneTitle
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(PaneTitleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(PaneTitleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the selected item.
	/// </summary>
	public object SelectedItem
	{
		get
		{
			return _selectedItem;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<object>((DirectPropertyBase<object>)(object)SelectedItemProperty, ref _selectedItem, value);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether item selection changes when keyboard focus changes.
	/// </summary>
	/// <remarks>
	/// Do not set this property to true if you have hierarchical navigation as things get weird. This
	/// behavior also occurs in WinUI
	/// </remarks>
	public bool SelectionFollowsFocus
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(SelectionFollowsFocusProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(SelectionFollowsFocusProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the navigation item that represents the entry point to app settings.
	/// </summary>
	public FANavigationViewItem SettingsItem
	{
		get
		{
			return _settingsItem;
		}
		internal set
		{
			((AvaloniaObject)this).SetAndRaise<FANavigationViewItem>((DirectPropertyBase<FANavigationViewItem>)(object)SettingsItemProperty, ref _settingsItem, value);
		}
	}

	/// <summary>
	/// Gets an object that provides calculated values that can be referenced as TemplateBinding sources 
	/// when defining templates for a NavigationView control.
	/// </summary>
	public FANavigationViewTemplateSettings TemplateSettings
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FANavigationViewTemplateSettings>(TemplateSettingsProperty);
		}
		protected set
		{
			((AvaloniaObject)this).SetValue<FANavigationViewTemplateSettings>(TemplateSettingsProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Occurs when the NavigationView pane is closing.
	/// </summary>
	public event TypedEventHandler<FANavigationView, FANavigationViewPaneClosingEventArgs> PaneClosing;

	/// <summary>
	/// Occurs when the NavigationView pane is closed.
	/// </summary>
	public event TypedEventHandler<FANavigationView, EventArgs> PaneClosed;

	/// <summary>
	/// Occurs when the NavigationView pane is opening.
	/// </summary>
	public event TypedEventHandler<FANavigationView, EventArgs> PaneOpening;

	/// <summary>
	/// Occurs when the NavigationView pane is opened.
	/// </summary>
	public event TypedEventHandler<FANavigationView, EventArgs> PaneOpened;

	/// <summary>
	/// Occurs when the back button receives an interaction such as a click or tap.
	/// </summary>
	public event EventHandler<FANavigationViewBackRequestedEventArgs> BackRequested;

	/// <summary>
	/// Occurs when the currently selected item changes.
	/// </summary>
	public event EventHandler<FANavigationViewSelectionChangedEventArgs> SelectionChanged;

	/// <summary>
	/// Occurs when an item in the menu receives an interaction such a a click or tap.
	/// </summary>
	public event EventHandler<FANavigationViewItemInvokedEventArgs> ItemInvoked;

	/// <summary>
	/// Occurs when the DisplayMode property changes.
	/// </summary>
	public event EventHandler<FANavigationViewDisplayModeChangedEventArgs> DisplayModeChanged;

	/// <summary>
	/// Occurs when a node in the tree starts to expand.
	/// </summary>
	public event EventHandler<FANavigationViewItemExpandingEventArgs> ItemExpanding;

	/// <summary>
	/// Occurs when a node in the tree is collapsed.
	/// </summary>
	public event EventHandler<FANavigationViewItemCollapsedEventArgs> ItemCollapsed;

	public FANavigationView()
	{
		TemplateSettings = new FANavigationViewTemplateSettings();
		_sizeChangedRevoker = AvaloniaObjectExtensions.GetObservable<Rect>((AvaloniaObject)(object)this, (AvaloniaProperty<Rect>)(object)Visual.BoundsProperty).Subscribe(OnSizeChanged);
		AvaloniaList<IEnumerable> obj = new AvaloniaList<IEnumerable>(2);
		obj.Add((IEnumerable)null);
		obj.Add((IEnumerable)null);
		_selectionModelSource = obj;
		_topDataProvider = new TopNavigationViewDataProvider(this);
		MenuItems = (IList<object>)new AvaloniaList<object>();
		FooterMenuItems = (IList<object>)new AvaloniaList<object>();
		_topDataProvider.OnRawDataChanged(delegate(NotifyCollectionChangedEventArgs args)
		{
			OnTopNavDataSourceChanged(args);
		});
		((Control)this).Loaded += OnNavViewLoaded;
		_selectionModel = new SelectionModel
		{
			SingleSelect = true,
			Source = _selectionModelSource
		};
		_selectionModel.SelectionChanged += OnSelectionModelSelectionChanged;
		_selectionModel.ChildrenRequested += OnSelectionModelChildrenRequested;
		_itemsFactory = new NavigationViewItemsFactory();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		try
		{
			_fromOnApplyTemplate = true;
			UnhookEventsAndClearFields();
			((TemplatedControl)this).OnApplyTemplate(e);
			_paneToggleButton = NameScopeExtensions.Get<Button>(e.NameScope, "TogglePaneButton");
			if (_paneToggleButton != null)
			{
				_paneToggleButton.Click += OnPaneToggleButtonClick;
				SetPaneToggleButtonAutomationName();
			}
			_leftNavPaneHeaderContentBorder = NameScopeExtensions.Get<ContentControl>(e.NameScope, "PaneHeaderContentBorder");
			_leftNavPaneCustomContentBorder = NameScopeExtensions.Get<ContentControl>(e.NameScope, "PaneCustomContentBorder");
			_leftNavFooterContentBorder = NameScopeExtensions.Get<ContentControl>(e.NameScope, "FooterContentBorder");
			_paneHeaderOnTopPane = NameScopeExtensions.Get<ContentControl>(e.NameScope, "PaneHeaderOnTopPane");
			_paneTitleOnTopPane = NameScopeExtensions.Get<ContentControl>(e.NameScope, "PaneTitleOnTopPane");
			_paneCustomContentOnTopPane = NameScopeExtensions.Get<ContentControl>(e.NameScope, "PaneCustomContentOnTopPane");
			_paneFooterOnTopPane = NameScopeExtensions.Get<ContentControl>(e.NameScope, "PaneFooterOnTopPane");
			_splitView = NameScopeExtensions.Get<SplitView>(e.NameScope, "RootSplitView");
			if (_splitView != null)
			{
				_splitViewRevokers = new FACompositeDisposable(AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)_splitView, (AvaloniaProperty)(object)SplitView.IsPaneOpenProperty).Subscribe(OnSplitViewClosedCompactChanged), AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)_splitView, (AvaloniaProperty)(object)SplitView.DisplayModeProperty).Subscribe(OnSplitViewClosedCompactChanged));
				_splitView.PaneClosed += OnSplitViewPaneClosed;
				_splitView.PaneClosing += OnSplitViewPaneClosing;
				_splitView.PaneOpened += OnSplitViewPaneOpened;
				_splitView.PaneOpening += OnSplitViewPaneOpening;
				UpdateIsClosedCompact();
			}
			_topNavGrid = NameScopeExtensions.Get<Grid>(e.NameScope, "TopNavGrid");
			_leftNavRepeater = NameScopeExtensions.Get<FAItemsRepeater>(e.NameScope, "MenuItemsHost");
			if (_leftNavRepeater != null)
			{
				(_leftNavRepeater.Layout as FAStackLayout).DisableVirtualization = true;
				_leftNavRepeater.ElementPrepared += OnRepeaterElementPrepared;
				_leftNavRepeater.ElementClearing += OnRepeaterElementClearing;
				((Control)_leftNavRepeater).Loaded += OnRepeaterLoaded;
				((InputElement)_leftNavRepeater).GettingFocus += OnRepeaterGettingFocus;
				_leftNavRepeater.ItemTemplate = (IDataTemplate)(object)_itemsFactory;
			}
			_topNavRepeater = NameScopeExtensions.Get<FAItemsRepeater>(e.NameScope, "TopNavMenuItemsHost");
			if (_topNavRepeater != null)
			{
				(_topNavRepeater.Layout as FAStackLayout).DisableVirtualization = true;
				_topNavRepeater.ElementPrepared += OnRepeaterElementPrepared;
				_topNavRepeater.ElementClearing += OnRepeaterElementClearing;
				((Control)_topNavRepeater).Loaded += OnRepeaterLoaded;
				((InputElement)_topNavRepeater).GettingFocus += OnRepeaterGettingFocus;
				_topNavRepeater.ItemTemplate = (IDataTemplate)(object)_itemsFactory;
			}
			_topNavRepeaterOverflowView = NameScopeExtensions.Get<FAItemsRepeater>(e.NameScope, "TopNavMenuItemsOverflowHost");
			if (_topNavRepeaterOverflowView != null)
			{
				(_topNavRepeaterOverflowView.Layout as FAStackLayout).DisableVirtualization = true;
				_topNavRepeaterOverflowView.ElementPrepared += OnRepeaterElementPrepared;
				_topNavRepeaterOverflowView.ElementClearing += OnRepeaterElementClearing;
				_topNavRepeater.ItemTemplate = (IDataTemplate)(object)_itemsFactory;
			}
			_topNavOverflowButton = NameScopeExtensions.Get<Button>(e.NameScope, "TopNavOverflowButton");
			if (_topNavOverflowButton != null)
			{
				FlyoutBase flyout = _topNavOverflowButton.Flyout;
				FlyoutBase obj = ((flyout is PopupFlyoutBase) ? flyout : null);
				if (obj != null)
				{
					((PopupFlyoutBase)obj).Closing += OnFlyoutClosing;
				}
				AutomationProperties.SetName((StyledElement)(object)_topNavOverflowButton, FALocalizationHelper.Instance.GetLocalizedStringResource("NavigationOverflowButtonName"));
				if (ToolTip.GetTip((Control)(object)_topNavOverflowButton) != null)
				{
					ToolTip.SetTip((Control)(object)_topNavOverflowButton, (object)FALocalizationHelper.Instance.GetLocalizedStringResource("NavigationOverflowButtonToolTip"));
				}
			}
			_leftNavFooterMenuRepeater = NameScopeExtensions.Get<FAItemsRepeater>(e.NameScope, "FooterMenuItemsHost");
			if (_leftNavFooterMenuRepeater != null)
			{
				(_leftNavFooterMenuRepeater.Layout as FAStackLayout).DisableVirtualization = true;
				_leftNavFooterMenuRepeater.ElementPrepared += OnRepeaterElementPrepared;
				_leftNavFooterMenuRepeater.ElementClearing += OnRepeaterElementClearing;
				((Control)_leftNavFooterMenuRepeater).Loaded += OnRepeaterLoaded;
				((InputElement)_leftNavFooterMenuRepeater).GettingFocus += OnRepeaterGettingFocus;
				_leftNavFooterMenuRepeater.ItemTemplate = (IDataTemplate)(object)_itemsFactory;
			}
			_topNavFooterMenuRepeater = NameScopeExtensions.Get<FAItemsRepeater>(e.NameScope, "TopFooterMenuItemsHost");
			if (_topNavFooterMenuRepeater != null)
			{
				(_topNavFooterMenuRepeater.Layout as FAStackLayout).DisableVirtualization = true;
				_topNavFooterMenuRepeater.ElementPrepared += OnRepeaterElementPrepared;
				_topNavFooterMenuRepeater.ElementClearing += OnRepeaterElementClearing;
				((Control)_topNavFooterMenuRepeater).Loaded += OnRepeaterLoaded;
				((InputElement)_topNavFooterMenuRepeater).GettingFocus += OnRepeaterGettingFocus;
				_topNavFooterMenuRepeater.ItemTemplate = (IDataTemplate)(object)_itemsFactory;
			}
			_topNavContentOverlayAreaGrid = NameScopeExtensions.Get<Border>(e.NameScope, "TopNavContentOverlayAreaGrid");
			_leftNavAutoSuggestBoxPresenter = NameScopeExtensions.Get<ContentControl>(e.NameScope, "PaneAutoSuggestBoxPresenter");
			_topNavAutoSuggestBoxPresenter = NameScopeExtensions.Get<ContentControl>(e.NameScope, "TopPaneAutoSuggestBoxPresenter");
			_paneContentGrid = NameScopeExtensions.Get<Grid>(e.NameScope, "PaneContentGrid");
			_contentLeftPadding = (Control)(object)NameScopeExtensions.Get<Rectangle>(e.NameScope, "ContentLeftPadding");
			Grid val = NameScopeExtensions.Get<Grid>(e.NameScope, "PlaceholderGrid");
			if (val != null)
			{
				_paneHeaderCloseButtonColumn = ((AvaloniaList<ColumnDefinition>)(object)val.ColumnDefinitions)[0];
				_paneHeaderToggleButtonColumn = ((AvaloniaList<ColumnDefinition>)(object)val.ColumnDefinitions)[1];
				_paneHeaderContentBorderRow = ((AvaloniaList<RowDefinition>)(object)val.RowDefinitions)[0];
			}
			_paneTitleFrameworkElement = NameScopeExtensions.Get<Control>(e.NameScope, "PaneTitleTextBlock");
			_paneTitlePresenter = NameScopeExtensions.Get<ContentControl>(e.NameScope, "PaneTitlePresenter");
			_paneTitleHolderFrameworkElement = NameScopeExtensions.Get<Control>(e.NameScope, "PaneTitleHolder");
			if (_paneTitleHolderFrameworkElement != null)
			{
				_paneTitleHolderRevoker = AvaloniaObjectExtensions.GetObservable<Rect>((AvaloniaObject)(object)_paneTitleHolderFrameworkElement, (AvaloniaProperty<Rect>)(object)Visual.BoundsProperty).Subscribe(OnPaneTitleHolderSizeChanged);
			}
			_paneSearchButton = NameScopeExtensions.Get<Button>(e.NameScope, "PaneAutoSuggestButton");
			if (_paneSearchButton != null)
			{
				_paneSearchButton.Click += OnPaneSearchButtonClick;
				string localizedStringResource = FALocalizationHelper.Instance.GetLocalizedStringResource("NavigationViewSearchButtonName");
				AutomationProperties.SetName((StyledElement)(object)_paneSearchButton, localizedStringResource);
				ToolTip.SetTip((Control)(object)_paneSearchButton, (object)localizedStringResource);
			}
			_backButton = NameScopeExtensions.Get<Button>(e.NameScope, "NavigationViewBackButton");
			if (_backButton != null)
			{
				_backButton.Click += OnBackButtonClicked;
				string localizedStringResource2 = FALocalizationHelper.Instance.GetLocalizedStringResource("NavigationBackButtonToolTip");
				ToolTip.SetTip((Control)(object)_backButton, (object)localizedStringResource2);
				AutomationProperties.SetName((StyledElement)(object)_backButton, localizedStringResource2);
			}
			_closeButton = NameScopeExtensions.Get<Button>(e.NameScope, "NavigationViewCloseButton");
			if (_closeButton != null)
			{
				_closeButton.Click += OnPaneToggleButtonClick;
				ToolTip.SetTip((Control)(object)_closeButton, (object)FALocalizationHelper.Instance.GetLocalizedStringResource("NavigationButtonOpenName"));
			}
			if (_paneContentGrid != null)
			{
				_itemsContainerRow = ((AvaloniaList<RowDefinition>)(object)_paneContentGrid.RowDefinitions)[((AvaloniaList<RowDefinition>)(object)_paneContentGrid.RowDefinitions).Count - 1];
			}
			_menuItemsScrollViewer = NameScopeExtensions.Get<ScrollViewer>(e.NameScope, "MenuItemsScrollViewer");
			_footerItemsScrollViewer = NameScopeExtensions.Get<ScrollViewer>(e.NameScope, "FooterItemsScrollViewer");
			_itemsContainer = NameScopeExtensions.Find<Control>(e.NameScope, "ItemsContainerGrid");
			if (_itemsContainerRow != null)
			{
				_itemsContainerSizeRevoker = AvaloniaObjectExtensions.GetObservable<Rect>((AvaloniaObject)(object)_itemsContainer, (AvaloniaProperty<Rect>)(object)Visual.BoundsProperty).Subscribe(OnItemsContainerSizeChanged);
			}
			UpdatePaneShadow();
			_appliedTemplate = true;
			UpdatePaneDisplayMode();
			UpdateHeaderVisibility();
			UpdatePaneTitleFrameworkElementParents();
			UpdateTitleBarPadding();
			UpdatePaneTabFocusNavigation();
			UpdateBackAndCloseButtonsVisibility();
			UpdatePaneVisibility();
			UpdateVisualState();
			UpdatePaneLayout();
			UpdatePaneOverlayGroup();
			UpdateRepeaterItemsSource(forceSelectionModelUpdate: true);
			UpdateFooterRepeaterItemsSource(sourceCollectionReset: true, sourceCollectionChanged: true);
		}
		finally
		{
			_fromOnApplyTemplate = false;
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (IsTopNavigationView && IsTopPrimaryListVisible)
		{
			if (double.IsInfinity(((Size)(ref availableSize)).Width))
			{
				_topDataProvider.MoveAllItemsToPrimaryList();
			}
			else
			{
				HandleTopNavigationMeasureOverride(availableSize);
			}
		}
		((Layoutable)this).LayoutUpdated += OnLayoutUpdated;
		return ((Layoutable)this).MeasureOverride(availableSize);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		((ContentControl)this).OnPropertyChanged(change);
		Rect bounds;
		if (change.Property == (AvaloniaProperty)(object)CompactModeThresholdWidthProperty || change.Property == (AvaloniaProperty)(object)ExpandedModeThresholdWidthProperty)
		{
			bounds = ((Visual)this).Bounds;
			UpdateAdaptiveLayout(((Rect)(ref bounds)).Width);
		}
		else if (change.Property == (AvaloniaProperty)(object)AlwaysShowHeaderProperty || change.Property == (AvaloniaProperty)(object)HeaderedContentControl.HeaderProperty)
		{
			UpdateHeaderVisibility();
		}
		else if (change.Property == (AvaloniaProperty)(object)PaneTitleProperty)
		{
			UpdatePaneTitleFrameworkElementParents();
			UpdateBackAndCloseButtonsVisibility();
			UpdatePaneToggleSize();
		}
		else if (change.Property == (AvaloniaProperty)(object)PaneDisplayModeProperty)
		{
			_wasForceClosed = false;
			var (oldMode, newMode) = AvaloniaPropertyChangedExtensions.GetOldAndNewValue<FANavigationViewPaneDisplayMode>(change);
			UpdatePaneToggleButtonVisibility();
			UpdatePaneDisplayMode(oldMode, newMode);
			UpdatePaneTitleFrameworkElementParents();
			UpdatePaneVisibility();
			UpdateVisualState();
			UpdatePaneButtonWidths();
		}
		else if (change.Property == (AvaloniaProperty)(object)IsPaneVisibleProperty)
		{
			UpdatePaneVisibility();
			UpdateVisualStateForDisplayModeGroup(DisplayMode);
			if (!IsPaneVisible && IsPaneOpen)
			{
				ClosePane();
			}
			if (IsPaneVisible && DisplayMode == FANavigationViewDisplayMode.Expanded && !IsPaneOpen)
			{
				OpenPane();
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)AutoCompleteBoxProperty)
		{
			InvalidateTopNavPrimaryLayout();
			_ = change.NewValue is AutoCompleteBox;
			UpdateVisualState();
		}
		else if (change.Property == (AvaloniaProperty)(object)IsPaneToggleButtonVisibleProperty)
		{
			UpdatePaneTitleFrameworkElementParents();
			UpdateBackAndCloseButtonsVisibility();
			UpdatePaneToggleButtonVisibility();
			UpdateVisualState();
		}
		else if (change.Property == (AvaloniaProperty)(object)IsSettingsVisibleProperty)
		{
			UpdateFooterRepeaterItemsSource(sourceCollectionReset: false, sourceCollectionChanged: true);
		}
		else if (change.Property == (AvaloniaProperty)(object)CompactPaneLengthProperty)
		{
			UpdatePaneButtonWidths();
		}
		else if (change.Property == (AvaloniaProperty)(object)MenuItemTemplateProperty || change.Property == (AvaloniaProperty)(object)MenuItemTemplateSelectorProperty)
		{
			UpdateNavigationViewItemsFactory();
		}
		else if (change.Property == (AvaloniaProperty)(object)PaneFooterProperty)
		{
			UpdatePaneLayout();
		}
		else if (change.Property == (AvaloniaProperty)(object)SelectedItemProperty)
		{
			OnSelectedItemPropertyChanged(change.OldValue, change.NewValue);
		}
		else if (change.Property == (AvaloniaProperty)(object)IsBackButtonVisibleProperty)
		{
			UpdateBackAndCloseButtonsVisibility();
			bounds = ((Visual)this).Bounds;
			UpdateAdaptiveLayout(((Rect)(ref bounds)).Width);
			if (IsTopNavigationView)
			{
				InvalidateTopNavPrimaryLayout();
			}
			if (_backButton != null)
			{
				((Layoutable)_backButton).InvalidateMeasure();
			}
			UpdatePaneLayout();
		}
		else if (change.Property == (AvaloniaProperty)(object)MenuItemsSourceProperty)
		{
			UpdateRepeaterItemsSource(forceSelectionModelUpdate: true);
		}
		else if (change.Property == (AvaloniaProperty)(object)MenuItemsProperty)
		{
			UpdateRepeaterItemsSource(forceSelectionModelUpdate: true);
		}
		else if (change.Property == (AvaloniaProperty)(object)FooterMenuItemsSourceProperty)
		{
			UpdateFooterRepeaterItemsSource(sourceCollectionReset: true, sourceCollectionChanged: true);
		}
		else if (change.Property == (AvaloniaProperty)(object)FooterMenuItemsProperty)
		{
			UpdateFooterRepeaterItemsSource(sourceCollectionReset: true, sourceCollectionChanged: true);
		}
		else if (change.Property == (AvaloniaProperty)(object)IsPaneOpenProperty)
		{
			OnIsPaneOpenChanged();
			UpdateVisualStateForDisplayModeGroup(_displayMode);
		}
		else if (change.Property == (AvaloniaProperty)(object)OpenPaneLengthProperty)
		{
			bounds = ((Visual)this).Bounds;
			UpdateOpenPaneWidth(((Rect)(ref bounds)).Width);
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		_tabKeyPrecedesFocusChange = false;
		Key key = e.Key;
		if ((int)key != 2)
		{
			if ((int)key != 3)
			{
				if ((int)key == 23 && (e.KeyModifiers & 1) == 1 && IsPaneOpen && IsLightDismissable)
				{
					((RoutedEventArgs)e).Handled = AttemptClosePaneLightly();
				}
			}
			else
			{
				_tabKeyPrecedesFocusChange = true;
			}
		}
		else if (IsPaneOpen && IsLightDismissable)
		{
			((RoutedEventArgs)e).Handled = AttemptClosePaneLightly();
		}
		((InputElement)this).OnKeyDown(e);
	}

	protected override bool RegisterContentPresenter(ContentPresenter presenter)
	{
		if (((StyledElement)presenter).Name == "ContentPresenter")
		{
			return true;
		}
		return ((HeaderedContentControl)this).RegisterContentPresenter(presenter);
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return (AutomationPeer)(object)new FANavigationViewAutomationPeer((Control)(object)this);
	}

	private void OnLayoutUpdated(object sender, EventArgs e)
	{
		((Layoutable)this).LayoutUpdated -= OnLayoutUpdated;
		if (_lastSelectedItemPendingAnimationInTopNav != null)
		{
			object lastSelectedItemPendingAnimationInTopNav = _lastSelectedItemPendingAnimationInTopNav;
			_lastSelectedItemPendingAnimationInTopNav = null;
			AnimateSelectionChanged(lastSelectedItemPendingAnimationInTopNav);
		}
		if (_orientationChangedPendingAnimation)
		{
			_orientationChangedPendingAnimation = false;
			AnimateSelectionChanged(SelectedItem);
		}
	}

	private void OnNavViewLoaded(object sender, RoutedEventArgs e)
	{
		if (_updateVisualStateForDisplayModeFromOnLoaded)
		{
			_updateVisualStateForDisplayModeFromOnLoaded = false;
			UpdateVisualStateForDisplayModeGroup(DisplayMode);
		}
		UpdatePaneButtonWidths();
	}

	private void OnRepeaterLoaded(object sender, RoutedEventArgs args)
	{
		object selectedItem = SelectedItem;
		if (selectedItem != null && !IsSelectionSuppressed(selectedItem))
		{
			if (!IsSelectionSuppressed(selectedItem))
			{
				((ListBoxItem)NavigationViewItemOrSettingsContentFromData(selectedItem)).IsSelected = true;
				UpdateSelectionModelSelectionForSelectedItem(selectedItem);
			}
			AnimateSelectionChanged(selectedItem);
		}
	}

	private void UpdateRepeaterItemsSource(bool forceSelectionModelUpdate)
	{
		IEnumerable menuItemsSource = MenuItemsSource;
		IEnumerable enumerable;
		if (menuItemsSource != null)
		{
			enumerable = menuItemsSource;
		}
		else
		{
			UpdateSelectionForMenuItems();
			enumerable = _menuItems;
		}
		if (forceSelectionModelUpdate)
		{
			_selectionModelSource[0] = enumerable;
		}
		ItemsSourceView menuItemsSource2 = _menuItemsSource;
		if (menuItemsSource2 != null)
		{
			menuItemsSource2.CollectionChanged -= OnMenuItemsSourceCollectionChanged;
		}
		if (enumerable != null)
		{
			_menuItemsSource = ItemsSourceView.GetOrCreate(enumerable);
			_menuItemsSource.CollectionChanged += OnMenuItemsSourceCollectionChanged;
		}
		if (IsTopNavigationView)
		{
			UpdateLeftRepeaterItemSource(null);
			UpdateTopNavRepeatersItemSource(enumerable);
			InvalidateTopNavPrimaryLayout();
		}
		else
		{
			UpdateTopNavRepeatersItemSource(null);
			UpdateLeftRepeaterItemSource(enumerable);
		}
	}

	private void UpdateLeftRepeaterItemSource(IEnumerable items)
	{
		UpdateItemsRepeaterItemsSource(_leftNavRepeater, items);
		UpdatePaneLayout();
	}

	private void UpdateTopNavRepeatersItemSource(IEnumerable items)
	{
		_topDataProvider.SetDataSource(items);
		UpdateTopNavPrimaryRepeaterItemsSource(items);
		UpdateTopNavOverflowRepeaterItemsSource(items);
	}

	private void UpdateTopNavPrimaryRepeaterItemsSource(IEnumerable items)
	{
		if (items != null)
		{
			UpdateItemsRepeaterItemsSource(_topNavRepeater, _topDataProvider.GetPrimaryItems());
		}
		else
		{
			UpdateItemsRepeaterItemsSource(_topNavRepeater, null);
		}
	}

	private void UpdateTopNavOverflowRepeaterItemsSource(IEnumerable items)
	{
		if (_topNavRepeaterOverflowView == null)
		{
			return;
		}
		if (_topNavRepeaterOverflowView.ItemsSourceView != null)
		{
			_topNavRepeaterOverflowView.ItemsSourceView.CollectionChanged -= OnOverflowItemsSourceCollectionChanged;
		}
		if (items != null)
		{
			IList<object> overflowItems = _topDataProvider.GetOverflowItems();
			_topNavRepeaterOverflowView.ItemsSource = overflowItems;
			if (_topNavRepeater.ItemsSourceView != null)
			{
				_topNavRepeaterOverflowView.ItemsSourceView.CollectionChanged += OnOverflowItemsSourceCollectionChanged;
			}
		}
		else
		{
			_topNavRepeaterOverflowView.ItemsSource = null;
		}
	}

	private void UpdateItemsRepeaterItemsSource(FAItemsRepeater ir, IEnumerable source)
	{
		if (ir != null)
		{
			ir.ItemsSource = source;
		}
	}

	private void UpdateFooterRepeaterItemsSource(bool sourceCollectionReset, bool sourceCollectionChanged)
	{
		if (!_appliedTemplate)
		{
			return;
		}
		IEnumerable footerMenuItemsSource = FooterMenuItemsSource;
		IEnumerable enumerable;
		if (footerMenuItemsSource != null)
		{
			enumerable = footerMenuItemsSource;
		}
		else
		{
			UpdateSelectionForMenuItems();
			enumerable = _footerMenuItems;
		}
		UpdateItemsRepeaterItemsSource(_leftNavFooterMenuRepeater, null);
		UpdateItemsRepeaterItemsSource(_topNavFooterMenuRepeater, null);
		if ((_settingsItem == null) | sourceCollectionChanged | sourceCollectionReset)
		{
			List<object> list = new List<object>(enumerable.Count() + 1);
			if (_settingsItem == null)
			{
				FANavigationViewItem fANavigationViewItem = new FANavigationViewItem();
				((StyledElement)fANavigationViewItem).Name = "SettingsItem";
				_itemsFactory.SettingsItem = fANavigationViewItem;
				_settingsItem = fANavigationViewItem;
			}
			if (sourceCollectionReset && _footerItemsSource != null)
			{
				_footerItemsSource.CollectionChanged -= OnFooterItemsSourceCollectionChanged;
				_footerItemsSource = null;
			}
			if (_footerItemsSource == null)
			{
				_footerItemsSource = ItemsSourceView.GetOrCreate(enumerable);
				_footerItemsSource.CollectionChanged += OnFooterItemsSourceCollectionChanged;
			}
			if (_footerItemsSource != null)
			{
				int count = _footerItemsSource.Count;
				for (int i = 0; i < count; i++)
				{
					list.Add(_footerItemsSource.GetAt(i));
				}
				if (IsSettingsVisible)
				{
					CreateAndHookEventsToSettings();
					list.Add(_settingsItem);
				}
			}
			_selectionModelSource[1] = list;
		}
		if (IsTopNavigationView)
		{
			UpdateItemsRepeaterItemsSource(_topNavFooterMenuRepeater, _selectionModelSource[1]);
			return;
		}
		if (_leftNavFooterMenuRepeater != null)
		{
			UpdateItemsRepeaterItemsSource(_leftNavFooterMenuRepeater, _selectionModelSource[1]);
			((Layoutable)_leftNavFooterMenuRepeater).InvalidateMeasure();
			((Layoutable)_leftNavFooterMenuRepeater).InvalidateArrange();
			UpdatePaneLayout();
		}
		FANavigationViewItem settingsItem = _settingsItem;
		if (settingsItem != null)
		{
			ControlExtensions.BringIntoView((Control)(object)settingsItem);
		}
	}

	internal void OnRepeaterElementPrepared(object sender, FAItemsRepeaterElementPreparedEventArgs args)
	{
		if (!(args.Element is FANavigationViewItemBase fANavigationViewItemBase))
		{
			return;
		}
		fANavigationViewItemBase.SetNavigationViewParent(this);
		fANavigationViewItemBase.IsTopLevelItem = IsTopLevelItem(fANavigationViewItemBase);
		fANavigationViewItemBase.IsInNavigationViewOwnedRepeater = true;
		fANavigationViewItemBase.Position = position(sender as FAItemsRepeater);
		FANavigationViewItem parentNavigationViewItemForContainer = GetParentNavigationViewItemForContainer(fANavigationViewItemBase);
		if (parentNavigationViewItemForContainer != null)
		{
			fANavigationViewItemBase.Depth = ((!parentNavigationViewItemForContainer.ShouldRepeaterShowInFlyout) ? (parentNavigationViewItemForContainer.Depth + 1) : 0);
		}
		else
		{
			fANavigationViewItemBase.Depth = 0;
		}
		ApplyCustomMenuItemContainerStyling(fANavigationViewItemBase, sender as FAItemsRepeater, args.Index);
		SetNavigationViewItemBaseRevokers(fANavigationViewItemBase);
		if (!(args.Element is FANavigationViewItem fANavigationViewItem))
		{
			return;
		}
		int depth = ((fANavigationViewItemBase.Position != NavigationViewRepeaterPosition.TopPrimary) ? (fANavigationViewItemBase.Depth + 1) : 0);
		fANavigationViewItem.PropagateDepthToChildren(depth);
		SetNavigationViewItemRevokers(fANavigationViewItem);
		object obj = MenuItemFromContainer(fANavigationViewItem);
		if (SelectedItem == obj && ((Visual)fANavigationViewItem).IsEffectivelyVisible)
		{
			if (_isSelectionChangedPending)
			{
				_ = _pendingSelectionChangedItem;
			}
			((Layoutable)fANavigationViewItem).LayoutUpdated += OnSelectedItemLayoutUpdated;
		}
		NavigationViewRepeaterPosition position(FAItemsRepeater ir)
		{
			if (IsTopNavigationView)
			{
				if (ir == _topNavRepeater)
				{
					return NavigationViewRepeaterPosition.TopPrimary;
				}
				if (ir == _topNavFooterMenuRepeater)
				{
					return NavigationViewRepeaterPosition.TopFooter;
				}
				return NavigationViewRepeaterPosition.TopOverflow;
			}
			if (ir == _leftNavFooterMenuRepeater)
			{
				return NavigationViewRepeaterPosition.LeftFooter;
			}
			return NavigationViewRepeaterPosition.LeftNav;
		}
	}

	private void ApplyCustomMenuItemContainerStyling(FANavigationViewItemBase item, FAItemsRepeater ir, int index)
	{
		ControlTheme menuItemContainerTheme = MenuItemContainerTheme;
		((StyledElement)item).Theme = menuItemContainerTheme;
	}

	internal void OnRepeaterElementClearing(object sender, FAItemsRepeaterElementClearingEventArgs args)
	{
		if (args.Element is FANavigationViewItemBase fANavigationViewItemBase)
		{
			fANavigationViewItemBase.Depth = 0;
			fANavigationViewItemBase.IsTopLevelItem = false;
			fANavigationViewItemBase.IsInNavigationViewOwnedRepeater = false;
			ClearNavigationViewItemBaseRevokers(fANavigationViewItemBase);
			if (fANavigationViewItemBase is FANavigationViewItem fANavigationViewItem)
			{
				((InputElement)fANavigationViewItem).Tapped -= OnNavigationViewItemTapped;
				((InputElement)fANavigationViewItem).KeyDown -= OnNavigationViewItemKeyDown;
				((InputElement)fANavigationViewItem).GotFocus -= OnNavigationViewItemGotFocus;
				((AvaloniaObject)this).GetValue<FACompositeDisposable>((StyledProperty<FACompositeDisposable>)(object)NavigationViewItemBaseRevokersProperty)?.Dispose();
				((AvaloniaObject)this).SetValue<FACompositeDisposable>((StyledProperty<FACompositeDisposable>)(object)NavigationViewItemBaseRevokersProperty, (FACompositeDisposable)null, (BindingPriority)0);
			}
		}
	}

	private void OnRepeaterGettingFocus(object sender, FocusChangingEventArgs e)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		if (_tabKeyPrecedesFocusChange && _selectionModel.SelectedIndex != IndexPath.Unselected && ((int)e.NavigationMethod == 2 || (int)e.NavigationMethod == 1) && e.OldFocusedElement is Control)
		{
			_ = sender is FAItemsRepeater;
		}
		_tabKeyPrecedesFocusChange = false;
	}

	private void UpdateNavigationViewItemsFactory()
	{
		if (MenuItemTemplate == null)
		{
			_itemsFactory.UserElementFactory(MenuItemTemplateSelector);
		}
		else
		{
			_itemsFactory.UserElementFactory(MenuItemTemplate);
		}
	}

	private void OnMenuItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (!IsTopNavigationView)
		{
			UpdatePaneLayout();
		}
	}

	private void OnFooterItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		UpdateFooterRepeaterItemsSource(sourceCollectionReset: false, sourceCollectionChanged: true);
		UpdatePaneLayout();
	}

	private void OnOverflowItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		if (_topNavRepeaterOverflowView != null && _topNavRepeaterOverflowView.ItemsSourceView != null && _topNavRepeaterOverflowView.ItemsSourceView.Count == 0)
		{
			SetOverflowButtonVisibility(vis: false);
		}
	}

	private void OnSizeChanged(Rect r)
	{
		UpdateOpenPaneWidth(((Rect)(ref r)).Width);
		UpdateAdaptiveLayout(((Rect)(ref r)).Width);
		UpdateTitleBarPadding();
		UpdateBackAndCloseButtonsVisibility();
		UpdatePaneLayout();
	}

	private void OnItemsContainerSizeChanged(Rect rc)
	{
		UpdatePaneLayout();
	}

	private void OnTopNavDataSourceChanged(NotifyCollectionChangedEventArgs args)
	{
		CloseTopNavigationViewFlyout();
		if (_topNavigationMode != TopNavigationViewLayoutState.Uninitialized)
		{
			_topDataProvider.MoveAllItemsToPrimaryList();
		}
		_lastSelectedItemPendingAnimationInTopNav = null;
	}

	private void OnSelectedItemPropertyChanged(object oldItem, object newItem)
	{
		ChangeSelection(oldItem, newItem);
		if (_appliedTemplate && IsTopNavigationView && newItem != null && _topDataProvider.IndexOf(newItem) != -1 && _topDataProvider.IndexOf(newItem, NavigationViewSplitVectorID.PrimaryList) == -1)
		{
			InvalidateTopNavPrimaryLayout();
		}
	}

	private void OnIsPaneOpenChanged()
	{
		bool isPaneOpen = IsPaneOpen;
		if (isPaneOpen && _wasForceClosed)
		{
			_wasForceClosed = false;
		}
		else if (!_isOpenPaneForInteraction && !isPaneOpen)
		{
			if (_splitView != null)
			{
				_wasForceClosed = _splitView.IsPaneOpen;
			}
			else
			{
				_wasForceClosed = true;
			}
		}
		SetPaneToggleButtonAutomationName();
		UpdatePaneTabFocusNavigation();
		UpdateSettingsItemToolTip();
		UpdatePaneTitleFrameworkElementParents();
		UpdatePaneOverlayGroup();
		UpdatePaneButtonWidths();
	}

	private void OnSelectionModelChildrenRequested(object sender, SelectionModelChildrenRequestedEventArgs e)
	{
		if (e.SourceIndex.GetSize() == 1)
		{
			e.Children = e.Source;
			return;
		}
		if (e.Source is FANavigationViewItem nvi)
		{
			e.Children = GetChildren(nvi);
			return;
		}
		IEnumerable childrenForItemInIndexPath = GetChildrenForItemInIndexPath(e.SourceIndex, forceRealize: true);
		if (childrenForItemInIndexPath != null)
		{
			e.Children = childrenForItemInIndexPath;
		}
	}

	private void OnSelectionModelSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
	{
		object selectedItem = _selectionModel.SelectedItem;
		if (_shouldIgnoreNextSelectionChange || selectedItem == SelectedItem || !_appliedTemplate)
		{
			return;
		}
		bool flag = true;
		IndexPath selIndex = _selectionModel.SelectedIndex;
		if (IsTopNavigationView && selIndex != IndexPath.Unselected && selIndex.GetSize() > 1 && selIndex.GetAt(0) == 0 && !_topDataProvider.IsItemInPrimaryList(selIndex.GetAt(1)))
		{
			if (itemShouldBeMoved(selIndex))
			{
				SelectAndMoveOverflowItem(selectedItem, selIndex, closeFlyout: true);
				flag = false;
			}
			else
			{
				_moveTopNavOverflowItemOnFlyoutClose = true;
			}
		}
		if (flag)
		{
			SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(selectedItem);
		}
		bool itemShouldBeMoved(IndexPath p)
		{
			if (GetContainerForIndexPath(selIndex) is FANavigationViewItem nvi && DoesNavigationViewItemHaveChildren(nvi))
			{
				return false;
			}
			return true;
		}
	}

	private void SelectAndMoveOverflowItem(object selItem, IndexPath selIndex, bool closeFlyout)
	{
		try
		{
			_selectionChangeFromOverflowMenu = true;
			if (closeFlyout)
			{
				CloseTopNavigationViewFlyout();
			}
			if (!IsSelectionSuppressed(selItem))
			{
				SelectOverflowItem(selItem, selIndex);
			}
		}
		finally
		{
			_selectionChangeFromOverflowMenu = false;
		}
	}

	private void CloseFlyoutIfRequired(FANavigationViewItem selItem)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Invalid comparison between Unknown and I4
		IndexPath selectedIndex = _selectionModel.SelectedIndex;
		bool flag = false;
		if (_splitView != null)
		{
			SplitViewDisplayMode displayMode = _splitView.DisplayMode;
			flag = (!_splitView.IsPaneOpen && ((int)displayMode == 3 || (int)displayMode == 1)) || PaneDisplayMode == FANavigationViewPaneDisplayMode.Top;
		}
		if (flag && selectedIndex != IndexPath.Unselected && !DoesNavigationViewItemHaveChildren(selItem) && GetContainerForIndex(selectedIndex.GetAt(1), selectedIndex.GetAt(0) == 1) is FANavigationViewItem { ShouldRepeaterShowInFlyout: not false } fANavigationViewItem)
		{
			fANavigationViewItem.IsExpanded = false;
		}
	}

	private void RaiseSelectionChangedEvent(object nextItem, bool isSettings, NavigationRecommendedTransitionDirection recDir)
	{
		FANavigationViewItemBase selectedItemContainer = null;
		if (nextItem != null)
		{
			FANavigationViewItemBase fANavigationViewItemBase = NavigationViewItemBaseOrSettingsContentFromData(nextItem);
			if (fANavigationViewItemBase != null)
			{
				selectedItemContainer = fANavigationViewItemBase;
			}
			else
			{
				FANavigationViewItemBase containerForIndexPath = GetContainerForIndexPath(_selectionModel.SelectedIndex, lastVisible: false, forceRealize: true);
				if (containerForIndexPath != null)
				{
					selectedItemContainer = containerForIndexPath;
				}
			}
		}
		FANavigationViewSelectionChangedEventArgs e = new FANavigationViewSelectionChangedEventArgs
		{
			SelectedItem = nextItem,
			IsSettingsSelected = isSettings,
			SelectedItemContainer = selectedItemContainer,
			RecommendedNavigationTransitionInfo = CreateNavigationTransitionInfo(recDir)
		};
		SelectionChanged?.Invoke(this, e);
	}

	private void ChangeSelection(object prevItem, object nextItem)
	{
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		bool isSettings = IsSettingsItem(nextItem);
		if (IsSelectionSuppressed(nextItem))
		{
			UndoSelectionAndRevertSelectionTo(prevItem, nextItem);
			RaiseItemInvoked(nextItem, isSettings);
			return;
		}
		NavigationRecommendedTransitionDirection navigationRecommendedTransitionDirection = NavigationRecommendedTransitionDirection.Default;
		if (IsTopNavigationView)
		{
			if (_selectionChangeFromOverflowMenu)
			{
				navigationRecommendedTransitionDirection = NavigationRecommendedTransitionDirection.FromOverflow;
			}
			else if (prevItem != null && nextItem != null)
			{
				navigationRecommendedTransitionDirection = GetRecommendedTransitionDirection((Control)(object)NavigationViewItemBaseOrSettingsContentFromData(prevItem), (Control)(object)NavigationViewItemBaseOrSettingsContentFromData(nextItem));
			}
		}
		object selectedItem = SelectedItem;
		if (_shouldRaiseItemInvokedAfterSelection)
		{
			_shouldRaiseItemInvokedAfterSelection = false;
			RaiseItemInvoked(nextItem, isSettings, NavigationViewItemOrSettingsContentFromData(nextItem), navigationRecommendedTransitionDirection);
		}
		if (selectedItem != SelectedItem)
		{
			return;
		}
		UnselectPrevItem(prevItem, nextItem);
		ChangeSelectStatusForItem(nextItem, selected: true);
		UpdateSelectionModelSelectionForSelectedItem(nextItem);
		try
		{
			if (!_shouldIgnoreUIASelectionRaiseAsExpandCollapseWillRaise && ControlAutomationPeer.FromElement((Control)(object)this) is FANavigationViewAutomationPeer fANavigationViewAutomationPeer)
			{
				fANavigationViewAutomationPeer.RaiseSelectionChangedEvent(prevItem, nextItem);
			}
		}
		finally
		{
			_shouldIgnoreUIASelectionRaiseAsExpandCollapseWillRaise = false;
		}
		FANavigationViewItem fANavigationViewItem = NavigationViewItemOrSettingsContentFromData(nextItem);
		if (fANavigationViewItem != null)
		{
			AnimateSelectionChanged(nextItem);
			RaiseSelectionChangedEvent(nextItem, isSettings, navigationRecommendedTransitionDirection);
			ClosePaneIfNecessaryAfterItemIsClicked(fANavigationViewItem);
			return;
		}
		_isSelectionChangedPending = true;
		_pendingSelectionChangedItem = nextItem;
		_pendingSelectionChangedDirection = navigationRecommendedTransitionDirection;
		Dispatcher.UIThread.Post((Action)delegate
		{
			CompletePendingSelectionChange();
		}, default(DispatcherPriority));
	}

	private void CompletePendingSelectionChange()
	{
		if (_isSelectionChangedPending)
		{
			AnimateSelectionChanged(FindLowestLevelContainerToDisplaySelectionIndicator());
			_isSelectionChangedPending = false;
			object pendingSelectionChangedItem = _pendingSelectionChangedItem;
			NavigationRecommendedTransitionDirection pendingSelectionChangedDirection = _pendingSelectionChangedDirection;
			_pendingSelectionChangedItem = null;
			_pendingSelectionChangedDirection = NavigationRecommendedTransitionDirection.FromOverflow;
			RaiseSelectionChangedEvent(pendingSelectionChangedItem, IsSettingsItem(pendingSelectionChangedItem), pendingSelectionChangedDirection);
		}
	}

	private void UpdateSelectionModelSelectionForSelectedItem(object selectedItem)
	{
		IndexPath unselected = IndexPath.Unselected;
		FANavigationViewItemBase fANavigationViewItemBase = NavigationViewItemBaseOrSettingsContentFromData(selectedItem);
		unselected = ((fANavigationViewItemBase == null) ? GetIndexPathOfItem(selectedItem) : GetIndexPathForContainer(fANavigationViewItemBase));
		if (unselected != IndexPath.Unselected && unselected.GetSize() > 0)
		{
			try
			{
				_shouldIgnoreNextSelectionChange = true;
				UpdateSelectionModelSelection(unselected);
			}
			finally
			{
				_shouldIgnoreNextSelectionChange = false;
			}
		}
	}

	private void UpdateSelectionModelSelection(IndexPath ip)
	{
		IndexPath selectedIndex = _selectionModel.SelectedIndex;
		_selectionModel.SelectAt(ip);
		UpdateIsChildSelected(selectedIndex, ip);
	}

	private void UpdateIsChildSelected(IndexPath prevIP, IndexPath nextIP)
	{
		if (prevIP != IndexPath.Unselected && prevIP.GetSize() > 0)
		{
			UpdateIsChildSelectedForIndexPath(prevIP, isChildSelected: false);
		}
		if (nextIP != IndexPath.Unselected && nextIP.GetSize() > 0)
		{
			UpdateIsChildSelectedForIndexPath(nextIP, isChildSelected: true);
		}
	}

	private void UpdateIsChildSelectedForIndexPath(IndexPath ip, bool isChildSelected)
	{
		Control val = GetContainerForIndex(ip.GetAt(1), ip.GetAt(0) == 1);
		int num = 2;
		while (val != null)
		{
			if (val is FANavigationViewItem fANavigationViewItem)
			{
				fANavigationViewItem.IsChildSelected = isChildSelected;
				FAItemsRepeater getRepeater = fANavigationViewItem.GetRepeater;
				if (getRepeater != null && num < ip.GetSize() - 1)
				{
					val = getRepeater.TryGetElement(ip.GetAt(num));
					num++;
					continue;
				}
			}
			val = null;
		}
	}

	private void RaiseItemInvoked(object item, bool isSettings, FANavigationViewItemBase container = null, NavigationRecommendedTransitionDirection recDir = NavigationRecommendedTransitionDirection.Default)
	{
		FANavigationViewItemInvokedEventArgs e = new FANavigationViewItemInvokedEventArgs();
		if (container != null)
		{
			item = ((ContentControl)container).Content;
		}
		else if (!isSettings)
		{
			FANavigationViewItemBase fANavigationViewItemBase = NavigationViewItemBaseOrSettingsContentFromData(item);
			item = ((ContentControl)fANavigationViewItemBase).Content;
			container = fANavigationViewItemBase;
		}
		else
		{
			container = item as FANavigationViewItemBase;
		}
		e.InvokedItem = item;
		e.InvokedItemContainer = container;
		e.IsSettingsInvoked = isSettings;
		e.RecommendedNavigationTransitionInfo = CreateNavigationTransitionInfo(recDir);
		ItemInvoked?.Invoke(this, e);
	}

	private void SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(object selItem)
	{
		SelectedItem = selItem;
	}

	private void ChangeSelectStatusForItem(object item, bool selected)
	{
		FANavigationViewItem fANavigationViewItem = NavigationViewItemOrSettingsContentFromData(item);
		if (fANavigationViewItem != null)
		{
			((ListBoxItem)fANavigationViewItem).IsSelected = selected;
		}
		else
		{
			if (!selected)
			{
				return;
			}
			IndexPath indexPathOfItem = GetIndexPathOfItem(item);
			if (indexPathOfItem != IndexPath.Unselected && indexPathOfItem.GetSize() > 0)
			{
				try
				{
					_shouldIgnoreNextSelectionChange = true;
					UpdateSelectionModelSelection(indexPathOfItem);
				}
				finally
				{
					_shouldIgnoreNextSelectionChange = false;
				}
			}
		}
	}

	private void UnselectPrevItem(object prevItem, object nextItem)
	{
		if (prevItem != null && prevItem != nextItem)
		{
			try
			{
				_shouldIgnoreNextSelectionChange = true;
				ChangeSelectStatusForItem(prevItem, selected: false);
			}
			finally
			{
				_shouldIgnoreNextSelectionChange = false;
			}
		}
	}

	private void UndoSelectionAndRevertSelectionTo(object prevSelectedItem, object nextItem)
	{
		object selectedItem = null;
		if (prevSelectedItem != null)
		{
			if (IsSelectionSuppressed(prevSelectedItem))
			{
				AnimateSelectionChanged(null);
			}
			else
			{
				ChangeSelectStatusForItem(prevSelectedItem, selected: true);
				AnimateSelectionChangedToItem(prevSelectedItem);
				selectedItem = prevSelectedItem;
			}
		}
		else
		{
			ChangeSelectStatusForItem(nextItem, selected: false);
		}
		SelectedItem = selectedItem;
	}

	private void SelectOverflowItem(object item, IndexPath ip)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		object obj = item;
		if (ip.GetSize() > 2)
		{
			obj = GetItemFromIndex(_topNavRepeaterOverflowView, _topDataProvider.ConvertOriginalIndexToIndex(ip.GetAt(1)));
		}
		int num = _topDataProvider.IndexOf(obj);
		double widthForItem = _topDataProvider.GetWidthForItem(num);
		bool flag = !_topDataProvider.IsValidWidthForItem(num);
		if (!flag)
		{
			double getTopNavigationViewActualWidth = GetTopNavigationViewActualWidth;
			double num2 = MeasureTopNavigationViewDesiredWidth(Size.Infinity);
			int num3 = -1;
			if (SelectedItem != null)
			{
				num3 = _topDataProvider.IndexOf(SelectedItem);
				if (num3 != -1)
				{
					_topDataProvider.GetWidthForItem(num3);
				}
			}
			double num4 = num2 + widthForItem - getTopNavigationViewActualWidth;
			IList<int> list = FindMovableItemsToBeRemovedFromPrimaryList(num4, new List<int>(0));
			double availableWidth = _topDataProvider.CalculateWidthForItems(list) - num4;
			IList<int> list2 = FindMovableItemsRecoverToPrimaryList(availableWidth, new int[1] { num });
			list2.Add(num);
			_lastSelectedItemPendingAnimationInTopNav = obj;
			if (ip != IndexPath.Unselected && ip.GetSize() > 0)
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (ip.GetAt(1) == list[i])
					{
						if (_activeIndicator != null)
						{
							AnimateSelectionChanged(null);
						}
						break;
					}
				}
			}
			if (_topDataProvider.HasInvalidWidth(list2))
			{
				flag = true;
			}
			else
			{
				_topDataProvider.MoveItemsToPrimaryList(list2);
				_topDataProvider.MoveItemsOutOfPrimaryList(list);
				if (NeedRearrangeOfTopElementsAfterOverflowSelectionChange(num))
				{
					flag = true;
				}
				if (!flag)
				{
					SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(item);
					((Layoutable)this).InvalidateMeasure();
				}
			}
		}
		if (flag)
		{
			_topDataProvider.MoveAllItemsToPrimaryList();
			SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(item);
			InvalidateTopNavPrimaryLayout();
		}
	}

	private void UpdateSelectionForMenuItems()
	{
		if (SelectedItem == null)
		{
			bool flag = false;
			flag = UpdateSelectedItemFromMenuItems(_menuItems);
			UpdateSelectedItemFromMenuItems(_footerMenuItems, flag);
		}
	}

	private bool UpdateSelectedItemFromMenuItems(IEnumerable menuItems, bool foundFirstSelected = false)
	{
		for (int i = 0; i < menuItems.Count(); i++)
		{
			if (!(menuItems.ElementAt(i) is FANavigationViewItem fANavigationViewItem) || !((ListBoxItem)fANavigationViewItem).IsSelected)
			{
				continue;
			}
			if (!foundFirstSelected)
			{
				try
				{
					_shouldIgnoreNextSelectionChange = true;
					SelectedItem = fANavigationViewItem;
					foundFirstSelected = true;
				}
				finally
				{
					_shouldIgnoreNextSelectionChange = false;
				}
			}
			else
			{
				((ListBoxItem)fANavigationViewItem).IsSelected = false;
			}
		}
		return foundFirstSelected;
	}

	private void SetNavigationViewItemBaseRevokers(FANavigationViewItemBase nvib)
	{
		FACompositeDisposable fACompositeDisposable = new FACompositeDisposable(AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)nvib, (AvaloniaProperty)(object)Visual.IsVisibleProperty).Subscribe(OnNavigationViewItemBaseVisibilityPropertyChanged));
		((AvaloniaObject)nvib).SetValue<FACompositeDisposable>((StyledProperty<FACompositeDisposable>)(object)NavigationViewItemBaseRevokersProperty, fACompositeDisposable, (BindingPriority)0);
	}

	private void SetNavigationViewItemRevokers(FANavigationViewItem nvi)
	{
		FACompositeDisposable fACompositeDisposable = ((AvaloniaObject)nvi).GetValue<FACompositeDisposable>((StyledProperty<FACompositeDisposable>)(object)NavigationViewItemBaseRevokersProperty);
		if (fACompositeDisposable == null)
		{
			fACompositeDisposable = new FACompositeDisposable();
			((AvaloniaObject)nvi).SetValue<FACompositeDisposable>((StyledProperty<FACompositeDisposable>)(object)NavigationViewItemBaseRevokersProperty, fACompositeDisposable, (BindingPriority)0);
		}
		fACompositeDisposable.Add(InteractiveExtensions.AddDisposableHandler<KeyEventArgs>((Interactive)(object)nvi, InputElement.KeyDownEvent, (EventHandler<KeyEventArgs>)OnNavigationViewItemKeyDown, (RoutingStrategies)5, false));
		fACompositeDisposable.Add(InteractiveExtensions.AddDisposableHandler<FocusChangedEventArgs>((Interactive)(object)nvi, InputElement.GotFocusEvent, (EventHandler<FocusChangedEventArgs>)OnNavigationViewItemGotFocus, (RoutingStrategies)5, false));
		fACompositeDisposable.Add(InteractiveExtensions.AddDisposableHandler<TappedEventArgs>((Interactive)(object)nvi, InputElement.TappedEvent, (EventHandler<TappedEventArgs>)OnNavigationViewItemTapped, (RoutingStrategies)5, false));
		fACompositeDisposable.Add(AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)nvi, (AvaloniaProperty)(object)ListBoxItem.IsSelectedProperty).Subscribe(OnNavigationViewItemIsSelectedPropertyChanged));
		fACompositeDisposable.Add(AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)nvi, (AvaloniaProperty)(object)FANavigationViewItem.IsExpandedProperty).Subscribe(OnNavigationViewItemExpandedPropertyChanged));
	}

	private void ClearNavigationViewItemBaseRevokers(FANavigationViewItemBase nvib)
	{
		((AvaloniaObject)nvib).GetValue<FACompositeDisposable>((StyledProperty<FACompositeDisposable>)(object)NavigationViewItemBaseRevokersProperty)?.Dispose();
		((AvaloniaObject)nvib).SetValue<FACompositeDisposable>((StyledProperty<FACompositeDisposable>)(object)NavigationViewItemBaseRevokersProperty, (FACompositeDisposable)null, (BindingPriority)0);
	}

	private void OnNavigationViewItemIsSelectedPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		if (!(args.Sender is FANavigationViewItem fANavigationViewItem))
		{
			return;
		}
		bool flag = IsContainerTheSelectedItemInTheSelectionModel(fANavigationViewItem);
		bool isSelected = ((ListBoxItem)fANavigationViewItem).IsSelected;
		if (isSelected && !flag)
		{
			IndexPath indexPathForContainer = GetIndexPathForContainer(fANavigationViewItem);
			UpdateSelectionModelSelection(indexPathForContainer);
		}
		else if (!isSelected & flag)
		{
			IndexPath indexPathForContainer2 = GetIndexPathForContainer(fANavigationViewItem);
			IndexPath selectedIndex = _selectionModel.SelectedIndex;
			if (selectedIndex != IndexPath.Unselected)
			{
				if (indexPathForContainer2.CompareTo(selectedIndex) == 0)
				{
					_selectionModel.DeselectAt(indexPathForContainer2);
				}
				else if (!IsPaneOpen && indexPathForContainer2.GetSize() == 0)
				{
					UpdateIsChildSelected(selectedIndex, IndexPath.Unselected);
					if (_prevIndicator == null && _nextIndicator == null && _activeIndicator != null)
					{
						ResetElementAnimationProperties(_activeIndicator, 0.0);
						_activeIndicator = null;
					}
					_selectionModel.DeselectAt(selectedIndex);
				}
			}
		}
		if (isSelected)
		{
			fANavigationViewItem.IsChildSelected = false;
		}
	}

	private void OnNavigationViewItemExpandedPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		if (args.Sender is FANavigationViewItem fANavigationViewItem)
		{
			if (fANavigationViewItem.IsExpanded)
			{
				RaiseExpandingEvent(fANavigationViewItem);
			}
			ShowHideChildrenItemsRepeater(fANavigationViewItem);
			if (!fANavigationViewItem.IsExpanded)
			{
				RaiseCollapsedEvent(fANavigationViewItem);
			}
		}
	}

	private void OnNavigationViewItemBaseVisibilityPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		UpdatePaneLayout();
	}

	private void RaiseItemInvokedForNavigationViewItem(FANavigationViewItem nvi)
	{
		object item = null;
		object selectedItem = SelectedItem;
		FAItemsRepeater parentItemsRepeaterForContainer = GetParentItemsRepeaterForContainer(nvi);
		if (parentItemsRepeaterForContainer.ItemsSourceView != null)
		{
			int elementIndex = parentItemsRepeaterForContainer.GetElementIndex((Control)(object)nvi);
			if (elementIndex != -1)
			{
				item = parentItemsRepeaterForContainer.ItemsSourceView.GetAt(elementIndex);
			}
		}
		NavigationRecommendedTransitionDirection recDir = NavigationRecommendedTransitionDirection.Default;
		if (IsTopNavigationView && nvi.SelectsOnInvoked)
		{
			if (parentItemsRepeaterForContainer == _topNavRepeaterOverflowView)
			{
				recDir = NavigationRecommendedTransitionDirection.FromOverflow;
			}
			else if (selectedItem != null)
			{
				recDir = GetRecommendedTransitionDirection((Control)(object)NavigationViewItemBaseOrSettingsContentFromData(selectedItem), (Control)(object)nvi);
			}
		}
		RaiseItemInvoked(item, IsSettingsItem(nvi), nvi, recDir);
	}

	private void OnNavigationViewItemInvoked(FANavigationViewItem nvi)
	{
		_shouldRaiseItemInvokedAfterSelection = true;
		object selectedItem = SelectedItem;
		bool flag = _selectionModel != null && nvi.SelectsOnInvoked;
		if (flag)
		{
			IndexPath indexPathForContainer = GetIndexPathForContainer(nvi);
			DoesNavigationViewItemHaveChildren(nvi);
			UpdateSelectionModelSelection(indexPathForContainer);
		}
		if (selectedItem == SelectedItem)
		{
			RaiseItemInvokedForNavigationViewItem(nvi);
		}
		ToggleIsExpandedNavigationViewItem(nvi);
		ClosePaneIfNecessaryAfterItemIsClicked(nvi);
		if (flag)
		{
			CloseFlyoutIfRequired(nvi);
		}
	}

	private void OnNavigationViewItemGotFocus(object sender, FocusChangedEventArgs e)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		FANavigationViewItem fANavigationViewItem = (FANavigationViewItem)sender;
		if (!SelectionFollowsFocus || (int)e.NavigationMethod == 3 || !fANavigationViewItem.SelectsOnInvoked || ((ListBoxItem)fANavigationViewItem).IsSelected)
		{
			return;
		}
		if (IsTopNavigationView)
		{
			FAItemsRepeater parentItemsRepeaterForContainer = GetParentItemsRepeaterForContainer(fANavigationViewItem);
			if (parentItemsRepeaterForContainer != null && parentItemsRepeaterForContainer != _topNavRepeaterOverflowView)
			{
				OnNavigationViewItemInvoked(fANavigationViewItem);
			}
		}
		else
		{
			OnNavigationViewItemInvoked(fANavigationViewItem);
		}
	}

	private void OnNavigationViewItemKeyDown(object sender, KeyEventArgs args)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		if ((int)args.Key == 6 || (int)args.Key == 18)
		{
			if (sender is FANavigationViewItem nvi)
			{
				HandleKeyEventForNavigationViewItem(nvi, args);
			}
		}
		else if (sender is FANavigationViewItem nvi2)
		{
			HandleKeyEventForNavigationViewItem(nvi2, args);
		}
	}

	private void HandleKeyEventForNavigationViewItem(FANavigationViewItem nvi, KeyEventArgs args)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected I4, but got Unknown
		Key key = args.Key;
		if ((int)key != 6)
		{
			switch (key - 18)
			{
			default:
				return;
			case 0:
				break;
			case 4:
				((RoutedEventArgs)args).Handled = true;
				KeyboardFocusFirstItemFromItem(nvi);
				return;
			case 3:
				((RoutedEventArgs)args).Handled = true;
				KeyboardFocusLastItemFromItem(nvi);
				return;
			case 7:
				if (IsTopNavigationView)
				{
					FocusNextDownItem(nvi, args);
				}
				return;
			case 8:
				if (!IsTopNavigationView)
				{
					FocusNextDownItem(nvi, args);
				}
				return;
			case 5:
				if (IsTopNavigationView)
				{
					FocusNextUpItem(nvi, args);
				}
				return;
			case 6:
				if (!IsTopNavigationView)
				{
					FocusNextUpItem(nvi, args);
				}
				return;
			case 1:
			case 2:
				return;
			}
		}
		((RoutedEventArgs)args).Handled = true;
		OnNavigationViewItemInvoked(nvi);
	}

	private void KeyboardFocusFirstItemFromItem(FANavigationViewItemBase nvib)
	{
		FAItemsRepeater fAItemsRepeater = null;
		fAItemsRepeater = ((_lastItemExpandedIntoFlyout == null) ? GetParentRootItemsRepeaterForContainer(nvib) : _lastItemExpandedIntoFlyout.GetRepeater);
		Control firstFocusableElement = GetFirstFocusableElement(fAItemsRepeater);
		if (firstFocusableElement != null)
		{
			((InputElement)firstFocusableElement).Focus((NavigationMethod)2, (KeyModifiers)0);
		}
	}

	private void KeyboardFocusLastItemFromItem(FANavigationViewItemBase nvib)
	{
		FAItemsRepeater fAItemsRepeater = null;
		fAItemsRepeater = ((_lastItemExpandedIntoFlyout == null) ? GetParentRootItemsRepeaterForContainer(nvib) : _lastItemExpandedIntoFlyout.GetRepeater);
		Control lastFocusableElement = GetLastFocusableElement(fAItemsRepeater);
		if (lastFocusableElement != null)
		{
			((InputElement)lastFocusableElement).Focus((NavigationMethod)2, (KeyModifiers)0);
		}
	}

	private void FocusNextUpItem(FANavigationViewItem nvi, KeyEventArgs args)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		if (((RoutedEventArgs)args).Source != nvi)
		{
			return;
		}
		bool flag = false;
		FindNextElementOptions val = new FindNextElementOptions();
		TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)this);
		object obj = ((topLevel != null) ? ((ContentControl)topLevel).Content : null);
		val.set_SearchRoot((InputElement)((obj is InputElement) ? obj : null));
		FindNextElementOptions val2 = val;
		TopLevel topLevel2 = TopLevel.GetTopLevel((Visual)(object)this);
		if (((topLevel2 != null) ? topLevel2.FocusManager.FindNextElement((NavigationDirection)6, val2) : null) is FANavigationViewItem fANavigationViewItem && fANavigationViewItem.Depth == nvi.Depth)
		{
			if (DoesNavigationViewItemHaveChildren(fANavigationViewItem))
			{
				FAItemsRepeater getRepeater = fANavigationViewItem.GetRepeater;
				if (getRepeater != null)
				{
					IInputElement obj2 = FocusManager.FindLastFocusableElement((IInputElement)(object)getRepeater);
					Control val3 = (Control)(object)((obj2 is Control) ? obj2 : null);
					if (val3 != null)
					{
						((RoutedEventArgs)args).Handled = ((InputElement)val3).Focus((NavigationMethod)2, (KeyModifiers)0);
					}
					else
					{
						((RoutedEventArgs)args).Handled = ((InputElement)fANavigationViewItem).Focus((NavigationMethod)2, (KeyModifiers)0);
					}
				}
			}
			else
			{
				flag = false;
			}
		}
		if (flag && !((RoutedEventArgs)args).Handled && nvi.Depth > 0)
		{
			FANavigationViewItem parentNavigationViewItemForContainer = GetParentNavigationViewItemForContainer(nvi);
			if (parentNavigationViewItemForContainer != null)
			{
				((RoutedEventArgs)args).Handled = ((InputElement)parentNavigationViewItemForContainer).Focus((NavigationMethod)2, (KeyModifiers)0);
			}
		}
	}

	private void FocusNextDownItem(FANavigationViewItem nvi, KeyEventArgs args)
	{
		if (((RoutedEventArgs)args).Source != nvi || !DoesNavigationViewItemHaveChildren(nvi))
		{
			return;
		}
		FAItemsRepeater getRepeater = nvi.GetRepeater;
		if (getRepeater != null)
		{
			IInputElement val = FocusManager.FindFirstFocusableElement((IInputElement)(object)getRepeater);
			if (val != null)
			{
				((RoutedEventArgs)args).Handled = val.Focus((NavigationMethod)2, (KeyModifiers)0);
			}
		}
	}

	private Control GetFirstFocusableElement(FAItemsRepeater ir)
	{
		if (ir == null)
		{
			return null;
		}
		FAItemsSourceView itemsSourceView = ir.ItemsSourceView;
		if (itemsSourceView != null)
		{
			int num = itemsSourceView.Count - 1;
			for (int i = 0; i <= num; i++)
			{
				Control val = ir.TryGetElement(i);
				if (val != null && ((InputElement)val).Focusable)
				{
					return val;
				}
			}
		}
		return null;
	}

	private Control GetLastFocusableElement(FAItemsRepeater ir)
	{
		if (ir == null)
		{
			return null;
		}
		FAItemsSourceView itemsSourceView = ir.ItemsSourceView;
		if (itemsSourceView != null)
		{
			for (int num = itemsSourceView.Count - 1; num >= 0; num--)
			{
				Control val = ir.TryGetElement(num);
				if (val != null && ((InputElement)val).Focusable)
				{
					return val;
				}
			}
		}
		return null;
	}

	private void OnNavigationViewItemTapped(object sender, RoutedEventArgs e)
	{
		FANavigationViewItem fANavigationViewItem = (FANavigationViewItem)sender;
		OnNavigationViewItemInvoked(fANavigationViewItem);
		((InputElement)fANavigationViewItem).Focus((NavigationMethod)0, (KeyModifiers)0);
		e.Handled = true;
	}

	private void Expand(FANavigationViewItem nvi)
	{
		ChangeIsExpandedNavigationViewItem(nvi, isExpanded: true);
	}

	private void Collapse(FANavigationViewItem nvi)
	{
		ChangeIsExpandedNavigationViewItem(nvi, isExpanded: false);
	}

	private void ToggleIsExpandedNavigationViewItem(FANavigationViewItem nvi)
	{
		ChangeIsExpandedNavigationViewItem(nvi, !nvi.IsExpanded);
	}

	private void ChangeIsExpandedNavigationViewItem(FANavigationViewItem nvi, bool isExpanded)
	{
		if (DoesNavigationViewItemHaveChildren(nvi))
		{
			nvi.IsExpanded = isExpanded;
		}
	}

	private void ShowHideChildrenItemsRepeater(FANavigationViewItem nvi)
	{
		nvi.ShowHideChildren();
		if (nvi.ShouldRepeaterShowInFlyout)
		{
			_lastItemExpandedIntoFlyout = (nvi.IsExpanded ? nvi : null);
		}
		if (!((ListBoxItem)nvi).IsSelected && nvi.IsChildSelected)
		{
			if (!nvi.IsRepeaterVisible && nvi.IsChildSelected)
			{
				AnimateSelectionChanged(nvi);
			}
			else
			{
				AnimateSelectionChanged(FindLowestLevelContainerToDisplaySelectionIndicator());
			}
		}
		nvi.RotateExpandCollapseChevron(nvi.IsExpanded);
	}

	private void CollapseTopLevelMenuItems(FANavigationViewPaneDisplayMode oldDMode)
	{
		if (oldDMode == FANavigationViewPaneDisplayMode.Top)
		{
			CollapseMenuItemsInRepeater(_topNavRepeater);
			CollapseMenuItemsInRepeater(_topNavRepeaterOverflowView);
		}
		else
		{
			CollapseMenuItemsInRepeater(_leftNavRepeater);
		}
	}

	private void CollapseMenuItemsInRepeater(FAItemsRepeater ir)
	{
		for (int i = 0; i < GetContainerCountInRepeater(ir); i++)
		{
			if (ir.TryGetElement(i) is FANavigationViewItem nvi)
			{
				ChangeIsExpandedNavigationViewItem(nvi, isExpanded: false);
			}
		}
	}

	private void RaiseExpandingEvent(FANavigationViewItemBase nvib)
	{
		FANavigationViewItemExpandingEventArgs e = new FANavigationViewItemExpandingEventArgs(this);
		e.ExpandingItemContainer = nvib;
		ItemExpanding?.Invoke(this, e);
	}

	private void RaiseCollapsedEvent(FANavigationViewItemBase nvib)
	{
		FANavigationViewItemCollapsedEventArgs e = new FANavigationViewItemCollapsedEventArgs(this);
		e.CollapsedItemContainer = nvib;
		ItemCollapsed?.Invoke(this, e);
	}

	private void OnSelectedItemLayoutUpdated(object sender, EventArgs args)
	{
		if (_isSelectionChangedPending)
		{
			_isSelectionChangedPending = false;
			object pendingSelectionChangedItem = _pendingSelectionChangedItem;
			NavigationRecommendedTransitionDirection pendingSelectionChangedDirection = _pendingSelectionChangedDirection;
			_pendingSelectionChangedItem = null;
			_pendingSelectionChangedDirection = NavigationRecommendedTransitionDirection.Default;
			((Layoutable)((sender is Control) ? sender : null)).LayoutUpdated -= OnSelectedItemLayoutUpdated;
			FANavigationViewItem fANavigationViewItem = NavigationViewItemOrSettingsContentFromData(pendingSelectionChangedItem);
			if (fANavigationViewItem != null)
			{
				AnimateSelectionChanged(fANavigationViewItem);
			}
			RaiseSelectionChangedEvent(pendingSelectionChangedItem, IsSettingsItem(pendingSelectionChangedItem), pendingSelectionChangedDirection);
		}
	}

	private void UpdateAdaptiveLayout(double width, bool forceSetDisplayMode = false)
	{
		if (IsTopNavigationView || _splitView == null)
		{
			return;
		}
		FANavigationViewDisplayMode fANavigationViewDisplayMode = FANavigationViewDisplayMode.Compact;
		switch (PaneDisplayMode)
		{
		case FANavigationViewPaneDisplayMode.Auto:
			if (width >= ExpandedModeThresholdWidth)
			{
				fANavigationViewDisplayMode = FANavigationViewDisplayMode.Expanded;
			}
			else if (width > 0.0 && width < CompactModeThresholdWidth)
			{
				fANavigationViewDisplayMode = FANavigationViewDisplayMode.Minimal;
			}
			break;
		case FANavigationViewPaneDisplayMode.Left:
			fANavigationViewDisplayMode = FANavigationViewDisplayMode.Expanded;
			break;
		case FANavigationViewPaneDisplayMode.LeftCompact:
			fANavigationViewDisplayMode = FANavigationViewDisplayMode.Compact;
			break;
		case FANavigationViewPaneDisplayMode.LeftMinimal:
			fANavigationViewDisplayMode = FANavigationViewDisplayMode.Minimal;
			break;
		}
		if (!forceSetDisplayMode && _initialNonForcedModeUpdate)
		{
			if (fANavigationViewDisplayMode == FANavigationViewDisplayMode.Minimal || fANavigationViewDisplayMode == FANavigationViewDisplayMode.Compact)
			{
				ClosePane();
			}
			_initialNonForcedModeUpdate = false;
		}
		FANavigationViewDisplayMode displayMode = DisplayMode;
		SetDisplayMode(fANavigationViewDisplayMode, forceSetDisplayMode);
		if (fANavigationViewDisplayMode == FANavigationViewDisplayMode.Expanded && IsPaneVisible && !_wasForceClosed)
		{
			OpenPane();
		}
		if (displayMode == FANavigationViewDisplayMode.Expanded && fANavigationViewDisplayMode == FANavigationViewDisplayMode.Compact)
		{
			ClosePane();
		}
		if (fANavigationViewDisplayMode == FANavigationViewDisplayMode.Minimal)
		{
			ClosePane();
		}
	}

	private void UpdatePaneLayout()
	{
		double totalHeight;
		if (!IsTopNavigationView)
		{
			totalHeight = totalAvailableHeight();
			if (totalHeight > 0.0 && _menuItemsScrollViewer != null)
			{
				((Layoutable)_menuItemsScrollViewer).MaxHeight = heightForMenuItems();
			}
		}
		double heightForMenuItems()
		{
			//IL_0253: Unknown result type (might be due to invalid IL or missing references)
			//IL_0258: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_0128: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			double num = totalHeight / 2.0;
			if (_footerItemsScrollViewer != null)
			{
				if (_leftNavFooterMenuRepeater != null)
				{
					Rect bounds;
					if (_leftNavRepeater != null)
					{
						double num2 = 0.0;
						double num3 = 0.0;
						if (((Visual)_leftNavFooterMenuRepeater).IsVisible)
						{
							num3 = ((Layoutable)_leftNavFooterMenuRepeater).Margin.Vertical();
						}
						double num4 = num3;
						Size val = LayoutHelper.MeasureChild((Layoutable)(object)_leftNavFooterMenuRepeater, Size.Infinity, default(Thickness));
						num2 = num4 + ((Size)(ref val)).Height;
						double num5 = 0.0;
						if (_leftNavFooterContentBorder != null)
						{
							double num6 = 0.0;
							if (((Visual)_leftNavFooterContentBorder).IsVisible)
							{
								num6 = ((Layoutable)_leftNavFooterContentBorder).Margin.Vertical();
							}
							bounds = ((Visual)_leftNavFooterContentBorder).Bounds;
							num5 = ((Rect)(ref bounds)).Height + num6;
						}
						val = ((Layoutable)_leftNavRepeater).DesiredSize;
						double height = ((Size)(ref val)).Height;
						bounds = ((Visual)_leftNavRepeater).Bounds;
						double num7 = ((Rect)(ref bounds)).Height + (((Visual)_leftNavRepeater).IsVisible ? ((Layoutable)_leftNavRepeater).Margin.Vertical() : 0.0);
						double num8 = num2 + num5;
						if (_footerItemsSource.Count == 0 && !IsSettingsVisible)
						{
							PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":separator", false);
							return totalHeight;
						}
						if (_menuItemsSource.Count == 0)
						{
							((Layoutable)_footerItemsScrollViewer).MaxHeight = totalHeight;
							PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":separator", false);
							return 0.0;
						}
						if (totalHeight >= height + num8)
						{
							((Layoutable)_footerItemsScrollViewer).MaxHeight = num2;
							PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":separator", false);
							return totalHeight - num2;
						}
						if (height <= num)
						{
							((Layoutable)_footerItemsScrollViewer).MaxHeight = totalHeight - num7;
							PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":separator", true);
							return num7;
						}
						if (num2 <= num)
						{
							((Layoutable)_footerItemsScrollViewer).MaxHeight = num2;
							PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":separator", true);
							return totalHeight - num2;
						}
						((Layoutable)_footerItemsScrollViewer).MaxHeight = num;
						PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":separator", true);
						return num;
					}
					double num9 = totalHeight;
					bounds = ((Visual)_leftNavFooterMenuRepeater).Bounds;
					return num9 - ((Rect)(ref bounds)).Height;
				}
				((Layoutable)_footerItemsScrollViewer).MaxHeight = num;
			}
			return num;
		}
		double totalAvailableHeight()
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			if (_itemsContainerRow != null)
			{
				Control itemsContainer = _itemsContainer;
				double num = ((itemsContainer != null) ? ((Layoutable)itemsContainer).Margin.Vertical() : 0.0);
				return _itemsContainerRow.ActualHeight - num;
			}
			return 0.0;
		}
	}

	private void OnPaneTitleHolderSizeChanged(Rect r)
	{
		UpdateBackAndCloseButtonsVisibility();
	}

	private void OpenPane()
	{
		try
		{
			_isOpenPaneForInteraction = true;
			IsPaneOpen = true;
		}
		finally
		{
			_isOpenPaneForInteraction = false;
		}
	}

	private void ClosePane()
	{
		try
		{
			_isOpenPaneForInteraction = true;
			IsPaneOpen = false;
		}
		finally
		{
			_isOpenPaneForInteraction = false;
		}
	}

	private bool AttemptClosePaneLightly()
	{
		FANavigationViewPaneClosingEventArgs e = new FANavigationViewPaneClosingEventArgs();
		PaneClosing?.Invoke(this, e);
		if (!e.Cancel || _wasForceClosed)
		{
			_blockNextClosingEvent = true;
			ClosePane();
			return true;
		}
		return false;
	}

	private void UpdatePaneTabFocusNavigation()
	{
		_ = _appliedTemplate;
	}

	private void ClosePaneIfNecessaryAfterItemIsClicked(FANavigationViewItem item)
	{
		if (IsPaneOpen && DisplayMode != FANavigationViewDisplayMode.Expanded && !DoesNavigationViewItemHaveChildren(item) && !_shouldIgnoreNextSelectionChange)
		{
			ClosePane();
		}
	}

	private void InvalidateTopNavPrimaryLayout()
	{
		if (_appliedTemplate && IsTopNavigationView)
		{
			((Layoutable)this).InvalidateMeasure();
		}
	}

	private void ResetAndRearrangeTopNavItems(Size availableSize)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (HasTopNavigationViewItemNotInPrimaryList())
		{
			_topDataProvider.MoveAllItemsToPrimaryList();
		}
		ArrangeTopNavItems(availableSize);
	}

	private void HandleTopNavigationMeasureOverride(Size availableSize)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (HasTopNavigationViewItemNotInPrimaryList())
		{
			HandleTopNavigationMeasureOverrideOverflow(availableSize);
		}
		else
		{
			HandleTopNavigationMeasureOverrideNormal(availableSize);
		}
		if (_topNavigationMode == TopNavigationViewLayoutState.Uninitialized)
		{
			_topNavigationMode = TopNavigationViewLayoutState.Initialized;
		}
	}

	private void HandleTopNavigationMeasureOverrideNormal(Size availableSize)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (MeasureTopNavigationViewDesiredWidth(Size.Infinity) > ((Size)(ref availableSize)).Width)
		{
			ResetAndRearrangeTopNavItems(availableSize);
		}
	}

	private void HandleTopNavigationMeasureOverrideOverflow(Size availableSize)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		double num = MeasureTopNavigationViewDesiredWidth(Size.Infinity);
		if (num > ((Size)(ref availableSize)).Width)
		{
			ShrinkTopNavigationSize(num, availableSize);
		}
		else if (num < ((Size)(ref availableSize)).Width)
		{
			double num2 = _topDataProvider.WidthRequiredToRecoveryAllItemsToPrimary();
			if (((Size)(ref availableSize)).Width >= num + num2 + (double)_topNavigationRecoveryGracePeriodWidth)
			{
				ResetAndRearrangeTopNavItems(availableSize);
				return;
			}
			IList<int> indexes = FindMovableItemsRecoverToPrimaryList(((Size)(ref availableSize)).Width - num, new List<int>(0));
			_topDataProvider.MoveItemsToPrimaryList(indexes);
		}
	}

	private void ArrangeTopNavItems(Size availableSize)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		SetOverflowButtonVisibility(vis: false);
		double num = MeasureTopNavigationViewDesiredWidth(Size.Infinity);
		if (!(num < ((Size)(ref availableSize)).Width))
		{
			SetOverflowButtonVisibility(vis: true);
			double num2 = MeasureTopNavigationViewDesiredWidth(Size.Infinity);
			_topDataProvider.OverflowButtonWidth = num2 - num;
			ShrinkTopNavigationSize(num2, availableSize);
		}
	}

	private bool NeedRearrangeOfTopElementsAfterOverflowSelectionChange(int selOriginalIndex)
	{
		bool flag = false;
		_topDataProvider.GetPrimaryItems();
		int primaryListSize = _topDataProvider.PrimaryListSize;
		int num = _topDataProvider.ConvertOriginalIndexToIndex(selOriginalIndex);
		if (num < primaryListSize - 1)
		{
			int num2 = num + 1;
			int num3 = selOriginalIndex + 1;
			int num4 = selOriginalIndex - 1;
			if (num > 0 && _topDataProvider.ConvertPrimaryIndexToIndex(new int[1] { num2 - 1 })[0] != num4)
			{
				flag = true;
			}
			while (!flag && num2 < primaryListSize)
			{
				IList<int> list = _topDataProvider.ConvertPrimaryIndexToIndex(new int[1] { num2 });
				if (num3 != list[0])
				{
					flag = true;
					break;
				}
				num2++;
				num3++;
			}
		}
		return flag;
	}

	private void ShrinkTopNavigationSize(double desWidth, Size availableSize)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		UpdateTopNavigationWidthCache();
		int selectedItemIndex = SelectedItemIndex;
		double num = MeasureTopNavMenuItemsHostDesiredWidth(Size.Infinity) - (desWidth - ((Size)(ref availableSize)).Width);
		if (num >= 0.0)
		{
			IList<int> list = FindMovableItemsBeyondAvailableWidth(num);
			KeepAtLeastOneItemInPrimaryList(list, shouldKeepFirst: true);
			_topDataProvider.MoveItemsOutOfPrimaryList(list);
		}
		desWidth = MeasureTopNavigationViewDesiredWidth(Size.Infinity);
		double num2 = desWidth - ((Size)(ref availableSize)).Width;
		if (num2 > 0.0)
		{
			IList<int> list2 = FindMovableItemsToBeRemovedFromPrimaryList(num2, new int[1] { selectedItemIndex });
			KeepAtLeastOneItemInPrimaryList(list2, shouldKeepFirst: false);
			_topDataProvider.MoveItemsOutOfPrimaryList(list2);
		}
	}

	private IList<int> FindMovableItemsRecoverToPrimaryList(double availableWidth, IList<int> includeItems)
	{
		List<int> list = new List<int>(includeItems.Count + 4);
		int size = _topDataProvider.Size;
		for (int i = 0; i < includeItems.Count; i++)
		{
			list.Add(includeItems[i]);
			availableWidth -= _topDataProvider.GetWidthForItem(includeItems[i]);
		}
		int j;
		for (j = 0; j < size; j++)
		{
			if (!(availableWidth > 0.0))
			{
				break;
			}
			if (!_topDataProvider.IsItemInPrimaryList(j) && !includeItems.Contains(j))
			{
				double widthForItem = _topDataProvider.GetWidthForItem(j);
				if (!(availableWidth >= widthForItem))
				{
					break;
				}
				list.Add(j);
				availableWidth -= widthForItem;
			}
		}
		if (j == size && list.Count != 0)
		{
			list.RemoveAt(list.Count - 1);
		}
		return list;
	}

	private IList<int> FindMovableItemsToBeRemovedFromPrimaryList(double widthAtLeastToBeRemoved, IList<int> excludeItems)
	{
		List<int> list = new List<int>();
		int num = _topDataProvider.Size - 1;
		while (num >= 0 && widthAtLeastToBeRemoved > 0.0)
		{
			if (_topDataProvider.IsItemInPrimaryList(num) && !excludeItems.Contains(num))
			{
				list.Add(num);
				widthAtLeastToBeRemoved -= _topDataProvider.GetWidthForItem(num);
			}
			num--;
		}
		return list;
	}

	private IList<int> FindMovableItemsBeyondAvailableWidth(double availableWidth)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		List<int> list = new List<int>();
		if (_topNavRepeater != null)
		{
			int num = _topDataProvider.IndexOf(SelectedItem, NavigationViewSplitVectorID.PrimaryList);
			int primaryListSize = _topDataProvider.PrimaryListSize;
			double num2 = 0.0;
			for (int i = 0; i < primaryListSize; i++)
			{
				if (i == num)
				{
					continue;
				}
				bool flag = true;
				if (num2 <= availableWidth)
				{
					Control val = _topNavRepeater.TryGetElement(i);
					if (val != null)
					{
						double num3 = num2;
						Size desiredSize = ((Layoutable)val).DesiredSize;
						num2 = num3 + ((Size)(ref desiredSize)).Width;
						flag = num2 > availableWidth;
					}
				}
				if (flag)
				{
					list.Add(i);
				}
			}
		}
		return _topDataProvider.ConvertPrimaryIndexToIndex(list);
	}

	private void KeepAtLeastOneItemInPrimaryList(IList<int> itemInPrimaryToBeRemoved, bool shouldKeepFirst)
	{
		if (itemInPrimaryToBeRemoved.Count > 0 && itemInPrimaryToBeRemoved.Count == _topDataProvider.PrimaryListSize)
		{
			if (shouldKeepFirst)
			{
				itemInPrimaryToBeRemoved.RemoveAt(0);
			}
			else
			{
				itemInPrimaryToBeRemoved.RemoveAt(itemInPrimaryToBeRemoved.Count - 1);
			}
		}
	}

	private void UpdateTopNavigationWidthCache()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		int primaryListSize = _topDataProvider.PrimaryListSize;
		if (_topNavRepeater == null)
		{
			return;
		}
		for (int i = 0; i < primaryListSize; i++)
		{
			Control val = _topNavRepeater.TryGetElement(i);
			if (val != null)
			{
				TopNavigationViewDataProvider topDataProvider = _topDataProvider;
				int indexInPrimary = i;
				Size desiredSize = ((Layoutable)val).DesiredSize;
				topDataProvider.UpdateWidthForPrimaryItem(indexInPrimary, ((Size)(ref desiredSize)).Width);
				continue;
			}
			break;
		}
	}

	private void OnSplitViewClosedCompactChanged(AvaloniaPropertyChangedEventArgs args)
	{
		if (args.Property == (AvaloniaProperty)(object)SplitView.IsPaneOpenProperty || args.Property == (AvaloniaProperty)(object)SplitView.DisplayModeProperty)
		{
			UpdateIsClosedCompact();
		}
	}

	private void OnSplitViewPaneClosed(object sender, RoutedEventArgs e)
	{
		if (e.Source == _splitView)
		{
			PaneClosed?.Invoke(this, EventArgs.Empty);
		}
	}

	private void OnSplitViewPaneClosing(object sender, CancelRoutedEventArgs e)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Invalid comparison between Unknown and I4
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Invalid comparison between Unknown and I4
		if (((RoutedEventArgs)e).Source != _splitView)
		{
			return;
		}
		bool flag = false;
		if (!_blockNextClosingEvent)
		{
			FANavigationViewPaneClosingEventArgs e2 = new FANavigationViewPaneClosingEventArgs();
			e2.SplitViewClosingArgs = e;
			PaneClosing?.Invoke(this, e2);
			flag = e2.Cancel;
		}
		else
		{
			_blockNextClosingEvent = false;
		}
		if (!flag && _splitView != null && _leftNavRepeater != null)
		{
			if ((int)_splitView.DisplayMode == 1 || (int)_splitView.DisplayMode == 3)
			{
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":listsizecompact", true);
				UpdatePaneToggleSize();
			}
			else
			{
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":listsizecompact", false);
			}
		}
	}

	private void OnSplitViewPaneOpened(object sender, RoutedEventArgs e)
	{
		if (e.Source == _splitView)
		{
			PaneOpened?.Invoke(this, EventArgs.Empty);
		}
	}

	private void OnSplitViewPaneOpening(object sender, RoutedEventArgs e)
	{
		if (e.Source == _splitView)
		{
			if (_leftNavRepeater != null)
			{
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":listsizecompact", false);
			}
			PaneOpening?.Invoke(this, EventArgs.Empty);
		}
	}

	private void OnPaneToggleButtonClick(object sender, RoutedEventArgs e)
	{
		if (IsPaneOpen)
		{
			_wasForceClosed = true;
			ClosePane();
		}
		else
		{
			_wasForceClosed = false;
			OpenPane();
		}
	}

	private void OnPaneSearchButtonClick(object sender, RoutedEventArgs e)
	{
		_wasForceClosed = false;
		OpenPane();
		if (AutoCompleteBox != null)
		{
			((InputElement)AutoCompleteBox).Focus((NavigationMethod)1, (KeyModifiers)0);
		}
	}

	private void OnBackButtonClicked(object sender, RoutedEventArgs e)
	{
		FANavigationViewBackRequestedEventArgs e2 = new FANavigationViewBackRequestedEventArgs();
		BackRequested?.Invoke(this, e2);
	}

	private void UpdatePaneButtonWidths()
	{
		TemplateSettings.PaneToggleButtonWidth = CompactPaneLength;
		TemplateSettings.SmallerPaneToggleButtonWidth = CompactPaneLength - 8.0;
	}

	private void UpdatePaneToggleButtonVisibility()
	{
		TemplateSettings.PaneToggleButtonVisibility = IsPaneToggleButtonVisible && !IsTopNavigationView;
	}

	private void UpdatePaneToggleSize()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Invalid comparison between Unknown and I4
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Invalid comparison between Unknown and I4
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Invalid comparison between Unknown and I4
		if (_splitView == null)
		{
			return;
		}
		double paneToggleButtonWidth = TemplateSettings.PaneToggleButtonWidth;
		double width = paneToggleButtonWidth;
		if (ShouldShowBackButton && (int)_splitView.DisplayMode == 2)
		{
			double num = paneToggleButtonWidth;
			Button backButton = _backButton;
			paneToggleButtonWidth = num + ((backButton != null) ? ((Layoutable)backButton).Width : 40.0);
		}
		if (!_isClosedCompact && !string.IsNullOrEmpty(PaneTitle))
		{
			if ((int)_splitView.DisplayMode == 2 && IsPaneOpen)
			{
				paneToggleButtonWidth = OpenPaneLength;
				width = OpenPaneLength - (double)((ShouldShowBackButton || ShouldShowCloseButton) ? 40 : 0);
			}
			else if ((int)_splitView.DisplayMode != 2 || IsPaneOpen)
			{
				paneToggleButtonWidth = OpenPaneLength;
				width = OpenPaneLength;
			}
		}
		Button paneToggleButton = _paneToggleButton;
		if (paneToggleButton != null)
		{
			((Layoutable)paneToggleButton).Width = width;
		}
	}

	private void UpdateBackAndCloseButtonsVisibility()
	{
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		if (!_appliedTemplate)
		{
			return;
		}
		bool shouldShowBackButton = ShouldShowBackButton;
		NavigationViewVisualStateDisplayMode visualStateDisplayMode = GetVisualStateDisplayMode(DisplayMode);
		bool flag = (visualStateDisplayMode == NavigationViewVisualStateDisplayMode.Minimal && !IsTopNavigationView) || visualStateDisplayMode == NavigationViewVisualStateDisplayMode.MinimalWithBackButton;
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		TemplateSettings.BackButtonVisibility = shouldShowBackButton;
		if (_paneToggleButton != null && IsPaneToggleButtonVisible)
		{
			num4 = GetPaneToggleButtonHeight();
			num2 = GetPaneToggleButtonWidth();
			if (flag)
			{
				num = num2;
			}
		}
		if (_backButton != null && (flag & shouldShowBackButton))
		{
			num += ((Layoutable)_backButton).Width;
		}
		if (_closeButton != null)
		{
			((Visual)_closeButton).IsVisible = ShouldShowCloseButton;
			if (ShouldShowCloseButton)
			{
				num4 = Math.Max(num4, ((Layoutable)_closeButton).Height);
				if (flag)
				{
					num3 = ((Layoutable)_closeButton).Width;
					num += num3;
				}
			}
		}
		if (_contentLeftPadding != null)
		{
			((Layoutable)_contentLeftPadding).Width = num;
		}
		if (_paneHeaderToggleButtonColumn != null)
		{
			_paneHeaderToggleButtonColumn.Width = new GridLength(num2);
		}
		if (_paneHeaderCloseButtonColumn != null)
		{
			_paneHeaderCloseButtonColumn.Width = new GridLength(num3);
		}
		if (_paneTitleHolderFrameworkElement != null && num4 == 0.0 && ((Visual)_paneTitleHolderFrameworkElement).IsVisible)
		{
			Rect bounds = ((Visual)_paneTitleHolderFrameworkElement).Bounds;
			num4 = ((Rect)(ref bounds)).Height;
		}
		if (_paneHeaderContentBorderRow != null)
		{
			_paneHeaderContentBorderRow.MinHeight = num4;
		}
		if (_paneContentGrid != null && ((AvaloniaList<RowDefinition>)(object)_paneContentGrid.RowDefinitions).Count >= 1)
		{
			int num5 = 0;
			if (!IsOverlay & shouldShowBackButton)
			{
				num5 = 40;
			}
			else if (_backButton == null)
			{
				num5 = 56;
			}
			((AvaloniaList<RowDefinition>)(object)_paneContentGrid.RowDefinitions)[1].Height = new GridLength((double)num5);
		}
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":backbuttoncollapsed", !shouldShowBackButton);
		UpdateTitleBarPadding();
	}

	private void SetDisplayMode(FANavigationViewDisplayMode dMode, bool forceSetDisplayMode = false)
	{
		UpdateVisualStateForDisplayModeGroup(dMode);
		if (forceSetDisplayMode || DisplayMode != dMode)
		{
			UpdateHeaderVisibility(dMode);
			UpdatePaneTabFocusNavigation();
			UpdatePaneToggleSize();
			RaiseDisplayModeChanged(dMode);
		}
	}

	private NavigationViewVisualStateDisplayMode GetVisualStateDisplayMode(FANavigationViewDisplayMode dMode)
	{
		FANavigationViewPaneDisplayMode paneDisplayMode = PaneDisplayMode;
		if (IsTopNavigationView)
		{
			return NavigationViewVisualStateDisplayMode.Minimal;
		}
		if (paneDisplayMode == FANavigationViewPaneDisplayMode.Left || (paneDisplayMode == FANavigationViewPaneDisplayMode.Auto && dMode == FANavigationViewDisplayMode.Expanded))
		{
			return NavigationViewVisualStateDisplayMode.Expanded;
		}
		if (paneDisplayMode == FANavigationViewPaneDisplayMode.LeftCompact || (paneDisplayMode == FANavigationViewPaneDisplayMode.Auto && dMode == FANavigationViewDisplayMode.Compact))
		{
			return NavigationViewVisualStateDisplayMode.Compact;
		}
		if (ShouldShowBackButton || ShouldShowCloseButton)
		{
			return NavigationViewVisualStateDisplayMode.MinimalWithBackButton;
		}
		return NavigationViewVisualStateDisplayMode.Minimal;
	}

	private void UpdateVisualStateForDisplayModeGroup(FANavigationViewDisplayMode dMode)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		if (_splitView != null)
		{
			NavigationViewVisualStateDisplayMode visualStateDisplayMode = GetVisualStateDisplayMode(dMode);
			SplitViewDisplayMode displayMode = (SplitViewDisplayMode)2;
			switch (visualStateDisplayMode)
			{
			case NavigationViewVisualStateDisplayMode.MinimalWithBackButton:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":minimalwithback", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":minimal", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topnavminimal", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":compact", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":expanded", false);
				displayMode = (SplitViewDisplayMode)2;
				break;
			case NavigationViewVisualStateDisplayMode.Minimal:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":minimalwithback", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":minimal", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topnavminimal", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":compact", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":expanded", false);
				displayMode = (SplitViewDisplayMode)2;
				break;
			case NavigationViewVisualStateDisplayMode.Compact:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":minimalwithback", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":minimal", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topnavminimal", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":compact", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":expanded", false);
				displayMode = (SplitViewDisplayMode)3;
				break;
			case NavigationViewVisualStateDisplayMode.Expanded:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":minimalwithback", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":minimal", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topnavminimal", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":compact", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":expanded", true);
				displayMode = (SplitViewDisplayMode)1;
				break;
			}
			if (!IsPaneVisible)
			{
				displayMode = (SplitViewDisplayMode)3;
			}
			if (IsTopNavigationView)
			{
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":minimalwithback", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":minimal", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topnavminimal", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":compact", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":expanded", false);
			}
			if (_fromOnApplyTemplate)
			{
				_updateVisualStateForDisplayModeFromOnLoaded = true;
			}
			else
			{
				_splitView.DisplayMode = displayMode;
			}
		}
	}

	private void UpdateVisualState()
	{
		if (_appliedTemplate)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":autosuggestcollapsed", AutoCompleteBox == null);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":settingscollapsed", IsSettingsVisible);
			if (!IsTopNavigationView)
			{
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":panetogglecollapsed", !IsPaneToggleButtonVisible || _isLeftPaneTitleEmpty);
			}
		}
	}

	private void RaiseDisplayModeChanged(FANavigationViewDisplayMode mode)
	{
		DisplayMode = mode;
		FANavigationViewDisplayModeChangedEventArgs e = new FANavigationViewDisplayModeChangedEventArgs(mode);
		DisplayModeChanged?.Invoke(this, e);
	}

	private void UpdatePaneOverlayGroup()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		if (_splitView != null)
		{
			if (IsPaneOpen && ((int)_splitView.DisplayMode == 3 || (int)_splitView.DisplayMode == 2))
			{
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":panenotoverlaying", false);
			}
			else
			{
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":panenotoverlaying", true);
			}
		}
	}

	private void AnimateSelectionChangedToItem(object selItem)
	{
		if (selItem != null && !IsSelectionSuppressed(selItem))
		{
			AnimateSelectionChanged(selItem);
		}
	}

	private void AnimateSelectionChanged(object nextItem)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		if (_lastSelectedItemPendingAnimationInTopNav != null || !_appliedTemplate)
		{
			return;
		}
		Control activeIndicator = _activeIndicator;
		Control val = FindSelectionIndicator(nextItem);
		if (_activeIndicator == null && nextItem != null && val == null)
		{
			Dispatcher.UIThread.Post((Action)delegate
			{
				AnimateSelectionChanged(nextItem);
			}, default(DispatcherPriority));
		}
		bool flag = false;
		if (_prevIndicator != null || _nextIndicator != null)
		{
			if (val != null && _nextIndicator == val)
			{
				if (activeIndicator != null && _prevIndicator == null)
				{
					ResetElementAnimationProperties(activeIndicator, 0.0);
				}
				flag = true;
			}
			else
			{
				OnAnimationComplete();
			}
		}
		if (flag)
		{
			return;
		}
		Grid paneContentGrid = _paneContentGrid;
		if (activeIndicator != val && paneContentGrid != null && activeIndicator != null && val != null && FAUISettings.AreAnimationsEnabled())
		{
			ResetElementAnimationProperties(activeIndicator, 1.0);
			ResetElementAnimationProperties(val, 1.0);
			Point val2 = default(Point);
			Matrix val3 = (Matrix)(((_003F?)VisualExtensions.TransformToVisual((Visual)(object)activeIndicator, (Visual)(object)_paneContentGrid)) ?? Matrix.Identity);
			Matrix val4 = (Matrix)(((_003F?)VisualExtensions.TransformToVisual((Visual)(object)val, (Visual)(object)_paneContentGrid)) ?? Matrix.Identity);
			Point val5 = ((Matrix)(ref val3)).Transform(val2);
			Point val6 = ((Matrix)(ref val4)).Transform(val2);
			Rect bounds = ((Visual)activeIndicator).Bounds;
			Size size = ((Rect)(ref bounds)).Size;
			bounds = ((Visual)val).Bounds;
			Size size2 = ((Rect)(ref bounds)).Size;
			bool flag2 = false;
			double num;
			double num2;
			if (IsTopNavigationView)
			{
				num = ((Point)(ref val5)).X;
				num2 = ((Point)(ref val6)).X;
				flag2 = ((Point)(ref val5)).Y == ((Point)(ref val6)).Y;
			}
			else
			{
				num = ((Point)(ref val5)).Y;
				num2 = ((Point)(ref val6)).Y;
				flag2 = ((Point)(ref val5)).X == ((Point)(ref val6)).X;
			}
			ElementComposition.GetElementVisual((Visual)(object)this);
			if (!flag2)
			{
				bool flag3 = ((Point)(ref val5)).Y < ((Point)(ref val6)).Y;
				bounds = ((Visual)activeIndicator).Bounds;
				double height = ((Rect)(ref bounds)).Height;
				bounds = ((Visual)activeIndicator).Bounds;
				if (height > ((Rect)(ref bounds)).Width)
				{
					PlayIndicatorNonSameLevelAnimations(activeIndicator, isOutgoing: true, !flag3);
				}
				else
				{
					PlayIndicatorNonSameLevelTopPrimaryAnimation(activeIndicator, isOutgoing: true);
				}
				bounds = ((Visual)val).Bounds;
				double height2 = ((Rect)(ref bounds)).Height;
				bounds = ((Visual)val).Bounds;
				if (height2 > ((Rect)(ref bounds)).Width)
				{
					PlayIndicatorNonSameLevelAnimations(val, isOutgoing: false, flag3);
				}
				else
				{
					PlayIndicatorNonSameLevelTopPrimaryAnimation(val, isOutgoing: false);
				}
			}
			else
			{
				double to = num2 - num;
				double num3 = num - num2;
				PlayIndicatorAnimations(activeIndicator, 0.0, to, size, size2, isOutgoing: true);
				PlayIndicatorAnimations(val, num3, 0.0, size, size2, isOutgoing: false);
			}
			_prevIndicator = activeIndicator;
			_nextIndicator = val;
			DispatcherTimer.RunOnce((Action)OnAnimationComplete, TimeSpan.FromMilliseconds(700L), DispatcherPriority.Render);
		}
		else if (activeIndicator != val)
		{
			ResetElementAnimationProperties(activeIndicator, 0.0);
			ResetElementAnimationProperties(val, 1.0);
		}
		_activeIndicator = val;
	}

	private void PlayIndicatorNonSameLevelAnimations(Control indicator, bool isOutgoing, bool fromTop)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		CompositionVisual elementVisual = ElementComposition.GetElementVisual((Visual)(object)indicator);
		if (elementVisual != null)
		{
			Compositor compositor = ((CompositionObject)elementVisual).Compositor;
			double num = (isOutgoing ? 1 : 0);
			double num2 = ((!isOutgoing) ? 1 : 0);
			Vector3DKeyFrameAnimation val = compositor.CreateVector3DKeyFrameAnimation();
			val.InsertKeyFrame(0f, new Vector3D(1.0, num, 1.0));
			val.InsertKeyFrame(1f, new Vector3D(1.0, num2, 1.0));
			((KeyFrameAnimation)val).Duration = TimeSpan.FromMilliseconds(600L);
			Rect bounds = ((Visual)indicator).Bounds;
			Size size = ((Rect)(ref bounds)).Size;
			double num3 = (IsTopNavigationView ? ((Size)(ref size)).Width : ((Size)(ref size)).Height);
			double num4 = (fromTop ? 0.0 : num3);
			Vector3D centerPoint = elementVisual.CenterPoint;
			((Vector3D)(ref centerPoint))._002Ector(((Vector3D)(ref centerPoint)).X, num4, ((Vector3D)(ref centerPoint)).Z);
			elementVisual.CenterPoint = centerPoint;
			((CompositionObject)elementVisual).StartAnimation("Scale", (CompositionAnimation)(object)val);
			if (isOutgoing)
			{
				ScalarKeyFrameAnimation val2 = compositor.CreateScalarKeyFrameAnimation();
				val2.InsertKeyFrame(0f, 1f);
				((KeyFrameAnimation)val2).Duration = TimeSpan.FromMilliseconds(600L);
				((CompositionObject)elementVisual).StartAnimation("Opacity", (CompositionAnimation)(object)val2);
			}
		}
	}

	private void PlayIndicatorNonSameLevelTopPrimaryAnimation(Control indicator, bool isOutgoing)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		CompositionVisual elementVisual = ElementComposition.GetElementVisual((Visual)(object)indicator);
		if (elementVisual != null)
		{
			Compositor compositor = ((CompositionObject)elementVisual).Compositor;
			double num = (isOutgoing ? 1 : 0);
			double num2 = ((!isOutgoing) ? 1 : 0);
			Vector3DKeyFrameAnimation val = compositor.CreateVector3DKeyFrameAnimation();
			Vector3D scale = elementVisual.Scale;
			double y = ((Vector3D)(ref scale)).Y;
			scale = elementVisual.Scale;
			val.InsertKeyFrame(0f, new Vector3D(num, y, ((Vector3D)(ref scale)).Z));
			scale = elementVisual.Scale;
			double y2 = ((Vector3D)(ref scale)).Y;
			scale = elementVisual.Scale;
			val.InsertKeyFrame(1f, new Vector3D(num2, y2, ((Vector3D)(ref scale)).Z));
			((KeyFrameAnimation)val).Duration = TimeSpan.FromMilliseconds(600L);
			Rect bounds = ((Visual)indicator).Bounds;
			Size size = ((Rect)(ref bounds)).Size;
			double num3 = ((Size)(ref size)).Width / 2.0;
			Vector3D centerPoint = elementVisual.CenterPoint;
			((Vector3D)(ref centerPoint))._002Ector(((Vector3D)(ref centerPoint)).X, num3, ((Vector3D)(ref centerPoint)).Z);
			elementVisual.CenterPoint = centerPoint;
			((CompositionObject)elementVisual).StartAnimation("Scale", (CompositionAnimation)(object)val);
		}
	}

	private void PlayIndicatorAnimations(Control indicator, double from, double to, Size beginSize, Size endSize, bool isOutgoing)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Expected O, but got Unknown
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0431: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0469: Unknown result type (might be due to invalid IL or missing references)
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04db: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0506: Unknown result type (might be due to invalid IL or missing references)
		//IL_050b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0515: Unknown result type (might be due to invalid IL or missing references)
		//IL_051a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0523: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		CompositionVisual elementVisual = ElementComposition.GetElementVisual((Visual)(object)indicator);
		if (elementVisual != null)
		{
			Compositor compositor = ((CompositionObject)elementVisual).Compositor;
			Rect bounds = ((Visual)indicator).Bounds;
			Size size = ((Rect)(ref bounds)).Size;
			double num = (IsTopNavigationView ? ((Size)(ref size)).Width : ((Size)(ref size)).Height);
			double num2 = 1.0;
			double num3 = 1.0;
			if (IsTopNavigationView && Math.Abs(((Size)(ref size)).Width) > 0.001)
			{
				num2 = ((Size)(ref beginSize)).Width / ((Size)(ref size)).Width;
				num3 = ((Size)(ref endSize)).Width / ((Size)(ref size)).Width;
			}
			SplineEasing val = new SplineEasing(0.9, 0.1, 1.0, 0.2);
			SplineEasing val2 = new SplineEasing(0.1, 0.9, 0.2, 1.0);
			StepEasingFunction stepEasingFunction = new StepEasingFunction
			{
				Steps = 5
			};
			if (isOutgoing)
			{
				ScalarKeyFrameAnimation val3 = compositor.CreateScalarKeyFrameAnimation();
				val3.InsertKeyFrame(0f, 1f);
				val3.InsertKeyFrame(0.333f, 1f, (IEasing)(object)stepEasingFunction);
				val3.InsertKeyFrame(1f, 0f, (IEasing)(object)val2);
				((KeyFrameAnimation)val3).Duration = TimeSpan.FromMilliseconds(600L);
				((CompositionObject)elementVisual).StartAnimation("Opacity", (CompositionAnimation)(object)val3);
			}
			Vector3D val5;
			if (!IsTopNavigationView)
			{
				Vector3DKeyFrameAnimation val4 = compositor.CreateVector3DKeyFrameAnimation();
				val5 = elementVisual.Offset;
				double x = ((Vector3D)(ref val5)).X;
				double num4 = ((from < to) ? from : (from + num * (num2 - 1.0)));
				val5 = elementVisual.Offset;
				val4.InsertKeyFrame(0f, new Vector3D(x, num4, ((Vector3D)(ref val5)).Z));
				val5 = elementVisual.Offset;
				double x2 = ((Vector3D)(ref val5)).X;
				double num5 = ((from < to) ? (to + num * (num3 - 1.0)) : to);
				val5 = elementVisual.Offset;
				val4.InsertKeyFrame(0.333f, new Vector3D(x2, num5, ((Vector3D)(ref val5)).Z), (IEasing)(object)stepEasingFunction);
				((KeyFrameAnimation)val4).Duration = TimeSpan.FromMilliseconds(600L);
				Vector3DKeyFrameAnimation val6 = compositor.CreateVector3DKeyFrameAnimation();
				val6.InsertKeyFrame(0f, new Vector3D(1.0, num2, 1.0));
				val6.InsertKeyFrame(0.333f, new Vector3D(1.0, Math.Abs(to - from) / num + ((from < to) ? num3 : num2), 1.0), (IEasing)(object)val);
				val6.InsertKeyFrame(1f, new Vector3D(1.0, num3, num3), (IEasing)(object)val2);
				((KeyFrameAnimation)val6).Duration = TimeSpan.FromMilliseconds(600L);
				Vector3DKeyFrameAnimation val7 = compositor.CreateVector3DKeyFrameAnimation();
				val5 = elementVisual.CenterPoint;
				double x3 = ((Vector3D)(ref val5)).X;
				double num6 = ((from < to) ? 0.0 : num);
				val5 = elementVisual.CenterPoint;
				val7.InsertKeyFrame(0f, new Vector3D(x3, num6, ((Vector3D)(ref val5)).Z));
				val5 = elementVisual.CenterPoint;
				double x4 = ((Vector3D)(ref val5)).X;
				double num7 = ((from < to) ? num : 0.0);
				val5 = elementVisual.CenterPoint;
				val7.InsertKeyFrame(1f, new Vector3D(x4, num7, ((Vector3D)(ref val5)).Z), (IEasing)(object)stepEasingFunction);
				((KeyFrameAnimation)val7).Duration = TimeSpan.FromMilliseconds(200L);
				((CompositionObject)elementVisual).StartAnimation("Offset", (CompositionAnimation)(object)val4);
				((CompositionObject)elementVisual).StartAnimation("Scale", (CompositionAnimation)(object)val6);
				((CompositionObject)elementVisual).StartAnimation("CenterPoint", (CompositionAnimation)(object)val7);
			}
			else
			{
				Vector3DKeyFrameAnimation val8 = compositor.CreateVector3DKeyFrameAnimation();
				double num8 = ((from < to) ? from : (from + num * (num2 - 1.0)));
				val5 = elementVisual.Offset;
				double y = ((Vector3D)(ref val5)).Y;
				val5 = elementVisual.Offset;
				val8.InsertKeyFrame(0f, new Vector3D(num8, y, ((Vector3D)(ref val5)).Z));
				double num9 = ((from < to) ? (to + num * (num3 - 1.0)) : to);
				val5 = elementVisual.Offset;
				double y2 = ((Vector3D)(ref val5)).Y;
				val5 = elementVisual.Offset;
				val8.InsertKeyFrame(0.333f, new Vector3D(num9, y2, ((Vector3D)(ref val5)).Z), (IEasing)(object)stepEasingFunction);
				((KeyFrameAnimation)val8).Duration = TimeSpan.FromMilliseconds(600L);
				Vector3DKeyFrameAnimation val9 = compositor.CreateVector3DKeyFrameAnimation();
				val9.InsertKeyFrame(0f, new Vector3D(num2, 1.0, 1.0));
				val9.InsertKeyFrame(0.333f, new Vector3D(Math.Abs(to - from) / num + ((from < to) ? num3 : num2), 1.0, 1.0), (IEasing)(object)val);
				val9.InsertKeyFrame(1f, new Vector3D(1.0, num3, num3), (IEasing)(object)val2);
				((KeyFrameAnimation)val9).Duration = TimeSpan.FromMilliseconds(600L);
				Vector3DKeyFrameAnimation val10 = compositor.CreateVector3DKeyFrameAnimation();
				double num10 = ((from < to) ? 0.0 : num);
				val5 = elementVisual.CenterPoint;
				double y3 = ((Vector3D)(ref val5)).Y;
				val5 = elementVisual.CenterPoint;
				val10.InsertKeyFrame(0f, new Vector3D(num10, y3, ((Vector3D)(ref val5)).Z));
				double num11 = ((from < to) ? num : 0.0);
				val5 = elementVisual.CenterPoint;
				double y4 = ((Vector3D)(ref val5)).Y;
				val5 = elementVisual.CenterPoint;
				val10.InsertKeyFrame(1f, new Vector3D(num11, y4, ((Vector3D)(ref val5)).Z), (IEasing)(object)stepEasingFunction);
				((KeyFrameAnimation)val10).Duration = TimeSpan.FromMilliseconds(200L);
				((CompositionObject)elementVisual).StartAnimation("Offset", (CompositionAnimation)(object)val8);
				((CompositionObject)elementVisual).StartAnimation("Scale", (CompositionAnimation)(object)val9);
				((CompositionObject)elementVisual).StartAnimation("CenterPoint", (CompositionAnimation)(object)val10);
			}
		}
	}

	private void OnAnimationComplete()
	{
		Control prevIndicator = _prevIndicator;
		ResetElementAnimationProperties(prevIndicator, 0.0);
		_prevIndicator = null;
		prevIndicator = _nextIndicator;
		ResetElementAnimationProperties(prevIndicator, 1.0);
		_nextIndicator = null;
	}

	private void ResetElementAnimationProperties(Control element, double desiredOpacity)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if (element != null)
		{
			((Visual)element).Opacity = desiredOpacity;
			CompositionVisual elementVisual = ElementComposition.GetElementVisual((Visual)(object)element);
			if (elementVisual != null)
			{
				elementVisual.Offset = new Vector3D(0.0, 0.0, 0.0);
				elementVisual.Scale = new Vector3D(1.0, 1.0, 1.0);
				elementVisual.Opacity = (float)desiredOpacity;
			}
		}
	}

	private Control FindSelectionIndicator(object item)
	{
		if (item != null)
		{
			FANavigationViewItem fANavigationViewItem = NavigationViewItemOrSettingsContentFromData(item);
			if (fANavigationViewItem != null)
			{
				Control selectionIndicator = fANavigationViewItem.SelectionIndicator;
				if (selectionIndicator != null)
				{
					return selectionIndicator;
				}
				((Layoutable)fANavigationViewItem).UpdateLayout();
				return fANavigationViewItem.SelectionIndicator;
			}
		}
		return null;
	}

	private FANavigationViewItem FindLowestLevelContainerToDisplaySelectionIndicator()
	{
		int num = 1;
		IndexPath selectedIndex = _selectionModel.SelectedIndex;
		if (selectedIndex != IndexPath.Unselected && selectedIndex.GetSize() > 1)
		{
			FANavigationViewItem fANavigationViewItem = GetContainerForIndex(selectedIndex.GetAt(num), selectedIndex.GetAt(0) == 1) as FANavigationViewItem;
			if (fANavigationViewItem != null)
			{
				bool flag = fANavigationViewItem.IsRepeaterVisible;
				while (((fANavigationViewItem != null) & flag) && !((ListBoxItem)fANavigationViewItem).IsSelected && fANavigationViewItem.IsChildSelected)
				{
					num++;
					flag = false;
					if (fANavigationViewItem.GetRepeater != null)
					{
						if (fANavigationViewItem.GetRepeater.TryGetElement(selectedIndex.GetAt(num)) is FANavigationViewItem fANavigationViewItem2)
						{
							fANavigationViewItem = fANavigationViewItem2;
							flag = fANavigationViewItem.IsRepeaterVisible;
						}
						else
						{
							fANavigationViewItem = null;
						}
					}
				}
				return fANavigationViewItem;
			}
		}
		return null;
	}

	private void CreateAndHookEventsToSettings()
	{
		if (_settingsItem != null)
		{
			_settingsItem.IconSource = _settingsIconSource;
			string localizedStringResource = FALocalizationHelper.Instance.GetLocalizedStringResource("SettingsButtonName");
			((Control)_settingsItem).Tag = localizedStringResource;
			UpdateSettingsItemToolTip();
			if (!IsTopNavigationView)
			{
				((ContentControl)_settingsItem).Content = localizedStringResource;
			}
			else
			{
				((ContentControl)_settingsItem).Content = null;
			}
			SettingsItem = _settingsItem;
		}
	}

	private void UpdateIsClosedCompact()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		if (_splitView != null)
		{
			SplitViewDisplayMode displayMode = _splitView.DisplayMode;
			_isClosedCompact = !_splitView.IsPaneOpen && ((int)displayMode == 1 || (int)displayMode == 3);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":closedcompact", _isClosedCompact);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":listsizecompact", _isClosedCompact);
			UpdateTitleBarPadding();
			UpdateBackAndCloseButtonsVisibility();
			UpdatePaneToggleSize();
		}
	}

	private void UpdateSettingsItemToolTip()
	{
		if (_settingsItem != null)
		{
			if (!IsTopNavigationView && IsPaneOpen)
			{
				ToolTip.SetTip((Control)(object)_settingsItem, (object)null);
			}
			else
			{
				ToolTip.SetTip((Control)(object)_settingsItem, (object)FALocalizationHelper.Instance.GetLocalizedStringResource("SettingsButtonName"));
			}
		}
	}

	private void UpdatePaneTitleFrameworkElementParents()
	{
		if (_paneTitleHolderFrameworkElement == null)
		{
			return;
		}
		bool isPaneToggleButtonVisible = IsPaneToggleButtonVisible;
		bool isTopNavigationView = IsTopNavigationView;
		_isLeftPaneTitleEmpty = (isPaneToggleButtonVisible | isTopNavigationView) || string.IsNullOrEmpty(PaneTitle) || (PaneDisplayMode == FANavigationViewPaneDisplayMode.LeftMinimal && !IsPaneOpen);
		((Visual)_paneTitleHolderFrameworkElement).IsVisible = !_isLeftPaneTitleEmpty;
		if (_paneTitleFrameworkElement == null)
		{
			return;
		}
		Action action = SetPaneTitleFrameworkElementParent((ContentControl)(object)_paneToggleButton, _paneTitleFrameworkElement, isTopNavigationView || !isPaneToggleButtonVisible);
		Action action2 = SetPaneTitleFrameworkElementParent(_paneTitlePresenter, _paneTitleFrameworkElement, isTopNavigationView | isPaneToggleButtonVisible);
		Action action3 = SetPaneTitleFrameworkElementParent(_paneTitleOnTopPane, _paneTitleFrameworkElement, !isTopNavigationView | isPaneToggleButtonVisible);
		if (action != null)
		{
			action();
			((Visual)_paneTitleOnTopPane).IsVisible = false;
		}
		else if (action2 != null)
		{
			action2();
			((Visual)_paneTitleOnTopPane).IsVisible = false;
		}
		else if (action3 != null)
		{
			action3();
			if (_paneTitleOnTopPane != null)
			{
				((Visual)_paneTitleOnTopPane).IsVisible = !string.IsNullOrEmpty(PaneTitle) && PaneTitle.Length != 0;
			}
		}
	}

	private Action SetPaneTitleFrameworkElementParent(ContentControl parent, Control paneTitle, bool shouldNotContainPaneTitle)
	{
		if (parent != null && parent.Content == paneTitle == shouldNotContainPaneTitle)
		{
			if (!shouldNotContainPaneTitle)
			{
				return delegate
				{
					parent.Content = paneTitle;
				};
			}
			parent.Content = null;
		}
		return null;
	}

	private void OnSettingsInvoked()
	{
		if (_settingsItem != null)
		{
			OnNavigationViewItemInvoked(_settingsItem);
		}
	}

	internal void TopNavigationViewItemContentChanged()
	{
		if (_appliedTemplate)
		{
			if (MenuItemsSource == null)
			{
				_topDataProvider.InvalidWidthCache();
			}
			((Layoutable)this).InvalidateMeasure();
		}
	}

	private void CloseTopNavigationViewFlyout()
	{
		Button topNavOverflowButton = _topNavOverflowButton;
		if (topNavOverflowButton != null)
		{
			FlyoutBase flyout = topNavOverflowButton.Flyout;
			if (flyout != null)
			{
				flyout.Hide();
			}
		}
	}

	private void OnFlyoutClosing(object sender, CancelEventArgs args)
	{
		if (!_moveTopNavOverflowItemOnFlyoutClose || _selectionChangeFromOverflowMenu)
		{
			return;
		}
		_moveTopNavOverflowItemOnFlyoutClose = false;
		IndexPath selectedIndex = _selectionModel.SelectedIndex;
		if (selectedIndex.GetSize() > 0)
		{
			if (GetContainerForIndex(selectedIndex.GetAt(1), inFooter: false) is FANavigationViewItem fANavigationViewItem)
			{
				fANavigationViewItem.IsExpanded = false;
			}
			SelectAndMoveOverflowItem(SelectedItem, selectedIndex, closeFlyout: false);
		}
	}

	private void UpdatePaneDisplayMode()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (_appliedTemplate)
		{
			if (!IsTopNavigationView)
			{
				Rect bounds = ((Visual)this).Bounds;
				UpdateAdaptiveLayout(((Rect)(ref bounds)).Width, forceSetDisplayMode: true);
				SwapPaneHeaderContent(_leftNavPaneHeaderContentBorder, _paneHeaderOnTopPane, (AvaloniaProperty)(object)PaneHeaderProperty);
				SwapPaneHeaderContent(_leftNavPaneCustomContentBorder, _paneCustomContentOnTopPane, (AvaloniaProperty)(object)PaneCustomContentProperty);
				SwapPaneHeaderContent(_leftNavFooterContentBorder, _paneFooterOnTopPane, (AvaloniaProperty)(object)PaneFooterProperty);
				CreateAndHookEventsToSettings();
			}
			else
			{
				ClosePane();
				SetDisplayMode(FANavigationViewDisplayMode.Minimal, forceSetDisplayMode: true);
				SwapPaneHeaderContent(_paneHeaderOnTopPane, _leftNavPaneHeaderContentBorder, (AvaloniaProperty)(object)PaneHeaderProperty);
				SwapPaneHeaderContent(_paneCustomContentOnTopPane, _leftNavPaneCustomContentBorder, (AvaloniaProperty)(object)PaneCustomContentProperty);
				SwapPaneHeaderContent(_paneFooterOnTopPane, _leftNavFooterContentBorder, (AvaloniaProperty)(object)PaneFooterProperty);
				CreateAndHookEventsToSettings();
			}
			UpdateContentBindingsForPaneDisplayMode();
			UpdateRepeaterItemsSource(forceSelectionModelUpdate: false);
			UpdateFooterRepeaterItemsSource(sourceCollectionReset: false, sourceCollectionChanged: false);
			if (SelectedItem != null)
			{
				_orientationChangedPendingAnimation = true;
			}
		}
	}

	private void UpdatePaneDisplayMode(FANavigationViewPaneDisplayMode oldMode, FANavigationViewPaneDisplayMode newMode)
	{
		if (!_appliedTemplate)
		{
			return;
		}
		UpdatePaneDisplayMode();
		if (IsTopNavigationView || newMode != PaneDisplayMode)
		{
			return;
		}
		if (IsPaneOpen)
		{
			if (newMode == FANavigationViewPaneDisplayMode.LeftMinimal)
			{
				ClosePane();
			}
		}
		else if (oldMode == FANavigationViewPaneDisplayMode.LeftMinimal && newMode == FANavigationViewPaneDisplayMode.Left)
		{
			OpenPane();
		}
	}

	private void UpdatePaneVisibility()
	{
		if (IsPaneVisible)
		{
			if (IsTopNavigationView)
			{
				TemplateSettings.LeftPaneVisibility = false;
				TemplateSettings.TopPaneVisibility = true;
			}
			else
			{
				TemplateSettings.LeftPaneVisibility = true;
				TemplateSettings.TopPaneVisibility = false;
			}
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":panecollapsed", false);
		}
		else
		{
			TemplateSettings.LeftPaneVisibility = false;
			TemplateSettings.TopPaneVisibility = false;
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":panecollapsed", true);
		}
	}

	private void SwapPaneHeaderContent(ContentControl newParent, ContentControl oldParent, AvaloniaProperty targetProperty)
	{
		if (newParent != null)
		{
			if (oldParent != null)
			{
				((AvaloniaObject)oldParent).SetValue<object>(ContentControl.ContentProperty, (object)null, (BindingPriority)0);
			}
			((AvaloniaObject)newParent)[!(AvaloniaProperty)(object)ContentControl.ContentProperty] = ((AvaloniaObject)this)[!targetProperty];
		}
	}

	private void UpdateContentBindingsForPaneDisplayMode()
	{
		ContentControl val = null;
		ContentControl val2 = null;
		if (!IsTopNavigationView)
		{
			val = _leftNavAutoSuggestBoxPresenter;
			val2 = _topNavAutoSuggestBoxPresenter;
		}
		else
		{
			val = _topNavAutoSuggestBoxPresenter;
			val2 = _leftNavAutoSuggestBoxPresenter;
		}
		if (val != null)
		{
			if (val2 != null)
			{
				((AvaloniaObject)val2).SetValue<object>(ContentControl.ContentProperty, (object)null, (BindingPriority)0);
			}
			((AvaloniaObject)val)[!(AvaloniaProperty)(object)ContentControl.ContentProperty] = ((AvaloniaObject)this)[!(AvaloniaProperty)(object)AutoCompleteBoxProperty];
		}
	}

	private void UpdateHeaderVisibility()
	{
		if (_appliedTemplate)
		{
			UpdateHeaderVisibility(DisplayMode);
		}
	}

	private void UpdateHeaderVisibility(FANavigationViewDisplayMode dMode)
	{
		bool flag = ((HeaderedContentControl)this).Header != null && (AlwaysShowHeader || (!IsTopNavigationView && dMode == FANavigationViewDisplayMode.Minimal));
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":headercollapsed", !flag);
	}

	private void UpdateTitleBarPadding()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		if (!_appliedTemplate)
		{
			return;
		}
		bool flag = _paneTitleHolderFrameworkElement != null && ((Visual)_paneTitleHolderFrameworkElement).IsVisible;
		bool flag2 = !flag && ((Visual)_paneToggleButton).IsVisible;
		if (!(flag | flag2))
		{
			return;
		}
		Thickness margin = default(Thickness);
		if (ShouldShowBackButton)
		{
			if (IsOverlay)
			{
				((Thickness)(ref margin))._002Ector(40.0, 0.0, 0.0, 0.0);
			}
			else
			{
				((Thickness)(ref margin))._002Ector(0.0, 40.0, 0.0, 0.0);
			}
		}
		else if (ShouldShowCloseButton && IsOverlay)
		{
			((Thickness)(ref margin))._002Ector(40.0, 0.0, 0.0, 0.0);
		}
		if (flag)
		{
			((Layoutable)_paneTitleHolderFrameworkElement).Margin = margin;
		}
		else
		{
			((Layoutable)_paneToggleButton).Margin = margin;
		}
	}

	private void UpdatePaneShadow()
	{
	}

	private T GetContainerForData<T>(object data) where T : Control
	{
		if (data == null)
		{
			return default(T);
		}
		T val = (T)((data is T) ? data : null);
		if (val != null)
		{
			return val;
		}
		FAItemsRepeater fAItemsRepeater = (IsTopNavigationView ? _topNavRepeater : _leftNavRepeater);
		int indexFromItem = GetIndexFromItem(fAItemsRepeater, data);
		if (indexFromItem >= 0)
		{
			Control val2 = fAItemsRepeater.TryGetElement(indexFromItem);
			if (val2 != null)
			{
				T val3 = (T)(object)((val2 is T) ? val2 : null);
				if (val3 == null)
				{
					return default(T);
				}
				return val3;
			}
		}
		FAItemsRepeater fAItemsRepeater2 = (IsTopNavigationView ? _topNavFooterMenuRepeater : _leftNavFooterMenuRepeater);
		indexFromItem = GetIndexFromItem(fAItemsRepeater2, data);
		if (indexFromItem >= 0)
		{
			Control val4 = fAItemsRepeater2.TryGetElement(indexFromItem);
			if (val4 != null)
			{
				T val5 = (T)(object)((val4 is T) ? val4 : null);
				if (val5 == null)
				{
					return default(T);
				}
				return val5;
			}
		}
		Control val6 = SearchEntireTreeForContainer(fAItemsRepeater, data);
		if (val6 != null)
		{
			T val7 = (T)(object)((val6 is T) ? val6 : null);
			if (val7 == null)
			{
				return default(T);
			}
			return val7;
		}
		val6 = SearchEntireTreeForContainer(fAItemsRepeater2, data);
		if (val6 != null)
		{
			T val8 = (T)(object)((val6 is T) ? val6 : null);
			if (val8 == null)
			{
				return default(T);
			}
			return val8;
		}
		return default(T);
	}

	private Control SearchEntireTreeForContainer(FAItemsRepeater ir, object data)
	{
		int indexFromItem = GetIndexFromItem(ir, data);
		if (indexFromItem != -1)
		{
			return ir.TryGetElement(indexFromItem);
		}
		for (int i = 0; i < GetContainerCountInRepeater(ir); i++)
		{
			if (ir.TryGetElement(i) is FANavigationViewItem { GetRepeater: not null } fANavigationViewItem)
			{
				Control val = SearchEntireTreeForContainer(fANavigationViewItem.GetRepeater, data);
				if (val != null)
				{
					return val;
				}
			}
		}
		return null;
	}

	private IndexPath SearchEntireTreeForIndexPath(FAItemsRepeater ir, object data, bool isFooterRepeater)
	{
		for (int i = 0; i < GetContainerCountInRepeater(ir); i++)
		{
			if (ir.TryGetElement(i) is FANavigationViewItem nviParent)
			{
				IndexPath ip = new IndexPath(new int[2]
				{
					isFooterRepeater ? 1 : 0,
					i
				});
				IndexPath indexPath = SearchEntireTreeForIndexPath(nviParent, data, ip);
				if (indexPath != IndexPath.Unselected)
				{
					return indexPath;
				}
			}
		}
		return IndexPath.Unselected;
	}

	private IndexPath SearchEntireTreeForIndexPath(FANavigationViewItem nviParent, object data, IndexPath ip)
	{
		bool flag = false;
		FAItemsRepeater getRepeater = nviParent.GetRepeater;
		if (getRepeater != null && DoesRepeaterHaveRealizedContainers(getRepeater))
		{
			flag = true;
			for (int i = 0; i < GetContainerCountInRepeater(getRepeater); i++)
			{
				if (getRepeater.TryGetElement(i) is FANavigationViewItem fANavigationViewItem)
				{
					IndexPath indexPath = ip.CloneWithChildIndex(i);
					if (((ContentControl)fANavigationViewItem).Content == data)
					{
						return indexPath;
					}
					IndexPath indexPath2 = SearchEntireTreeForIndexPath(fANavigationViewItem, data, indexPath);
					if (indexPath2 != IndexPath.Unselected)
					{
						return indexPath2;
					}
				}
				else
				{
					flag = false;
				}
			}
		}
		if (!flag)
		{
			IEnumerable children = GetChildren(nviParent);
			if (children != null)
			{
				for (int j = 0; j < children.Count(); j++)
				{
					IndexPath result = ip.CloneWithChildIndex(j);
					object obj = children.ElementAt(j);
					if (obj == data)
					{
						return result;
					}
					_ = ResolveContainerForItem(obj, j) is FANavigationViewItem;
				}
			}
		}
		return IndexPath.Unselected;
	}

	private FANavigationViewItemBase ResolveContainerForItem(object item, int index)
	{
		FAElementFactoryGetArgs fAElementFactoryGetArgs = new FAElementFactoryGetArgs();
		fAElementFactoryGetArgs.Data = item;
		fAElementFactoryGetArgs.Index = index;
		if (_itemsFactory.GetElement(fAElementFactoryGetArgs) is FANavigationViewItemBase result)
		{
			return result;
		}
		return null;
	}

	private void RecycleContainer(Control container)
	{
		FAElementFactoryRecycleArgs fAElementFactoryRecycleArgs = new FAElementFactoryRecycleArgs();
		fAElementFactoryRecycleArgs.Element = container;
		_itemsFactory.RecycleElement(fAElementFactoryRecycleArgs);
	}

	private Control GetContainerForIndex(int index, bool inFooter)
	{
		if (IsTopNavigationView)
		{
			FAItemsRepeater obj = (inFooter ? _topNavFooterMenuRepeater : (_topDataProvider.IsItemInPrimaryList(index) ? _topNavRepeater : _topNavRepeaterOverflowView));
			int index2 = (inFooter ? index : _topDataProvider.ConvertOriginalIndexToIndex(index));
			return obj.TryGetElement(index2);
		}
		return (Control)(object)((inFooter ? _leftNavFooterMenuRepeater.TryGetElement(index) : _leftNavRepeater.TryGetElement(index)) as FANavigationViewItemBase);
	}

	private FANavigationViewItemBase GetContainerForIndexPath(IndexPath ip, bool lastVisible = false, bool forceRealize = false)
	{
		if (ip != IndexPath.Unselected && ip.GetSize() > 0)
		{
			Control containerForIndex = GetContainerForIndex(ip.GetAt(1), ip.GetAt(0) == 1);
			if (containerForIndex != null)
			{
				if (lastVisible && containerForIndex is FANavigationViewItem { IsExpanded: false } fANavigationViewItem)
				{
					return fANavigationViewItem;
				}
				return GetContainerForIndexPath(containerForIndex, ip, lastVisible, forceRealize);
			}
		}
		return null;
	}

	private FANavigationViewItemBase GetContainerForIndexPath(Control first, IndexPath ip, bool lastVisible, bool forceRealize)
	{
		Control val = first;
		if (ip.GetSize() > 2)
		{
			for (int i = 2; i < ip.GetSize(); i++)
			{
				bool flag = false;
				if (val is FANavigationViewItem fANavigationViewItem)
				{
					if (lastVisible && !fANavigationViewItem.IsExpanded)
					{
						return fANavigationViewItem;
					}
					FAItemsRepeater getRepeater = fANavigationViewItem.GetRepeater;
					if (getRepeater != null)
					{
						int at = ip.GetAt(i);
						Control val2 = (forceRealize ? getRepeater.GetOrCreateElement(at) : getRepeater.TryGetElement(at));
						if (val2 != null)
						{
							val = val2;
							flag = true;
						}
					}
				}
				if (!flag)
				{
					return null;
				}
			}
		}
		return val as FANavigationViewItemBase;
	}

	private IEnumerable GetChildrenForItemInIndexPath(IndexPath ip, bool forceRealize)
	{
		if (ip != IndexPath.Unselected && ip.GetSize() > 1)
		{
			Control containerForIndex = GetContainerForIndex(ip.GetAt(1), ip.GetAt(0) == 1);
			if (containerForIndex != null)
			{
				return GetChildrenForItemInIndexPath(containerForIndex, ip, forceRealize);
			}
		}
		return null;
	}

	private IEnumerable GetChildrenForItemInIndexPath(Control first, IndexPath ip, bool forceRealize)
	{
		Control val = first;
		bool flag = false;
		if (ip.GetSize() > 2)
		{
			for (int i = 2; i < ip.GetSize(); i++)
			{
				bool flag2 = false;
				if (val is FANavigationViewItem fANavigationViewItem)
				{
					int at = ip.GetAt(i);
					FAItemsRepeater getRepeater = fANavigationViewItem.GetRepeater;
					if (getRepeater != null && DoesRepeaterHaveRealizedContainers(getRepeater))
					{
						Control val2 = getRepeater.TryGetElement(at);
						if (val2 != null)
						{
							val = val2;
							flag2 = true;
						}
					}
					else if (forceRealize)
					{
						IEnumerable children = GetChildren(fANavigationViewItem);
						if (children != null)
						{
							if (flag)
							{
								RecycleContainer((Control)(object)fANavigationViewItem);
								flag = false;
							}
							object obj = children.ElementAt(at);
							if (obj != null && ResolveContainerForItem(obj, at) is FANavigationViewItem fANavigationViewItem2)
							{
								val = (Control)(object)fANavigationViewItem2;
								flag = true;
								flag2 = true;
							}
						}
					}
				}
				if (!flag2)
				{
					return null;
				}
			}
		}
		if (val is FANavigationViewItem fANavigationViewItem3)
		{
			IEnumerable children2 = GetChildren(fANavigationViewItem3);
			if (flag)
			{
				RecycleContainer((Control)(object)fANavigationViewItem3);
			}
			return children2;
		}
		return null;
	}

	private void UpdateOpenPaneWidth(double width)
	{
		if (!IsTopNavigationView && _splitView != null)
		{
			_openPaneWidth = Math.Max(0.0, Math.Min(width, OpenPaneLength));
			TemplateSettings.OpenPaneWidth = _openPaneWidth;
		}
	}

	private void SetPaneToggleButtonAutomationName()
	{
		string text = ((!IsPaneOpen) ? FALocalizationHelper.Instance.GetLocalizedStringResource("NavigationButtonClosedName") : FALocalizationHelper.Instance.GetLocalizedStringResource("NavigationButtonOpenName"));
		if (_paneToggleButton != null)
		{
			ToolTip.SetTip((Control)(object)_paneToggleButton, (object)text);
		}
	}

	private bool VerifyInPane(Visual focus, Visual parent)
	{
		if (parent == null)
		{
			return false;
		}
		if (_backButton != null && (object)focus == _backButton)
		{
			return true;
		}
		if (_closeButton != null && (object)focus == _closeButton)
		{
			return true;
		}
		if (_paneToggleButton != null && (object)focus == _paneToggleButton)
		{
			return true;
		}
		while (focus != null)
		{
			if (focus == parent)
			{
				return true;
			}
			focus = VisualExtensions.GetVisualParent(focus);
		}
		return false;
	}

	private Control SearchTreeForLowestFocusItem(FANavigationViewItem start)
	{
		if (DoesNavigationViewItemHaveChildren(start) && start.IsExpanded)
		{
			for (int num = start.GetRepeater.ItemsSourceView.Count - 1; num >= 0; num--)
			{
				if (start.GetRepeater.TryGetElement(num) is FANavigationViewItem start2)
				{
					return SearchTreeForLowestFocusItem(start2);
				}
			}
		}
		return (Control)(object)start;
	}

	private double GetPaneToggleButtonWidth()
	{
		object obj = default(object);
		if (!ResourceNodeExtensions.TryFindResource((IResourceHost)(object)this, (object)"PaneToggleButtonWidth", ref obj))
		{
			return 40.0;
		}
		return (double)obj;
	}

	private double GetPaneToggleButtonHeight()
	{
		object obj = default(object);
		if (!ResourceNodeExtensions.TryFindResource((IResourceHost)(object)this, (object)"PaneToggleButtonHeight", ref obj))
		{
			return 40.0;
		}
		return (double)obj;
	}

	private bool IsTopLevelItem(FANavigationViewItemBase nvib)
	{
		return IsRootItemsRepeater(GetParentItemsRepeaterForContainer(nvib));
	}

	private bool DoesNavigationViewItemHaveChildren(FANavigationViewItem nvi)
	{
		IEnumerable enumerable = nvi?.MenuItemsSource;
		if (enumerable != null)
		{
			return enumerable.Count() > 0;
		}
		if (nvi != null)
		{
			if (nvi.MenuItems == null || nvi.MenuItems.Count() <= 0)
			{
				return nvi.HasUnrealizedChildren;
			}
			return true;
		}
		return false;
	}

	private bool IsSelectionSuppressed(object item)
	{
		if (item != null)
		{
			FANavigationViewItem fANavigationViewItem = NavigationViewItemOrSettingsContentFromData(item);
			if (fANavigationViewItem == null)
			{
				return false;
			}
			return !fANavigationViewItem.SelectsOnInvoked;
		}
		return false;
	}

	private bool IsRootItemsRepeater(object ir)
	{
		if (ir != null)
		{
			if (ir != _topNavRepeater && ir != _leftNavRepeater && ir != _topNavRepeaterOverflowView && ir != _leftNavFooterMenuRepeater)
			{
				return ir == _topNavFooterMenuRepeater;
			}
			return true;
		}
		return false;
	}

	private bool IsRootGridOfFlyout(object item)
	{
		Panel val = (Panel)((item is Panel) ? item : null);
		if (val != null)
		{
			return ((StyledElement)val).Name == "FlyoutRootGrid";
		}
		return false;
	}

	private FAItemsRepeater GetParentRootItemsRepeaterForContainer(FANavigationViewItemBase nvib)
	{
		FAItemsRepeater parentItemsRepeaterForContainer = GetParentItemsRepeaterForContainer(nvib);
		while (!IsRootItemsRepeater(parentItemsRepeaterForContainer))
		{
			nvib = GetParentNavigationViewItemForContainer(nvib);
			if (nvib == null)
			{
				return null;
			}
			parentItemsRepeaterForContainer = GetParentItemsRepeaterForContainer(nvib);
		}
		return parentItemsRepeaterForContainer;
	}

	private FAItemsRepeater GetParentItemsRepeaterForContainer(FANavigationViewItemBase nvib)
	{
		if (nvib == null)
		{
			return null;
		}
		return VisualExtensions.FindAncestorOfType<FAItemsRepeater>((Visual)(object)nvib, false);
	}

	private FANavigationViewItem GetParentNavigationViewItemForContainer(FANavigationViewItemBase nvib)
	{
		FAItemsRepeater parentItemsRepeaterForContainer = GetParentItemsRepeaterForContainer(nvib);
		if (!IsRootItemsRepeater(parentItemsRepeaterForContainer))
		{
			return VisualExtensions.FindAncestorOfType<FANavigationViewItem>((Visual)(object)parentItemsRepeaterForContainer, false);
		}
		return null;
	}

	private IndexPath GetIndexPathForContainer(FANavigationViewItemBase nvib)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		List<int> list = new List<int>(4);
		bool flag = false;
		Control element = (Control)(object)nvib;
		Visual val = VisualExtensions.GetVisualParent((Visual)(object)nvib);
		if (val == null)
		{
			return IndexPath.CreateFromIndices(list);
		}
		while (val != null && !IsRootItemsRepeater(val) && !IsRootGridOfFlyout(val))
		{
			if (val is FAItemsRepeater fAItemsRepeater)
			{
				list.Insert(0, fAItemsRepeater.GetElementIndex(element));
			}
			element = (Control)val;
			val = VisualExtensions.GetVisualParent(val);
		}
		if (IsRootGridOfFlyout(val) && _lastItemExpandedIntoFlyout != null)
		{
			element = (Control)(object)_lastItemExpandedIntoFlyout;
			val = (Visual)(object)(IsTopNavigationView ? _topNavRepeater : _leftNavRepeater);
		}
		if ((object)val == _topNavRepeaterOverflowView)
		{
			int elementIndex = _topNavRepeaterOverflowView.GetElementIndex(element);
			object value = _topDataProvider.GetOverflowItems()[elementIndex];
			int item = _topDataProvider.IndexOf(value);
			list.Insert(0, item);
		}
		else if ((object)val == _topNavRepeater)
		{
			int elementIndex2 = _topNavRepeater.GetElementIndex(element);
			object value2 = _topDataProvider.GetPrimaryItems()[elementIndex2];
			int item2 = _topDataProvider.IndexOf(value2);
			list.Insert(0, item2);
		}
		else if (val is FAItemsRepeater fAItemsRepeater2)
		{
			list.Insert(0, fAItemsRepeater2.GetElementIndex(element));
		}
		flag = (object)val == _leftNavFooterMenuRepeater || (object)val == _topNavFooterMenuRepeater;
		list.Insert(0, flag ? 1 : 0);
		return IndexPath.CreateFromIndices(list);
	}

	private FANavigationViewItemBase NavigationViewItemBaseOrSettingsContentFromData(object data)
	{
		return GetContainerForData<FANavigationViewItemBase>(data);
	}

	private FANavigationViewItem NavigationViewItemOrSettingsContentFromData(object data)
	{
		return GetContainerForData<FANavigationViewItem>(data);
	}

	internal object MenuItemFromContainer(object container)
	{
		if (container is FANavigationViewItemBase fANavigationViewItemBase)
		{
			FAItemsRepeater parentItemsRepeaterForContainer = GetParentItemsRepeaterForContainer(fANavigationViewItemBase);
			if (parentItemsRepeaterForContainer != null)
			{
				int elementIndex = parentItemsRepeaterForContainer.GetElementIndex((Control)(object)fANavigationViewItemBase);
				if (elementIndex >= 0)
				{
					return GetItemFromIndex(parentItemsRepeaterForContainer, elementIndex);
				}
			}
		}
		return null;
	}

	private Control ContainerFromMenuItem(object item)
	{
		return (Control)(object)NavigationViewItemBaseOrSettingsContentFromData(item);
	}

	private bool IsSettingsItem(object item)
	{
		if (item != null && _settingsItem != null)
		{
			if (item != _settingsItem)
			{
				return ((ContentControl)_settingsItem).Content == item;
			}
			return true;
		}
		return false;
	}

	private double MeasureTopNavigationViewDesiredWidth(Size availableSize)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		Size val = LayoutHelper.MeasureChild((Layoutable)(object)_topNavGrid, availableSize, default(Thickness));
		return ((Size)(ref val)).Width;
	}

	private double MeasureTopNavMenuItemsHostDesiredWidth(Size availableSize)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		Size val = LayoutHelper.MeasureChild((Layoutable)(object)_topNavRepeater, availableSize, default(Thickness));
		return ((Size)(ref val)).Width;
	}

	private bool HasTopNavigationViewItemNotInPrimaryList()
	{
		return _topDataProvider.PrimaryListSize != _topDataProvider.Size;
	}

	private void SetOverflowButtonVisibility(bool vis)
	{
		TemplateSettings.OverflowButtonVisibility = vis;
	}

	private bool NeedTopPadding()
	{
		return false;
	}

	private int GetContainerCountInRepeater(FAItemsRepeater ir)
	{
		if (ir != null && ir.ItemsSourceView != null)
		{
			return ir.ItemsSourceView.Count;
		}
		return -1;
	}

	private bool DoesRepeaterHaveRealizedContainers(FAItemsRepeater ir)
	{
		if (ir != null)
		{
			return ir.TryGetElement(0) != null;
		}
		return false;
	}

	private int GetIndexFromItem(FAItemsRepeater ir, object data)
	{
		if (ir != null && ir.ItemsSourceView != null)
		{
			return ir.ItemsSourceView.IndexOf(data);
		}
		return -1;
	}

	private object GetItemFromIndex(FAItemsRepeater ir, int index)
	{
		if (ir != null && ir.ItemsSourceView != null)
		{
			return ir.ItemsSourceView.GetAt(index);
		}
		return null;
	}

	private IndexPath GetIndexPathOfItem(object item)
	{
		if (item is FANavigationViewItemBase nvib)
		{
			return GetIndexPathForContainer(nvib);
		}
		if (IsTopNavigationView)
		{
			IndexPath indexPath = SearchEntireTreeForIndexPath(_topNavRepeater, item, isFooterRepeater: false);
			if (indexPath != IndexPath.Unselected)
			{
				return indexPath;
			}
			indexPath = SearchEntireTreeForIndexPath(_topNavRepeaterOverflowView, item, isFooterRepeater: false);
			if (indexPath != IndexPath.Unselected)
			{
				return indexPath;
			}
			indexPath = SearchEntireTreeForIndexPath(_topNavFooterMenuRepeater, item, isFooterRepeater: true);
			if (indexPath != IndexPath.Unselected)
			{
				return indexPath;
			}
		}
		else
		{
			IndexPath indexPath2 = SearchEntireTreeForIndexPath(_leftNavFooterMenuRepeater, item, isFooterRepeater: true);
			if (indexPath2 != IndexPath.Unselected)
			{
				return indexPath2;
			}
			indexPath2 = SearchEntireTreeForIndexPath(_leftNavFooterMenuRepeater, item, isFooterRepeater: true);
			if (indexPath2 != IndexPath.Unselected)
			{
				return indexPath2;
			}
		}
		return IndexPath.Unselected;
	}

	private bool IsContainerTheSelectedItemInTheSelectionModel(FANavigationViewItemBase nvib)
	{
		object selectedItem = _selectionModel.SelectedItem;
		if (selectedItem == null)
		{
			return false;
		}
		FANavigationViewItemBase fANavigationViewItemBase = selectedItem as FANavigationViewItemBase;
		if (fANavigationViewItemBase == null)
		{
			fANavigationViewItemBase = GetContainerForIndexPath(_selectionModel.SelectedIndex);
		}
		return fANavigationViewItemBase == nvib;
	}

	internal FANavigationViewItem GetSelectedContainer()
	{
		if (SelectedItem == null)
		{
			return null;
		}
		if (SelectedItem is FANavigationViewItem result)
		{
			return result;
		}
		return NavigationViewItemOrSettingsContentFromData(SelectedItem);
	}

	private IEnumerable GetChildren(FANavigationViewItem nvi)
	{
		if (nvi.MenuItems.Count <= 0)
		{
			return nvi.MenuItemsSource;
		}
		return nvi.MenuItems;
	}

	private FAItemsRepeater GetChildRepeaterForIndexPath(IndexPath ip)
	{
		if (GetContainerForIndexPath(ip) is FANavigationViewItem fANavigationViewItem)
		{
			return fANavigationViewItem.GetRepeater;
		}
		return null;
	}

	private NavigationRecommendedTransitionDirection GetRecommendedTransitionDirection(Control prev, Control next)
	{
		NavigationRecommendedTransitionDirection result = NavigationRecommendedTransitionDirection.Default;
		FAItemsRepeater topNavRepeater = _topNavRepeater;
		if (prev != null && next != null && topNavRepeater != null)
		{
			IndexPath indexPathForContainer = GetIndexPathForContainer(prev as FANavigationViewItemBase);
			IndexPath indexPathForContainer2 = GetIndexPathForContainer(next as FANavigationViewItemBase);
			result = indexPathForContainer.CompareTo(indexPathForContainer2) switch
			{
				-1 => NavigationRecommendedTransitionDirection.FromRight, 
				1 => NavigationRecommendedTransitionDirection.FromLeft, 
				_ => NavigationRecommendedTransitionDirection.Default, 
			};
		}
		return result;
	}

	private FANavigationTransitionInfo CreateNavigationTransitionInfo(NavigationRecommendedTransitionDirection recDir)
	{
		if (recDir == NavigationRecommendedTransitionDirection.FromOverflow)
		{
			recDir = NavigationRecommendedTransitionDirection.FromRight;
		}
		if (recDir == NavigationRecommendedTransitionDirection.FromLeft || recDir == NavigationRecommendedTransitionDirection.FromRight)
		{
			return new FASlideNavigationTransitionInfo
			{
				Effect = ((recDir == NavigationRecommendedTransitionDirection.FromRight) ? FASlideNavigationTransitionEffect.FromRight : FASlideNavigationTransitionEffect.FromLeft)
			};
		}
		return new FAEntranceNavigationTransitionInfo();
	}

	private void UnhookEventsAndClearFields()
	{
		if (_paneToggleButton != null)
		{
			_paneToggleButton.Click -= OnPaneToggleButtonClick;
			_paneToggleButton = null;
		}
		if (_splitView != null)
		{
			_splitViewRevokers?.Dispose();
			_splitView.PaneClosed -= OnSplitViewPaneClosed;
			_splitView.PaneClosing -= OnSplitViewPaneClosing;
			_splitView.PaneOpened -= OnSplitViewPaneOpened;
			_splitView.PaneOpening -= OnSplitViewPaneOpening;
			_splitView = null;
		}
		if (_leftNavRepeater != null)
		{
			_leftNavRepeater.ElementClearing -= OnRepeaterElementClearing;
			_leftNavRepeater.ElementPrepared -= OnRepeaterElementPrepared;
			((Control)_leftNavRepeater).Loaded -= OnRepeaterLoaded;
			((InputElement)_leftNavRepeater).GettingFocus -= OnRepeaterGettingFocus;
			_leftNavRepeater = null;
		}
		if (_topNavRepeater != null)
		{
			_topNavRepeater.ElementClearing -= OnRepeaterElementClearing;
			_topNavRepeater.ElementPrepared -= OnRepeaterElementPrepared;
			((Control)_topNavRepeater).Loaded -= OnRepeaterLoaded;
			((InputElement)_topNavRepeater).GettingFocus -= OnRepeaterGettingFocus;
			_topNavRepeater = null;
		}
		if (_topNavRepeaterOverflowView != null)
		{
			_topNavRepeaterOverflowView.ElementClearing -= OnRepeaterElementClearing;
			_topNavRepeaterOverflowView.ElementPrepared -= OnRepeaterElementPrepared;
			_topNavRepeaterOverflowView = null;
		}
		if (_topNavOverflowButton != null)
		{
			FlyoutBase flyout = _topNavOverflowButton.Flyout;
			FlyoutBase obj = ((flyout is PopupFlyoutBase) ? flyout : null);
			if (obj != null)
			{
				((PopupFlyoutBase)obj).Closing -= OnFlyoutClosing;
			}
		}
		if (_leftNavFooterMenuRepeater != null)
		{
			_leftNavFooterMenuRepeater.ElementClearing -= OnRepeaterElementClearing;
			_leftNavFooterMenuRepeater.ElementPrepared -= OnRepeaterElementPrepared;
			((Control)_leftNavFooterMenuRepeater).Loaded -= OnRepeaterLoaded;
			((InputElement)_leftNavFooterMenuRepeater).GettingFocus -= OnRepeaterGettingFocus;
			_leftNavFooterMenuRepeater = null;
		}
		if (_topNavFooterMenuRepeater != null)
		{
			_topNavFooterMenuRepeater.ElementClearing -= OnRepeaterElementClearing;
			_topNavFooterMenuRepeater.ElementPrepared -= OnRepeaterElementPrepared;
			((Control)_topNavFooterMenuRepeater).Loaded -= OnRepeaterLoaded;
			((InputElement)_topNavFooterMenuRepeater).GettingFocus -= OnRepeaterGettingFocus;
			_topNavFooterMenuRepeater = null;
		}
		_paneTitleHolderRevoker?.Dispose();
		_paneTitleHolderRevoker = null;
		Button paneSearchButton = _paneSearchButton;
		if (paneSearchButton != null)
		{
			paneSearchButton.Click -= OnPaneSearchButtonClick;
		}
		Button backButton = _backButton;
		if (backButton != null)
		{
			backButton.Click -= OnBackButtonClicked;
		}
		Button closeButton = _closeButton;
		if (closeButton != null)
		{
			closeButton.Click -= OnPaneToggleButtonClick;
		}
		_itemsContainerSizeRevoker?.Dispose();
		_itemsContainerSizeRevoker = null;
		_itemsContainerSizeRevoker?.Dispose();
	}

	/// <summary>
	/// Coerces a double to ensure its valid for use, i.e. &gt;= 0 and not NaN or infinity
	/// Used for: CompactModeThresholdWidthProperty, CompactPaneLengthProperty, 
	/// ExpandedModeThresholdWidthProperty, and OpenPaneLengthProperty
	/// </summary>
	/// <returns></returns>
	private static double CoercePropertyValueToGreaterThanZero(AvaloniaObject arg1, double arg2)
	{
		if (double.IsNaN(arg2) || double.IsInfinity(arg2))
		{
			return 0.0;
		}
		return Math.Max(arg2, 0.0);
	}

	static FANavigationView()
	{
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		_settingsIconSource = new FASymbolIconSource
		{
			Symbol = FASymbol.Settings
		};
		AlwaysShowHeaderProperty = AvaloniaProperty.Register<FANavigationView, bool>("AlwaysShowHeader", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		AutoCompleteBoxProperty = AvaloniaProperty.Register<FANavigationView, AutoCompleteBox>("AutoCompleteBox", (AutoCompleteBox)null, false, (BindingMode)1, (Func<AutoCompleteBox, bool>)null, (Func<AvaloniaObject, AutoCompleteBox, AutoCompleteBox>)null, false);
		CompactModeThresholdWidthProperty = AvaloniaProperty.Register<FANavigationView, double>("CompactModeThresholdWidth", 641.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)CoercePropertyValueToGreaterThanZero, false);
		CompactPaneLengthProperty = AvaloniaProperty.Register<FANavigationView, double>("CompactPaneLength", 48.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)CoercePropertyValueToGreaterThanZero, false);
		ContentOverlayProperty = AvaloniaProperty.Register<FANavigationView, Control>("ContentOverlay", (Control)null, false, (BindingMode)1, (Func<Control, bool>)null, (Func<AvaloniaObject, Control, Control>)null, false);
		DisplayModeProperty = AvaloniaProperty.RegisterDirect<FANavigationView, FANavigationViewDisplayMode>("DisplayMode", (Func<FANavigationView, FANavigationViewDisplayMode>)((FANavigationView x) => x.DisplayMode), (Action<FANavigationView, FANavigationViewDisplayMode>)null, FANavigationViewDisplayMode.Minimal, (BindingMode)1, false);
		ExpandedModeThresholdWidthProperty = AvaloniaProperty.Register<FANavigationView, double>("ExpandedModeThresholdWidth", 1008.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)CoercePropertyValueToGreaterThanZero, false);
		FooterMenuItemsProperty = AvaloniaProperty.RegisterDirect<FANavigationView, IList<object>>("FooterMenuItems", (Func<FANavigationView, IList<object>>)((FANavigationView x) => x.FooterMenuItems), (Action<FANavigationView, IList<object>>)null, (IList<object>)null, (BindingMode)1, false);
		FooterMenuItemsSourceProperty = AvaloniaProperty.Register<FANavigationView, IEnumerable>("FooterMenuItemsSource", (IEnumerable)null, false, (BindingMode)1, (Func<IEnumerable, bool>)null, (Func<AvaloniaObject, IEnumerable, IEnumerable>)null, false);
		IsBackButtonVisibleProperty = AvaloniaProperty.Register<FANavigationView, bool>("IsBackButtonVisible", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		IsBackEnabledProperty = AvaloniaProperty.Register<FANavigationView, bool>("IsBackEnabled", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		IsPaneOpenProperty = SplitView.IsPaneOpenProperty.AddOwner<FANavigationView>(new StyledPropertyMetadata<bool>(Optional<bool>.op_Implicit(true), (BindingMode)0, (Func<AvaloniaObject, bool, bool>)null, false));
		IsPaneToggleButtonVisibleProperty = AvaloniaProperty.Register<FANavigationView, bool>("IsPaneToggleButtonVisible", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		IsPaneVisibleProperty = AvaloniaProperty.Register<FANavigationView, bool>("IsPaneVisible", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		IsSettingsVisibleProperty = AvaloniaProperty.Register<FANavigationView, bool>("IsSettingsVisible", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		MenuItemsProperty = AvaloniaProperty.RegisterDirect<FANavigationView, IList<object>>("MenuItems", (Func<FANavigationView, IList<object>>)((FANavigationView o) => o.MenuItems), (Action<FANavigationView, IList<object>>)null, (IList<object>)null, (BindingMode)1, false);
		MenuItemsSourceProperty = AvaloniaProperty.Register<FANavigationView, IEnumerable>("MenuItemsSource", (IEnumerable)null, false, (BindingMode)1, (Func<IEnumerable, bool>)null, (Func<AvaloniaObject, IEnumerable, IEnumerable>)null, false);
		MenuItemTemplateProperty = AvaloniaProperty.Register<FANavigationView, IDataTemplate>("MenuItemTemplate", (IDataTemplate)null, false, (BindingMode)1, (Func<IDataTemplate, bool>)null, (Func<AvaloniaObject, IDataTemplate, IDataTemplate>)null, false);
		MenuItemTemplateSelectorProperty = AvaloniaProperty.Register<FANavigationView, FADataTemplateSelector>("MenuItemTemplateSelector", (FADataTemplateSelector)null, false, (BindingMode)1, (Func<FADataTemplateSelector, bool>)null, (Func<AvaloniaObject, FADataTemplateSelector, FADataTemplateSelector>)null, false);
		MenuItemContainerThemeProperty = AvaloniaProperty.Register<FANavigationView, ControlTheme>("MenuItemContainerTheme", (ControlTheme)null, false, (BindingMode)1, (Func<ControlTheme, bool>)null, (Func<AvaloniaObject, ControlTheme, ControlTheme>)null, false);
		OpenPaneLengthProperty = AvaloniaProperty.Register<FANavigationView, double>("OpenPaneLength", 320.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)CoercePropertyValueToGreaterThanZero, false);
		PaneCustomContentProperty = AvaloniaProperty.Register<FANavigationView, Control>("PaneCustomContent", (Control)null, false, (BindingMode)1, (Func<Control, bool>)null, (Func<AvaloniaObject, Control, Control>)null, false);
		PaneDisplayModeProperty = AvaloniaProperty.Register<FANavigationView, FANavigationViewPaneDisplayMode>("PaneDisplayMode", FANavigationViewPaneDisplayMode.Auto, false, (BindingMode)1, (Func<FANavigationViewPaneDisplayMode, bool>)null, (Func<AvaloniaObject, FANavigationViewPaneDisplayMode, FANavigationViewPaneDisplayMode>)null, false);
		PaneFooterProperty = AvaloniaProperty.Register<FANavigationView, Control>("PaneFooter", (Control)null, false, (BindingMode)1, (Func<Control, bool>)null, (Func<AvaloniaObject, Control, Control>)null, false);
		PaneHeaderProperty = AvaloniaProperty.Register<FANavigationView, Control>("PaneHeader", (Control)null, false, (BindingMode)1, (Func<Control, bool>)null, (Func<AvaloniaObject, Control, Control>)null, false);
		PaneTitleProperty = AvaloniaProperty.Register<FANavigationView, string>("PaneTitle", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);
		SelectedItemProperty = SelectingItemsControl.SelectedItemProperty.AddOwner<FANavigationView>((Func<FANavigationView, object>)((FANavigationView x) => x.SelectedItem), (Action<FANavigationView, object>)delegate(FANavigationView x, object? v)
		{
			x.SelectedItem = v;
		}, (object)null, (BindingMode)2, false);
		SelectionFollowsFocusProperty = AvaloniaProperty.Register<FANavigationView, bool>("SelectionFollowsFocus", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		SettingsItemProperty = AvaloniaProperty.RegisterDirect<FANavigationView, FANavigationViewItem>("SettingsItem", (Func<FANavigationView, FANavigationViewItem>)((FANavigationView x) => x.SettingsItem), (Action<FANavigationView, FANavigationViewItem>)null, (FANavigationViewItem)null, (BindingMode)1, false);
		TemplateSettingsProperty = AvaloniaProperty.Register<FANavigationView, FANavigationViewTemplateSettings>("TemplateSettings", (FANavigationViewTemplateSettings)null, false, (BindingMode)1, (Func<FANavigationViewTemplateSettings, bool>)null, (Func<AvaloniaObject, FANavigationViewTemplateSettings, FANavigationViewTemplateSettings>)null, false);
		NavigationViewItemBaseRevokersProperty = AvaloniaProperty.RegisterAttached<FANavigationView, FANavigationViewItemBase, FACompositeDisposable>("NavigationViewItemBaseRevokers", (FACompositeDisposable)null, false, (BindingMode)1, (Func<FACompositeDisposable, bool>)null, (Func<AvaloniaObject, FACompositeDisposable, FACompositeDisposable>)null);
	}
}
