using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Data;

namespace FluentAvalonia.UI.Controls.Primitives;

/// <summary>
/// Represents the ListView used in the TabStrip of a <see cref="T:FluentAvalonia.UI.Controls.FATabView" />
/// </summary>
/// <remarks>
/// This control should not be used outside of a TabView
/// </remarks>
[PseudoClasses(new string[] { ":reorder" })]
[TemplatePart("ScrollViewer", typeof(ScrollViewer))]
public sealed class FATabViewListView : ListBox
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.Primitives.FATabViewListView.CanReorderItems" /> property
	/// </summary>
	public static readonly StyledProperty<bool> CanReorderItemsProperty = AvaloniaProperty.Register<FATabViewListView, bool>("CanReorderItems", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.Primitives.FATabViewListView.CanDragItems" /> property
	/// </summary>
	public static readonly StyledProperty<bool> CanDragItemsProperty = AvaloniaProperty.Register<FATabViewListView, bool>("CanDragItems", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	private FATabViewItem _dragItem;

	private int _dragIndex = -1;

	private bool _isDragItemFocused;

	private bool _isDragItemSelected;

	private bool _isInDrag;

	private bool _isInReorder;

	private IDisposable _dragItemOpacitySub;

	private Point? _initialPoint;

	private double _cxDrag = double.NaN;

	private double _cyDrag = double.NaN;

	private Control _parent;

	private bool _isDragWithinTabStrip;

	private bool _isDraggingOverSelf;

	private LiveReorderHelper _liveReorderHelper;

	private Point? _lastDragOverPoint;

	private PointerPressedEventArgs _initArgs;

	private DispatcherTimer _scrollTimer;

	private Vector _currentAutoPanVelocity;

	private const string s_tpScrollViewer = "ScrollViewer";

	private const string s_pcReorder = ":reorder";

	private const string s_pcLeftShort = ":leftShort";

	private const string s_pcRightShort = ":rightShort";

	/// <summary>
	/// Gets or sets whether this ListView can reorder items
	/// </summary>
	public bool CanReorderItems
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(CanReorderItemsProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(CanReorderItemsProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether dragging items is supported on this ListView
	/// </summary>
	public bool CanDragItems
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(CanDragItemsProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(CanDragItemsProperty, value, (BindingPriority)0);
		}
	}

	internal ScrollViewer Scroller { get; private set; }

	internal event EventHandler<DragEventArgs> DragEnter;

	internal event EventHandler<DragEventArgs> DragOver;

	internal event EventHandler<DragEventArgs> DragLeave;

	internal event EventHandler<DragEventArgs> Drop;

	/// <summary>
	/// Occurs when a drag operation that involves one of the items in the view is initiated.
	/// </summary>
	public event DragItemsStartingEventHandler DragItemsStarting;

	/// <summary>
	/// Occurs when a drag operation that involves one of the items in the view is ended.
	/// </summary>
	public event TypedEventHandler<FATabViewListView, DragItemsCompletedEventArgs> DragItemsCompleted;

	public FATabViewListView()
	{
		((ItemsControl)this).ItemsView.CollectionChanged += OnItemsChanged;
		((InputElement)this).Tapped += delegate(object? s, TappedEventArgs e)
		{
			object source = ((RoutedEventArgs)e).Source;
			Visual val = (Visual)((source is Visual) ? source : null);
			if (val != null)
			{
				FATabViewItem fATabViewItem = VisualExtensions.FindAncestorOfType<FATabViewItem>(val, true);
				if (fATabViewItem != null)
				{
					int num = ((ItemsControl)this).IndexFromContainer((Control)(object)fATabViewItem);
					((SelectingItemsControl)this).UpdateSelection(num, true, false, false, false, false);
					((RoutedEventArgs)e).Handled = true;
				}
			}
		};
		((Interactive)this).AddHandler<DragEventArgs>(DragDrop.DragOverEvent, (EventHandler<DragEventArgs>)OnListViewDragOver, (RoutingStrategies)5, false);
		((Interactive)this).AddHandler<DragEventArgs>(DragDrop.DropEvent, (EventHandler<DragEventArgs>)OnListViewDrop, (RoutingStrategies)5, false);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		((ListBox)this).OnApplyTemplate(e);
		Scroller = NameScopeExtensions.Find<ScrollViewer>(e.NameScope, "ScrollViewer");
		_parent = (Control)(object)VisualExtensions.FindAncestorOfType<FATabView>((Visual)(object)this, false);
		((Interactive)_parent).AddHandler<DragEventArgs>(DragDrop.DragLeaveEvent, (EventHandler<DragEventArgs>)OnParentDragEnter, (RoutingStrategies)5, false);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((SelectingItemsControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)SelectingItemsControl.SelectedIndexProperty)
		{
			UpdateBottomBorderVisualState();
		}
	}

	protected override bool NeedsContainerOverride(object item, int index, out object recycleKey)
	{
		bool flag = item is FATabViewItem;
		recycleKey = (flag ? null : "FATabViewItem");
		return !flag;
	}

	protected override Control CreateContainerForItemOverride(object item, int index, object recycleKey)
	{
		if (((ITemplate<object, Control>)(object)DataTemplateExtensions.FindDataTemplate((Control)(object)this, item, ((ItemsControl)this).ItemTemplate))?.Build(item) is FATabViewItem fATabViewItem)
		{
			fATabViewItem.IsContainerFromTemplate = true;
			return (Control)(object)fATabViewItem;
		}
		return (Control)(object)new FATabViewItem();
	}

	protected override void ContainerForItemPreparedOverride(Control container, object item, int index)
	{
		FATabViewItem fATabViewItem = container as FATabViewItem;
		FATabViewTabStripLocation newLocation = FATabViewTabStripLocation.Top;
		if (fATabViewItem.ParentTabView == null)
		{
			FATabView fATabView = VisualExtensions.FindAncestorOfType<FATabView>((Visual)(object)container, false);
			if (fATabView != null)
			{
				fATabViewItem.OnTabViewWidthModeChanged(fATabView.TabWidthMode);
				fATabViewItem.ParentTabView = fATabView;
				newLocation = fATabView.TabStripLocation;
			}
		}
		else
		{
			newLocation = fATabViewItem.ParentTabView.TabStripLocation;
		}
		fATabViewItem.HandleTabStripLocationChanged(newLocation);
		if (container == item || fATabViewItem.IsContainerFromTemplate)
		{
			IDataTemplate itemTemplate = ((ItemsControl)this).ItemTemplate;
			if (((ContentControl)fATabViewItem).ContentTemplate == itemTemplate)
			{
				((ContentControl)fATabViewItem).ContentTemplate = DataTemplateExtensions.FindDataTemplate((Control)(object)this, item, (IDataTemplate)null);
			}
			((SelectingItemsControl)this).ContainerForItemPreparedOverride(container, item, index);
			return;
		}
		fATabViewItem.Header = item;
		IDataTemplate val = DataTemplateExtensions.FindDataTemplate((Control)(object)this, item, ((ItemsControl)this).ItemTemplate);
		if (val != null)
		{
			fATabViewItem.HeaderTemplate = val;
		}
		((SelectingItemsControl)this).ContainerForItemPreparedOverride(container, item, index);
		if (fATabViewItem.IsSelected)
		{
			return;
		}
		int selectedIndex = ((SelectingItemsControl)this).SelectedIndex;
		int num = -1;
		if (selectedIndex != -1)
		{
			if (index == selectedIndex)
			{
				num = 0;
			}
			else if (index == selectedIndex - 1)
			{
				num = 1;
			}
			else if (index == selectedIndex + 1)
			{
				num = 2;
			}
		}
		PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)fATabViewItem).Classes, ":noborder", num == 0);
		PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)fATabViewItem).Classes, ":borderLeft", num == 1);
		PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)fATabViewItem).Classes, ":borderRight", num == 2);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs args)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (((RoutedEventArgs)args).Handled)
		{
			return;
		}
		if (CanDragItems || CanReorderItems)
		{
			PointerPoint currentPoint = ((PointerEventArgs)args).GetCurrentPoint((Visual)(object)this);
			PointerPointProperties properties = ((PointerPoint)(ref currentPoint)).Properties;
			if (((PointerPointProperties)(ref properties)).IsLeftButtonPressed)
			{
				_initialPoint = ((PointerPoint)(ref currentPoint)).Position;
				object source = ((RoutedEventArgs)args).Source;
				_dragItem = VisualExtensions.FindAncestorOfType<FATabViewItem>((Visual)((source is Visual) ? source : null), true);
				if (_dragItem == null)
				{
					return;
				}
				_dragIndex = ((ItemsControl)this).IndexFromContainer((Control)(object)_dragItem);
				_isDragItemFocused = ((InputElement)_dragItem).IsFocused;
				_isDragItemSelected = _dragItem.IsSelected;
				_initArgs = args;
				UpdateDragInfo();
			}
		}
		((InputElement)this).OnPointerPressed(args);
	}

	protected override void OnPointerMoved(PointerEventArgs args)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (((RoutedEventArgs)args).Handled)
		{
			return;
		}
		if (_initialPoint.HasValue && (!_isInDrag || !_isInReorder))
		{
			Point val = args.GetPosition((Visual)(object)this) - _initialPoint.Value;
			if (double.Abs(((Point)(ref val)).X) > _cxDrag || double.Abs(((Point)(ref val)).Y) > _cyDrag)
			{
				BeginDragReorder();
			}
		}
		((InputElement)this).OnPointerMoved(args);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs args)
	{
		((Control)this).OnPointerReleased(args);
		CancelDrag();
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs args)
	{
		((InputElement)this).OnPointerCaptureLost(args);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
	}

	private async void BeginDragReorder()
	{
		DataPackage dataPackage = new DataPackage();
		DragItemsStartingEventArgs e = null;
		object[] dragItems = null;
		bool canReorderItems = CanReorderItems;
		if (CanDragItems)
		{
			dragItems = new object[1] { ((ItemsControl)this).ItemsView.GetAt(_dragIndex) };
			e = new DragItemsStartingEventArgs
			{
				Items = dragItems,
				Data = dataPackage
			};
			DragItemsStarting?.Invoke(this, e);
			if (e.Cancel)
			{
				CancelDrag();
				return;
			}
			_isInDrag = true;
		}
		if (canReorderItems)
		{
			ParametrizedLogger valueOrDefault;
			if (!DragDrop.GetAllowDrop((Interactive)(object)this))
			{
				CancelDrag();
				ParametrizedLogger? val = Logger.TryGet((LogEventLevel)1, "TabView");
				if (val.HasValue)
				{
					valueOrDefault = val.GetValueOrDefault();
					((ParametrizedLogger)(ref valueOrDefault)).Log((object)"TabView", "User disabled this TabView as as drop target - canceling drag");
				}
				return;
			}
			IEnumerable itemsSource = ((ItemsControl)this).ItemsSource;
			if (itemsSource != null && (!(itemsSource is INotifyCollectionChanged) || itemsSource is IList { IsReadOnly: not false }))
			{
				CancelDrag();
				ParametrizedLogger? val = Logger.TryGet((LogEventLevel)1, "TabView");
				if (val.HasValue)
				{
					valueOrDefault = val.GetValueOrDefault();
					((ParametrizedLogger)(ref valueOrDefault)).Log((object)"TabView", "Attempted to initiate Drag/Reorder without INCC / mutable collection");
				}
				return;
			}
			_dragItemOpacitySub = ((AvaloniaObject)_dragItem).SetValue<double>(Visual.OpacityProperty, 0.0, (BindingPriority)(-1));
			_isInReorder = true;
			_isDraggingOverSelf = true;
		}
		DragDropEffects val2 = (DragDropEffects)((e == null) ? 2 : ((int)e.Data.RequestedOperation));
		DragDropEffects result = await DragDrop.DoDragDropAsync(_initArgs, (IDataTransfer)(object)dataPackage, val2);
		SetPendingAutoPanVelocity(default(Vector));
		DestroyStartEdgeScrollTimer();
		if (_isInReorder)
		{
			_dragItemOpacitySub?.Dispose();
			_isInReorder = false;
		}
		if (_isInDrag)
		{
			DragItemsCompletedEventArgs args = new DragItemsCompletedEventArgs(result, dragItems);
			DragItemsCompleted?.Invoke(this, args);
			_isInDrag = false;
		}
		CancelDrag();
		_liveReorderHelper?.ClearContainerBoundsCache(clearCompletely: true);
	}

	private void OnListViewDragOver(object sender, DragEventArgs e)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		if (!_lastDragOverPoint.HasValue)
		{
			_lastDragOverPoint = e.GetPosition((Visual)(object)this);
		}
		else
		{
			Point position = e.GetPosition((Visual)(object)this);
			double x = ((Point)(ref position)).X;
			Point value = _lastDragOverPoint.Value;
			if (double.Abs(x - ((Point)(ref value)).X) < 1E-05)
			{
				double y = ((Point)(ref position)).Y;
				value = _lastDragOverPoint.Value;
				if (double.Abs(y - ((Point)(ref value)).Y) < 1E-05)
				{
					return;
				}
			}
			_lastDragOverPoint = position;
		}
		bool canReorderItems = CanReorderItems;
		bool flag = !_isDraggingOverSelf & canReorderItems;
		if (!_isDragWithinTabStrip)
		{
			_isDragWithinTabStrip = true;
			DragEnter?.Invoke(this, e);
		}
		else
		{
			if (!_isInReorder)
			{
				DragOver?.Invoke(this, e);
			}
			_isDraggingOverSelf = _dragItem != null;
		}
		Process(_isInReorder, canReorderItems, e);
		if (_scrollTimer == null && (_isInReorder | flag))
		{
			if (_liveReorderHelper == null)
			{
				_liveReorderHelper = new LiveReorderHelper(this);
			}
			_liveReorderHelper.ProcessLiveReorder(e, _dragIndex);
		}
		ComputeEdgeScrollVelocity(e.GetPosition((Visual)(object)this), out var pVelocity);
		SetPendingAutoPanVelocity(pVelocity);
		static void Process(bool isInReorder, bool canReorder, DragEventArgs args)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (!((RoutedEventArgs)args).Handled)
			{
				DragDropEffects val = (DragDropEffects)((isInReorder | canReorder) ? 2 : 0);
				args.DragEffects &= val;
			}
		}
	}

	private void OnParentDragEnter(object sender, DragEventArgs e)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (_isDragWithinTabStrip)
		{
			Rect bounds = ((Visual)this).Bounds;
			Rect val = default(Rect);
			((Rect)(ref val))._002Ector(((Rect)(ref bounds)).Size);
			Point position = e.GetPosition((Visual)(object)this);
			if (!((Rect)(ref val)).Contains(position))
			{
				SetPendingAutoPanVelocity(default(Vector));
				DestroyStartEdgeScrollTimer();
				_isDragWithinTabStrip = false;
				_isDraggingOverSelf = false;
				_liveReorderHelper?.ResetAllItemsForLiveReorder();
				DragLeave?.Invoke(this, e);
			}
		}
	}

	private void OnListViewDrop(object sender, DragEventArgs e)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (!((RoutedEventArgs)e).Handled)
		{
			if (DropCausesReorder())
			{
				Point position = e.GetPosition((Visual)(object)this);
				OnReorderDrop(position);
				e.DragEffects = (DragDropEffects)2;
				((RoutedEventArgs)e).Handled = true;
			}
			else
			{
				_liveReorderHelper?.ResetAllItemsForLiveReorder();
			}
			Drop?.Invoke(this, e);
		}
	}

	private void CancelDrag()
	{
		_initArgs = null;
		_initialPoint = null;
		_isInDrag = (_isInReorder = false);
		_dragIndex = -1;
		_dragItem = null;
		_isDragItemSelected = (_isDragItemFocused = false);
		_isDraggingOverSelf = false;
		_lastDragOverPoint = null;
		_isDragWithinTabStrip = false;
		_liveReorderHelper?.ResetAllItemsForLiveReorder();
	}

	private bool DropCausesReorder()
	{
		if (_isDraggingOverSelf)
		{
			if (CanReorderItems)
			{
				return DragDrop.GetAllowDrop((Interactive)(object)this);
			}
			return false;
		}
		return false;
	}

	private void OnReorderDrop(Point dropPoint)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		int dragIndex = _dragIndex;
		bool isDragItemFocused = _isDragItemFocused;
		bool isDragItemSelected = _isDragItemSelected;
		int num = _liveReorderHelper.GetInsertionIndexForLiveReorder();
		_liveReorderHelper.ResetAllItemsForLiveReorder();
		if (dragIndex == num)
		{
			return;
		}
		if (num == -1)
		{
			num = _liveReorderHelper.GetClosestElement(dropPoint, requestingInsertionIndex: true);
		}
		object at = ((ItemsControl)this).ItemsView.GetAt(_dragIndex);
		if (dragIndex < num)
		{
			num--;
		}
		IEnumerable itemsSource = ((ItemsControl)this).ItemsSource;
		if (itemsSource is IList list)
		{
			try
			{
				list.RemoveAt(dragIndex);
				list.Insert(num, at);
			}
			catch
			{
			}
		}
		else if (itemsSource == null)
		{
			ItemCollection items = ((ItemsControl)this).Items;
			try
			{
				items.RemoveAt(dragIndex);
				items.Insert(num, at);
			}
			catch
			{
			}
		}
		((Layoutable)this).UpdateLayout();
		((ItemsControl)this).ScrollIntoView(num);
		if (isDragItemFocused)
		{
			Control val = ((ItemsControl)this).ContainerFromIndex(num);
			if (val != null)
			{
				((InputElement)val).Focus((NavigationMethod)0, (KeyModifiers)0);
			}
		}
		if (isDragItemSelected)
		{
			((SelectingItemsControl)this).SelectedIndex = num;
		}
	}

	private void ComputeEdgeScrollVelocity(Point dragPoint, out Vector pVelocity)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Invalid comparison between Unknown and I4
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Invalid comparison between Unknown and I4
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		bool flag2 = false;
		Size val = default(Size);
		Size val2 = default(Size);
		Vector val3 = default(Vector);
		if (Scroller != null)
		{
			ScrollBarVisibility verticalScrollBarVisibility = Scroller.VerticalScrollBarVisibility;
			ScrollBarVisibility horizontalScrollBarVisibility = Scroller.HorizontalScrollBarVisibility;
			val = Scroller.Extent;
			val2 = Scroller.Viewport;
			val3 = Scroller.Offset;
			flag = (int)verticalScrollBarVisibility > 0;
			flag2 = (int)horizontalScrollBarVisibility == 3;
		}
		double num = 0.0;
		double num2 = 0.0;
		Rect bounds;
		Size size;
		if (flag2)
		{
			bounds = ((Visual)this).Bounds;
			size = ((Rect)(ref bounds)).Size;
			double edgeDistanceThreshold = ((Size)(ref size)).Width * 0.2;
			double value = 0.0;
			num = 0.0 - ComputeEdgeScrollVelocityFromEdgeDistance(((Point)(ref dragPoint)).X, edgeDistanceThreshold);
			if (num == 0.0)
			{
				bounds = ((Visual)this).Bounds;
				size = ((Rect)(ref bounds)).Size;
				num = ComputeEdgeScrollVelocityFromEdgeDistance(((Size)(ref size)).Width - ((Point)(ref dragPoint)).X, edgeDistanceThreshold);
				value = ((Size)(ref val)).Width - ((Size)(ref val2)).Width;
			}
			if (FAMathHelpers.IsClose(value, ((Vector)(ref val3)).X, 0.05))
			{
				num = 0.0;
			}
		}
		if (flag && num == 0.0)
		{
			bounds = ((Visual)this).Bounds;
			size = ((Rect)(ref bounds)).Size;
			double edgeDistanceThreshold2 = ((Size)(ref size)).Height * 0.2;
			double value2 = 0.0;
			num2 = 0.0 - ComputeEdgeScrollVelocityFromEdgeDistance(((Point)(ref dragPoint)).Y, edgeDistanceThreshold2);
			if (num2 == 0.0)
			{
				bounds = ((Visual)this).Bounds;
				size = ((Rect)(ref bounds)).Size;
				num2 = ComputeEdgeScrollVelocityFromEdgeDistance(((Size)(ref size)).Height - ((Point)(ref dragPoint)).Y, edgeDistanceThreshold2);
				value2 = ((Size)(ref val)).Height - ((Size)(ref val2)).Height;
			}
			if (FAMathHelpers.IsClose(value2, ((Vector)(ref val3)).Y, 0.05))
			{
				num2 = 0.0;
			}
		}
		pVelocity = new Vector(num, num2);
	}

	private static double ComputeEdgeScrollVelocityFromEdgeDistance(in double distFromEdge, double edgeDistanceThreshold = 100.0)
	{
		if (distFromEdge <= edgeDistanceThreshold)
		{
			return 200.0 - distFromEdge / edgeDistanceThreshold * 175.0;
		}
		return 0.0;
	}

	private void SetPendingAutoPanVelocity(Vector velocity)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (!IsStationary(velocity))
		{
			if (!IsStationary(_currentAutoPanVelocity))
			{
				_currentAutoPanVelocity = velocity;
				EnsureStartEdgeScrollTimer();
			}
			else
			{
				_currentAutoPanVelocity = velocity;
			}
			_liveReorderHelper?.ResetAllItemsForLiveReorder();
			_dragItemOpacitySub?.Dispose();
			return;
		}
		DestroyStartEdgeScrollTimer();
		_currentAutoPanVelocity = default(Vector);
		ScrollWithVelocity(default(Vector));
		_dragItemOpacitySub?.Dispose();
		if (((ItemsControl)this).ContainerFromIndex(_dragIndex) != null)
		{
			_dragItemOpacitySub = ((AvaloniaObject)_dragItem).SetValue<double>(Visual.OpacityProperty, 0.0, (BindingPriority)(-1));
		}
	}

	private void EnsureStartEdgeScrollTimer()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		if (_scrollTimer == null)
		{
			_scrollTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(50L), DispatcherPriority.Normal, (EventHandler)StartEdgeScrollTimerTick);
		}
		_scrollTimer.Start();
	}

	private void DestroyStartEdgeScrollTimer()
	{
		DispatcherTimer scrollTimer = _scrollTimer;
		if (scrollTimer != null)
		{
			scrollTimer.Stop();
		}
		_scrollTimer = null;
	}

	private void StartEdgeScrollTimerTick(object sender, EventArgs args)
	{
		ScrollWithVelocity(in _currentAutoPanVelocity);
	}

	private void ScrollWithVelocity(in Vector velocity)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		ScrollViewer scroller = Scroller;
		Vector offset = scroller.Offset;
		offset += velocity;
		scroller.Offset = offset;
	}

	private static bool IsStationary(Vector v)
	{
		if (FAMathHelpers.IsZero(((Vector)(ref v)).X))
		{
			return FAMathHelpers.IsZero(((Vector)(ref v)).Y);
		}
		return false;
	}

	internal Orientation? GetLogicalOrientation()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		Panel itemsPanelRoot = ((ItemsControl)this).ItemsPanelRoot;
		VirtualizingStackPanel val = (VirtualizingStackPanel)(object)((itemsPanelRoot is VirtualizingStackPanel) ? itemsPanelRoot : null);
		if (val != null)
		{
			return val.Orientation;
		}
		StackPanel val2 = (StackPanel)(object)((itemsPanelRoot is StackPanel) ? itemsPanelRoot : null);
		if (val2 != null)
		{
			return val2.Orientation;
		}
		return null;
	}

	internal void HandleTabStripLocationChanged(FATabViewTabStripLocation newLocation, string oldClass, string newClass)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Invalid comparison between Unknown and I4
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Invalid comparison between Unknown and I4
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		if (oldClass != null)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, oldClass, false);
		}
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, newClass, true);
		if (Scroller != null)
		{
			if (oldClass != null)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)Scroller).Classes, oldClass, false);
			}
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)Scroller).Classes, newClass, true);
		}
		Panel itemsPanelRoot = ((ItemsControl)this).ItemsPanelRoot;
		if (itemsPanelRoot == null)
		{
			return;
		}
		Enumerator<Control> enumerator = ((AvaloniaList<Control>)(object)itemsPanelRoot.Children).GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current is FATabViewItem fATabViewItem)
				{
					fATabViewItem.HandleTabStripLocationChanged(newLocation);
				}
			}
		}
		finally
		{
			((IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
		}
		VirtualizingStackPanel val = (VirtualizingStackPanel)(object)((itemsPanelRoot is VirtualizingStackPanel) ? itemsPanelRoot : null);
		if (val != null)
		{
			if ((int)val.Orientation == 1 && (newLocation == FATabViewTabStripLocation.Top || newLocation == FATabViewTabStripLocation.Bottom))
			{
				val.Orientation = (Orientation)0;
			}
			else if ((int)val.Orientation == 0 && (newLocation == FATabViewTabStripLocation.Left || newLocation == FATabViewTabStripLocation.Right))
			{
				val.Orientation = (Orientation)1;
			}
			return;
		}
		StackPanel val2 = (StackPanel)(object)((itemsPanelRoot is StackPanel) ? itemsPanelRoot : null);
		if (val2 != null)
		{
			if ((int)val2.Orientation == 1 && (newLocation == FATabViewTabStripLocation.Top || newLocation == FATabViewTabStripLocation.Bottom))
			{
				val2.Orientation = (Orientation)0;
			}
			else if ((int)val2.Orientation == 0 && (newLocation == FATabViewTabStripLocation.Left || newLocation == FATabViewTabStripLocation.Right))
			{
				val2.Orientation = (Orientation)1;
			}
		}
		else
		{
			ILogSink sink = Logger.Sink;
			if (sink != null)
			{
				sink.Log((LogEventLevel)3, "TabView", (object)this, "User has TabView with non-stacking panel, which may not be compatible with TabStripLocation changes");
			}
		}
	}

	private void UpdateBottomBorderVisualState()
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftShort", ((SelectingItemsControl)this).SelectedIndex == 0);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightShort", ((SelectingItemsControl)this).SelectedIndex == ((ItemsControl)this).ItemsView.Count - 1);
	}

	private void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		VisualExtensions.FindAncestorOfType<FATabView>((Visual)(object)this, false)?.OnItemsChanged(e);
	}

	private void UpdateDragInfo()
	{
		TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)this);
		FAUISettings.GetSystemDragSize((topLevel != null) ? topLevel.RenderScaling : 1.0, out _cxDrag, out _cyDrag);
	}
}
