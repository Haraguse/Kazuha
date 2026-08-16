using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using FluentAvalonia.Collections;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls.Primitives;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// A control used to display a set of tabs and their respective content
/// </summary>
[PseudoClasses(new string[] { ":noborder", ":borderLeft", ":borderRight", ":singleBorder" })]
[PseudoClasses(new string[] { ":top", ":left", ":bottom", ":right" })]
[TemplatePart("TabContentPresenter", typeof(ContentPresenter))]
[TemplatePart("RightContentPresenter", typeof(ContentPresenter))]
[TemplatePart("TabContainerGrid", typeof(Grid))]
[TemplatePart("TabListView", typeof(FATabViewListView))]
[TemplatePart("AddButton", typeof(Button))]
public class FATabView : TemplatedControl
{
	private class TabViewCommand : ICommand
	{
		public Action<object> ExecuteHandler { get; }

		event EventHandler ICommand.CanExecuteChanged
		{
			add
			{
			}
			remove
			{
			}
		}

		public TabViewCommand(Action<object> execute)
		{
			ExecuteHandler = execute;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			ExecuteHandler(parameter);
		}
	}

	private enum TabViewCommandType
	{
		CtrlF4,
		CtrlTab,
		CtrlShftTab
	}

	private TabViewCommand _keyboardAcceleratorHandler;

	private bool _updateTabWidthOnPointerLeave;

	private bool _pointerInTabstrip;

	private ColumnDefinition _leftContentColumn;

	private ColumnDefinition _tabColumn;

	private ColumnDefinition _addButtonColumn;

	private ColumnDefinition _rightContentColumn;

	private FATabViewListView _listView;

	private ContentPresenter _tabContentPresenter;

	private ContentPresenter _rightContentPresenter;

	private Grid _tabContainerGrid;

	private ScrollViewer _scrollViewer;

	private RepeatButton _scrollDecreaseButton;

	private RepeatButton _scrollIncreaseButton;

	private Button _addButton;

	private ItemsPresenter _itemsPresenter;

	private Border _verticalPaneResizeHandle;

	private bool _isDraggingPane;

	private Point? _initDragPanePoint;

	private double _startingPaneSize;

	private bool _isSwitchingTabLocation;

	private IDisposable _listViewCanReorderItemsPropertyChangedRevoker;

	private IDisposable _listViewAllowDropPropertyChangedRevoker;

	private string _tabCloseButtonTooltipText;

	private Size _previousAvailableSize;

	private bool _isDragging;

	private bool _isItemDraggedOver;

	private double? _expandedWidthForDragOver;

	private static double c_tabMinimumWidth = 48.0;

	private static double c_tabMaximumWidth = 200.0;

