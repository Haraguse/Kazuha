using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

public class FAFlowLayout : FAVirtualizingLayout, IOrientationBasedMeasures, IFlowLayoutAlgorithmDelegates
{
	public static readonly StyledProperty<FAFlowLayoutLineAlignment> LineAlignmentProperty = AvaloniaProperty.Register<FAFlowLayout, FAFlowLayoutLineAlignment>("LineAlignment", FAFlowLayoutLineAlignment.Start, false, (BindingMode)1, (Func<FAFlowLayoutLineAlignment, bool>)null, (Func<AvaloniaObject, FAFlowLayoutLineAlignment, FAFlowLayoutLineAlignment>)null, false);

	public static readonly StyledProperty<double> MinColumnSpacingProperty = AvaloniaProperty.Register<FAFlowLayout, double>("MinColumnSpacing", 0.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);

	public static readonly StyledProperty<double> MinRowSpacingProperty = AvaloniaProperty.Register<FAFlowLayout, double>("MinRowSpacing", 0.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);

	public static readonly StyledProperty<Orientation> OrientationProperty = StackPanel.OrientationProperty.AddOwner<FAFlowLayout>((StyledPropertyMetadata<Orientation>)null);

	private FlowLayoutAlgorithm.LineAlignment _lineAlignment;

	private double _minColumnSpacing;

	private double _minRowSpacing;

	private Orientation _orientation;

