using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a data-driven collection control that incorporates a flexible layout system,
/// custom views, and virtualization, with no default UI or interaction policies.
/// </summary>
public class FAItemsRepeater : Panel
{
	internal const short _maxStackLayoutIterations = 60;

	internal static Point ClearedElementsArrangePosition;

	internal static Rect InvalidRect;

	private readonly TransitionManager _transitionManager;

	private readonly ViewManager _viewManager;

	private readonly ViewportManager _viewportManager;

	private FAItemsSourceView _itemsSourceView;

	private IFAElementFactory _itemTemplateWrapper;

	private FAVirtualizingLayoutContext _layoutContext;

	private object _layoutState;

	private NotifyCollectionChangedEventArgs _processingItemsSourceChange;

	private Size _lastAvailableSize;

	private bool _isLayoutInProgress;

	private Point _layoutOrigin;

	private FAItemsRepeaterElementPreparedEventArgs _elementPreparedArgs;

	private FAItemsRepeaterElementClearingEventArgs _elementClearingArgs;

	private FAItemsRepeaterElementIndexChangedEventArgs _elementIndexChangedArgs;

	private int _loadedCounter;

	private int _unloadedCounter;

	private byte _stackLayoutMeasureCounter;

	private bool _isItemTemplateEmpty;

	private bool _ownsTransitionProvider = true;

	private bool _wasLayoutChangedCalled;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAItemsRepeater.VerticalCacheLength" /> property
	/// </summary>
	public static readonly StyledProperty<double> VerticalCacheLengthProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAItemsRepeater.HorizontalCacheLength" /> property
	/// </summary>
	public static readonly StyledProperty<double> HorizontalCacheLengthProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAItemsRepeater.Layout" /> property
	/// </summary>
	public static readonly StyledProperty<FALayout> LayoutProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAItemsRepeater.ItemsSource" /> property
	/// </summary>
	public static readonly StyledProperty<object> ItemsSourceProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAItemsRepeater.VerticalCacheLength" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAItemsRepeater.ItemTransitionProvider" /> property
	/// </summary>
	public static readonly StyledProperty<FAItemCollectionTransitionProvider> ItemTransitionProviderProperty;

	internal static readonly AttachedProperty<VirtualizationInfo> VirtualizationInfoProperty;

