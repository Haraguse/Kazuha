using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Positions elements sequentially from left to right or top to bottom in a wrapping layout.
/// </summary>
public class FAUniformGridLayout : FAVirtualizingLayout, IOrientationBasedMeasures, IFlowLayoutAlgorithmDelegates
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAUniformGridLayout.Orientation" /> property
	/// </summary>
	public static readonly StyledProperty<Orientation> OrientationProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAUniformGridLayout.MinItemWidth" /> property
	/// </summary>
	public static readonly StyledProperty<double> MinItemWidthProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAUniformGridLayout.MinItemHeight" /> property
	/// </summary>
	public static readonly StyledProperty<double> MinItemHeightProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAUniformGridLayout.MinRowSpacing" /> property
	/// </summary>
	public static readonly StyledProperty<double> MinRowSpacingProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAUniformGridLayout.MinColumnSpacing" /> property
	/// </summary>
	public static readonly StyledProperty<double> MinColumnSpacingProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAUniformGridLayout.ItemsJustification" /> property
	/// </summary>
	public static readonly StyledProperty<FAUniformGridLayoutItemsJustification> ItemsJustificationProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAUniformGridLayout.ItemsStretch" /> property
	/// </summary>
	public static readonly StyledProperty<FAUniformGridLayoutItemsStretch> ItemsStretchProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAUniformGridLayout.MaximumRowsOrColumns" /> property
	/// </summary>
	public static readonly StyledProperty<int> MaximumRowsOrColumnsProperty;

	private double _minItemWidth = double.NaN;

	private double _minItemHeight = double.NaN;

	private double _minRowSpacing;

	private double _minColumnSpacing;

	private FAUniformGridLayoutItemsJustification _itemsJustification;

	private FAUniformGridLayoutItemsStretch _itemsStretch;

	private int _maximumRowsOrColumns = int.MaxValue;

	/// <summary>
	/// Gets or sets the axis along which items are laid out.
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

	/// <summary>
	/// Gets or sets the minimum width of each item.
	/// </summary>
	public double MinItemWidth
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(MinItemWidthProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(MinItemWidthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the minimum height of each item.
	/// </summary>
	public double MinItemHeight
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(MinItemHeightProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(MinItemHeightProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the minimum space between items on the vertical axis.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the minimum space between items on the horizontal axis.
	/// </summary>
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

	/// <summary>
	/// Gets or sets a value that indicates how items are aligned on the non-scrolling or non-virtualizing axis.
	/// </summary>
	public FAUniformGridLayoutItemsJustification ItemsJustification
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAUniformGridLayoutItemsJustification>(ItemsJustificationProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAUniformGridLayoutItemsJustification>(ItemsJustificationProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates how items are sized to fill the available space.
	/// </summary>
	public FAUniformGridLayoutItemsStretch ItemsStretch
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAUniformGridLayoutItemsStretch>(ItemsStretchProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAUniformGridLayoutItemsStretch>(ItemsStretchProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the maximum number of items rendered per row or column, based on the orientation of the UniformGridLayout.
	/// </summary>
	public int MaximumRowsOrColumns
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<int>(MaximumRowsOrColumnsProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<int>(MaximumRowsOrColumnsProperty, value, (BindingPriority)0);
		}
	}

	private ScrollOrientation ScrollOrientation { get; set; } = ScrollOrientation.Vertical;

	private double LineSpacing
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			if ((int)Orientation != 0)
			{
				return _minColumnSpacing;
			}
			return _minRowSpacing;
		}
	}

	private double MinItemSpacing
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			if ((int)Orientation != 0)
			{
				return _minRowSpacing;
			}
			return _minColumnSpacing;
		}
	}

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

	public FAUniformGridLayout()
	{
		base.LayoutId = "UniformGridLayout";
		UpdateIndexBasedLayoutOrientation((Orientation)0);
	}

	protected internal override void InitializeForContextCore(FAVirtualizingLayoutContext context)
	{
		object layoutState = context.LayoutState;
		UniformGridLayoutState uniformGridLayoutState = layoutState as UniformGridLayoutState;
		if (uniformGridLayoutState == null)
		{
			if (layoutState != null)
			{
				throw new InvalidOperationException("LayoutState must derive from UniformGridLayoutState");
			}
			uniformGridLayoutState = new UniformGridLayoutState();
		}
		uniformGridLayoutState.InitializeForContext(context, this);
	}

	protected internal override void UninitializeForContextCore(FAVirtualizingLayoutContext context)
	{
		GetAsGridState(context.LayoutState).UninitializeForContext(context);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		((AvaloniaObject)this).OnPropertyChanged(change);
		AvaloniaProperty property = change.Property;
		if (property == (AvaloniaProperty)(object)OrientationProperty)
		{
			Orientation newValue = AvaloniaPropertyChangedExtensions.GetNewValue<Orientation>(change);
			ScrollOrientation = (((int)newValue == 0) ? ScrollOrientation.Vertical : ScrollOrientation.Horizontal);
			UpdateIndexBasedLayoutOrientation(newValue);
		}
		else if (property == (AvaloniaProperty)(object)MinColumnSpacingProperty)
		{
			_minColumnSpacing = AvaloniaPropertyChangedExtensions.GetNewValue<double>(change);
		}
		else if (property == (AvaloniaProperty)(object)MinRowSpacingProperty)
		{
			_minRowSpacing = AvaloniaPropertyChangedExtensions.GetNewValue<double>(change);
		}
		else if (property == (AvaloniaProperty)(object)ItemsJustificationProperty)
		{
			_itemsJustification = AvaloniaPropertyChangedExtensions.GetNewValue<FAUniformGridLayoutItemsJustification>(change);
		}
		else if (property == (AvaloniaProperty)(object)ItemsStretchProperty)
		{
			_itemsStretch = AvaloniaPropertyChangedExtensions.GetNewValue<FAUniformGridLayoutItemsStretch>(change);
		}
		else if (property == (AvaloniaProperty)(object)MinItemWidthProperty)
		{
			_minItemWidth = AvaloniaPropertyChangedExtensions.GetNewValue<double>(change);
		}
		else if (property == (AvaloniaProperty)(object)MinItemHeightProperty)
		{
			_minItemHeight = AvaloniaPropertyChangedExtensions.GetNewValue<double>(change);
		}
		else if (property == (AvaloniaProperty)(object)MaximumRowsOrColumnsProperty)
		{
			_maximumRowsOrColumns = AvaloniaPropertyChangedExtensions.GetNewValue<int>(change);
		}
		InvalidateLayout();
	}

	protected internal override Size MeasureOverride(FAVirtualizingLayoutContext context, Size availableSize)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		GetAsGridState(context.LayoutState).EnsureElementSize(availableSize, context, _minItemWidth, _minItemHeight, _itemsStretch, Orientation, MinRowSpacing, MinColumnSpacing, _maximumRowsOrColumns);
		return GetFlowAlgorithm(context).Measure(availableSize, context, isWrapping: true, MinItemSpacing, LineSpacing, _maximumRowsOrColumns, ScrollOrientation, disableVirtualization: false, base.LayoutId);
	}

	protected internal override Size ArrangeOverride(FAVirtualizingLayoutContext context, Size finalSize)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		Size result = GetFlowAlgorithm(context).Arrange(finalSize, context, isWrapping: true, (FlowLayoutAlgorithm.LineAlignment)_itemsJustification, base.LayoutId);
		GetAsGridState(context.LayoutState).InvalidateElementSize();
		return result;
	}

	protected internal override void OnItemsChangedCore(FAVirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
	{
		GetFlowAlgorithm(context).OnItemsSourceChanged(source, args, context);
		InvalidateLayout();
	}

	Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(int index, Size availableSize, FAVirtualizingLayoutContext context)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		UniformGridLayoutState asGridState = GetAsGridState(context.LayoutState);
		return new Size(asGridState.EffectiveItemWidth, asGridState.EffectiveItemHeight);
	}

	Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(int index, Size measureSize, Size desiredSize, FAVirtualizingLayoutContext context)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		UniformGridLayoutState asGridState = GetAsGridState(context.LayoutState);
		return new Size(asGridState.EffectiveItemWidth, asGridState.EffectiveItemHeight);
	}

	bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace)
	{
		return remainingSpace < 0.0;
	}

	FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(Size availableSize, FAVirtualizingLayoutContext context)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		Rect layoutRectForDataIndex = default(Rect);
		((Rect)(ref layoutRectForDataIndex))._002Ector(double.NaN, double.NaN, double.NaN, double.NaN);
		int index = -1;
		int itemCount = context.ItemCount;
		Rect realizationRect = context.RealizationRect;
		if (itemCount > 0 && this.MajorSize(realizationRect) > 0.0)
		{
			Rect lastExtent = GetAsGridState(context.LayoutState).FlowAlgorithm.LastExtent;
			int itemsPerLine = GetItemsPerLine(availableSize, context);
			double num = (double)(itemCount / itemsPerLine) * GetMajorSizeWithSpacing(context);
			double num2 = this.MajorStart(realizationRect) - this.MajorStart(lastExtent);
			if (num2 + this.MajorSize(realizationRect) >= 0.0 && num2 <= num)
			{
				int num3 = (int)(Math.Max(0.0, this.MajorStart(realizationRect) - this.MajorStart(lastExtent)) / GetMajorSizeWithSpacing(context));
				index = Math.Max(0, Math.Min(itemCount - 1, num3 * itemsPerLine));
				layoutRectForDataIndex = GetLayoutRectForDataIndex(availableSize, index, lastExtent, context);
			}
		}
		return new FlowLayoutAnchorInfo
		{
			Index = index,
			Offset = this.MajorStart(layoutRectForDataIndex)
		};
	}

	FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(int targetIndex, Size availableSize, FAVirtualizingLayoutContext context)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		int index = -1;
		double offset = double.NaN;
		int itemCount = context.ItemCount;
		if (targetIndex >= 0 && targetIndex < itemCount)
		{
			int itemsPerLine = GetItemsPerLine(availableSize, context);
			int num = targetIndex / itemsPerLine * itemsPerLine;
			index = num;
			UniformGridLayoutState asGridState = GetAsGridState(context.LayoutState);
			offset = this.MajorStart(GetLayoutRectForDataIndex(availableSize, num, asGridState.FlowAlgorithm.LastExtent, context));
		}
		return new FlowLayoutAnchorInfo
		{
			Index = index,
			Offset = offset
		};
	}

	Rect IFlowLayoutAlgorithmDelegates.Algorithm_GetExtent(Size availableSize, FAVirtualizingLayoutContext context, Control firstRealized, int firstRealizedItemIndex, Rect firstRealizedLayoutBounds, Control lastRealized, int lastRealizedItemIndex, Rect lastRealiedLayoutBounds)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		Rect rect = default(Rect);
		int itemCount = context.ItemCount;
		double num = this.Minor(availableSize);
		int num2 = (int)Math.Min(Math.Max(1L, (!double.IsInfinity(num)) ? ((long)(uint)((num + MinItemSpacing) / GetMinorSizeWithSpacing(context))) : ((long)itemCount)), Math.Max(1L, _maximumRowsOrColumns));
		double majorSizeWithSpacing = GetMajorSizeWithSpacing(context);
		if (itemCount > 0)
		{
			this.SetMinorSize(ref rect, (!double.IsInfinity(num) && _itemsStretch == FAUniformGridLayoutItemsStretch.Fill) ? num : Math.Max(0.0, (double)num2 * GetMinorSizeWithSpacing(context) - MinItemSpacing));
			this.SetMajorSize(ref rect, Math.Max(0.0, (double)(itemCount / num2) * majorSizeWithSpacing - LineSpacing));
			if (firstRealized != null)
			{
				this.SetMajorStart(ref rect, this.MajorStart(firstRealizedLayoutBounds) - (double)(firstRealizedItemIndex / num2) * majorSizeWithSpacing);
				int num3 = itemCount - lastRealizedItemIndex - 1;
				this.SetMajorSize(ref rect, this.MajorEnd(lastRealiedLayoutBounds) - this.MajorStart(rect) + (double)(num3 / num2) * majorSizeWithSpacing);
			}
		}
		return rect;
	}

	void IFlowLayoutAlgorithmDelegates.Algorithm_OnElementMeasured(Control element, int index, Size availableSize, Size measureSize, Size desiredSize, Size provisionalArrangeSize, FAVirtualizingLayoutContext context)
	{
	}

	void IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged(int startIndex, int countInLine, double lineSize, FAVirtualizingLayoutContext context)
	{
	}

	private int GetItemsPerLine(Size availableSize, FAVirtualizingLayoutContext context)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return (int)Math.Min(Math.Max(1u, (uint)((this.Minor(availableSize) + MinItemSpacing) / GetMinorSizeWithSpacing(context))), Math.Max(1L, _maximumRowsOrColumns));
	}

	private double GetMinorSizeWithSpacing(FAVirtualizingLayoutContext context)
	{
		double minItemSpacing = MinItemSpacing;
		UniformGridLayoutState asGridState = GetAsGridState(context.LayoutState);
		if (ScrollOrientation != ScrollOrientation.Vertical)
		{
			return asGridState.EffectiveItemHeight + minItemSpacing;
		}
		return asGridState.EffectiveItemWidth + minItemSpacing;
	}

	private double GetMajorSizeWithSpacing(FAVirtualizingLayoutContext context)
	{
		double lineSpacing = LineSpacing;
		UniformGridLayoutState asGridState = GetAsGridState(context.LayoutState);
		if (ScrollOrientation != ScrollOrientation.Vertical)
		{
			return asGridState.EffectiveItemWidth + lineSpacing;
		}
		return asGridState.EffectiveItemHeight + lineSpacing;
	}

	private Rect GetLayoutRectForDataIndex(Size availableSize, int index, Rect lastExtent, FAVirtualizingLayoutContext context)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		int itemsPerLine = GetItemsPerLine(availableSize, context);
		int num = index / itemsPerLine;
		int num2 = index - num * itemsPerLine;
		UniformGridLayoutState asGridState = GetAsGridState(context.LayoutState);
		return this.MinorMajorRect((double)num2 * GetMinorSizeWithSpacing(context) + this.MinorStart(lastExtent), (double)num * GetMajorSizeWithSpacing(context) + this.MajorStart(lastExtent), (ScrollOrientation == ScrollOrientation.Vertical) ? asGridState.EffectiveItemWidth : asGridState.EffectiveItemHeight, (ScrollOrientation == ScrollOrientation.Vertical) ? asGridState.EffectiveItemHeight : asGridState.EffectiveItemWidth);
	}

	private UniformGridLayoutState GetAsGridState(object state)
	{
		return state as UniformGridLayoutState;
	}

	private FlowLayoutAlgorithm GetFlowAlgorithm(FAVirtualizingLayoutContext context)
	{
		return GetAsGridState(context.LayoutState).FlowAlgorithm;
	}

	private void InvalidateLayout()
	{
		InvalidateMeasure();
	}

	private void UpdateIndexBasedLayoutOrientation(Orientation orientation)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		base.IndexBasedLayoutOrientation = (((int)orientation != 0) ? FAIndexBasedLayoutOrientation.TopToBottom : FAIndexBasedLayoutOrientation.LeftToRight);
	}

	static FAUniformGridLayout()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		OrientationProperty = StackPanel.OrientationProperty.AddOwner<FAUniformGridLayout>(new StyledPropertyMetadata<Orientation>(Optional<Orientation>.op_Implicit((Orientation)0), (BindingMode)0, (Func<AvaloniaObject, Orientation, Orientation>)null, false));
		MinItemWidthProperty = AvaloniaProperty.Register<FAUniformGridLayout, double>("MinItemWidth", 0.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);
		MinItemHeightProperty = AvaloniaProperty.Register<FAUniformGridLayout, double>("MinItemHeight", 0.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);
		MinRowSpacingProperty = AvaloniaProperty.Register<FAUniformGridLayout, double>("MinRowSpacing", 0.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);
		MinColumnSpacingProperty = AvaloniaProperty.Register<FAUniformGridLayout, double>("MinColumnSpacing", 0.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);
		ItemsJustificationProperty = AvaloniaProperty.Register<FAUniformGridLayout, FAUniformGridLayoutItemsJustification>("ItemsJustification", FAUniformGridLayoutItemsJustification.Start, false, (BindingMode)1, (Func<FAUniformGridLayoutItemsJustification, bool>)null, (Func<AvaloniaObject, FAUniformGridLayoutItemsJustification, FAUniformGridLayoutItemsJustification>)null, false);
		ItemsStretchProperty = AvaloniaProperty.Register<FAUniformGridLayout, FAUniformGridLayoutItemsStretch>("ItemsStretch", FAUniformGridLayoutItemsStretch.None, false, (BindingMode)1, (Func<FAUniformGridLayoutItemsStretch, bool>)null, (Func<AvaloniaObject, FAUniformGridLayoutItemsStretch, FAUniformGridLayoutItemsStretch>)null, false);
		MaximumRowsOrColumnsProperty = AvaloniaProperty.Register<FAUniformGridLayout, int>("MaximumRowsOrColumns", -1, false, (BindingMode)1, (Func<int, bool>)null, (Func<AvaloniaObject, int, int>)null, false);
	}
}
