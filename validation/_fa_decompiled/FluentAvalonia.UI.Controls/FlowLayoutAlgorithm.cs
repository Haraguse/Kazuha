using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace FluentAvalonia.UI.Controls;

internal class FlowLayoutAlgorithm : IOrientationBasedMeasures
{
	public enum LineAlignment
	{
		Start,
		Center,
		End,
		SpaceAround,
		SpaceBetween,
		SpaceEvenly
	}

	private readonly ElementManager _elementManager = new ElementManager();

	private Size _lastAvailableSize;

	private double _lastItemSpacing;

	private bool _collectionChangePending;

	private FAVirtualizingLayoutContext _context;

	private IFlowLayoutAlgorithmDelegates _algorithmCallbacks;

	private Rect _lastExtent;

	private int _firstRealizedDataIndexInsideRealizationWindow = -1;

	private int _lastRealizedDataIndexInsideRealizationWindow = -1;

	private bool _scrollOrientationSameAsFlow;

	public ScrollOrientation ScrollOrientation { get; set; }

	public Rect LastExtent
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _lastExtent;
		}
	}

	public void InitializeForContext(FAVirtualizingLayoutContext context, IFlowLayoutAlgorithmDelegates callbacks)
	{
		_algorithmCallbacks = callbacks;
		_context = context;
		_elementManager.SetContext(context);
	}

	public void UninitializeForContext(FAVirtualizingLayoutContext context)
	{
		if (IsVirtualizingContext())
		{
			_elementManager.ClearRealizedRange();
		}
		context.LayoutStateCore = null;
	}

	public Size Measure(Size availableSize, FAVirtualizingLayoutContext context, bool isWrapping, double minItemSpacing, double lineSpacing, int maxItemsPerLine, ScrollOrientation orientation, bool disableVirtualization, string layoutId)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		ScrollOrientation = orientation;
		_scrollOrientationSameAsFlow = double.IsInfinity(this.Minor(availableSize));
		RealizationRect();
		int recommendedAnchorIndex = _context.RecommendedAnchorIndex;
		if (_elementManager.IsIndexValidInData(recommendedAnchorIndex) && !_elementManager.IsDataIndexRealized(recommendedAnchorIndex))
		{
			MakeAnchor(_context, recommendedAnchorIndex, availableSize);
		}
		if (!disableVirtualization)
		{
			_elementManager.OnBeginMeasure(orientation);
		}
		int anchorIndex = GetAnchorIndex(availableSize, isWrapping, minItemSpacing, disableVirtualization, layoutId);
		Generate(GenerateDirection.Forward, anchorIndex, availableSize, minItemSpacing, lineSpacing, maxItemsPerLine, disableVirtualization, layoutId);
		Generate(GenerateDirection.Backward, anchorIndex, availableSize, minItemSpacing, lineSpacing, maxItemsPerLine, disableVirtualization, layoutId);
		if (isWrapping && IsReflowRequired())
		{
			Rect rect = _elementManager.GetLayoutBoundsForRealizedIndex(0);
			this.SetMinorStart(ref rect, 0.0);
			_elementManager.SetLayoutBoundsForRealizedIndex(0, rect);
			Generate(GenerateDirection.Forward, 0, availableSize, minItemSpacing, lineSpacing, maxItemsPerLine, disableVirtualization, layoutId);
		}
		RaiseLineArranged();
		_collectionChangePending = false;
		_lastExtent = EstimateExtent(availableSize, layoutId);
		SetLayoutOrigin();
		return ((Rect)(ref _lastExtent)).Size;
	}

	public Size Arrange(Size finalSize, FAVirtualizingLayoutContext context, bool isWrapping, LineAlignment lineAlignment, string layoutId)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		ArrangeVirtualizingLayout(finalSize, lineAlignment, isWrapping, layoutId);
		return new Size(Math.Max(((Size)(ref finalSize)).Width, ((Rect)(ref _lastExtent)).Width), Math.Max(((Size)(ref finalSize)).Height, ((Rect)(ref _lastExtent)).Height));
	}

	public void MakeAnchor(FAVirtualizingLayoutContext context, int index, Size availableSize)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		_elementManager.ClearRealizedRange();
		for (int i = _algorithmCallbacks.Algorithm_GetAnchorForTargetElement(index, availableSize, context).Index; i < index + 1; i++)
		{
			Control orCreateElementAt = context.GetOrCreateElementAt(i, FAElementRealizationOptions.ForceCreate | FAElementRealizationOptions.SuppressAutoRecycle);
			((Layoutable)orCreateElementAt).Measure(_algorithmCallbacks.Algorithm_GetMeasureSize(i, availableSize, context));
			_elementManager.Add(orCreateElementAt, i);
		}
	}

	public void OnItemsSourceChanged(object source, NotifyCollectionChangedEventArgs args, FAVirtualizingLayoutContext context)
	{
		_elementManager.DataSourceChanged(source, args);
		_collectionChangePending = true;
	}

	public Size MeasureElement(Control element, int index, Size availableSize, FAVirtualizingLayoutContext context)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		Size val = _algorithmCallbacks.Algorithm_GetMeasureSize(index, availableSize, context);
		((Layoutable)element).Measure(val);
		Size val2 = _algorithmCallbacks.Algorithm_GetProvisionalArrangeSize(index, val, ((Layoutable)element).DesiredSize, context);
		_algorithmCallbacks.Algorithm_OnElementMeasured(element, index, availableSize, val, ((Layoutable)element).DesiredSize, val2, context);
		return val2;
	}

	private int GetAnchorIndex(Size availableSize, bool isWrapping, double minItemSpacing, bool disableVirtualization, string layoutId)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		int num = -1;
		Point val = default(Point);
		FAVirtualizingLayoutContext context = _context;
		if (!IsVirtualizingContext() | disableVirtualization)
		{
			num = ((context.ItemCountCore() <= 0) ? (-1) : 0);
		}
		else
		{
			bool flag = _elementManager.IsWindowConnected(RealizationRect(), ScrollOrientation, _scrollOrientationSameAsFlow);
			bool flag2 = isWrapping && (this.Minor(_lastAvailableSize) != this.Minor(availableSize) || _lastItemSpacing != minItemSpacing || _collectionChangePending);
			int recommendedAnchorIndex = _context.RecommendedAnchorIndex;
			if (recommendedAnchorIndex >= 0 && _elementManager.IsDataIndexRealized(recommendedAnchorIndex))
			{
				num = _algorithmCallbacks.Algorithm_GetAnchorForTargetElement(recommendedAnchorIndex, availableSize, context).Index;
				if (_elementManager.IsDataIndexRealized(num))
				{
					Rect layoutBoundsForDataIndex = _elementManager.GetLayoutBoundsForDataIndex(num);
					if (flag2)
					{
						val = this.MinorMajorPoint(0.0, this.MajorStart(layoutBoundsForDataIndex));
					}
					else
					{
						((Point)(ref val))._002Ector(((Rect)(ref layoutBoundsForDataIndex)).X, ((Rect)(ref layoutBoundsForDataIndex)).Y);
					}
				}
				else
				{
					for (int num2 = _elementManager.GetDataIndexFromRealizedRangeIndex(0) - 1; num2 >= num; num2--)
					{
						_elementManager.EnsureElementRealized(forward: false, num2, layoutId);
					}
					Rect layoutBoundsForDataIndex2 = _elementManager.GetLayoutBoundsForDataIndex(recommendedAnchorIndex);
					val = this.MinorMajorPoint(0.0, this.MajorStart(layoutBoundsForDataIndex2));
				}
			}
			else if (flag2 || !flag)
			{
				FlowLayoutAnchorInfo flowLayoutAnchorInfo = _algorithmCallbacks.Algorithm_GetAnchorForRealizationRect(availableSize, context);
				num = flowLayoutAnchorInfo.Index;
				val = this.MinorMajorPoint(0.0, flowLayoutAnchorInfo.Offset);
			}
			else
			{
				num = _elementManager.GetDataIndexFromRealizedRangeIndex(0);
				Rect layoutBoundsForRealizedIndex = _elementManager.GetLayoutBoundsForRealizedIndex(0);
				((Point)(ref val))._002Ector(((Rect)(ref layoutBoundsForRealizedIndex)).X, ((Rect)(ref layoutBoundsForRealizedIndex)).Y);
			}
		}
		_firstRealizedDataIndexInsideRealizationWindow = (_lastRealizedDataIndexInsideRealizationWindow = num);
		if (_elementManager.IsIndexValidInData(num))
		{
			if (!_elementManager.IsDataIndexRealized(num))
			{
				_elementManager.ClearRealizedRange();
				Control orCreateElementAt = _context.GetOrCreateElementAt(num, FAElementRealizationOptions.ForceCreate | FAElementRealizationOptions.SuppressAutoRecycle);
				_elementManager.Add(orCreateElementAt, num);
			}
			Control realizedElement = _elementManager.GetRealizedElement(num);
			Size val2 = MeasureElement(realizedElement, num, availableSize, context);
			Rect bounds = default(Rect);
			((Rect)(ref bounds))._002Ector(val, val2);
			_elementManager.SetLayoutBoundsForDataIndex(num, bounds);
		}
		else
		{
			_elementManager.ClearRealizedRange();
		}
		_lastAvailableSize = availableSize;
		_lastItemSpacing = minItemSpacing;
		return num;
	}

	private void Generate(GenerateDirection direction, int anchorIndex, Size availableSize, double minItemSpacing, double lineSpacing, int maxItemsPerLine, bool disableVirtualization, string layoutId)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		if (anchorIndex == -1)
		{
			return;
		}
		int num = ((direction == GenerateDirection.Forward) ? 1 : (-1));
		int num2 = anchorIndex;
		int i = anchorIndex + num;
		Rect layoutBoundsForDataIndex = _elementManager.GetLayoutBoundsForDataIndex(anchorIndex);
		double num3 = this.MajorStart(layoutBoundsForDataIndex);
		double num4 = this.MajorSize(layoutBoundsForDataIndex);
		int num5 = 1;
		bool flag = false;
		Rect rect = default(Rect);
		for (; _elementManager.IsIndexValidInData(i); i += num)
		{
			if (!disableVirtualization && !ShouldContinueFillingUpSpace(num2, direction))
			{
				break;
			}
			_elementManager.EnsureElementRealized(direction == GenerateDirection.Forward, i, layoutId);
			Control realizedElement = _elementManager.GetRealizedElement(i);
			Size size = MeasureElement(realizedElement, i, availableSize, _context);
			_elementManager.GetRealizedElement(num2);
			((Rect)(ref rect))._002Ector(0.0, 0.0, ((Size)(ref size)).Width, ((Size)(ref size)).Height);
			Rect layoutBoundsForDataIndex2 = _elementManager.GetLayoutBoundsForDataIndex(num2);
			if (direction == GenerateDirection.Forward)
			{
				double remainingSpace = this.Minor(availableSize) - (this.MinorStart(layoutBoundsForDataIndex2) + this.MinorSize(layoutBoundsForDataIndex2) + minItemSpacing + this.Minor(size));
				if (num5 >= maxItemsPerLine || _algorithmCallbacks.Algorithm_ShouldBreakLine(i, remainingSpace))
				{
					this.SetMinorStart(ref rect, 0.0);
					this.SetMajorStart(ref rect, this.MajorStart(layoutBoundsForDataIndex2) + num4 + lineSpacing);
					if (flag)
					{
						for (int j = 0; j < num5; j++)
						{
							int dataIndex = i - 1 - j;
							Rect rect2 = _elementManager.GetLayoutBoundsForDataIndex(dataIndex);
							this.SetMajorSize(ref rect2, num4);
							_elementManager.SetLayoutBoundsForDataIndex(dataIndex, rect2);
						}
					}
					num4 = this.MajorSize(rect);
					num3 = this.MajorStart(rect);
					flag = false;
					num5 = 1;
				}
				else
				{
					this.SetMinorStart(ref rect, this.MinorStart(layoutBoundsForDataIndex2) + this.MinorSize(layoutBoundsForDataIndex2) + minItemSpacing);
					this.SetMajorStart(ref rect, num3);
					num4 = Math.Max(num4, this.MajorSize(rect));
					flag = this.MajorSize(layoutBoundsForDataIndex2) != this.MajorSize(rect);
					num5++;
				}
			}
			else
			{
				double remainingSpace2 = this.MinorStart(layoutBoundsForDataIndex2) - (this.Minor(size) + minItemSpacing);
				if (num5 >= maxItemsPerLine || _algorithmCallbacks.Algorithm_ShouldBreakLine(i, remainingSpace2))
				{
					double num6 = this.Minor(availableSize);
					this.SetMinorStart(ref rect, (!double.IsInfinity(num6)) ? (num6 - this.Minor(size)) : (this.MinorSize(LastExtent) - this.Minor(size)));
					this.SetMajorStart(ref rect, num3 - this.Major(size) - lineSpacing);
					if (flag)
					{
						double num7 = this.MajorStart(_elementManager.GetLayoutBoundsForDataIndex(i + num5 + 1));
						for (int k = 0; k < num5; k++)
						{
							int num8 = i + 1 + k;
							if (num8 != anchorIndex)
							{
								Rect rect3 = _elementManager.GetLayoutBoundsForDataIndex(num8);
								this.SetMajorStart(ref rect3, num7 - num4 - lineSpacing);
								this.SetMajorSize(ref rect3, num4);
								_elementManager.SetLayoutBoundsForDataIndex(num8, rect3);
							}
						}
					}
					num4 = this.MajorSize(rect);
					num3 = this.MajorStart(rect);
					flag = false;
					num5 = 1;
				}
				else
				{
					this.SetMinorStart(ref rect, this.MinorStart(layoutBoundsForDataIndex2) - this.Minor(size) - minItemSpacing);
					this.SetMajorStart(ref rect, num3);
					num4 = Math.Max(num4, this.MajorSize(rect));
					flag = this.MajorSize(layoutBoundsForDataIndex2) != this.MajorSize(rect);
					num5++;
				}
			}
			_elementManager.SetLayoutBoundsForDataIndex(i, rect);
			num2 = i;
		}
		if (direction == GenerateDirection.Forward)
		{
			int itemCount = _context.ItemCount;
			_lastRealizedDataIndexInsideRealizationWindow = ((num2 == itemCount - 1) ? (itemCount - 1) : (num2 - 1));
			_lastRealizedDataIndexInsideRealizationWindow = Math.Max(0, _lastRealizedDataIndexInsideRealizationWindow);
		}
		else
		{
			int itemCount2 = _context.ItemCount;
			_firstRealizedDataIndexInsideRealizationWindow = ((num2 != 0) ? (num2 + 1) : 0);
			_firstRealizedDataIndexInsideRealizationWindow = Math.Min(itemCount2 - 1, _firstRealizedDataIndexInsideRealizationWindow);
		}
		_elementManager.DiscardElementsOutsideWindow(direction == GenerateDirection.Forward, i);
	}

	private bool IsReflowRequired()
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (_elementManager.GetRealizedElementCount() > 0 && _elementManager.GetDataIndexFromRealizedRangeIndex(0) == 0)
		{
			Rect layoutBoundsForRealizedIndex;
			if (ScrollOrientation != ScrollOrientation.Vertical)
			{
				layoutBoundsForRealizedIndex = _elementManager.GetLayoutBoundsForRealizedIndex(0);
				return ((Rect)(ref layoutBoundsForRealizedIndex)).Y != 0.0;
			}
			layoutBoundsForRealizedIndex = _elementManager.GetLayoutBoundsForRealizedIndex(0);
			return ((Rect)(ref layoutBoundsForRealizedIndex)).X != 0.0;
		}
		return false;
	}

	private bool ShouldContinueFillingUpSpace(int index, GenerateDirection direction)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		if (!IsVirtualizingContext())
		{
			return true;
		}
		Rect realizationRect = _context.RealizationRect;
		Rect layoutBoundsForDataIndex = _elementManager.GetLayoutBoundsForDataIndex(index);
		double num = this.MajorStart(layoutBoundsForDataIndex);
		double num2 = this.MajorEnd(layoutBoundsForDataIndex);
		double num3 = this.MajorStart(realizationRect);
		double num4 = this.MajorEnd(realizationRect);
		double num5 = this.MinorStart(layoutBoundsForDataIndex);
		double num6 = this.MinorEnd(layoutBoundsForDataIndex);
		double num7 = this.MinorStart(realizationRect);
		double num8 = this.MinorEnd(realizationRect);
		return (direction != GenerateDirection.Forward) ? (num2 > num3 && num6 > num7) : (num < num4 && num5 < num8);
	}

	private Rect EstimateExtent(Size availableSize, string layoutId)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		Control firstRealized = null;
		Rect firstRealizedLayoutBounds = default(Rect);
		Control lastRealized = null;
		Rect lastRealizedLayoutBounds = default(Rect);
		int firstRealizedItemIndex = -1;
		int lastRealizedItemIndex = -1;
		if (_elementManager.GetRealizedElementCount() > 0)
		{
			firstRealized = _elementManager.GetAt(0);
			firstRealizedLayoutBounds = _elementManager.GetLayoutBoundsForRealizedIndex(0);
			firstRealizedItemIndex = _elementManager.GetDataIndexFromRealizedRangeIndex(0);
			int num = _elementManager.GetRealizedElementCount() - 1;
			lastRealized = _elementManager.GetAt(num);
			lastRealizedItemIndex = _elementManager.GetDataIndexFromRealizedRangeIndex(num);
			lastRealizedLayoutBounds = _elementManager.GetLayoutBoundsForRealizedIndex(num);
		}
		return _algorithmCallbacks.Algorithm_GetExtent(availableSize, _context, firstRealized, firstRealizedItemIndex, firstRealizedLayoutBounds, lastRealized, lastRealizedItemIndex, lastRealizedLayoutBounds);
	}

	private void RaiseLineArranged()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		Rect val = RealizationRect();
		if ((((Rect)(ref val)).Width == 0.0 && ((Rect)(ref val)).Height == 0.0) || _elementManager.GetRealizedElementCount() <= 0)
		{
			return;
		}
		int num = 0;
		Rect layoutBoundsForDataIndex = _elementManager.GetLayoutBoundsForDataIndex(_firstRealizedDataIndexInsideRealizationWindow);
		double num2 = this.MajorStart(layoutBoundsForDataIndex);
		double num3 = this.MajorSize(layoutBoundsForDataIndex);
		for (int i = _firstRealizedDataIndexInsideRealizationWindow; i <= _lastRealizedDataIndexInsideRealizationWindow; i++)
		{
			Rect layoutBoundsForDataIndex2 = _elementManager.GetLayoutBoundsForDataIndex(i);
			if (this.MajorStart(layoutBoundsForDataIndex2) != num2)
			{
				_algorithmCallbacks.Algorithm_OnLineArranged(i - num, num, num3, _context);
				num = 0;
				num2 = this.MajorStart(layoutBoundsForDataIndex2);
				num3 = 0.0;
			}
			num3 = Math.Max(num3, this.MajorSize(layoutBoundsForDataIndex2));
			num++;
			layoutBoundsForDataIndex = layoutBoundsForDataIndex2;
		}
		_algorithmCallbacks.Algorithm_OnLineArranged(_lastRealizedDataIndexInsideRealizationWindow - num + 1, num, num3, _context);
	}

	private void ArrangeVirtualizingLayout(Size finalSize, LineAlignment lineAlignment, bool isWrapping, string layoutId)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		int realizedElementCount = _elementManager.GetRealizedElementCount();
		if (realizedElementCount <= 0)
		{
			return;
		}
		int num = 1;
		Rect rect = _elementManager.GetLayoutBoundsForRealizedIndex(0);
		double num2 = this.MajorStart(rect);
		double spaceAtLineStart = this.MinorStart(rect);
		double num3 = 0.0;
		double num4 = this.MajorSize(rect);
		for (int i = 1; i < realizedElementCount; i++)
		{
			Rect layoutBoundsForRealizedIndex = _elementManager.GetLayoutBoundsForRealizedIndex(i);
			if (this.MajorStart(layoutBoundsForRealizedIndex) != num2)
			{
				num3 = this.Minor(finalSize) - this.MinorStart(rect) - this.MinorSize(rect);
				PerformLineAlignment(i - num, num, spaceAtLineStart, num3, num4, lineAlignment, isWrapping, finalSize, layoutId);
				spaceAtLineStart = this.MinorStart(layoutBoundsForRealizedIndex);
				num = 0;
				num2 = this.MajorStart(layoutBoundsForRealizedIndex);
				num4 = 0.0;
			}
			num++;
			num4 = Math.Max(num4, this.MajorSize(layoutBoundsForRealizedIndex));
			rect = layoutBoundsForRealizedIndex;
		}
		if (num > 0)
		{
			double spaceAtLineEnd = this.Minor(finalSize) - this.MinorStart(rect) - this.MinorSize(rect);
			PerformLineAlignment(realizedElementCount - num, num, spaceAtLineStart, spaceAtLineEnd, num4, lineAlignment, isWrapping, finalSize, layoutId);
		}
	}

	private void PerformLineAlignment(int lineStartIndex, int countInLine, double spaceAtLineStart, double spaceAtLineEnd, double lineSize, LineAlignment lineAlignment, bool isWrapping, Size finalSize, string layoutId)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		for (int i = lineStartIndex; i < lineStartIndex + countInLine; i++)
		{
			Rect rect = _elementManager.GetLayoutBoundsForRealizedIndex(i);
			this.SetMajorSize(ref rect, lineSize);
			if (!_scrollOrientationSameAsFlow && (spaceAtLineStart != 0.0 || spaceAtLineEnd != 0.0))
			{
				double num = spaceAtLineStart + spaceAtLineEnd;
				switch (lineAlignment)
				{
				case LineAlignment.Start:
					this.SetMinorStart(ref rect, this.MinorStart(rect) - spaceAtLineStart);
					break;
				case LineAlignment.End:
					this.SetMinorStart(ref rect, this.MinorStart(rect) + spaceAtLineEnd);
					break;
				case LineAlignment.Center:
				{
					double num6 = this.MinorStart(rect);
					this.SetMinorStart(ref rect, num6 - spaceAtLineStart);
					this.SetMinorStart(ref rect, num6 + num / 2.0);
					break;
				}
				case LineAlignment.SpaceAround:
				{
					double num4 = ((countInLine >= 1) ? (num / (double)(countInLine * 2)) : 0.0);
					double num5 = this.MinorStart(rect);
					this.SetMinorStart(ref rect, num5 - spaceAtLineStart);
					this.SetMinorStart(ref rect, num5 + num4 * (double)((i - lineStartIndex + 1) * 2 - 1));
					break;
				}
				case LineAlignment.SpaceBetween:
				{
					double num7 = ((countInLine > 1) ? (num / (double)(countInLine - 1)) : 0.0);
					double num8 = this.MinorStart(rect);
					this.SetMinorStart(ref rect, num8 - spaceAtLineStart);
					this.SetMinorStart(ref rect, num8 + num7 * (double)(i - lineStartIndex));
					break;
				}
				case LineAlignment.SpaceEvenly:
				{
					double num2 = ((countInLine >= 1) ? (num / (double)(countInLine + 1)) : 0.0);
					double num3 = this.MinorStart(rect);
					this.SetMinorStart(ref rect, num3 - spaceAtLineStart);
					this.SetMinorStart(ref rect, num3 + num2 * (double)(i - lineStartIndex + 1));
					break;
				}
				}
			}
			((Rect)(ref rect))._002Ector(((Rect)(ref rect)).X - ((Rect)(ref _lastExtent)).X, ((Rect)(ref rect)).Y - ((Rect)(ref _lastExtent)).Y, ((Rect)(ref rect)).Width, ((Rect)(ref rect)).Height);
			if (!isWrapping)
			{
				this.SetMinorSize(ref rect, Math.Max(this.MinorSize(rect), this.Minor(finalSize)));
			}
			((Layoutable)_elementManager.GetAt(i)).Arrange(rect);
		}
	}

	private Rect RealizationRect()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (!IsVirtualizingContext())
		{
			return new Rect(0.0, 0.0, double.PositiveInfinity, double.PositiveInfinity);
		}
		return _context.RealizationRect;
	}

	private void SetLayoutOrigin()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (IsVirtualizingContext())
		{
			_context.LayoutOrigin = ((Rect)(ref _lastExtent)).Position;
		}
	}

	internal Control GetElementIfRealized(int dataIndex)
	{
		if (_elementManager.IsDataIndexRealized(dataIndex))
		{
			return _elementManager.GetRealizedElement(dataIndex);
		}
		return null;
	}

	internal bool TryAddElement0(Control element)
	{
		if (_elementManager.GetRealizedElementCount() == 0)
		{
			_elementManager.Add(element, 0);
			return true;
		}
		return false;
	}

	private bool IsVirtualizingContext()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (_context != null)
		{
			Rect realizationRect = _context.RealizationRect;
			return !double.IsInfinity(((Rect)(ref realizationRect)).Height) && !double.IsInfinity(((Rect)(ref realizationRect)).Width);
		}
		return false;
	}
}