	/// <summary>
	/// Gets or sets a value that indicates the size of the buffer used to realize items when panning or scrolling vertically.
	/// </summary>
	public double VerticalCacheLength
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(VerticalCacheLengthProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(VerticalCacheLengthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates the size of the buffer used to realize items when panning or scrolling vertically.
	/// </summary>
	public double HorizontalCacheLength
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(HorizontalCacheLengthProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(HorizontalCacheLengthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the layout used to size and position elements in the ItemsRepeater.
	/// </summary>
	public FALayout Layout
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FALayout>(LayoutProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FALayout>(LayoutProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets an object source used to generate the content of the ItemsRepeater.
	/// </summary>
	public object ItemsSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(ItemsSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(ItemsSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the template used to display each item.
	/// </summary>
	[InheritDataTypeFromItems("ItemsSource")]
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
	/// Gets or sets the <see cref="T:FluentAvalonia.UI.Controls.FAItemCollectionTransitionProvider" /> for the ItemsRepeater
	/// </summary>
	public FAItemCollectionTransitionProvider ItemTransitionProvider
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAItemCollectionTransitionProvider>(ItemTransitionProviderProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAItemCollectionTransitionProvider>(ItemTransitionProviderProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets a standardized view of the supported interactions between a given ItemsSource object and the ItemsRepeater control and its associated components.
	/// </summary>
	/// <remarks>
	/// Note the return type is <see cref="T:FluentAvalonia.UI.Controls.FAItemsSourceView" /> and not the ItemsSourceView in Avalonia
	/// </remarks>
	public FAItemsSourceView ItemsSourceView => _itemsSourceView;

	internal Control MadeAnchor => _viewportManager.MadeAnchor;

	internal object LayoutState
	{
		get
		{
			return _layoutState;
		}
		set
		{
			_layoutState = value;
		}
	}

	internal TransitionManager TransitionManager => _transitionManager;

	internal Rect VisibleWindow
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return _viewportManager.GetLayoutVisibleWindow();
		}
	}

	internal Rect RealizationWindow
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return _viewportManager.GetLayoutRealizationWindow();
		}
	}

	internal Control SuggestedAnchor => _viewportManager.SuggestedAnchor;

	internal Point LayoutOrigin
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _layoutOrigin;
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			_layoutOrigin = value;
		}
	}

	internal IFAElementFactory ItemTemplateShim => _itemTemplateWrapper;

	internal ViewManager ViewManager => _viewManager;

	private bool IsProcessingCollectionChange => _processingItemsSourceChange != null;

	internal bool ShouldPhase => ContainerContentChanging != null;

	/// <summary>
	/// Occurs each time an element is prepared for use.
	/// </summary>
	public event TypedEventHandler<FAItemsRepeater, FAItemsRepeaterElementPreparedEventArgs> ElementPrepared;

	/// <summary>
	/// Occurs each time an element is cleared and made available to be re-used.
	/// </summary>
	public event TypedEventHandler<FAItemsRepeater, FAItemsRepeaterElementClearingEventArgs> ElementClearing;

	/// <summary>
	/// Occurs for each realized UIElement when the index for the item it represents has changed.
	/// </summary>
	public event TypedEventHandler<FAItemsRepeater, FAItemsRepeaterElementIndexChangedEventArgs> ElementIndexChanged;

	/// <summary>
	/// Occurs when container content is changing, used for Phased rendering
	/// </summary>
	public event TypedEventHandler<FAItemsRepeater, FAContainerContentChangingEventArgs> ContainerContentChanging;

	public FAItemsRepeater()
	{
		_viewportManager = new ViewportManager(this);
		_viewManager = new ViewManager(this);
		_transitionManager = new TransitionManager(this);
		((AvaloniaObject)this).SetCurrentValue<FALayout>(LayoutProperty, (FALayout)new FAStackLayout());
		AutomationProperties.SetAccessibilityView((StyledElement)(object)this, (AccessibilityView)1);
		((AvaloniaObject)this).SetValue<KeyboardNavigationMode>((StyledProperty<KeyboardNavigationMode>)(object)KeyboardNavigation.TabNavigationProperty, (KeyboardNavigationMode)3, (BindingPriority)0);
		XYFocus.SetNavigationModes((InputElement)(object)this, (XYFocusNavigationModes)7);
		((Control)this).Loaded += OnRepeaterLoaded;
		((Control)this).Unloaded += OnRepeaterUnloaded;
		((Layoutable)this).LayoutUpdated += OnLayoutUpdated;
		((Interactive)this).AddHandler<RequestBringIntoViewEventArgs>(Control.RequestBringIntoViewEvent, (EventHandler<RequestBringIntoViewEventArgs>)OnBringIntoViewRequested, (RoutingStrategies)5, false);
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return (AutomationPeer)(object)new FAItemsRepeaterAutomationPeer((Control)(object)this);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		if (_isLayoutInProgress)
		{
			throw new Exception("Reentrancy detected during layout");
		}
		if (IsProcessingCollectionChange)
		{
			throw new Exception("Cannot run layout in the middle of a collection change");
		}
		FALayout effectiveLayout = GetEffectiveLayout();
		if (effectiveLayout != null && effectiveLayout is FAStackLayout && ++_stackLayoutMeasureCounter >= 60)
		{
			Rect layoutExtent = _viewportManager.LayoutExtent;
			return new Size(((Rect)(ref layoutExtent)).Width - ((Rect)(ref layoutExtent)).X, ((Rect)(ref layoutExtent)).Height - ((Rect)(ref layoutExtent)).Y);
		}
		_viewportManager.OnOwnerMeasuring();
		try
		{
			_isLayoutInProgress = true;
			_viewManager.PrunePinnedElements();
			Rect layoutExtent2 = default(Rect);
			Size result = default(Size);
			if (effectiveLayout != null)
			{
				FAVirtualizingLayoutContext layoutContext = GetLayoutContext();
				if (_isItemTemplateEmpty)
				{
					((Rect)(ref layoutExtent2))._002Ector(((Point)(ref _layoutOrigin)).X, ((Point)(ref _layoutOrigin)).Y, 0.0, 0.0);
				}
				else
				{
					result = effectiveLayout.Measure(layoutContext, availableSize);
					((Rect)(ref layoutExtent2))._002Ector(((Point)(ref _layoutOrigin)).X, ((Point)(ref _layoutOrigin)).Y, ((Size)(ref result)).Width, ((Size)(ref result)).Height);
				}
				Controls children = ((Panel)this).Children;
				for (int i = 0; i < ((AvaloniaList<Control>)(object)children).Count; i++)
				{
					Control val = ((AvaloniaList<Control>)(object)children)[i];
					VirtualizationInfo virtualizationInfo = GetVirtualizationInfo(val);
					if (virtualizationInfo.Owner == VirtualizationInfo.ElementOwner.Layout && virtualizationInfo.AutoRecycleCandidate && !virtualizationInfo.KeepAlive)
					{
						ClearElementImpl(val);
					}
				}
			}
			_viewportManager.SetLayoutExtent(layoutExtent2);
			_lastAvailableSize = availableSize;
			return result;
		}
		finally
		{
			_isLayoutInProgress = false;
		}
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		if (_isLayoutInProgress)
		{
			throw new Exception("Reentrancy detected during layout");
		}
		if (IsProcessingCollectionChange)
		{
			throw new Exception("Cannot run layout in the middle of a collection change");
		}
		try
		{
			_isLayoutInProgress = true;
			Size val = default(Size);
			FALayout effectiveLayout = GetEffectiveLayout();
			if (effectiveLayout != null)
			{
				val = effectiveLayout.Arrange(GetLayoutContext(), finalSize);
			}
			_viewManager.OnOwnerArranged();
			Controls children = ((Panel)this).Children;
			Size result;
			for (int i = 0; i < ((AvaloniaList<Control>)(object)children).Count; i++)
			{
				Control val2 = ((AvaloniaList<Control>)(object)children)[i];
				VirtualizationInfo virtualizationInfo = GetVirtualizationInfo(val2);
				virtualizationInfo.KeepAlive = false;
				if (virtualizationInfo.Owner == VirtualizationInfo.ElementOwner.ElementFactory || virtualizationInfo.Owner == VirtualizationInfo.ElementOwner.PinnedPool)
				{
					double x = ((Point)(ref ClearedElementsArrangePosition)).X;
					result = ((Layoutable)val2).DesiredSize;
					double num = x - ((Size)(ref result)).Width;
					double y = ((Point)(ref ClearedElementsArrangePosition)).Y;
					result = ((Layoutable)val2).DesiredSize;
					((Layoutable)val2).Arrange(new Rect(num, y - ((Size)(ref result)).Height, 0.0, 0.0));
				}
				else
				{
					Rect bounds = ((Visual)val2).Bounds;
					if (virtualizationInfo.ArrangeBounds != InvalidRect && bounds != virtualizationInfo.ArrangeBounds)
					{
						_transitionManager.OnElementBoundsChanged(val2, virtualizationInfo.ArrangeBounds, bounds);
					}
					virtualizationInfo.ArrangeBounds = bounds;
				}
			}
			_viewportManager.OnOwnerArranged();
			_transitionManager.OnOwnerArranged();
			result = val;
			return result;
		}
		finally
		{
			_isLayoutInProgress = false;
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		((Control)this).OnPropertyChanged(args);
		AvaloniaProperty property = args.Property;
		if (property == (AvaloniaProperty)(object)ItemsSourceProperty)
		{
			if (args.NewValue != args.OldValue)
			{
				object newValue = args.NewValue;
				FAItemsSourceView fAItemsSourceView = newValue as FAItemsSourceView;
				if (newValue != null && fAItemsSourceView == null)
				{
					fAItemsSourceView = new FAItemsSourceView(newValue as IEnumerable);
				}
				OnDataSourcePropertyChanged(_itemsSourceView, fAItemsSourceView);
			}
		}
		else if (property == (AvaloniaProperty)(object)ItemTemplateProperty)
		{
			object oldValue = args.OldValue;
			object oldValue2 = ((oldValue is IDataTemplate) ? oldValue : null);
			object newValue2 = args.NewValue;
			OnItemTemplateChanged((IDataTemplate)oldValue2, (IDataTemplate)((newValue2 is IDataTemplate) ? newValue2 : null));
		}
		else if (property == (AvaloniaProperty)(object)LayoutProperty)
		{
			OnLayoutChanged(AvaloniaPropertyChangedExtensions.GetOldValue<FALayout>(args), AvaloniaPropertyChangedExtensions.GetNewValue<FALayout>(args));
		}
		else if (property == (AvaloniaProperty)(object)ItemTransitionProviderProperty)
		{
			OnTransitionProviderChanged(AvaloniaPropertyChangedExtensions.GetOldValue<FAItemCollectionTransitionProvider>(args), AvaloniaPropertyChangedExtensions.GetNewValue<FAItemCollectionTransitionProvider>(args));
		}
		else if (property == (AvaloniaProperty)(object)HorizontalCacheLengthProperty)
		{
			_viewportManager.HorizontalCacheLength = AvaloniaPropertyChangedExtensions.GetNewValue<double>(args);
		}
		else if (property == (AvaloniaProperty)(object)VerticalCacheLengthProperty)
		{
			_viewportManager.VerticalCacheLength = AvaloniaPropertyChangedExtensions.GetNewValue<double>(args);
		}
	}

	public int GetElementIndex(Control element)
	{
		return GetElementIndexImpl(element);
	}

	public Control TryGetElement(int index)
	{
		return GetElementFromIndexImpl(index);
	}

	public void PinElement(Control element)
	{
		_viewManager.UpdatePin(element, addPin: true);
	}

	public void UnpinElement(Control element)
	{
		_viewManager.UpdatePin(element, addPin: false);
	}

	public Control GetOrCreateElement(int index)
	{
		return GetOrCreateElementImpl(index);
	}

	internal void OnElementPrepared(Control element, int index, VirtualizationInfo vInfo)
	{
		_viewportManager.OnElementPrepared(element, vInfo);
		if (ElementPrepared != null)
		{
			if (_elementPreparedArgs == null)
			{
				_elementPreparedArgs = new FAItemsRepeaterElementPreparedEventArgs(element, index);
			}
			else
			{
				_elementPreparedArgs.Update(element, index);
			}
			ElementPrepared(this, _elementPreparedArgs);
		}
	}

	internal void OnElementClearing(Control element)
	{
		if (ElementClearing != null)
		{
			if (_elementClearingArgs == null)
			{
				_elementClearingArgs = new FAItemsRepeaterElementClearingEventArgs(element);
			}
			else
			{
				_elementClearingArgs.Update(element);
			}
			ElementClearing(this, _elementClearingArgs);
		}
	}

	internal void OnElementIndexChanged(Control element, int oldIndex, int newIndex)
	{
		if (ElementIndexChanged != null)
		{
			if (_elementIndexChangedArgs == null)
			{
				_elementIndexChangedArgs = new FAItemsRepeaterElementIndexChangedEventArgs(element, oldIndex, newIndex);
			}
			else
			{
				_elementIndexChangedArgs.Update(element, oldIndex, newIndex);
			}
			ElementIndexChanged(this, _elementIndexChangedArgs);
		}
	}

	internal Control GetElementImpl(int index, bool forceCreate, bool suppressAutoRecycle)
	{
		return _viewManager.GetElement(index, forceCreate, suppressAutoRecycle);
	}

	internal void ClearElementImpl(Control element)
	{
		bool isClearedDueToCollectionChange = IsProcessingCollectionChange && (_processingItemsSourceChange.Action == NotifyCollectionChangedAction.Remove || _processingItemsSourceChange.Action == NotifyCollectionChangedAction.Replace || _processingItemsSourceChange.Action == NotifyCollectionChangedAction.Reset);
		_viewManager.ClearElement(element, isClearedDueToCollectionChange);
		_viewportManager.OnElementCleared(element);
	}

	private int GetElementIndexImpl(Control element)
	{
		if ((object)VisualExtensions.GetVisualParent((Visual)(object)element) == this)
		{
			VirtualizationInfo vInfo = TryGetVirtualizationInfo(element);
			return _viewManager.GetElementIndex(vInfo);
		}
		return -1;
	}

	private Control GetElementFromIndexImpl(int index)
	{
		Control val = null;
		Controls children = ((Panel)this).Children;
		for (int i = 0; i < ((AvaloniaList<Control>)(object)children).Count; i++)
		{
			if (val != null)
			{
				break;
			}
			Control val2 = ((AvaloniaList<Control>)(object)children)[i];
			VirtualizationInfo virtualizationInfo = TryGetVirtualizationInfo(val2);
			if (virtualizationInfo != null && virtualizationInfo.IsRealized && virtualizationInfo.Index == index)
			{
				val = val2;
			}
		}
		return val;
	}

	private Control GetOrCreateElementImpl(int index)
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		if (ItemsSourceView == null)
		{
			throw new Exception("ItemsSource doesn't have a value");
		}
		if (index >= 0 && index >= ItemsSourceView.Count)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		if (_isLayoutInProgress)
		{
			throw new Exception("GetOrCreateElement invocation is not allowed during layout");
		}
		Control val = GetElementFromIndexImpl(index);
		bool flag = val == null;
		if (flag)
		{
			if (GetEffectiveLayout() == null)
			{
				throw new Exception("Cannot make an anchor when there is no attached layout");
			}
			val = GetLayoutContext().GetOrCreateElementAt(index);
			((Layoutable)val).Measure(Size.Infinity);
		}
		_viewportManager.OnMakeAnchor(val, flag);
		((Layoutable)this).InvalidateMeasure();
		return val;
	}

	private int Indent()
	{
		return 4;
	}

	private IEnumerable<Control> GetChildrenInTabFocusOrder()
	{
		return CreateChildrenInTabFocusOrderIterable();
	}

	private void OnBringIntoViewRequested(object sender, RequestBringIntoViewEventArgs args)
	{
		_viewportManager.OnBringIntoViewRequested(args);
	}

	private void OnRepeaterLoaded(object sender, RoutedEventArgs e)
	{
		if (_loadedCounter > _unloadedCounter)
		{
			((Layoutable)this).InvalidateMeasure();
			_viewportManager.ResetScrollers();
		}
		_loadedCounter++;
	}

	private void OnRepeaterUnloaded(object sender, RoutedEventArgs e)
	{
		_stackLayoutMeasureCounter = 0;
		_unloadedCounter++;
		if (_unloadedCounter == _loadedCounter)
		{
			_viewportManager.ResetScrollers();
		}
	}

	private void OnLayoutUpdated(object sender, EventArgs e)
	{
		_stackLayoutMeasureCounter = 0;
		EnsureDefaultLayoutState();
	}

	private void OnDataSourcePropertyChanged(FAItemsSourceView oldValue, FAItemsSourceView newValue)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (_isLayoutInProgress)
		{
			throw new Exception();
		}
		EnsureDefaultLayoutState();
		if (_itemsSourceView != null)
		{
			_itemsSourceView.CollectionChanged -= OnItemsSourceViewChanged;
		}
		_itemsSourceView = newValue;
		if (newValue != null)
		{
			_itemsSourceView.CollectionChanged += OnItemsSourceViewChanged;
		}
		FALayout effectiveLayout = GetEffectiveLayout();
		if (effectiveLayout == null)
		{
			return;
		}
		NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
		try
		{
			_processingItemsSourceChange = e;
			if (effectiveLayout is FAVirtualizingLayout fAVirtualizingLayout)
			{
				fAVirtualizingLayout.OnItemsChangedCore(GetLayoutContext(), newValue, e);
			}
			else if (Layout is FANonVirtualizingLayout)
			{
				Enumerator<Control> enumerator = ((AvaloniaList<Control>)(object)((Panel)this).Children).GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						Control current = enumerator.Current;
						if (GetVirtualizationInfo(current).IsRealized)
						{
							ClearElementImpl(current);
						}
					}
				}
				finally
				{
					((IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
				}
				((AvaloniaList<Control>)(object)((Panel)this).Children).Clear();
			}
			((Layoutable)this).InvalidateMeasure();
		}
		finally
		{
			_processingItemsSourceChange = null;
		}
	}

	private void OnItemTemplateChanged(IDataTemplate oldValue, IDataTemplate newValue)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		if (_isLayoutInProgress && oldValue != null)
		{
			throw new InvalidOperationException("ItemTemplate cannot be changed during layout.");
		}
		EnsureDefaultLayoutState();
		FALayout effectiveLayout = GetEffectiveLayout();
		if (effectiveLayout != null)
		{
			NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
			try
			{
				_processingItemsSourceChange = e;
				if (effectiveLayout is FAVirtualizingLayout fAVirtualizingLayout)
				{
					fAVirtualizingLayout.OnItemsChangedCore(GetLayoutContext(), newValue, e);
				}
				else if (effectiveLayout is FANonVirtualizingLayout)
				{
					Enumerator<Control> enumerator = ((AvaloniaList<Control>)(object)((Panel)this).Children).GetEnumerator();
					try
					{
						while (enumerator.MoveNext())
						{
							Control current = enumerator.Current;
							if (GetVirtualizationInfo(current).IsRealized)
							{
								ClearElementImpl(current);
							}
						}
					}
					finally
					{
						((IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
					}
				}
			}
			finally
			{
				_processingItemsSourceChange = null;
			}
		}
		_isItemTemplateEmpty = false;
		_itemTemplateWrapper = newValue as IFAElementFactory;
		if (_itemTemplateWrapper == null)
		{
			if (newValue != null)
			{
				_itemTemplateWrapper = new FAItemTemplateWrapper(newValue);
			}
			else if (newValue is FADataTemplateSelector selector)
			{
				_itemTemplateWrapper = new FAItemTemplateWrapper(selector);
			}
		}
		((Layoutable)this).InvalidateMeasure();
	}

	private void OnLayoutChanged(FALayout oldValue, FALayout newValue)
	{
		bool flag = !_wasLayoutChangedCalled;
		_wasLayoutChangedCalled = true;
		if (_isLayoutInProgress)
		{
			throw new InvalidOperationException("Layout cannot be changed during layout.");
		}
		_viewManager.OnLayoutChanging();
		_transitionManager.OnLayoutChanging();
		if ((oldValue == null) & !flag)
		{
			oldValue = GetDefaultLayout();
		}
		if (newValue == null)
		{
			newValue = GetDefaultLayout();
		}
		if (oldValue != null)
		{
			oldValue.UninitializeForContext(GetLayoutContext());
			newValue.MeasureInvalidated -= InvalidateMeasureForLayout;
			newValue.ArrangeInvalidated -= InvalidateArrangeForLayout;
			_stackLayoutMeasureCounter = 0;
			Controls children = ((Panel)this).Children;
			for (int i = 0; i < ((AvaloniaList<Control>)(object)children).Count; i++)
			{
				Control val = ((AvaloniaList<Control>)(object)children)[i];
				if (GetVirtualizationInfo(val).IsRealized)
				{
					ClearElementImpl(val);
				}
			}
			_layoutState = null;
		}
		if (newValue != null)
		{
			newValue.InitializeForContext(GetLayoutContext());
			newValue.MeasureInvalidated += InvalidateMeasureForLayout;
			newValue.ArrangeInvalidated += InvalidateArrangeForLayout;
			if (_ownsTransitionProvider)
			{
				_transitionManager.OnTransitionProviderChanged(newValue.CreateDefaultItemTransitionProvider());
			}
		}
		bool isVirtualizing = newValue != null && newValue is FAVirtualizingLayout;
		_viewportManager.OnLayoutChanged(isVirtualizing);
		((Layoutable)this).InvalidateMeasure();
	}

	private void OnTransitionProviderChanged(FAItemCollectionTransitionProvider _, FAItemCollectionTransitionProvider newValue)
	{
		_ownsTransitionProvider = false;
		_transitionManager.OnTransitionProviderChanged(newValue);
	}

	private void OnItemsSourceViewChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		if (_isLayoutInProgress)
		{
			throw new InvalidOperationException("Changes in data source are not allowed during layout.");
		}
		if (IsProcessingCollectionChange)
		{
			throw new InvalidOperationException("Changes in the data source are not allowed during another change in the data source.");
		}
		try
		{
			_processingItemsSourceChange = args;
			_transitionManager.OnItemsSourceChanged(sender, args);
			_viewManager.OnItemsSourceChanged(sender, args);
			FALayout effectiveLayout = GetEffectiveLayout();
			if (effectiveLayout != null)
			{
				if (effectiveLayout is FAVirtualizingLayout fAVirtualizingLayout)
				{
					fAVirtualizingLayout.OnItemsChangedCore(GetLayoutContext(), sender, args);
				}
				else
				{
					((Layoutable)this).InvalidateMeasure();
				}
			}
		}
		finally
		{
			_processingItemsSourceChange = null;
		}
	}

	private void InvalidateMeasureForLayout(FALayout sender, EventArgs args)
	{
		((Layoutable)this).InvalidateMeasure();
	}

	private void InvalidateArrangeForLayout(FALayout sender, EventArgs args)
	{
		((Layoutable)this).InvalidateArrange();
	}

	private void EnsureDefaultLayoutState()
	{
		if (!_wasLayoutChangedCalled)
		{
			FAVirtualizingLayout newValue = GetEffectiveLayout() as FAVirtualizingLayout;
			OnLayoutChanged(null, newValue);
		}
	}

	private FAVirtualizingLayoutContext GetLayoutContext()
	{
		if (_layoutContext == null)
		{
			_layoutContext = new RepeaterLayoutContext(this);
		}
		return _layoutContext;
	}

	private IEnumerable<Control> CreateChildrenInTabFocusOrderIterable()
	{
		if (((AvaloniaList<Control>)(object)((Panel)this).Children).Count == 0)
		{
			return new ChildrenInTabFocusOrderIterable(this);
		}
		return null;
	}

	private FALayout GetEffectiveLayout()
	{
		FALayout layout = Layout;
		if (layout != null)
		{
			return layout;
		}
		return GetDefaultLayout();
	}

	private FALayout GetDefaultLayout()
	{
		return new FAStackLayout();
	}

	internal static VirtualizationInfo GetVirtualizationInfo(Control c)
	{
		VirtualizationInfo virtualizationInfo = ((AvaloniaObject)c).GetValue<VirtualizationInfo>((StyledProperty<VirtualizationInfo>)(object)VirtualizationInfoProperty);
		if (virtualizationInfo == null)
		{
			virtualizationInfo = CreateAndInitializeVirtualizationInfo(c);
		}
		return virtualizationInfo;
	}

	internal static VirtualizationInfo TryGetVirtualizationInfo(Control c)
	{
		return GetVirtualizationInfo(c);
	}

	internal static VirtualizationInfo CreateAndInitializeVirtualizationInfo(Control element)
	{
		VirtualizationInfo virtualizationInfo = new VirtualizationInfo();
		((AvaloniaObject)element).SetValue<VirtualizationInfo>((StyledProperty<VirtualizationInfo>)(object)VirtualizationInfoProperty, virtualizationInfo, (BindingPriority)0);
		return virtualizationInfo;
	}

	internal void RaiseContainerContentChanging(FAContainerContentChangingEventArgs args)
	{
		ContainerContentChanging?.Invoke(this, args);
	}

	static FAItemsRepeater()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		ClearedElementsArrangePosition = new Point(-10000.0, -10000.0);
		InvalidRect = new Rect(-1.0, -1.0, -1.0, -1.0);
		VerticalCacheLengthProperty = AvaloniaProperty.Register<FAItemsRepeater, double>("VerticalCacheLength", 2.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);
		HorizontalCacheLengthProperty = AvaloniaProperty.Register<FAItemsRepeater, double>("HorizontalCacheLength", 2.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);
		LayoutProperty = AvaloniaProperty.Register<FAItemsRepeater, FALayout>("Layout", (FALayout)null, false, (BindingMode)1, (Func<FALayout, bool>)null, (Func<AvaloniaObject, FALayout, FALayout>)null, false);
		ItemsSourceProperty = AvaloniaProperty.Register<FAItemsRepeater, object>("ItemsSource", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);
		ItemTemplateProperty = ItemsControl.ItemTemplateProperty.AddOwner<FAItemsRepeater>((StyledPropertyMetadata<IDataTemplate>)null);
		ItemTransitionProviderProperty = AvaloniaProperty.Register<FAItemsRepeater, FAItemCollectionTransitionProvider>("ItemTransitionProvider", (FAItemCollectionTransitionProvider)null, false, (BindingMode)1, (Func<FAItemCollectionTransitionProvider, bool>)null, (Func<AvaloniaObject, FAItemCollectionTransitionProvider, FAItemCollectionTransitionProvider>)null, false);
		VirtualizationInfoProperty = AvaloniaProperty.RegisterAttached<FAItemsRepeater, Control, VirtualizationInfo>("VirtualizationInfo", (VirtualizationInfo)null, false, (BindingMode)1, (Func<VirtualizationInfo, bool>)null, (Func<AvaloniaObject, VirtualizationInfo, VirtualizationInfo>)null);
	}
}
