using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
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
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls.Internal;

namespace FluentAvalonia.UI.Controls;

[PseudoClasses(new string[] { ":icon", ":compact", ":closeCollapsed" })]
[PseudoClasses(new string[] { ":borderRight", ":borderLeft", ":noborder" })]
[PseudoClasses(new string[] { ":dragging" })]
[TemplatePart("TabSeparator", typeof(Visual))]
[TemplatePart("ContentPresenter", typeof(ContentPresenter))]
[TemplatePart("CloseButton", typeof(Button))]
[TemplatePart("SelectedBackgroundPath", typeof(Path))]
public class FATabViewItem : FASelectorItem
{
	private Button _closeButton;

	private ContentPresenter _headerContentPresenter;

	private FATabViewWidthMode _tabViewWidthMode;

	private FATabViewCloseButtonOverlayMode _closeButtonOverlayMode;

	private FACompositeDisposable _tabDragRevoker;

	private Path _selectedBackgroundPath;

	private FATabViewTabStripLocation _location;

	private bool _hasPointerCapture;

	private bool _isMiddlePointerButtonPressed;

	private bool _isPointerOver;

	private Point _lastPointerPressedPosition;

	private int _dragPointerId;

	private bool _isCheckingForDrag;

	private WeakReference<FATabView> _parentTabView;

	private const string c_overlayCornerRadiusKey = "OverlayCornerRadius";

	private const int c_targetRectWidthIncrement = 2;

	private TargetWeakEventSubscriber<FATabView, FATabViewTabDragStartingEventArgs> _startingDragSub;

	private TargetWeakEventSubscriber<FATabView, FATabViewTabDragCompletedEventArgs> _completedDragSub;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabViewItem.Header" /> property
	/// </summary>
	public static readonly StyledProperty<object> HeaderProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabViewItem.HeaderTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> HeaderTemplateProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabViewItem.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabViewItem.IsClosable" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsClosableProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabViewItem.TabViewTemplateSettings" /> property
	/// </summary>
	public static readonly StyledProperty<FATabViewItemTemplateSettings> TabViewTemplateSettingsProperty;

	private const string s_pcCloseCollapsed = ":closeCollapsed";

	private const string s_pcDragging = ":dragging";

	private const string s_tpSelectedBackgroundPathName = "SelectedBackgroundPath";

	private const string s_tpTabSeparator = "TabSeparator";

	private const string s_tpContentPresenter = "ContentPresenter";

	internal const string s_tpCloseButton = "CloseButton";

	protected internal FATabView ParentTabView
	{
		get
		{
			WeakReference<FATabView> parentTabView = _parentTabView;
			if (parentTabView != null && parentTabView.TryGetTarget(out var target))
			{
				return target;
			}
			return null;
		}
		set
		{
			_parentTabView = new WeakReference<FATabView>(value);
		}
	}

	public Visual TabSeparator { get; private set; }

	internal bool IsBeingDragged { get; set; }