	private static double c_scrollAmount = 50.0;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.TabWidthMode" /> property
	/// </summary>
	public static readonly StyledProperty<FATabViewWidthMode> TabWidthModeProperty = AvaloniaProperty.Register<FATabView, FATabViewWidthMode>("TabWidthMode", FATabViewWidthMode.Equal, false, (BindingMode)1, (Func<FATabViewWidthMode, bool>)null, (Func<AvaloniaObject, FATabViewWidthMode, FATabViewWidthMode>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.CloseButtonOverlayMode" /> property
	/// </summary>
	public static readonly StyledProperty<FATabViewCloseButtonOverlayMode> CloseButtonOverlayModeProperty = AvaloniaProperty.Register<FATabView, FATabViewCloseButtonOverlayMode>("CloseButtonOverlayMode", FATabViewCloseButtonOverlayMode.Auto, false, (BindingMode)1, (Func<FATabViewCloseButtonOverlayMode, bool>)null, (Func<AvaloniaObject, FATabViewCloseButtonOverlayMode, FATabViewCloseButtonOverlayMode>)null, false);

	/// <summary>
	/// Definse the <see cref="P:FluentAvalonia.UI.Controls.FATabView.TabStripHeader" /> property
	/// </summary>
	public static readonly StyledProperty<object> TabStripHeaderProperty = AvaloniaProperty.Register<FATabView, object>("TabStripHeader", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);

	/// <summary>
	/// Define the <see cref="P:FluentAvalonia.UI.Controls.FATabView.TabStripHeaderTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> TabStripHeaderTemplateProperty = AvaloniaProperty.Register<FATabView, IDataTemplate>("TabStripHeaderTemplate", (IDataTemplate)null, false, (BindingMode)1, (Func<IDataTemplate, bool>)null, (Func<AvaloniaObject, IDataTemplate, IDataTemplate>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.TabStripFooter" /> property
	/// </summary>
	public static readonly StyledProperty<object> TabStripFooterProperty = AvaloniaProperty.Register<FATabView, object>("TabStripFooter", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.TabStripFooterTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> TabStripFooterTemplateProperty = AvaloniaProperty.Register<FATabView, IDataTemplate>("TabStripFooterTemplate", (IDataTemplate)null, false, (BindingMode)1, (Func<IDataTemplate, bool>)null, (Func<AvaloniaObject, IDataTemplate, IDataTemplate>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.IsAddTabButtonVisible" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsAddTabButtonVisibleProperty = AvaloniaProperty.Register<FATabView, bool>("IsAddTabButtonVisible", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.AddTabButtonCommand" /> property
	/// </summary>
	public static readonly StyledProperty<ICommand> AddTabButtonCommandProperty = AvaloniaProperty.Register<FATabView, ICommand>("AddTabButtonCommand", (ICommand)null, false, (BindingMode)1, (Func<ICommand, bool>)null, (Func<AvaloniaObject, ICommand, ICommand>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.AddTabButtonCommandParameter" /> property
	/// </summary>
	public static readonly StyledProperty<object> AddTabButtonCommandParameterProperty = AvaloniaProperty.Register<FATabView, object>("AddTabButtonCommandParameter", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.TabItems" /> property
	/// </summary>
	public static readonly DirectProperty<FATabView, IList> TabItemsProperty = AvaloniaProperty.RegisterDirect<FATabView, IList>("TabItems", (Func<FATabView, IList>)((FATabView x) => x.TabItems), (Action<FATabView, IList>)null, (IList)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.TabItemsSource" /> property
	/// </summary>
	public static readonly StyledProperty<IEnumerable> TabItemsSourceProperty = AvaloniaProperty.Register<FATabView, IEnumerable>("TabItemsSource", (IEnumerable)null, false, (BindingMode)1, (Func<IEnumerable, bool>)null, (Func<AvaloniaObject, IEnumerable, IEnumerable>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.TabItemTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> TabItemTemplateProperty = AvaloniaProperty.Register<FATabView, IDataTemplate>("TabItemTemplate", (IDataTemplate)null, false, (BindingMode)1, (Func<IDataTemplate, bool>)null, (Func<AvaloniaObject, IDataTemplate, IDataTemplate>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.CanDragTabs" /> property
	/// </summary>
	public static readonly StyledProperty<bool> CanDragTabsProperty = AvaloniaProperty.Register<FATabView, bool>("CanDragTabs", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.CanReorderTabs" /> property
	/// </summary>
	public static readonly StyledProperty<bool> CanReorderTabsProperty = AvaloniaProperty.Register<FATabView, bool>("CanReorderTabs", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.AllowDropTabs" /> property
	/// </summary>
	public static readonly StyledProperty<bool> AllowDropTabsProperty = AvaloniaProperty.Register<FATabView, bool>("AllowDropTabs", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.SelectedIndex" /> property
	/// </summary>
	public static readonly DirectProperty<FATabView, int> SelectedIndexProperty = SelectingItemsControl.SelectedIndexProperty.AddOwner<FATabView>((Func<FATabView, int>)((FATabView x) => x.SelectedIndex), (Action<FATabView, int>)delegate(FATabView x, int v)
	{
		x.SelectedIndex = v;
	}, 0, (BindingMode)0, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.SelectedItem" /> property
	/// </summary>
	public static readonly DirectProperty<FATabView, object> SelectedItemProperty = SelectingItemsControl.SelectedItemProperty.AddOwner<FATabView>((Func<FATabView, object>)((FATabView x) => x.SelectedItem), (Action<FATabView, object>)delegate(FATabView x, object? v)
	{
		x.SelectedItem = v;
	}, (object)null, (BindingMode)0, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.TabStripLocation" /> property
	/// </summary>
	public static readonly StyledProperty<FATabViewTabStripLocation> TabStripLocationProperty = AvaloniaProperty.Register<FATabView, FATabViewTabStripLocation>("TabStripLocation", FATabViewTabStripLocation.Top, false, (BindingMode)1, (Func<FATabViewTabStripLocation, bool>)null, (Func<AvaloniaObject, FATabViewTabStripLocation, FATabViewTabStripLocation>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.IsVerticalPaneOpen" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsVerticalPaneOpenProperty = AvaloniaProperty.Register<FATabView, bool>("IsVerticalPaneOpen", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.VerticalOpenPaneLength" /> property
	/// </summary>
	public static readonly StyledProperty<double> VerticalOpenPaneLengthProperty = AvaloniaProperty.Register<FATabView, double>("VerticalOpenPaneLength", 225.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.MinimumVerticalOpenPaneLength" /> property
	/// </summary>
	public static readonly StyledProperty<double> MinimumVerticalOpenPaneLengthProperty = AvaloniaProperty.Register<FATabView, double>("MinimumVerticalOpenPaneLength", 40.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.MaximumVerticalOpenPaneLength" /> property
	/// </summary>
	public static readonly StyledProperty<double> MaximumVerticalOpenPaneLengthProperty = AvaloniaProperty.Register<FATabView, double>("MaximumVerticalOpenPaneLength", 700.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabView.VerticalPaneDisplayMode" /> property
	/// </summary>
	public static readonly StyledProperty<SplitViewDisplayMode> VerticalPaneDisplayModeProperty = AvaloniaProperty.Register<FATabView, SplitViewDisplayMode>("VerticalPaneDisplayMode", (SplitViewDisplayMode)0, false, (BindingMode)1, (Func<SplitViewDisplayMode, bool>)null, (Func<AvaloniaObject, SplitViewDisplayMode, SplitViewDisplayMode>)null, false);

	private IList _tabItems;

	private int _selectedIndex;

	private object _selectedItem;

	internal const string s_tpTabContentPresenter = "TabContentPresenter";

	private const string s_tpRightContentPresenter = "RightContentPresenter";

	private const string s_tpTabContainerGrid = "TabContainerGrid";

	private const string s_tpTabListView = "TabListView";

	internal const string s_tpAddButton = "AddButton";

	private const string s_tpScrollDecreaseButton = "ScrollDecreaseButton";

	private const string s_tpScrollIncreaseButton = "ScrollIncreaseButton";

	private const string s_tpPaneResizeHandle = "BorderResizeHandleHost";

	private static string c_tabViewItemMinWidthName = "TabViewItemMinWidth";

	private static string c_tabViewItemMaxWidthName = "TabViewItemMaxWidth";

	private const string s_pcSingleBorder = ":singleBorder";

	internal const string s_pcTop = ":top";

	internal const string s_pcLeft = ":left";

	internal const string s_pcRight = ":right";

	internal const string s_pcBottom = ":bottom";

	private static readonly string SR_TabViewCloseButtonTooltipWithKA = "TabViewCloseButtonTooltipWithKA";

	private static readonly string SR_TabViewAddButtonTooltip = "TabViewAddButtonTooltip";

	private static readonly string SR_TabViewScrollDecreaseButtonTooltip = "TabViewScrollDecreaseButtonTooltip";

	private static readonly string SR_TabViewScrollIncreaseButtonTooltip = "TabViewScrollIncreaseButtonTooltip";

	private static readonly string SR_TabViewAddButtonName = "TabViewAddButtonName";

	internal static readonly WeakEvent<FATabView, FATabViewTabDragStartingEventArgs> TabDragStartingWeakEvent = WeakEvent.Register<FATabView, FATabViewTabDragStartingEventArgs>((Func<FATabView, EventHandler<FATabViewTabDragStartingEventArgs>, Action>)delegate(FATabView c, EventHandler<FATabViewTabDragStartingEventArgs> s)
	{
		TypedEventHandler<FATabView, FATabViewTabDragStartingEventArgs> handler = delegate(FATabView _, FATabViewTabDragStartingEventArgs e)
		{
			s(c, e);
		};
		c.TabDragStarting += handler;
		return delegate
		{
			c.TabDragStarting -= handler;
		};
	});

	internal static readonly WeakEvent<FATabView, FATabViewTabDragCompletedEventArgs> TabDragCompletedWeakEvent = WeakEvent.Register<FATabView, FATabViewTabDragCompletedEventArgs>((Func<FATabView, EventHandler<FATabViewTabDragCompletedEventArgs>, Action>)delegate(FATabView c, EventHandler<FATabViewTabDragCompletedEventArgs> s)
	{
		TypedEventHandler<FATabView, FATabViewTabDragCompletedEventArgs> handler = delegate(FATabView _, FATabViewTabDragCompletedEventArgs e)
		{
			s(c, e);
		};
		c.TabDragCompleted += handler;
		return delegate
		{
			c.TabDragCompleted -= handler;
		};
	});

	/// <summary>
	/// Gets or sets how the tabs should be sized
	/// </summary>
	public FATabViewWidthMode TabWidthMode
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FATabViewWidthMode>(TabWidthModeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FATabViewWidthMode>(TabWidthModeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates the behavior of the close button within tabs
	/// </summary>
	public FATabViewCloseButtonOverlayMode CloseButtonOverlayMode
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FATabViewCloseButtonOverlayMode>(CloseButtonOverlayModeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FATabViewCloseButtonOverlayMode>(CloseButtonOverlayModeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the content that is shown to the left of the tab strip
	/// </summary>
	public object TabStripHeader
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(TabStripHeaderProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(TabStripHeaderProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the IDataTemplate used to dispaly the content of the TabStripHeader
	/// </summary>
	public IDataTemplate TabStripHeaderTemplate
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IDataTemplate>(TabStripHeaderTemplateProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IDataTemplate>(TabStripHeaderTemplateProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the content that is shown to the right of the tab strip
	/// </summary>
	public object TabStripFooter
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(TabStripFooterProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(TabStripFooterProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the IDataTemplate used to display the content of the TabStripFooter
	/// </summary>
	public IDataTemplate TabStripFooterTemplate
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IDataTemplate>(TabStripFooterTemplateProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IDataTemplate>(TabStripFooterTemplateProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether the add (+) tab button is visible
	/// </summary>
	public bool IsAddTabButtonVisible
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsAddTabButtonVisibleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsAddTabButtonVisibleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the command to onvoke when the add (+) tab button is tapped
	/// </summary>
	public ICommand AddTabButtonCommand
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ICommand>(AddTabButtonCommandProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ICommand>(AddTabButtonCommandProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the parameter to pass to the <see cref="P:FluentAvalonia.UI.Controls.FATabView.AddTabButtonCommand" /> property
	/// </summary>
	public object AddTabButtonCommandParameter
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(AddTabButtonCommandParameterProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(AddTabButtonCommandParameterProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the TabItems this TabView displays
	/// </summary>
	[Content]
	public IList TabItems
	{
		get
		{
			return _tabItems;
		}
		private set
		{
			((AvaloniaObject)this).SetAndRaise<IList>((DirectPropertyBase<IList>)(object)TabItemsProperty, ref _tabItems, value);
		}
	}

	/// <summary>
	/// Gets or sets the TabItems source for this TabView
	/// </summary>
	public IEnumerable TabItemsSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IEnumerable>(TabItemsSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IEnumerable>(TabItemsSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the IDataTemplate used to display each item
	/// </summary>
	public IDataTemplate TabItemTemplate
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IDataTemplate>(TabItemTemplateProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IDataTemplate>(TabItemTemplateProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether tabs can be dragged as a data payload
	/// </summary>
	public bool CanDragTabs
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(CanDragTabsProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(CanDragTabsProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the tabs in the TabStrip can be reordered through
	/// user interaction
	/// </summary>
	public bool CanReorderTabs
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(CanReorderTabsProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(CanReorderTabsProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that determines whether the TabView can be a drop target for the purposes
	/// of drag-and-drop operations
	/// </summary>
	public bool AllowDropTabs
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(AllowDropTabsProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(AllowDropTabsProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the index of the selected tab
	/// </summary>
	public int SelectedIndex
	{
		get
		{
			return _selectedIndex;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<int>((DirectPropertyBase<int>)(object)SelectedIndexProperty, ref _selectedIndex, value);
		}
	}

	/// <summary>
	/// Gets or sets the selected tab item
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
	/// Gets or sets the location of the tab strip for this TabView
	/// </summary>
	public FATabViewTabStripLocation TabStripLocation
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FATabViewTabStripLocation>(TabStripLocationProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FATabViewTabStripLocation>(TabStripLocationProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// When the tab strip is on the left or the right, returns whether the pane is open.
	/// If the tab strip is on the top or bottom, this has no effect
	/// </summary>
	public bool IsVerticalPaneOpen
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsVerticalPaneOpenProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsVerticalPaneOpenProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// When the tab strip is on the left or the right, returns pane's open length
	/// If the tab strip is on the top or bottom, this has no effect
	/// </summary>
	public double VerticalOpenPaneLength
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(VerticalOpenPaneLengthProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(VerticalOpenPaneLengthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// When the tab strip is on the left or the right, returns the minimum width
	/// the pane can be opened. If the tab strip is on the top or bottom, this has no effect
	/// </summary>
	public double MinimumVerticalOpenPaneLength
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(MinimumVerticalOpenPaneLengthProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(MinimumVerticalOpenPaneLengthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// When the tab strip is on the left or the right, returns the maximum width
	/// the pane can be opened. If the tab strip is on the top or bottom, this has no effect
	/// </summary>
	public double MaximumVerticalOpenPaneLength
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(MaximumVerticalOpenPaneLengthProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(MaximumVerticalOpenPaneLengthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// When the tab strip is on the left or the right, returns the display mode of the pane.
	/// If the tab strip is on the top or bottom, this has no effect.
	/// </summary>
	public SplitViewDisplayMode VerticalPaneDisplayMode
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<SplitViewDisplayMode>(VerticalPaneDisplayModeProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<SplitViewDisplayMode>(VerticalPaneDisplayModeProperty, value, (BindingPriority)0);
		}
	}

	internal FATabViewListView ListView => _listView;

	internal ContentPresenter TabContentPresenter => _tabContentPresenter;

	/// <summary>
	/// Raised when the user attempts to close a Tab via clicking the x-to-close button
	/// </summary>
	public event TypedEventHandler<FATabView, FATabViewTabCloseRequestedEventArgs> TabCloseRequested;

	/// <summary>
	/// Occurs when the user completes a drag and drop operation by dropping a tab outside 
	/// of the tab strip area
	/// </summary>
	public event TypedEventHandler<FATabView, FATabViewTabDroppedOutsideEventArgs> TabDroppedOutside;

	/// <summary>
	/// Occurs when the add (+) tab button has been clicked
	/// </summary>
	public event TypedEventHandler<FATabView, EventArgs> AddTabButtonClick;

	/// <summary>
	/// Raised when the items collection has changed
	/// </summary>
	public event TypedEventHandler<FATabView, NotifyCollectionChangedEventArgs> TabItemsChanged;

	/// <summary>
	/// Occurs when the currently selected tab changes
	/// </summary>
	public event SelectionChangedEventHandler SelectionChanged;

	/// <summary>
	/// Occurs when a drag operation is initiated
	/// </summary>
	public event TypedEventHandler<FATabView, FATabViewTabDragStartingEventArgs> TabDragStarting;

	/// <summary>
	/// Raised when the user completes the drag action
	/// </summary>
	public event TypedEventHandler<FATabView, FATabViewTabDragCompletedEventArgs> TabDragCompleted;

	/// <summary>
	/// Occurs when the input system reports an underlying drag event with the TabStrip as 
	/// the potential drop target
	/// </summary>
	public event EventHandler<DragEventArgs> TabStripDragOver;

	/// <summary>
	/// Occurs when the input system reports an underlying drop event with the TabStrip as
	/// the drop target
	/// </summary>
	public event EventHandler<DragEventArgs> TabStripDrop;

	public FATabView()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Expected O, but got Unknown
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Expected O, but got Unknown
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Expected O, but got Unknown
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		((TemplatedControl)this)._002Ector();
		TabItems = (IList)new AvaloniaList<object>();
		((Control)this).Loaded += OnLoaded;
		((Control)this).Unloaded += OnUnloaded;
		KeyModifiers commandModifiers = Application.Current.PlatformSettings.HotkeyConfiguration.CommandModifiers;
		_keyboardAcceleratorHandler = new TabViewCommand(OnKeyboardAcceleratorInvoked);
		KeyGesture gesture = new KeyGesture((Key)93, commandModifiers);
		((InputElement)this).KeyBindings.Add(new KeyBinding
		{
			Gesture = gesture,
			Command = _keyboardAcceleratorHandler,
			CommandParameter = TabViewCommandType.CtrlF4
		});
		((InputElement)this).KeyBindings.Add(new KeyBinding
		{
			Gesture = new KeyGesture((Key)3, commandModifiers),
			Command = _keyboardAcceleratorHandler,
			CommandParameter = TabViewCommandType.CtrlTab
		});
		((InputElement)this).KeyBindings.Add(new KeyBinding
		{
			Gesture = new KeyGesture((Key)3, (KeyModifiers)(commandModifiers | 4)),
			Command = _keyboardAcceleratorHandler,
			CommandParameter = TabViewCommandType.CtrlShftTab
		});
		_tabCloseButtonTooltipText = FALocalizationHelper.Instance.GetLocalizedStringResource(SR_TabViewCloseButtonTooltipWithKA);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", true);
		DragDrop.SetAllowDrop((Interactive)(object)this, true);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		UnhookEventsAndClearFields();
		_isItemDraggedOver = false;
		_expandedWidthForDragOver = null;
		((TemplatedControl)this).OnApplyTemplate(e);
		_tabContentPresenter = NameScopeExtensions.Find<ContentPresenter>(e.NameScope, "TabContentPresenter");
		_rightContentPresenter = NameScopeExtensions.Find<ContentPresenter>(e.NameScope, "RightContentPresenter");
		_tabContainerGrid = NameScopeExtensions.Get<Grid>(e.NameScope, "TabContainerGrid");
		if (((AvaloniaList<ColumnDefinition>)(object)_tabContainerGrid.ColumnDefinitions).Count > 0)
		{
			_leftContentColumn = ((AvaloniaList<ColumnDefinition>)(object)_tabContainerGrid.ColumnDefinitions)[0];
			_tabColumn = ((AvaloniaList<ColumnDefinition>)(object)_tabContainerGrid.ColumnDefinitions)[1];
			_addButtonColumn = ((AvaloniaList<ColumnDefinition>)(object)_tabContainerGrid.ColumnDefinitions)[2];
			_rightContentColumn = ((AvaloniaList<ColumnDefinition>)(object)_tabContainerGrid.ColumnDefinitions)[3];
		}
		else
		{
			((Control)_tabContainerGrid).SizeChanged += HandleTabContainerGridSizeChangedForVerticalTabView;
		}
		((InputElement)_tabContainerGrid).PointerEntered += OnTabStripPointerEnter;
		((InputElement)_tabContainerGrid).PointerExited += OnTabStripPointerLeave;
		_listView = NameScopeExtensions.Get<FATabViewListView>(e.NameScope, "TabListView");
		if (_listView != null)
		{
			((ICollection<ILogical>)((StyledElement)this).LogicalChildren).Add((ILogical)(object)_listView);
			((Control)_listView).Loaded += OnListViewLoaded;
			((SelectingItemsControl)_listView).SelectionChanged += OnListViewSelectionChanged;
			((Control)_listView).SizeChanged += OnListViewSizeChanged;
			_listView.DragItemsStarting += OnListViewDragItemsStarting;
			_listView.DragItemsCompleted += OnListViewDragItemsCompleted;
			_listView.DragOver += OnListViewDragOver;
			_listView.Drop += OnListViewDrop;
			_listView.DragEnter += OnListViewDragEnter;
			_listView.DragLeave += OnListViewDragLeave;
			((InputElement)_listView).GettingFocus += OnListViewGettingFocus;
			_listViewCanReorderItemsPropertyChangedRevoker = AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)_listView, (AvaloniaProperty)(object)FATabViewListView.CanReorderItemsProperty).Subscribe(delegate
			{
				OnListViewDraggingPropertyChanged();
			});
			_listViewAllowDropPropertyChangedRevoker = AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)_listView, (AvaloniaProperty)(object)DragDrop.AllowDropProperty).Subscribe(delegate
			{
				OnListViewDraggingPropertyChanged();
			});
		}
		_addButton = NameScopeExtensions.Find<Button>(e.NameScope, "AddButton");
		if (_addButton != null)
		{
			if (AutomationProperties.GetName((StyledElement)(object)_addButton) == null)
			{
				string localizedStringResource = FALocalizationHelper.Instance.GetLocalizedStringResource(SR_TabViewAddButtonName);
				AutomationProperties.SetName((StyledElement)(object)_addButton, localizedStringResource);
			}
			if (ToolTip.GetTip((Control)(object)_addButton) == null)
			{
				ToolTip.SetTip((Control)(object)_addButton, (object)FALocalizationHelper.Instance.GetLocalizedStringResource(SR_TabViewAddButtonTooltip));
			}
			_addButton.Click += OnAddButtonClick;
			((InputElement)_addButton).KeyDown += OnAddButtonKeyDown;
		}
		Border val = NameScopeExtensions.Get<Border>(e.NameScope, "BorderResizeHandleHost");
		if (val != null)
		{
			((InputElement)val).PointerPressed += OnPaneResizeHandlePointerPressed;
			((InputElement)val).PointerMoved += OnPaneResizeHandlePointerMoved;
			((InputElement)val).PointerReleased += OnPaneResizeHandlePointerReleased;
			((InputElement)val).PointerCaptureLost += OnPaneResizeHandlePointerCaptureLost;
			_verticalPaneResizeHandle = val;
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (((Size)(ref _previousAvailableSize)).Width != ((Size)(ref availableSize)).Width)
		{
			_previousAvailableSize = availableSize;
			UpdateTabWidths();
		}
		return ((Layoutable)this).MeasureOverride(availableSize);
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return (AutomationPeer)(object)new FATabViewAutomationPeer((Control)(object)this);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((Control)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)CloseButtonOverlayModeProperty)
		{
			OnCloseButtonOverlayModePropertyChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)SelectedIndexProperty)
		{
			OnSelectedIndexPropertyChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)SelectedItemProperty)
		{
			OnSelectedItemPropertyChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)TabItemsSourceProperty)
		{
			OnTabItemsSourcePropertyChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)TabWidthModeProperty)
		{
			OnTabWidthModePropertyChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)TabStripLocationProperty)
		{
			OnTabStripLocationPropertyChanged(change);
		}
	}

	internal void SetTabSeparatorOpacity(int index, int opacityValue)
	{
		if (ContainerFromIndex(index) is FATabViewItem fATabViewItem)
		{
			Visual tabSeparator = fATabViewItem.TabSeparator;
			if (tabSeparator != null)
			{
				tabSeparator.Opacity = opacityValue;
			}
		}
	}

	internal void SetTabSeparatorOpacity(int index)
	{
		int selectedIndex = SelectedIndex;
		if (index == selectedIndex || index + 1 == selectedIndex)
		{
			SetTabSeparatorOpacity(index, 0);
		}
		else
		{
			SetTabSeparatorOpacity(index, 1);
		}
	}

	protected virtual void OnTabStripLocationPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		ValueTuple<FATabViewTabStripLocation, FATabViewTabStripLocation> oldAndNewValue = AvaloniaPropertyChangedExtensions.GetOldAndNewValue<FATabViewTabStripLocation>(args);
		FATabViewTabStripLocation item = oldAndNewValue.Item1;
		FATabViewTabStripLocation item2 = oldAndNewValue.Item2;
		_isSwitchingTabLocation = true;
		if ((IsHorizontal(item) && !IsHorizontal(item2)) || (!IsHorizontal(item) && IsHorizontal(item2)))
		{
			UpdateTabContent();
		}
		string classForStripLocation = GetClassForStripLocation(AvaloniaPropertyChangedExtensions.GetOldValue<FATabViewTabStripLocation>(args));
		string classForStripLocation2 = GetClassForStripLocation(AvaloniaPropertyChangedExtensions.GetNewValue<FATabViewTabStripLocation>(args));
		((StyledElement)this).PseudoClasses.Remove(classForStripLocation);
		((StyledElement)this).PseudoClasses.Add(classForStripLocation2);
		_listView?.HandleTabStripLocationChanged(AvaloniaPropertyChangedExtensions.GetNewValue<FATabViewTabStripLocation>(args), classForStripLocation, classForStripLocation2);
		UpdateTabWidths();
		static bool IsHorizontal(FATabViewTabStripLocation loc)
		{
			if (loc != FATabViewTabStripLocation.Top)
			{
				return loc == FATabViewTabStripLocation.Bottom;
			}
			return true;
		}
	}

	private void OnListViewDraggingPropertyChanged()
	{
	}

	private void OnListViewGettingFocus(object sender, FocusChangingEventArgs args)
	{
	}

	private void OnSelectedIndexPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		UpdateSelectedIndex();
		SetTabSeparatorOpacity(AvaloniaPropertyChangedExtensions.GetOldValue<int>(args));
		SetTabSeparatorOpacity(AvaloniaPropertyChangedExtensions.GetOldValue<int>(args) - 1);
		SetTabSeparatorOpacity(AvaloniaPropertyChangedExtensions.GetNewValue<int>(args) - 1);
		SetTabSeparatorOpacity(AvaloniaPropertyChangedExtensions.GetNewValue<int>(args));
		UpdateBottomBorderLineVisualStates();
	}

	private void UpdateTabBottomBorderLineVisualStates()
	{
		int num = TabItems.Count();
		int selectedIndex = SelectedIndex;
		for (int i = 0; i < num; i++)
		{
			int num2 = -1;
			if (_isDragging)
			{
				num2 = 0;
			}
			else if (selectedIndex != -1)
			{
				if (i == selectedIndex)
				{
					num2 = 0;
				}
				else if (i == selectedIndex - 1)
				{
					num2 = 1;
				}
				else if (i == selectedIndex + 1)
				{
					num2 = 2;
				}
			}
			if (ContainerFromIndex(i) is FATabViewItem fATabViewItem)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)fATabViewItem).Classes, ":noborder", num2 == 0);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)fATabViewItem).Classes, ":borderLeft", num2 == 1);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)fATabViewItem).Classes, ":borderRight", num2 == 2);
			}
		}
	}

	private void UpdateBottomBorderLineVisualStates()
	{
		UpdateTabBottomBorderLineVisualStates();
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":singleBorder", _isDragging);
		if (_listView != null)
		{
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_listView).Classes, ":noborder", _isDragging);
		}
		if (_scrollViewer != null)
		{
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_scrollViewer).Classes, ":noborder", _isDragging);
		}
	}

	private void OnSelectedItemPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		UpdateSelectedItem();
	}

	private void OnTabItemsSourcePropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		UpdateListViewItemContainerTransitions();
	}

	private void UpdateListViewItemContainerTransitions()
	{
	}

	private void OnCanTearOutTabsPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
	}

	private void OnTabWidthModePropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		UpdateTabWidths();
		FATabViewWidthMode newValue = AvaloniaPropertyChangedExtensions.GetNewValue<FATabViewWidthMode>(change);
		int count = TabItems.Count;
		for (int i = 0; i < count; i++)
		{
			if (ContainerFromIndex(i) is FATabViewItem fATabViewItem)
			{
				fATabViewItem.OnTabViewWidthModeChanged(newValue);
			}
		}
	}

	private void OnCloseButtonOverlayModePropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		FATabViewCloseButtonOverlayMode newValue = AvaloniaPropertyChangedExtensions.GetNewValue<FATabViewCloseButtonOverlayMode>(change);
		int count = TabItems.Count;
		for (int i = 0; i < count; i++)
		{
			if (ContainerFromIndex(i) is FATabViewItem fATabViewItem)
			{
				fATabViewItem.OnCloseButtonOverlayModeChanged(newValue);
			}
		}
	}

	private void OnAddButtonClick(object sender, RoutedEventArgs args)
	{
		AddTabButtonClick?.Invoke(this, (EventArgs)(object)args);
	}

	private void OnLoaded(object sender, RoutedEventArgs args)
	{
		UpdateTabContent();
	}

	private void OnUnloaded(object sender, RoutedEventArgs args)
	{
	}

	private void OnListViewLoaded(object sender, RoutedEventArgs args)
	{
		FATabViewListView listView = _listView;
		ItemCollection items = ((ItemsControl)listView).Items;
		if (items != null && items != TabItems)
		{
			if (((ItemsControl)listView).ItemsSource == null)
			{
				if (_isSwitchingTabLocation)
				{
					foreach (object tabItem in TabItems)
					{
						if (tabItem is FATabViewItem fATabViewItem)
						{
							Visual visualParent = VisualExtensions.GetVisualParent((Visual)(object)fATabViewItem);
							Panel val = (Panel)(object)((visualParent is Panel) ? visualParent : null);
							if (val != null)
							{
								((AvaloniaList<Control>)(object)val.Children).Remove((Control)(object)fATabViewItem);
							}
						}
						items.Add(tabItem);
					}
					TabItems.Clear();
				}
				else
				{
					using PooledList<object> pooledList = new PooledList<object>(((ItemsSourceView)items).Count);
					foreach (object tabItem2 in TabItems)
					{
						pooledList.Add(tabItem2);
					}
					items.Clear();
					Span<object> span = pooledList.AsSpan();
					for (int i = 0; i < span.Length; i++)
					{
						object obj = span[i];
						items.Add(obj);
					}
				}
			}
			TabItems = (IList)items;
		}
		FATabViewTabStripLocation tabStripLocation = TabStripLocation;
		listView.HandleTabStripLocationChanged(tabStripLocation, null, GetClassForStripLocation(tabStripLocation));
		if (SelectedItem != null)
		{
			UpdateSelectedItem();
		}
		else
		{
			UpdateSelectedIndex();
		}
		SelectedIndex = ((SelectingItemsControl)listView).SelectedIndex;
		SelectedItem = ((SelectingItemsControl)listView).SelectedItem;
		if (_isSwitchingTabLocation)
		{
			_isSwitchingTabLocation = false;
			UpdateTabContent();
		}
		if (_itemsPresenter != null)
		{
			_itemsPresenter = ((ItemsControl)_listView).Presenter;
			((Control)_itemsPresenter).SizeChanged += OnItemsPresenterSizeChanged;
		}
		ScrollViewer val2 = (_scrollViewer = _listView.Scroller);
		if (val2 != null)
		{
			if (((Control)val2).IsLoaded)
			{
				OnScrollViewerLoaded(null, null);
			}
			else
			{
				((Control)val2).Loaded += OnScrollViewerLoaded;
			}
		}
		UpdateBottomBorderLineVisualStates();
	}

	private void OnTabStripPointerLeave(object sender, PointerEventArgs e)
	{
		_pointerInTabstrip = false;
		if (_updateTabWidthOnPointerLeave)
		{
			try
			{
				UpdateTabWidths();
			}
			finally
			{
				_updateTabWidthOnPointerLeave = false;
			}
		}
	}

	private void OnTabStripPointerEnter(object sender, PointerEventArgs e)
	{
		_pointerInTabstrip = true;
	}

	private void OnScrollViewerLoaded(object sender, RoutedEventArgs args)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		foreach (RepeatButton item in from x in TemplateExtensions.GetTemplateChildren((TemplatedControl)(object)_scrollViewer)
			where x is RepeatButton
			select x)
		{
			RepeatButton val = item;
			if (((StyledElement)val).Name == "ScrollDecreaseButton")
			{
				_scrollDecreaseButton = val;
				ToolTip.SetTip((Control)(object)_scrollDecreaseButton, (object)FALocalizationHelper.Instance.GetLocalizedStringResource(SR_TabViewScrollDecreaseButtonTooltip));
				((Button)_scrollDecreaseButton).Click += OnScrollDecreaseClick;
			}
			else if (((StyledElement)val).Name == "ScrollIncreaseButton")
			{
				_scrollIncreaseButton = val;
				ToolTip.SetTip((Control)(object)_scrollIncreaseButton, (object)FALocalizationHelper.Instance.GetLocalizedStringResource(SR_TabViewScrollIncreaseButtonTooltip));
				((Button)_scrollIncreaseButton).Click += OnScrollIncreaseClick;
			}
		}
		_scrollViewer.ScrollChanged += OnScrollViewerViewChanged;
		UpdateTabWidths();
	}

	private void OnScrollViewerViewChanged(object sender, ScrollChangedEventArgs args)
	{
		UpdateScrollViewerDecreaseAndIncreaseButtonsViewState();
		UpdateTabWidths();
	}

	private void UpdateScrollViewerDecreaseAndIncreaseButtonsViewState()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (_scrollViewer == null || _scrollDecreaseButton == null || _scrollIncreaseButton == null)
		{
			return;
		}
		Vector offset = _scrollViewer.Offset;
		double x = ((Vector)(ref offset)).X;
		Size val = _scrollViewer.Extent;
		double width = ((Size)(ref val)).Width;
		val = _scrollViewer.Viewport;
		double num = width - ((Size)(ref val)).Width;
		if (double.Abs(x - num) < 0.1)
		{
			RepeatButton scrollDecreaseButton = _scrollDecreaseButton;
			if (scrollDecreaseButton != null)
			{
				((InputElement)scrollDecreaseButton).IsEnabled = true;
			}
			RepeatButton scrollIncreaseButton = _scrollIncreaseButton;
			if (scrollIncreaseButton != null)
			{
				((InputElement)scrollIncreaseButton).IsEnabled = false;
			}
		}
		else if (double.Abs(x) < 0.1)
		{
			((InputElement)_scrollDecreaseButton).IsEnabled = false;
			((InputElement)_scrollIncreaseButton).IsEnabled = true;
		}
		else
		{
			((InputElement)_scrollDecreaseButton).IsEnabled = true;
			((InputElement)_scrollIncreaseButton).IsEnabled = true;
		}
	}

	private void OnItemsPresenterSizeChanged(object sender, SizeChangedEventArgs args)
	{
		if (!_updateTabWidthOnPointerLeave)
		{
			UpdateScrollViewerDecreaseAndIncreaseButtonsViewState();
			UpdateTabWidths();
			BringSelectedTabIntoView();
		}
	}

	private void HandleTabContainerGridSizeChangedForVerticalTabView(object sender, SizeChangedEventArgs e)
	{
		UpdateTabWidths();
	}

	private void BringSelectedTabIntoView()
	{
		if (SelectedItem != null)
		{
			((SelectedItem as FATabViewItem) ?? (ContainerFromItem(SelectedItem) as FATabViewItem))?.StartBringTabIntoView();
		}
	}

	internal void OnItemsChanged(object item)
	{
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		if (item is NotifyCollectionChangedEventArgs e)
		{
			TabItemsChanged?.Invoke(this, e);
			int numItems = TabItems.Count;
			int selectedIndex = ((SelectingItemsControl)_listView).SelectedIndex;
			int num = SelectedIndex;
			if (num != selectedIndex && selectedIndex != -1)
			{
				SelectedIndex = selectedIndex;
				num = selectedIndex;
			}
			if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				_updateTabWidthOnPointerLeave = true;
				if (numItems > 0 && (num == -1 || num == e.OldStartingIndex))
				{
					int num2 = e.OldStartingIndex;
					if (num2 >= numItems)
					{
						num2 = numItems - 1;
					}
					int num3 = num2;
					do
					{
						if (ContainerFromIndex(num3) is FATabViewItem fATabViewItem && ((InputElement)fATabViewItem).IsEffectivelyEnabled && ((Visual)fATabViewItem).IsEffectivelyVisible)
						{
							SelectedItem = ItemFromContainer((Control)(object)fATabViewItem);
							break;
						}
						num3++;
						if (num3 >= numItems)
						{
							num3 = 0;
						}
					}
					while (num3 != num2);
				}
				if (TabWidthMode == FATabViewWidthMode.Equal && (!_pointerInTabstrip || e.OldStartingIndex == TabItems.Count))
				{
					UpdateTabWidths(shouldUpdateWidths: true, fillAllAvailableSpace: false);
				}
			}
			else
			{
				Dispatcher.UIThread.Post((Action)delegate
				{
					UpdateTabWidths();
					SetTabSeparatorOpacity(numItems - 1);
				}, default(DispatcherPriority));
			}
		}
		UpdateBottomBorderLineVisualStates();
	}

	private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if (!_isSwitchingTabLocation)
		{
			SelectedIndex = ((SelectingItemsControl)_listView).SelectedIndex;
			SelectedItem = ((SelectingItemsControl)_listView).SelectedItem;
			Dispatcher.UIThread.Post((Action)UpdateTabContent, default(DispatcherPriority));
			SelectionChanged?.Invoke(this, args);
		}
	}

	private void OnListViewSizeChanged(object sender, SizeChangedEventArgs args)
	{
	}

	private FATabViewItem FindTabViewItemFromDragItem(object item)
	{
		FATabViewItem fATabViewItem = ContainerFromItem(item) as FATabViewItem;
		if (fATabViewItem == null)
		{
			fATabViewItem = VisualExtensions.FindAncestorOfType<FATabViewItem>((Visual)(object)fATabViewItem, false);
		}
		if (fATabViewItem == null)
		{
			int count = TabItems.Count;
			for (int i = 0; i < count; i++)
			{
				FATabViewItem fATabViewItem2 = ContainerFromIndex(i) as FATabViewItem;
				if (((ContentControl)fATabViewItem2).Content == item)
				{
					fATabViewItem = fATabViewItem2;
					break;
				}
			}
		}
		return fATabViewItem;
	}

	private void OnListViewDragItemsStarting(object sender, DragItemsStartingEventArgs args)
	{
		object item = args.Items[0];
		FATabViewItem tab = FindTabViewItemFromDragItem(item);
		FATabViewTabDragStartingEventArgs args2 = new FATabViewTabDragStartingEventArgs(args, item, tab);
		TabDragStarting?.Invoke(this, args2);
		UpdateBottomBorderLineVisualStates();
	}

	private void OnListViewDragOver(object sender, DragEventArgs args)
	{
		TabStripDragOver?.Invoke(this, args);
	}

	private void OnListViewDrop(object sender, DragEventArgs args)
	{
		if (!((RoutedEventArgs)args).Handled)
		{
			TabStripDrop?.Invoke(this, args);
		}
		UpdateIsItemDraggedOver(isItemDraggedOver: false);
	}

	private void OnListViewDragEnter(object sender, DragEventArgs args)
	{
		foreach (object tabItem in TabItems)
		{
			if (ContainerFromItem(tabItem) is FATabViewItem { IsBeingDragged: not false })
			{
				return;
			}
		}
		UpdateIsItemDraggedOver(isItemDraggedOver: true);
	}

	private void OnListViewDragLeave(object sender, DragEventArgs args)
	{
		UpdateIsItemDraggedOver(isItemDraggedOver: false);
	}

	private void OnListViewDragItemsCompleted(object sender, DragItemsCompletedEventArgs args)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		if (_listView != null)
		{
			SelectedIndex = ((SelectingItemsControl)_listView).SelectedIndex;
			SelectedItem = ((SelectingItemsControl)_listView).SelectedItem;
			BringSelectedTabIntoView();
		}
		object item = args.Items[0];
		FATabViewItem tab = FindTabViewItemFromDragItem(item);
		FATabViewTabDragCompletedEventArgs args2 = new FATabViewTabDragCompletedEventArgs(args, item, tab);
		TabDragCompleted?.Invoke(this, args2);
		if ((int)args.DropResult == 0)
		{
			FATabViewTabDroppedOutsideEventArgs args3 = new FATabViewTabDroppedOutsideEventArgs(item, tab);
			TabDroppedOutside?.Invoke(this, args3);
		}
		UpdateBottomBorderLineVisualStates();
	}

	private void UpdateTabContent()
	{
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected O, but got Unknown
		if (_tabContentPresenter == null)
		{
			return;
		}
		if (SelectedItem == null || _isSwitchingTabLocation)
		{
			_tabContentPresenter.Content = null;
			_tabContentPresenter.ContentTemplate = null;
			return;
		}
		FATabViewItem fATabViewItem = (SelectedItem as FATabViewItem) ?? (ContainerFromItem(SelectedItem) as FATabViewItem);
		if (fATabViewItem == null)
		{
			return;
		}
		bool shouldMoveFocusToNewTab = false;
		((InputElement)_tabContentPresenter).LosingFocus += TabContentPresenterLostFocus;
		_tabContentPresenter.Content = ((ContentControl)fATabViewItem).Content;
		_tabContentPresenter.ContentTemplate = ((ContentControl)fATabViewItem).ContentTemplate;
		if (shouldMoveFocusToNewTab)
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
				if (focusManager == null)
				{
					obj = null;
				}
				else
				{
					FindNextElementOptions val = new FindNextElementOptions();
					val.set_SearchRoot((InputElement)(object)_tabContentPresenter);
					obj = focusManager.FindNextElement((NavigationDirection)0, val);
				}
			}
			IInputElement val2 = (IInputElement)obj;
			if (val2 == null)
			{
				val2 = (IInputElement)(object)fATabViewItem;
			}
			if (val2 != null)
			{
				val2.Focus((NavigationMethod)0, (KeyModifiers)0);
			}
		}
		else
		{
			((InputElement)_tabContentPresenter).LosingFocus -= TabContentPresenterLostFocus;
		}
		void TabContentPresenterLostFocus(object sender, FocusChangingEventArgs args)
		{
			((InputElement)_tabContentPresenter).LosingFocus -= TabContentPresenterLostFocus;
			shouldMoveFocusToNewTab = true;
		}
	}

	internal void RequestCloseTab(FATabViewItem container, bool updateTabWidths)
	{
		bool flag = false;
		IInputElement focusedElement = TopLevel.GetTopLevel((Visual)(object)this).FocusManager.GetFocusedElement();
		for (Visual val = (Visual)(object)((focusedElement is Visual) ? focusedElement : null); val != null; val = VisualExtensions.GetVisualParent(val))
		{
			if ((object)val == container)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			((InputElement)container).LosingFocus += ContainerLosingFocus;
		}
		if (_listView != null)
		{
			FATabViewTabCloseRequestedEventArgs args = new FATabViewTabCloseRequestedEventArgs(ItemFromContainer((Control)(object)container), container);
			TabCloseRequested?.Invoke(this, args);
			container.RaiseRequestClose(args);
		}
		UpdateTabWidths(updateTabWidths);
		void ContainerLosingFocus(object sender, FocusChangingEventArgs e)
		{
			((InputElement)container).LosingFocus -= ContainerLosingFocus;
			if (!e.Canceled && !((RoutedEventArgs)e).Handled)
			{
				int num = IndexFromContainer((Control)(object)container);
				Control val2 = null;
				for (int i = num + 1; i < GetItemCount(); i++)
				{
					Control val3 = ContainerFromIndex(i);
					if (val3 != null && IsFocusable((InputElement)(object)val3))
					{
						val2 = val3;
						break;
					}
				}
				if (val2 == null)
				{
					for (int num2 = num - 1; num2 >= 0; num2--)
					{
						Control val4 = ContainerFromIndex(num2);
						if (val4 != null && IsFocusable((InputElement)(object)val4))
						{
							val2 = val4;
							break;
						}
					}
				}
				if ((object)val2 != e.NewFocusedElement)
				{
					if (val2 == null)
					{
						val2 = (Control)(object)_addButton;
					}
					((RoutedEventArgs)e).Handled = e.TrySetNewFocusedElement((IInputElement)(object)val2);
				}
			}
		}
	}

	private void OnScrollDecreaseClick(object sender, RoutedEventArgs args)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (_scrollViewer != null)
		{
			Vector offset = _scrollViewer.Offset;
			_scrollViewer.Offset = ((Vector)(ref offset)).WithX(((Vector)(ref offset)).X - c_scrollAmount);
		}
	}

	private void OnScrollIncreaseClick(object sender, RoutedEventArgs args)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (_scrollViewer != null)
		{
			Vector offset = _scrollViewer.Offset;
			_scrollViewer.Offset = ((Vector)(ref offset)).WithX(((Vector)(ref offset)).X + c_scrollAmount);
		}
	}

	private void UpdateTabWidths(bool shouldUpdateWidths = true, bool fillAllAvailableSpace = true)
	{
		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
		//IL_044e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0468: Unknown result type (might be due to invalid IL or missing references)
		//IL_046d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0498: Unknown result type (might be due to invalid IL or missing references)
		//IL_049d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		object obj = default(object);
		double num = (ResourceNodeExtensions.TryFindResource((IResourceHost)(object)this, (object)c_tabViewItemMaxWidthName, ref obj) ? ((double)obj) : c_tabMaximumWidth);
		double num2 = double.NaN;
		int num3 = TabItems.Count;
		if (_isItemDraggedOver)
		{
			num3++;
		}
		FATabViewTabStripLocation tabStripLocation = TabStripLocation;
		bool flag = tabStripLocation == FATabViewTabStripLocation.Top || tabStripLocation == FATabViewTabStripLocation.Bottom;
		Rect bounds;
		if ((_tabContainerGrid != null) & flag)
		{
			double num4 = 0.0;
			if (_leftContentColumn != null)
			{
				num4 += _leftContentColumn.ActualWidth;
			}
			if (_addButtonColumn != null)
			{
				num4 += _addButtonColumn.ActualWidth;
			}
			if (_rightContentColumn != null && _rightContentPresenter != null)
			{
				Size desiredSize = ((Layoutable)_rightContentPresenter).DesiredSize;
				_rightContentColumn.MinWidth = ((Size)(ref desiredSize)).Width;
				num4 += ((Size)(ref desiredSize)).Width;
			}
			if (_tabColumn != null)
			{
				double num5 = ((Size)(ref _previousAvailableSize)).Width - num4;
				if (num5 > 0.0)
				{
					if (TabWidthMode == FATabViewWidthMode.Equal)
					{
						object obj2 = default(object);
						double min = (ResourceNodeExtensions.TryFindResource((IResourceHost)(object)this, (object)c_tabViewItemMinWidthName, ref obj2) ? ((double)obj2) : c_tabMinimumWidth);
						Thickness padding = ((TemplatedControl)this).Padding;
						double num6 = 0.0;
						double num7 = 0.0;
						if (fillAllAvailableSpace)
						{
							num2 = double.Clamp((num5 - (padding.Horizontal() + num6 + num7)) / (double)TabItems.Count(), min, num);
						}
						else
						{
							double num8 = _tabColumn.ActualWidth - (padding.Horizontal() + num6 + num7);
							if (_scrollIncreaseButton != null && ((Visual)_scrollIncreaseButton).IsVisible)
							{
								double num9 = num8;
								bounds = ((Visual)_scrollIncreaseButton).Bounds;
								num8 = num9 - ((Rect)(ref bounds)).Width;
							}
							if (_scrollDecreaseButton != null && ((Visual)_scrollDecreaseButton).IsVisible)
							{
								double num10 = num8;
								bounds = ((Visual)_scrollDecreaseButton).Bounds;
								num8 = num10 - ((Rect)(ref bounds)).Width;
							}
							num2 = double.Clamp(num8 / (double)TabItems.Count(), min, num);
						}
						_tabColumn.MaxWidth = num5 + num6 + num7;
						double num11 = num2 * (double)num3 + num6 + num7 + padding.Horizontal();
						if (num11 > num5)
						{
							_tabColumn.Width = new GridLength(num5, (GridUnitType)1);
							if (_listView != null)
							{
								((AvaloniaObject)_listView).SetValue<ScrollBarVisibility>((StyledProperty<ScrollBarVisibility>)(object)ScrollViewer.HorizontalScrollBarVisibilityProperty, (ScrollBarVisibility)3, (BindingPriority)0);
								UpdateScrollViewerDecreaseAndIncreaseButtonsViewState();
							}
						}
						else
						{
							_tabColumn.Width = (_isItemDraggedOver ? new GridLength(num11, (GridUnitType)1) : new GridLength(1.0, (GridUnitType)0));
							if (_listView != null)
							{
								if (shouldUpdateWidths & fillAllAvailableSpace)
								{
									((AvaloniaObject)_listView).SetValue<ScrollBarVisibility>((StyledProperty<ScrollBarVisibility>)(object)ScrollViewer.HorizontalScrollBarVisibilityProperty, (ScrollBarVisibility)2, (BindingPriority)0);
								}
								else
								{
									RepeatButton scrollDecreaseButton = _scrollDecreaseButton;
									if (scrollDecreaseButton != null)
									{
										((InputElement)scrollDecreaseButton).IsEnabled = false;
									}
									RepeatButton scrollIncreaseButton = _scrollIncreaseButton;
									if (scrollIncreaseButton != null)
									{
										((InputElement)scrollIncreaseButton).IsEnabled = false;
									}
								}
							}
						}
					}
					else
					{
						_tabColumn.MaxWidth = num5;
						if (_listView != null)
						{
							if (_isItemDraggedOver)
							{
								if (!_expandedWidthForDragOver.HasValue)
								{
									bounds = ((Visual)_listView).Bounds;
									_expandedWidthForDragOver = ((Rect)(ref bounds)).Width + num;
								}
								_tabColumn.Width = new GridLength(_expandedWidthForDragOver.Value, (GridUnitType)1);
							}
							else
							{
								if (_expandedWidthForDragOver.HasValue)
								{
									_expandedWidthForDragOver = null;
								}
								_tabColumn.Width = new GridLength(1.0, (GridUnitType)0);
							}
							((Layoutable)_listView).MaxWidth = num5;
							ItemsPresenter itemsPresenter = _itemsPresenter;
							if (itemsPresenter != null)
							{
								bounds = ((Visual)itemsPresenter).Bounds;
								bool flag2 = ((Rect)(ref bounds)).Width > num5;
								((AvaloniaObject)_listView).SetValue<ScrollBarVisibility>((StyledProperty<ScrollBarVisibility>)(object)ScrollViewer.HorizontalScrollBarVisibilityProperty, (ScrollBarVisibility)(flag2 ? 3 : 2), (BindingPriority)0);
								if (flag2)
								{
									UpdateScrollViewerDecreaseAndIncreaseButtonsViewState();
								}
							}
						}
					}
				}
			}
		}
		if (!flag)
		{
			if (_listView != null)
			{
				((AvaloniaObject)_listView).SetValue<ScrollBarVisibility>((StyledProperty<ScrollBarVisibility>)(object)ScrollViewer.HorizontalScrollBarVisibilityProperty, (ScrollBarVisibility)0, (BindingPriority)0);
				((AvaloniaObject)_listView).SetValue<ScrollBarVisibility>((StyledProperty<ScrollBarVisibility>)(object)ScrollViewer.VerticalScrollBarVisibilityProperty, (ScrollBarVisibility)1, (BindingPriority)0);
			}
			if (_tabContainerGrid != null)
			{
				_ = _tabContainerGrid.RowDefinitions;
				double num12 = 0.0;
				Enumerator<Control> enumerator = ((AvaloniaList<Control>)(object)((Panel)_tabContainerGrid).Children).GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						Control current = enumerator.Current;
						if (!(current is FATabViewListView))
						{
							double num13 = num12;
							Size desiredSize2 = ((Layoutable)current).DesiredSize;
							num12 = num13 + ((Size)(ref desiredSize2)).Height;
						}
					}
				}
				finally
				{
					((IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
				}
				bounds = ((Visual)_tabContainerGrid).Bounds;
				double height = ((Rect)(ref bounds)).Height;
				if (_isItemDraggedOver)
				{
					num12 += num12 / (double)((AvaloniaList<Control>)(object)((Panel)_tabContainerGrid).Children).Count;
				}
				ScrollViewer scrollViewer = _scrollViewer;
				if (scrollViewer != null)
				{
					((Layoutable)scrollViewer).MaxHeight = double.Clamp(height - num12, 0.0, double.PositiveInfinity);
				}
			}
		}
		else if (_scrollViewer != null)
		{
			((Layoutable)_scrollViewer).MaxHeight = double.PositiveInfinity;
		}
		if (!shouldUpdateWidths && TabWidthMode == FATabViewWidthMode.Equal)
		{
			return;
		}
		foreach (object tabItem in TabItems)
		{
			FATabViewItem obj3 = (tabItem as FATabViewItem) ?? (ContainerFromItem(tabItem) as FATabViewItem);
			if (obj3 != null)
			{
				((Layoutable)obj3).Width = num2;
			}
		}
	}

	private void UpdateSelectedItem()
	{
		if (_listView != null)
		{
			((SelectingItemsControl)_listView).SelectedItem = SelectedItem;
		}
	}

	private void UpdateSelectedIndex()
	{
		if (_listView != null)
		{
			int selectedIndex = SelectedIndex;
			if (selectedIndex < ((ItemsControl)_listView).ItemCount)
			{
				((SelectingItemsControl)_listView).SelectedIndex = selectedIndex;
			}
		}
	}

	public Control ContainerFromItem(object item)
	{
		FATabViewListView listView = _listView;
		if (listView == null)
		{
			return null;
		}
		return ((ItemsControl)listView).ContainerFromItem(item);
	}

	public Control ContainerFromIndex(int index)
	{
		FATabViewListView listView = _listView;
		if (listView == null)
		{
			return null;
		}
		return ((ItemsControl)listView).ContainerFromIndex(index);
	}

	public int IndexFromContainer(Control container)
	{
		FATabViewListView listView = _listView;
		if (listView == null)
		{
			return -1;
		}
		return ((ItemsControl)listView).IndexFromContainer(container);
	}

	public object ItemFromContainer(Control container)
	{
		FATabViewListView listView = _listView;
		if (listView == null)
		{
			return null;
		}
		return ((ItemsControl)listView).ItemFromContainer(container);
	}

	private void OnPaneResizeHandlePointerPressed(object sender, PointerPressedEventArgs e)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (!((RoutedEventArgs)e).Handled)
		{
			PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)null);
			PointerPointProperties properties = ((PointerEventArgs)e).Properties;
			if (((PointerPointProperties)(ref properties)).IsLeftButtonPressed)
			{
				_initDragPanePoint = ((PointerPoint)(ref currentPoint)).Position;
				_startingPaneSize = VerticalOpenPaneLength;
			}
		}
	}

	private void OnPaneResizeHandlePointerMoved(object sender, PointerEventArgs e)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (((RoutedEventArgs)e).Handled || !_initDragPanePoint.HasValue)
		{
			return;
		}
		PointerPoint currentPoint = e.GetCurrentPoint((Visual)null);
		Point val = ((PointerPoint)(ref currentPoint)).Position - _initDragPanePoint.Value;
		double num = ((Point)(ref val)).X;
		if (!_isDraggingPane)
		{
			FAUISettings.GetSystemDragSize(TopLevel.GetTopLevel((Visual)(object)this).RenderScaling, out var cxDrag, out var _);
			if (double.Abs(num) < cxDrag)
			{
				return;
			}
			_isDraggingPane = true;
		}
		double minimumVerticalOpenPaneLength = MinimumVerticalOpenPaneLength;
		double maximumVerticalOpenPaneLength = MaximumVerticalOpenPaneLength;
		if (TabStripLocation == FATabViewTabStripLocation.Right)
		{
			num *= -1.0;
		}
		double num2 = double.Clamp(_startingPaneSize + num, minimumVerticalOpenPaneLength, maximumVerticalOpenPaneLength);
		((AvaloniaObject)this).SetCurrentValue<double>(VerticalOpenPaneLengthProperty, num2);
	}

	private void OnPaneResizeHandlePointerReleased(object sender, PointerReleasedEventArgs e)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		if (!((RoutedEventArgs)e).Handled && _initDragPanePoint.HasValue)
		{
			PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)null);
			PointerPointProperties properties = ((PointerPoint)(ref currentPoint)).Properties;
			if ((int)((PointerPointProperties)(ref properties)).PointerUpdateKind == 5)
			{
				_initDragPanePoint = null;
				_isDraggingPane = false;
			}
		}
	}

	private void OnPaneResizeHandlePointerCaptureLost(object sender, PointerCaptureLostEventArgs e)
	{
		if (!((RoutedEventArgs)e).Handled)
		{
			_initDragPanePoint = null;
			_isDraggingPane = false;
		}
	}

	private int GetItemCount()
	{
		return TabItemsSource?.Count() ?? TabItems.Count;
	}

	internal bool MoveFocus(bool moveForward)
	{
		TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)this);
		if (topLevel != null)
		{
			IInputElement focusedElement = topLevel.FocusManager.GetFocusedElement();
			Control val = (Control)(object)((focusedElement is Control) ? focusedElement : null);
			if (val == null)
			{
				return false;
			}
			using PooledList<Control> pooledList = new PooledList<Control>();
			for (int i = 0; i < GetItemCount(); i++)
			{
				if (ContainerFromIndex(i) is FATabViewItem fATabViewItem && IsFocusable((InputElement)(object)fATabViewItem))
				{
					pooledList.Add((Control)(object)fATabViewItem);
					Button closeButton = fATabViewItem.CloseButton;
					if (closeButton != null && IsFocusable((InputElement)(object)closeButton))
					{
						pooledList.Add((Control)(object)closeButton);
					}
				}
			}
			if (_addButton != null && IsFocusable((InputElement)(object)_addButton))
			{
				pooledList.Add((Control)(object)_addButton);
			}
			int num = pooledList.IndexOf(val);
			if (num == -1)
			{
				return false;
			}
			int count = pooledList.Count;
			int num2 = (moveForward ? 1 : (-1));
			int num3 = num + num2;
			if (num3 < 0)
			{
				num3 = count - 1;
			}
			else if (num3 >= count)
			{
				num3 = 0;
			}
			Control val2 = pooledList[num3];
			bool isTabStop = ((InputElement)val2).IsTabStop;
			try
			{
				((InputElement)val2).IsTabStop = true;
				return ((InputElement)val2).Focus((NavigationMethod)1, (KeyModifiers)0);
			}
			finally
			{
				((InputElement)val2).IsTabStop = isTabStop;
			}
		}
		return false;
	}

	private bool MoveSelection(bool moveForward)
	{
		int selectedIndex = SelectedIndex;
		int num = (moveForward ? 1 : (-1));
		int i = selectedIndex + num;
		int itemCount = GetItemCount();
		for (; i != selectedIndex; i += num)
		{
			if (i < 0)
			{
				i = itemCount - 1;
			}
			else if (i >= itemCount)
			{
				i = 0;
			}
			Control val = ContainerFromIndex(i);
			if (val != null && IsFocusable((InputElement)(object)val))
			{
				SelectedIndex = i;
				return true;
			}
		}
		return false;
	}

	private bool RequestCloseCurrentTab()
	{
		bool result = false;
		if (SelectedItem is FATabViewItem { IsClosable: not false } fATabViewItem)
		{
			RequestCloseTab(fATabViewItem, updateTabWidths: true);
			result = true;
		}
		return result;
	}

	protected virtual void OnKeyboardAcceleratorInvoked(object parameter)
	{
		switch ((TabViewCommandType)parameter)
		{
		case TabViewCommandType.CtrlF4:
			RequestCloseCurrentTab();
			break;
		case TabViewCommandType.CtrlTab:
			MoveSelection(moveForward: true);
			break;
		case TabViewCommandType.CtrlShftTab:
			MoveSelection(moveForward: false);
			break;
		}
	}

	private void OnAddButtonKeyDown(object sender, KeyEventArgs args)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Invalid comparison between Unknown and I4
		Button addButton = _addButton;
		if ((int)args.Key == 25)
		{
			((RoutedEventArgs)args).Handled = MoveFocus((int)((Visual)addButton).FlowDirection == 0);
		}
		else if ((int)args.Key == 23)
		{
			((RoutedEventArgs)args).Handled = MoveFocus((int)((Visual)addButton).FlowDirection == 1);
		}
	}

	private bool IsFocusable(InputElement obj, bool checkTabStop = false)
	{
		if (obj == null)
		{
			return false;
		}
		Control val = (Control)(object)((obj is Control) ? obj : null);
		if (val != null)
		{
			if (((Visual)val).IsEffectivelyVisible && ((InputElement)val).IsEffectivelyEnabled)
			{
				if (!((InputElement)val).IsTabStop)
				{
					return !checkTabStop;
				}
				return true;
			}
			return false;
		}
		return false;
	}

	private void UpdateIsItemDraggedOver(bool isItemDraggedOver)
	{
		if (_isItemDraggedOver != isItemDraggedOver)
		{
			_isItemDraggedOver = isItemDraggedOver;
			UpdateTabWidths();
		}
	}

	private void UnhookEventsAndClearFields()
	{
		if (_tabContainerGrid != null)
		{
			((InputElement)_tabContainerGrid).PointerEntered -= OnTabStripPointerEnter;
			((InputElement)_tabContainerGrid).PointerExited -= OnTabStripPointerLeave;
		}
		if (_listView != null)
		{
			((Control)_listView).Loaded -= OnListViewLoaded;
			((ICollection<ILogical>)((StyledElement)this).LogicalChildren).Remove((ILogical)(object)_listView);
			((SelectingItemsControl)_listView).SelectionChanged -= OnListViewSelectionChanged;
			((InputElement)_listView).GettingFocus -= OnListViewGettingFocus;
			_listView.DragItemsStarting -= OnListViewDragItemsStarting;
			_listView.DragItemsCompleted -= OnListViewDragItemsCompleted;
			_listView.DragOver -= OnListViewDragOver;
			_listView.Drop -= OnListViewDrop;
			_listView.DragEnter -= OnListViewDragEnter;
			_listView.DragLeave -= OnListViewDragLeave;
			_listViewAllowDropPropertyChangedRevoker?.Dispose();
			_listViewCanReorderItemsPropertyChangedRevoker?.Dispose();
		}
		Button addButton = _addButton;
		if (addButton != null)
		{
			addButton.Click -= OnAddButtonClick;
		}
		Button addButton2 = _addButton;
		if (addButton2 != null)
		{
			((InputElement)addButton2).KeyDown -= OnAddButtonKeyDown;
		}
		ItemsPresenter itemsPresenter = _itemsPresenter;
		if (itemsPresenter != null)
		{
			((Control)itemsPresenter).SizeChanged -= OnItemsPresenterSizeChanged;
		}
		RepeatButton scrollDecreaseButton = _scrollDecreaseButton;
		if (scrollDecreaseButton != null)
		{
			((Button)scrollDecreaseButton).Click -= OnScrollDecreaseClick;
		}
		RepeatButton scrollIncreaseButton = _scrollIncreaseButton;
		if (scrollIncreaseButton != null)
		{
			((Button)scrollIncreaseButton).Click -= OnScrollIncreaseClick;
		}
		ScrollViewer scrollViewer = _scrollViewer;
		if (scrollViewer != null)
		{
			scrollViewer.ScrollChanged -= OnScrollViewerViewChanged;
		}
		if (_verticalPaneResizeHandle != null)
		{
			((InputElement)_verticalPaneResizeHandle).PointerPressed -= OnPaneResizeHandlePointerPressed;
			((InputElement)_verticalPaneResizeHandle).PointerMoved -= OnPaneResizeHandlePointerMoved;
			((InputElement)_verticalPaneResizeHandle).PointerReleased -= OnPaneResizeHandlePointerReleased;
			((InputElement)_verticalPaneResizeHandle).PointerCaptureLost -= OnPaneResizeHandlePointerCaptureLost;
		}
		_leftContentColumn = null;
		_tabColumn = null;
		_addButtonColumn = null;
		_rightContentColumn = null;
		_listView = null;
		_tabContentPresenter = null;
		_rightContentPresenter = null;
		_tabContainerGrid = null;
		_scrollViewer = null;
		_scrollDecreaseButton = null;
		_scrollIncreaseButton = null;
		_addButton = null;
		_itemsPresenter = null;
	}

	internal static string GetClassForStripLocation(FATabViewTabStripLocation loc)
	{
		return loc switch
		{
			FATabViewTabStripLocation.Left => ":left", 
			FATabViewTabStripLocation.Bottom => ":bottom", 
			FATabViewTabStripLocation.Right => ":right", 
			_ => ":top", 
		};
	}

	internal string GetTabCloseButtonTooltipText()
	{
		return _tabCloseButtonTooltipText;
	}
}
