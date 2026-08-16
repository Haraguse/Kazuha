using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace FluentAvalonia.UI.Controls;

internal class UniformGridLayoutState
{
	private FlowLayoutAlgorithm _flowAlgorithm;

	private double _effectiveItemWidth;

	private double _effectiveItemHeight;

	private bool _isEffectiveSizeValid;

	public FlowLayoutAlgorithm FlowAlgorithm => _flowAlgorithm;

	public double EffectiveItemWidth => _effectiveItemWidth;

	public double EffectiveItemHeight => _effectiveItemHeight;

	public void InitializeForContext(FAVirtualizingLayoutContext context, IFlowLayoutAlgorithmDelegates callbacks)
	{
		if (_flowAlgorithm == null)
		{
			_flowAlgorithm = new FlowLayoutAlgorithm();
		}
		_flowAlgorithm.InitializeForContext(context, callbacks);
		context.LayoutStateCore = this;
	}

	public void UninitializeForContext(FAVirtualizingLayoutContext context)
	{
		_flowAlgorithm.UninitializeForContext(context);
	}

	public void EnsureElementSize(Size availableSize, FAVirtualizingLayoutContext context, double layoutItemWidth, double layoutItemHeight, FAUniformGridLayoutItemsStretch stretch, Orientation orientation, double minRowSpacing, double minColumnSpacing, int maxItemsPerLine)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		if (maxItemsPerLine == 0)
		{
			maxItemsPerLine = 1;
		}
		if (context.ItemCount <= 0)
		{
			return;
		}
		Control elementIfRealized = _flowAlgorithm.GetElementIfRealized(0);
		if (elementIfRealized != null)
		{
			((Layoutable)elementIfRealized).Measure(CalculateAvailableSize(availableSize, orientation, stretch, maxItemsPerLine, layoutItemWidth, layoutItemHeight, minRowSpacing, minColumnSpacing));
			SetSize(((Layoutable)elementIfRealized).DesiredSize, layoutItemWidth, layoutItemHeight, availableSize, stretch, orientation, minRowSpacing, minColumnSpacing, maxItemsPerLine);
		}
		else if (!_isEffectiveSizeValid)
		{
			Control orCreateElementAt = context.GetOrCreateElementAt(0, FAElementRealizationOptions.ForceCreate);
			if (orCreateElementAt != null)
			{
				((Layoutable)orCreateElementAt).Measure(CalculateAvailableSize(availableSize, orientation, stretch, maxItemsPerLine, layoutItemWidth, layoutItemHeight, minRowSpacing, minColumnSpacing));
				SetSize(((Layoutable)orCreateElementAt).DesiredSize, layoutItemWidth, layoutItemHeight, availableSize, stretch, orientation, minRowSpacing, minColumnSpacing, maxItemsPerLine);
				context.RecycleElement(orCreateElementAt);
			}
		}
		_isEffectiveSizeValid = true;
	}

	public Size CalculateAvailableSize(Size availableSize, Orientation orientation, FAUniformGridLayoutItemsStretch stretch, int maxItemsPerLine, double itemWidth, double itemHeight, double minRowSpacing, double minColumnSpacing)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if ((int)orientation == 0)
		{
			if (!double.IsNaN(itemWidth))
			{
				double num = itemWidth;
				if (stretch != FAUniformGridLayoutItemsStretch.None)
				{
					num += CalculateExtraPixelsInLine(maxItemsPerLine, ((Size)(ref availableSize)).Width, itemWidth, minColumnSpacing);
				}
				return new Size(num, ((Size)(ref availableSize)).Height);
			}
		}
		else if (!double.IsNaN(itemHeight))
		{
			double num2 = itemHeight;
			if (stretch != FAUniformGridLayoutItemsStretch.None)
			{
				num2 += CalculateExtraPixelsInLine(maxItemsPerLine, ((Size)(ref availableSize)).Height, itemHeight, minRowSpacing);
			}
			return new Size(((Size)(ref availableSize)).Width, num2);
		}
		return availableSize;
	}

	private double CalculateExtraPixelsInLine(int maxItemsPerLine, double availableSizeMinor, double itemSizeMinor, double minorItemSpacing)
	{
		int num = (int)Math.Max(1.0, availableSizeMinor / (itemSizeMinor + minorItemSpacing));
		int num2 = ((num != 0) ? Math.Min(maxItemsPerLine, num) : maxItemsPerLine);
		double num3 = (double)num2 * (itemSizeMinor + minorItemSpacing) - minorItemSpacing;
		return (int)(availableSizeMinor - num3) / num2;
	}

	private void SetSize(Size desiredItemSize, double layoutItemWidth, double layoutItemHeight, Size availableSize, FAUniformGridLayoutItemsStretch stretch, Orientation orientation, double minRowSpacing, double minColumnSpacing, int maxItemsPerLine)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Invalid comparison between Unknown and I4
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		maxItemsPerLine = ((maxItemsPerLine == 0) ? 1 : maxItemsPerLine);
		_effectiveItemWidth = (double.IsNaN(layoutItemWidth) ? ((Size)(ref desiredItemSize)).Width : layoutItemWidth);
		_effectiveItemHeight = (double.IsNaN(layoutItemHeight) ? ((Size)(ref desiredItemSize)).Height : layoutItemHeight);
		double num = (((int)orientation == 0) ? ((Size)(ref availableSize)).Width : ((Size)(ref availableSize)).Height);
		double minorItemSpacing = (((int)orientation == 1) ? minRowSpacing : minColumnSpacing);
		double num2 = (((int)orientation == 0) ? _effectiveItemWidth : _effectiveItemHeight);
		double num3 = 0.0;
		if (!double.IsInfinity(num))
		{
			num3 = CalculateExtraPixelsInLine(maxItemsPerLine, num, num2, minorItemSpacing);
		}
		switch (stretch)
		{
		case FAUniformGridLayoutItemsStretch.Fill:
			if ((int)orientation == 0)
			{
				_effectiveItemWidth += num3;
			}
			else
			{
				_effectiveItemHeight += num3;
			}
			break;
		case FAUniformGridLayoutItemsStretch.Uniform:
		{
			double num4 = (((int)orientation == 0) ? _effectiveItemHeight : _effectiveItemWidth) * (num3 / num2);
			if ((int)orientation == 0)
			{
				_effectiveItemWidth += num3;
				_effectiveItemHeight += num4;
			}
			else
			{
				_effectiveItemHeight += num3;
				_effectiveItemWidth += num4;
			}
			break;
		}
		}
	}

	internal void InvalidateElementSize()
	{
		_isEffectiveSizeValid = false;
	}
}
