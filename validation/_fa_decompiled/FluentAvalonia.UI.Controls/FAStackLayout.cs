using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents an <i>attached layout</i> that arranges child elements into a single line that can be
/// oriented horizontally or vertically
/// </summary>
public class FAStackLayout : FAVirtualizingLayout, IFlowLayoutAlgorithmDelegates, IOrientationBasedMeasures
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAStackLayout.Spacing" /> property
	/// </summary>
	public static readonly StyledProperty<double> SpacingProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAStackLayout.Orientation" /> property
	/// </summary>
	public static readonly StyledProperty<Orientation> OrientationProperty;

	private double _itemSpacing;

	/// <summary>
	/// Gets or sets a uniform distance (in pixels) between stacked items. It is applied
	/// in the direction of the StackLayout's Orientation
	/// </summary>
	public double Spacing
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(SpacingProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(SpacingProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the dimension by which child elements are stacked
	/// </summary>
	public Orientation Orientation
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<Orientation>(OrientationProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<Orientation>(OrientationProperty, value, (BindingPriority)0);
		}
	}

	public bool DisableVirtualization { get; set; }

	private ScrollOrientation ScrollOrientation { get; set; } = ScrollOrientation.Vertical;

	ScrollOrientation IOrientationBasedMeasures.ScrollOrientation
	{
		get
		{
			return ScrollOrientation;
		}
		set
		{
			ScrollOrientation = value;
		}
	}

	public FAStackLayout()
	{
		base.LayoutId = "StackLayout";
		UpdateIndexBasedLayoutOrientation((Orientation)1);
	}

	protected internal override void InitializeForContextCore(FAVirtualizingLayoutContext context)
	{
		object layoutState = context.LayoutState;
		StackLayoutState stackLayoutState = null;
		if (layoutState != null)
		{
			stackLayoutState = GetAsStackState(layoutState);
		}
		if (stackLayoutState == null)
		{
			if (layoutState != null)
			{
				throw new InvalidOperationException("LayoutState must derive from StackLayoutState.");
			}
			stackLayoutState = new StackLayoutState();
		}
		stackLayoutState.InitializeForContext(context, this);
	}

	protected internal override void UninitializeForContextCore(FAVirtualizingLayoutContext context)
	{
		GetAsStackState(context.LayoutState)?.UninitializeForContext(context);
	}

	protected internal override Size MeasureOverride(FAVirtualizingLayoutContext context, Size availableSize)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		if (context.LayoutState == null)
		{
			return default(Size);
		}
		GetAsStackState(context.LayoutState).OnMeasureStart();
		return GetFlowAlgorithm(context).Measure(availableSize, context, isWrapping: false, 0.0, _itemSpacing, int.MaxValue, ScrollOrientation, DisableVirtualization, base.LayoutId);
	}

	protected internal override Size ArrangeOverride(FAVirtualizingLayoutContext context, Size finalSize)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		if (context.LayoutState == null)
		{
			return default(Size);
		}
		return GetFlowAlgorithm(context).Arrange(finalSize, context, isWrapping: false, FlowLayoutAlgorithm.LineAlignment.Start, base.LayoutId);
	}

	protected internal override void OnItemsChangedCore(FAVirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
	{
		if (context.LayoutState != null)
		{
			GetAsStackState(context.LayoutState).FlowAlgorithm.OnItemsSourceChanged(source, args, context);
		}
		InvalidateLayout();
	}

	private FlowLayoutAnchorInfo GetAnchorForRealizationRect(Size availableSize, FAVirtualizingLayoutContext context)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		int index = -1;
		double offset = double.NaN;
		int itemCount = context.ItemCount;
		if (itemCount > 0)
		{
			Rect realizationRect = context.RealizationRect;
			StackLayoutState asStackState = GetAsStackState(context.LayoutState);
			Rect lastExtent = asStackState.FlowAlgorithm.LastExtent;
			double num = GetAverageElementSize(availableSize, context, asStackState) + _itemSpacing;
			double num2 = this.MajorStart(realizationRect) - this.MajorStart(lastExtent);
			double num3 = ((this.MajorSize(lastExtent) == 0.0) ? Math.Max(0.0, num * (double)itemCount - _itemSpacing) : this.MajorSize(lastExtent));
			if (itemCount > 0 && this.MajorSize(realizationRect) >= 0.0 && num2 + this.MajorSize(realizationRect) >= 0.0 && num2 <= num3)
			{
				index = (int)(num2 / num);
				offset = (double)index * num + this.MajorStart(lastExtent);
				index = Math.Max(0, Math.Min(itemCount - 1, index));
			}
		}
		return new FlowLayoutAnchorInfo
		{
			Index = index,
			Offset = offset
		};
	}

	private Rect GetExtent(Size availableSize, FAVirtualizingLayoutContext context, Control firstRealized, int firstRealizedItemIndex, Rect firstRealizedLayoutBounds, Control lastRealized, int lastRealizedItemIndex, Rect lastRealizedLayoutBounds)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		Rect rect = default(Rect);
		int itemCount = context.ItemCount;
		StackLayoutState asStackState = GetAsStackState(context.LayoutState);
		double num = GetAverageElementSize(availableSize, context, asStackState) + _itemSpacing;
		this.SetMinorSize(ref rect, asStackState.MaxArrangeBounds);
		this.SetMajorSize(ref rect, Math.Max(0.0, (double)itemCount * num - _itemSpacing));
		if (itemCount > 0 && firstRealized != null)
		{
			this.SetMajorStart(ref rect, this.MajorStart(firstRealizedLayoutBounds) - (double)firstRealizedItemIndex * num);
			int num2 = itemCount - lastRealizedItemIndex - 1;
			this.SetMajorSize(ref rect, this.MajorEnd(lastRealizedLayoutBounds) - this.MajorStart(rect) + (double)num2 * num);
		}
		return rect;
	}

	private void OnElementMeasured(Control element, int index, Size availableSize, Size measureSize, Size desiredSize, Size provisionalArrangeSize, FAVirtualizingLayoutContext context)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (context != null)
		{
			GetAsStackState(context.LayoutState).OnElementMeasured(index, this.Major(provisionalArrangeSize), this.Minor(provisionalArrangeSize));
		}
	}

	Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(int index, Size availableSize, FAVirtualizingLayoutContext context)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return availableSize;
	}

	Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(int index, Size measureSize, Size desiredSize, FAVirtualizingLayoutContext context)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		double num = this.Minor(measureSize);
		return this.MinorMajorSize((!double.IsInfinity(num)) ? Math.Max(num, this.Minor(desiredSize)) : this.Minor(desiredSize), this.Major(desiredSize));
	}

	bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace)
	{
		return true;
	}

	FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(Size availableSize, FAVirtualizingLayoutContext context)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return GetAnchorForRealizationRect(availableSize, context);
	}

	FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(int targetIndex, Size availableSize, FAVirtualizingLayoutContext context)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		double offset = double.NaN;
		int num = -1;
		int itemCount = context.ItemCount;
		if (targetIndex >= 0 && targetIndex < itemCount)
		{
			num = targetIndex;
			StackLayoutState asStackState = GetAsStackState(context.LayoutState);
			double num2 = GetAverageElementSize(availableSize, context, asStackState) + _itemSpacing;
			offset = (double)num * num2 + this.MajorStart(asStackState.FlowAlgorithm.LastExtent);
		}
		return new FlowLayoutAnchorInfo
		{
			Index = num,
			Offset = offset
		};
	}

	Rect IFlowLayoutAlgorithmDelegates.Algorithm_GetExtent(Size availableSize, FAVirtualizingLayoutContext context, Control firstRealized, int firstRealizedIndex, Rect firstRealizedLayoutBounds, Control lastRealized, int lastRealizedItemIndex, Rect lastRealizedLayoutBounds)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return GetExtent(availableSize, context, firstRealized, firstRealizedIndex, firstRealizedLayoutBounds, lastRealized, lastRealizedItemIndex, lastRealizedLayoutBounds);
	}

	void IFlowLayoutAlgorithmDelegates.Algorithm_OnElementMeasured(Control element, int index, Size availableSize, Size measureSize, Size desiredSize, Size provisionalArrangeSize, FAVirtualizingLayoutContext context)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		OnElementMeasured(element, index, availableSize, measureSize, desiredSize, provisionalArrangeSize, context);
	}

	void IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged(int startIndex, int countInLine, double lineSize, FAVirtualizingLayoutContext context)
	{
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		((AvaloniaObject)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)OrientationProperty)
		{
			Orientation newValue = AvaloniaPropertyChangedExtensions.GetNewValue<Orientation>(change);
			ScrollOrientation = (((int)newValue != 0) ? ScrollOrientation.Vertical : ScrollOrientation.Horizontal);
			UpdateIndexBasedLayoutOrientation(newValue);
		}
		else if (change.Property == (AvaloniaProperty)(object)SpacingProperty)
		{
			_itemSpacing = AvaloniaPropertyChangedExtensions.GetNewValue<double>(change);
		}
		InvalidateLayout();
	}

	private double GetAverageElementSize(Size availableSize, FAVirtualizingLayoutContext context, StackLayoutState state)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		double result = 0.0;
		if (context.ItemCount > 0)
		{
			if (state.TotalElementsMeasured == 0)
			{
				Control orCreateElementAt = context.GetOrCreateElementAt(0, FAElementRealizationOptions.ForceCreate | FAElementRealizationOptions.SuppressAutoRecycle);
				state.FlowAlgorithm.MeasureElement(orCreateElementAt, 0, availableSize, context);
				context.RecycleElement(orCreateElementAt);
			}
			result = Math.Round(state.TotalElementSize / (double)state.TotalElementsMeasured);
		}
		return result;
	}

	private void UpdateIndexBasedLayoutOrientation(Orientation orientation)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		base.IndexBasedLayoutOrientation = (((int)orientation != 0) ? FAIndexBasedLayoutOrientation.TopToBottom : FAIndexBasedLayoutOrientation.LeftToRight);
	}

	private void InvalidateLayout()
	{
		InvalidateMeasure();
	}

	private FlowLayoutAlgorithm GetFlowAlgorithm(FAVirtualizingLayoutContext context)
	{
		return GetAsStackState(context.LayoutState).FlowAlgorithm;
	}

	private StackLayoutState GetAsStackState(object state)
	{
		return state as StackLayoutState;
	}

	static FAStackLayout()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		SpacingProperty = StackPanel.SpacingProperty.AddOwner<FAStackLayout>((StyledPropertyMetadata<double>)null);
		OrientationProperty = StackPanel.OrientationProperty.AddOwner<FAStackLayout>(new StyledPropertyMetadata<Orientation>(Optional<Orientation>.op_Implicit((Orientation)1), (BindingMode)0, (Func<AvaloniaObject, Orientation, Orientation>)null, false));
	}
}