	public FAFlowLayoutLineAlignment LineAlignment
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAFlowLayoutLineAlignment>(LineAlignmentProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAFlowLayoutLineAlignment>(LineAlignmentProperty, value, (BindingPriority)0);
		}
	}

	public double MinColumnSpacing
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(MinColumnSpacingProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(MinColumnSpacingProperty, value, (BindingPriority)0);
		}
	}

	public double MinRowSpacing
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(MinRowSpacingProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(MinRowSpacingProperty, value, (BindingPriority)0);
		}
	}

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

	private ScrollOrientation ScrollOrientation { get; set; }

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

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		((AvaloniaObject)this).OnPropertyChanged(args);
		AvaloniaProperty property = args.Property;
		if (property == (AvaloniaProperty)(object)OrientationProperty)
		{
			ScrollOrientation scrollOrientation = (((int)AvaloniaPropertyChangedExtensions.GetNewValue<Orientation>(args) == 0) ? ScrollOrientation.Vertical : ScrollOrientation.Horizontal);
			ScrollOrientation = scrollOrientation;
		}
		else if (property == (AvaloniaProperty)(object)MinColumnSpacingProperty)
		{
			_minColumnSpacing = AvaloniaPropertyChangedExtensions.GetNewValue<double>(args);
		}
		else if (property == (AvaloniaProperty)(object)MinRowSpacingProperty)
		{
			_minRowSpacing = AvaloniaPropertyChangedExtensions.GetNewValue<double>(args);
		}
		else if (property == (AvaloniaProperty)(object)LineAlignmentProperty)
		{
			_lineAlignment = (FlowLayoutAlgorithm.LineAlignment)AvaloniaPropertyChangedExtensions.GetNewValue<FAFlowLayoutLineAlignment>(args);
		}
	}

	protected internal override void InitializeForContextCore(FAVirtualizingLayoutContext context)
	{
		object layoutState = context.LayoutState;
		FlowLayoutState flowLayoutState = null;
		if (layoutState != null)
		{
			flowLayoutState = GetAsFlowState(layoutState);
		}
		if (flowLayoutState == null)
		{
			if (layoutState != null)
			{
				throw new Exception();
			}
			flowLayoutState = new FlowLayoutState();
		}
		flowLayoutState.InitializeForContext(context, this);
	}

	protected internal override void UninitializeForContextCore(FAVirtualizingLayoutContext context)
	{
		GetAsFlowState(context.LayoutState).UninitializeForContext(context);
	}

	protected internal override Size MeasureOverride(FAVirtualizingLayoutContext context, Size availableSize)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		return GetFlowAlgorithm(context).Measure(availableSize, context, isWrapping: true, MinItemSpacing(), LineSpacing(), int.MaxValue, ScrollOrientation, disableVirtualization: false, base.LayoutId);
	}

	protected internal override Size ArrangeOverride(FAVirtualizingLayoutContext context, Size finalSize)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return GetFlowAlgorithm(context).Arrange(finalSize, context, isWrapping: true, _lineAlignment, base.LayoutId);
	}

	protected internal override void OnItemsChangedCore(FAVirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
	{
		GetFlowAlgorithm(context).OnItemsSourceChanged(source, args, context);
		InvalidateLayout();
	}

	private FlowLayoutState GetAsFlowState(object state)
	{
		return state as FlowLayoutState;
	}

	private void InvalidateLayout()
	{
		InvalidateMeasure();
	}

	private FlowLayoutAlgorithm GetFlowAlgorithm(FAVirtualizingLayoutContext context)
	{
		return GetAsFlowState(context.LayoutState).FlowAlgorithm;
	}

	private bool DoesRealizationWindowOverlapExtent(Rect realizationWindow, Rect extent)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (this.MajorEnd(realizationWindow) >= this.MajorStart(extent))
		{
			return this.MajorStart(realizationWindow) <= this.MajorEnd(extent);
		}
		return false;
	}

	private double LineSpacing()
	{
		if (ScrollOrientation != ScrollOrientation.Vertical)
		{
			return _minColumnSpacing;
		}
		return _minRowSpacing;
	}

	private double MinItemSpacing()
	{
		if (ScrollOrientation != ScrollOrientation.Vertical)
		{
			return _minRowSpacing;
		}
		return _minColumnSpacing;
	}

	Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(int index, Size availableSize, FAVirtualizingLayoutContext context)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return availableSize;
	}

	Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(int index, Size measureSize, Size desiredSize, FAVirtualizingLayoutContext context)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return desiredSize;
	}

	bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace)
	{
		return remainingSpace < 0.0;
	}

	FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(Size availableSize, FAVirtualizingLayoutContext context)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		int index = -1;
		double offset = double.NaN;
		int itemCount = context.ItemCount;
		if (itemCount > 0)
		{
			Rect realizationRect = context.RealizationRect;
			object layoutState = context.LayoutState;
			FlowLayoutState asFlowState = GetAsFlowState(layoutState);
			Rect lastExtent = asFlowState.FlowAlgorithm.LastExtent;
			double avgCountInLine = 0.0;
			double num = GetAverageLineInfo(availableSize, context, asFlowState, ref avgCountInLine) + LineSpacing();
			double majorSize = ((this.MajorSize(lastExtent) == 0.0) ? ((double)itemCount / avgCountInLine * num) : this.MajorSize(lastExtent));
			if (itemCount > 0 && this.MajorSize(realizationRect) > 0.0 && DoesRealizationWindowOverlapExtent(realizationRect, this.MinorMajorRect(this.MinorStart(lastExtent), this.MajorStart(lastExtent), this.Minor(availableSize), majorSize)))
			{
				double num2 = this.MajorStart(realizationRect) - this.MajorStart(lastExtent);
				int num3 = Math.Max(0, (int)(num2 / num));
				index = (int)((double)num3 * avgCountInLine);
				index = FAMathHelpers.Clamp(index, 0, itemCount - 1);
				offset = (double)num3 * num + this.MajorStart(lastExtent);
			}
		}
		return new FlowLayoutAnchorInfo
		{
			Index = index,
			Offset = offset
		};
	}

	FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(int targetIndex, Size availableSize, FAVirtualizingLayoutContext context)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		double offset = double.NaN;
		int index = -1;
		int itemCount = context.ItemCount;
		if (targetIndex >= 0 && targetIndex < itemCount)
		{
			index = targetIndex;
			object layoutState = context.LayoutState;
			FlowLayoutState asFlowState = GetAsFlowState(layoutState);
			double avgCountInLine = 0.0;
			double num = GetAverageLineInfo(availableSize, context, asFlowState, ref avgCountInLine) + LineSpacing();
			offset = (double)(int)((double)targetIndex / avgCountInLine) * num + this.MajorStart(asFlowState.FlowAlgorithm.LastExtent);
		}
		return new FlowLayoutAnchorInfo
		{
			Index = index,
			Offset = offset
		};
	}

	Rect IFlowLayoutAlgorithmDelegates.Algorithm_GetExtent(Size availableSize, FAVirtualizingLayoutContext context, Control firstRealized, int firstRealizedItemIndex, Rect firstRealizedLayoutBounds, Control lastRealized, int lastRealizedItemIndex, Rect lastRealizedLayoutBounds)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		Rect rect = default(Rect);
		int itemCount = context.ItemCount;
		if (itemCount > 0)
		{
			double num = this.Minor(availableSize);
			object layoutState = context.LayoutState;
			FlowLayoutState asFlowState = GetAsFlowState(layoutState);
			double avgCountInLine = 0.0;
			double num2 = GetAverageLineInfo(availableSize, context, asFlowState, ref avgCountInLine) + LineSpacing();
			if (firstRealized == null)
			{
				double num3 = LineSpacing();
				double num4 = MinItemSpacing();
				int num5 = (int)Math.Ceiling((double)itemCount / avgCountInLine);
				return (!double.IsInfinity(num)) ? this.MinorMajorRect(0.0, 0.0, num, Math.Max(0.0, (double)num5 * num2 - num3)) : this.MinorMajorRect(0.0, 0.0, Math.Max(0.0, (this.Minor(asFlowState.SpecialElementDesiredSize) + num4) * (double)itemCount - num4), Math.Max(0.0, num2 - num3));
			}
			int num6 = (int)((double)firstRealizedItemIndex / avgCountInLine);
			double value = this.MajorStart(firstRealizedLayoutBounds) - (double)num6 * num2;
			this.SetMajorStart(ref rect, value);
			int num7 = (int)((double)(itemCount - lastRealizedItemIndex - 1) / avgCountInLine);
			double value2 = this.MajorEnd(lastRealizedLayoutBounds) - this.MajorStart(rect) + (double)num7 * num2;
			this.SetMajorSize(ref rect, value2);
			this.SetMinorSize(ref rect, (!double.IsInfinity(num)) ? num : Math.Max(0.0, this.MinorEnd(lastRealizedLayoutBounds)));
		}
		return rect;
	}

	void IFlowLayoutAlgorithmDelegates.Algorithm_OnElementMeasured(Control element, int index, Size availableSize, Size measureSize, Size desiredSize, Size provisionalArrangeSize, FAVirtualizingLayoutContext context)
	{
	}

	void IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged(int startIndex, int countInLine, double lineSize, FAVirtualizingLayoutContext context)
	{
		GetAsFlowState(context.LayoutState).OnLineArranged(startIndex, countInLine, lineSize, context);
	}

	private double GetAverageLineInfo(Size availableSize, FAVirtualizingLayoutContext context, FlowLayoutState flowState, ref double avgCountInLine)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		avgCountInLine = 1.0;
		if (flowState.TotalLinesMeasured == 0)
		{
			Control orCreateElementAt = context.GetOrCreateElementAt(0, FAElementRealizationOptions.ForceCreate | FAElementRealizationOptions.SuppressAutoRecycle);
			Size val = flowState.FlowAlgorithm.MeasureElement(orCreateElementAt, 0, availableSize, context);
			context.RecycleElement(orCreateElementAt);
			int countInLine = Math.Max(1, (int)(this.Major(availableSize) / this.Minor(val)));
			flowState.OnLineArranged(0, countInLine, this.Major(val), context);
			flowState.SpecialElementDesiredSize = val;
		}
		avgCountInLine = Math.Max(1, (int)(flowState.TotalItemsPerLine / (double)flowState.TotalLinesMeasured));
		return Math.Round(flowState.TotalLineSize / (double)flowState.TotalLinesMeasured);
	}
}