	/// <summary>
	/// Gets or sets the content that appears inside the tabstrip to represent the tab
	/// </summary>
	public object Header
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(HeaderProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(HeaderProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the IDataTemplate used to display the <see cref="P:FluentAvalonia.UI.Controls.FATabViewItem.Header" /> content
	/// </summary>
	public IDataTemplate HeaderTemplate
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IDataTemplate>(HeaderTemplateProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IDataTemplate>(HeaderTemplateProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value for the IconSource to be displayed within the tab
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
	/// Gets or sets the value that determines if the tab shows a close button (default is true)
	/// </summary>
	public bool IsClosable
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsClosableProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsClosableProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets an object that provides calculated values that can be referenced as {TemplateBinding}
	/// markup extension sources when definign templates for a TabViewItem control
	/// </summary>
	public FATabViewItemTemplateSettings TabViewTemplateSettings
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FATabViewItemTemplateSettings>(TabViewTemplateSettingsProperty);
		}
		private set
		{
			((AvaloniaObject)this).SetValue<FATabViewItemTemplateSettings>(TabViewTemplateSettingsProperty, value, (BindingPriority)0);
		}
	}

	internal bool IsContainerFromTemplate { get; set; }

	internal Button CloseButton => _closeButton;

	/// <summary>
	/// Raised when the user attempts to close the TabViewItem via clicking the x-to-close
	/// button
	/// </summary>
	public event TypedEventHandler<FATabViewItem, FATabViewTabCloseRequestedEventArgs> CloseRequested;

	public FATabViewItem()
	{
		TabViewTemplateSettings = new FATabViewItemTemplateSettings();
		((Control)this).Loaded += OnLoaded;
		((Control)this).SizeChanged += OnSizeChanged;
	}

	static FATabViewItem()
	{
		HeaderProperty = HeaderedContentControl.HeaderProperty.AddOwner<FATabViewItem>((StyledPropertyMetadata<object>)null);
		HeaderTemplateProperty = HeaderedContentControl.HeaderTemplateProperty.AddOwner<FATabViewItem>((StyledPropertyMetadata<IDataTemplate>)null);
		IconSourceProperty = FASettingsExpander.IconSourceProperty.AddOwner<FATabViewItem>((StyledPropertyMetadata<FAIconSource>)null);
		IsClosableProperty = AvaloniaProperty.Register<FATabViewItem, bool>("IsClosable", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		TabViewTemplateSettingsProperty = AvaloniaProperty.Register<FATabViewItem, FATabViewItemTemplateSettings>("TabViewTemplateSettings", (FATabViewItemTemplateSettings)null, false, (BindingMode)1, (Func<FATabViewItemTemplateSettings, bool>)null, (Func<AvaloniaObject, FATabViewItemTemplateSettings, FATabViewItemTemplateSettings>)null, false);
		InputElement.FocusableProperty.OverrideDefaultValue<FATabViewItem>(true);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((ContentControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)FASelectorItem.IsSelectedProperty)
		{
			OnIsSelectedPropertyChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)HeaderProperty)
		{
			OnHeaderPropertyChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			OnIconSourcePropertyChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)IsClosableProperty)
		{
			OnIsClosablePropertyChanged(change);
		}
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		_tabDragRevoker?.Dispose();
		Path selectedBackgroundPath = _selectedBackgroundPath;
		if (selectedBackgroundPath != null)
		{
			((Control)selectedBackgroundPath).SizeChanged -= OnSelectedBackgroundPathSizeChanged;
		}
		Button closeButton = _closeButton;
		if (closeButton != null)
		{
			closeButton.Click -= OnCloseButtonClick;
		}
		((TemplatedControl)this).OnApplyTemplate(e);
		_selectedBackgroundPath = NameScopeExtensions.Find<Path>(e.NameScope, "SelectedBackgroundPath");
		Path selectedBackgroundPath2 = _selectedBackgroundPath;
		if (selectedBackgroundPath2 != null)
		{
			((Control)selectedBackgroundPath2).SizeChanged += OnSelectedBackgroundPathSizeChanged;
		}
		TabSeparator = NameScopeExtensions.Find<Visual>(e.NameScope, "TabSeparator");
		_headerContentPresenter = NameScopeExtensions.Find<ContentPresenter>(e.NameScope, "ContentPresenter");
		FATabView tabView = (((StyledElement)this).Parent as FATabView) ?? VisualExtensions.FindAncestorOfType<FATabView>((Visual)(object)this, false);
		_closeButton = NameScopeExtensions.Get<Button>(e.NameScope, "CloseButton");
		string.IsNullOrEmpty(AutomationProperties.GetName((StyledElement)(object)_closeButton));
		Button closeButton2 = _closeButton;
		if (closeButton2 != null)
		{
			closeButton2.Click += OnCloseButtonClick;
		}
		OnHeaderChanged();
		OnIconSourceChanged();
		if (tabView != null)
		{
			_startingDragSub = new TargetWeakEventSubscriber<FATabView, FATabViewTabDragStartingEventArgs>(tabView, (Action<FATabView, object, WeakEvent, FATabViewTabDragStartingEventArgs>)delegate(FATabView target, object? _, WeakEvent _, FATabViewTabDragStartingEventArgs e2)
			{
				e2.Tab?.OnTabDragStarting(target, e2);
			});
			FATabView.TabDragStartingWeakEvent.Subscribe(tabView, (IWeakEventSubscriber<FATabViewTabDragStartingEventArgs>)(object)_startingDragSub);
			_completedDragSub = new TargetWeakEventSubscriber<FATabView, FATabViewTabDragCompletedEventArgs>(tabView, (Action<FATabView, object, WeakEvent, FATabViewTabDragCompletedEventArgs>)delegate(FATabView target, object? _, WeakEvent _, FATabViewTabDragCompletedEventArgs e2)
			{
				e2.Tab?.OnTabDragCompleted(target, e2);
			});
			FATabView.TabDragCompletedWeakEvent.Subscribe(tabView, (IWeakEventSubscriber<FATabViewTabDragCompletedEventArgs>)(object)_completedDragSub);
			_tabDragRevoker = new FACompositeDisposable(new FADisposable(delegate
			{
				FATabView.TabDragStartingWeakEvent.Unsubscribe(tabView, (IWeakEventSubscriber<FATabViewTabDragStartingEventArgs>)(object)_startingDragSub);
			}), new FADisposable(delegate
			{
				FATabView.TabDragCompletedWeakEvent.Unsubscribe(tabView, (IWeakEventSubscriber<FATabViewTabDragCompletedEventArgs>)(object)_completedDragSub);
			}));
			_closeButtonOverlayMode = tabView.CloseButtonOverlayMode;
		}
		UpdateCloseButton();
		UpdateWidthModeVisualState();
		UpdateTabGeometry();
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Invalid comparison between Unknown and I4
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Invalid comparison between Unknown and I4
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		IPointer pointer = ((PointerEventArgs)e).Pointer;
		PointerType type = ((PointerEventArgs)e).Pointer.Type;
		PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)(object)this);
		PointerPointProperties properties;
		if ((int)type == 0 || (int)type == 2)
		{
			properties = ((PointerPoint)(ref currentPoint)).Properties;
			if (((PointerPointProperties)(ref properties)).IsLeftButtonPressed)
			{
				_lastPointerPressedPosition = ((PointerPoint)(ref currentPoint)).Position;
				BeginCheckingForDrag(pointer.Id);
				KeyModifiers commandModifiers = VisualExtensions.GetPlatformSettings((Visual)(object)TopLevel.GetTopLevel((Visual)(object)this)).HotkeyConfiguration.CommandModifiers;
				if ((KeyModifiers)(((PointerEventArgs)e).KeyModifiers & commandModifiers) == commandModifiers)
				{
					base.IsSelected = true;
					return;
				}
			}
		}
		else if ((int)type == 1)
		{
			_lastPointerPressedPosition = ((PointerPoint)(ref currentPoint)).Position;
			BeginCheckingForDrag(pointer.Id);
		}
		base.OnPointerPressed(e);
		PointerPoint currentPoint2 = ((PointerEventArgs)e).GetCurrentPoint((Visual)null);
		properties = ((PointerPoint)(ref currentPoint2)).Properties;
		if ((int)((PointerPointProperties)(ref properties)).PointerUpdateKind == 1)
		{
			_hasPointerCapture = true;
			_isMiddlePointerButtonPressed = true;
		}
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		if (ShouldStartDrag(e))
		{
			UpdateDragDropVisualState(isVisible: true);
		}
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I4
		base.OnPointerReleased(e);
		_ = ((PointerEventArgs)e).Pointer;
		StopCheckingForDrag(((PointerEventArgs)e).Pointer.Id);
		UpdateDragDropVisualState(isVisible: false);
		if (!_hasPointerCapture)
		{
			return;
		}
		PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)null);
		PointerPointProperties properties = ((PointerPoint)(ref currentPoint)).Properties;
		if ((int)((PointerPointProperties)(ref properties)).PointerUpdateKind == 6)
		{
			bool isMiddlePointerButtonPressed = _isMiddlePointerButtonPressed;
			_isMiddlePointerButtonPressed = false;
			if (isMiddlePointerButtonPressed && IsClosable)
			{
				RequestClose();
			}
		}
	}

	protected override void OnPointerEntered(PointerEventArgs e)
	{
		((InputElement)this).OnPointerEntered(e);
		_isPointerOver = true;
		if (_hasPointerCapture)
		{
			_isMiddlePointerButtonPressed = true;
		}
		UpdateCloseButton();
		HideLeftAdjacentTabSeparator();
	}

	protected override void OnPointerExited(PointerEventArgs e)
	{
		((InputElement)this).OnPointerExited(e);
		_isPointerOver = false;
		_isMiddlePointerButtonPressed = false;
		UpdateCloseButton();
		RestoreLeftAdjacentTabSeparatorVisibility();
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		base.OnPointerCaptureLost(e);
		StopCheckingForDrag(e.Pointer.Id);
		if (_hasPointerCapture)
		{
			_hasPointerCapture = false;
			_isMiddlePointerButtonPressed = false;
		}
		RestoreLeftAdjacentTabSeparatorVisibility();
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return (AutomationPeer)(object)new FATabViewItemAutomationPeer((ContentControl)(object)this);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Invalid comparison between Unknown and I4
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I4
		if (!((RoutedEventArgs)e).Handled && ((int)e.Key == 23 || (int)e.Key == 25))
		{
			bool num = (e.KeyModifiers & 1) == 1;
			bool flag = (e.KeyModifiers & 4) == 4;
			if (!num || !flag)
			{
				bool moveForward = ((int)((Visual)this).FlowDirection == 0 && (int)e.Key == 25) || ((int)((Visual)this).FlowDirection == 1 && (int)e.Key == 23);
				((RoutedEventArgs)e).Handled = ParentTabView?.MoveFocus(moveForward) ?? false;
			}
		}
		if (!((RoutedEventArgs)e).Handled)
		{
			((InputElement)this).OnKeyDown(e);
		}
	}

	private bool IsOutsideDragRectangle(Point testPoint, Point dragRectangleCenter)
	{
		double num = double.Abs(((Point)(ref testPoint)).X - ((Point)(ref dragRectangleCenter)).X);
		double num2 = double.Abs(((Point)(ref testPoint)).Y - ((Point)(ref dragRectangleCenter)).Y);
		FAUISettings.GetSystemDragSize(TopLevel.GetTopLevel((Visual)(object)this).RenderScaling, out var cxDrag, out var cyDrag);
		cxDrag *= 2.0;
		cyDrag *= 2.0;
		if (!(num > cxDrag))
		{
			return num2 > cyDrag;
		}
		return true;
	}

	private bool ShouldStartDrag(PointerEventArgs args)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (_isCheckingForDrag)
		{
			PointerPoint currentPoint = args.GetCurrentPoint((Visual)(object)this);
			if (IsOutsideDragRectangle(((PointerPoint)(ref currentPoint)).Position, _lastPointerPressedPosition))
			{
				return _dragPointerId == args.Pointer.Id;
			}
		}
		return false;
	}

	private void BeginCheckingForDrag(int pointerId)
	{
		_dragPointerId = pointerId;
		_isCheckingForDrag = true;
	}

	private void StopCheckingForDrag(int pointerId)
	{
		if (_isCheckingForDrag && _dragPointerId == pointerId)
		{
			_dragPointerId = 0;
			_isCheckingForDrag = false;
		}
	}

	private void UpdateTabGeometry()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Expected O, but got Unknown
		if (_location != FATabViewTabStripLocation.Left && _location != FATabViewTabStripLocation.Right)
		{
			bool num = _location == FATabViewTabStripLocation.Top;
			Rect bounds = ((Visual)this).Bounds;
			double height = ((Rect)(ref bounds)).Height;
			object obj = default(object);
			CornerRadius val = (ResourceNodeExtensions.TryFindResource((IResourceHost)(object)this, (object)"OverlayCornerRadius", ref obj) ? ((CornerRadius)obj) : default(CornerRadius));
			double topLeft = ((CornerRadius)(ref val)).TopLeft;
			double topRight = ((CornerRadius)(ref val)).TopRight;
			StringBuilder stringBuilder = StringBuilderCache.Acquire("F1 M0,{0}  a 4,4 0 0 0 4,-4  L 4,{1}  a {2},{3} 0 0 1 {4},-{5}  l {6},0  a {7},{8} 0 0 1 {9},{10}  l 0,{11}  a 4,4 0 0 0 4,4 Z".Length * 2);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			InlineArray12<object> buffer = default(InlineArray12<object>);
			buffer[0] = height;
			buffer[1] = topLeft;
			buffer[2] = topLeft;
			buffer[3] = topLeft;
			buffer[4] = topLeft;
			buffer[5] = topLeft;
			ref object reference = ref buffer[6];
			bounds = ((Visual)this).Bounds;
			reference = ((Rect)(ref bounds)).Width - (topLeft + topRight);
			buffer[7] = topRight;
			buffer[8] = topRight;
			buffer[9] = topRight;
			buffer[10] = topRight;
			buffer[11] = height - (4.0 + topRight);
			stringBuilder.AppendFormat((IFormatProvider?)invariantCulture, "F1 M0,{0}  a 4,4 0 0 0 4,-4  L 4,{1}  a {2},{3} 0 0 1 {4},-{5}  l {6},0  a {7},{8} 0 0 1 {9},{10}  l 0,{11}  a 4,4 0 0 0 4,4 Z", (ReadOnlySpan<object?>)buffer);
			StreamGeometry val2 = StreamGeometry.Parse(StringBuilderCache.GetStringAndRelease(stringBuilder));
			if (!num)
			{
				bounds = ((Geometry)val2).Bounds;
				double num2 = ((Rect)(ref bounds)).Width * 0.5;
				bounds = ((Geometry)val2).Bounds;
				((Geometry)val2).Transform = (Transform)new RotateTransform(180.0, num2, ((Rect)(ref bounds)).Height * 0.5);
			}
			TabViewTemplateSettings.TabGeometry = (Geometry)(object)val2;
		}
	}

	private void OnIsSelectedPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change))
		{
			((AvaloniaObject)this).SetValue<int>(Visual.ZIndexProperty, 20, (BindingPriority)0);
			StartBringTabIntoView();
		}
		else
		{
			((AvaloniaObject)this).SetValue<int>(Visual.ZIndexProperty, 0, (BindingPriority)0);
		}
		UpdateWidthModeVisualState();
		UpdateCloseButton();
	}

	private void OnTabDragStarting(FATabView sender, FATabViewTabDragStartingEventArgs args)
	{
	}

	private void OnTabDragCompleted(FATabView sender, FATabViewTabDragCompletedEventArgs args)
	{
		StopCheckingForDrag(_dragPointerId);
		UpdateDragDropVisualState(isVisible: false);
	}

	internal void OnCloseButtonOverlayModeChanged(FATabViewCloseButtonOverlayMode mode)
	{
		_closeButtonOverlayMode = mode;
		UpdateCloseButton();
	}

	internal void OnTabViewWidthModeChanged(FATabViewWidthMode mode)
	{
		_tabViewWidthMode = mode;
		UpdateWidthModeVisualState();
	}

	internal void HandleTabStripLocationChanged(FATabViewTabStripLocation newLocation)
	{
		_location = newLocation;
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", newLocation == FATabViewTabStripLocation.Top);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", newLocation == FATabViewTabStripLocation.Bottom);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", newLocation == FATabViewTabStripLocation.Right);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", newLocation == FATabViewTabStripLocation.Left);
		UpdateTabGeometry();
		if (_selectedBackgroundPath != null)
		{
			OnSelectedBackgroundPathSizeChanged(null, null);
		}
	}

	private void UpdateCloseButton()
	{
		bool flag = !IsClosable || (_closeButtonOverlayMode == FATabViewCloseButtonOverlayMode.OnPointerOver && ((!base.IsSelected && !_isPointerOver) ? true : false));
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":closeCollapsed", flag);
	}

	private void UpdateWidthModeVisualState()
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":compact", !base.IsSelected && _tabViewWidthMode == FATabViewWidthMode.Compact);
	}

	private void UpdateDragDropVisualState(bool isVisible)
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":dragging", isVisible);
	}

	/// <summary>
	/// Let's the TabView know this Tab would like to close, causing the TabView's
	/// TabCloseRequested event to fire
	/// </summary>
	public void RequestClose()
	{
		(ParentTabView ?? VisualExtensions.FindAncestorOfType<FATabView>((Visual)(object)this, false)).RequestCloseTab(this, updateTabWidths: false);
	}

	internal void RaiseRequestClose(FATabViewTabCloseRequestedEventArgs args)
	{
		CloseRequested?.Invoke(this, args);
	}

	private void OnCloseButtonClick(object sender, RoutedEventArgs e)
	{
		RequestClose();
	}

	private void OnIsClosablePropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		UpdateCloseButton();
	}

	private void OnHeaderPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		OnHeaderChanged();
	}

	private void OnHeaderChanged()
	{
		ContentPresenter headerContentPresenter = _headerContentPresenter;
		if (headerContentPresenter != null)
		{
			headerContentPresenter.Content = Header;
		}
	}

	private void HideLeftAdjacentTabSeparator()
	{
		FATabView parentTabView = ParentTabView;
		if (parentTabView != null)
		{
			int num = parentTabView.IndexFromContainer((Control)(object)this);
			parentTabView.SetTabSeparatorOpacity(num - 1, 0);
		}
	}

	private void RestoreLeftAdjacentTabSeparatorVisibility()
	{
		FATabView parentTabView = ParentTabView;
		if (parentTabView != null)
		{
			int num = parentTabView.IndexFromContainer((Control)(object)this);
			parentTabView.SetTabSeparatorOpacity(num - 1);
		}
	}

	private void OnIconSourcePropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		OnIconSourceChanged();
	}

	private void OnIconSourceChanged()
	{
		if (IconSource != null)
		{
			TabViewTemplateSettings.IconElement = FAIconHelpers.CreateFromUnknown(IconSource);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", true);
		}
		else
		{
			TabViewTemplateSettings.IconElement = null;
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", false);
		}
	}

	internal void StartBringTabIntoView()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		Size desiredSize = ((Layoutable)this).DesiredSize;
		double num = ((Size)(ref desiredSize)).Width + 2.0;
		desiredSize = ((Layoutable)this).DesiredSize;
		Rect targetRect = default(Rect);
		((Rect)(ref targetRect))._002Ector(0.0, 0.0, num, ((Size)(ref desiredSize)).Height);
		((Interactive)this).RaiseEvent((RoutedEventArgs)new RequestBringIntoViewEventArgs
		{
			RoutedEvent = (RoutedEvent)(object)Control.RequestBringIntoViewEvent,
			TargetObject = (Visual)(object)this,
			TargetRect = targetRect,
			Source = this
		});
	}

	private void OnSelectedBackgroundPathSizeChanged(object sender, SizeChangedEventArgs e)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		if (_location != FATabViewTabStripLocation.Left && _location != FATabViewTabStripLocation.Right)
		{
			bool flag = _location == FATabViewTabStripLocation.Top;
			Path selectedBackgroundPath = _selectedBackgroundPath;
			Rect bounds = ((Visual)selectedBackgroundPath).Bounds;
			double y = ((Rect)(ref bounds)).Y;
			double num = double.Round(y);
			if (num > y)
			{
				TranslateTransform renderTransform = new TranslateTransform(0.0, flag ? (num - y) : (y - num));
				((Visual)selectedBackgroundPath).RenderTransform = (ITransform)(object)renderTransform;
			}
			else if (((Visual)selectedBackgroundPath).RenderTransform != null)
			{
				((Visual)selectedBackgroundPath).RenderTransform = null;
			}
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Dispatcher.UIThread.Post((Action)delegate
		{
			UpdateTabGeometry();
		}, default(DispatcherPriority));
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		FATabView parentTabView = ParentTabView;
		parentTabView?.SetTabSeparatorOpacity(parentTabView.IndexFromContainer((Control)(object)this));
	}
}
