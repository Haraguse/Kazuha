using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace FluentAvalonia.UI.Controls;

internal class BreadcrumbLayout : FANonVirtualizingLayout
{
	private Size _availableSize;

	private FABreadcrumbBarItem _ellipsisButton;

	private WeakReference<FABreadcrumbBar> _breadcrumb;

	private bool _ellipsisIsRendered;

	private int _firstRenderedItemIndexAfterEllipsis;

	private int _visibleItemsCount;

	internal ref readonly bool EllipsisIsRendered => ref _ellipsisIsRendered;

	internal ref readonly int FirstRenderedItemIndexAfterEllipsis => ref _firstRenderedItemIndexAfterEllipsis;

	internal ref readonly int GetVisibleItemsCount => ref _visibleItemsCount;

	public BreadcrumbLayout()
	{
	}

	public BreadcrumbLayout(FABreadcrumbBar breadcrumb)
	{
		_breadcrumb = new WeakReference<FABreadcrumbBar>(breadcrumb);
	}

	protected internal override void InitializeForContextCore(FALayoutContext context)
	{
	}

	protected internal override void UninitializeForContextCore(FALayoutContext context)
	{
	}

	public int GetItemCount(FANonVirtualizingLayoutContext context)
	{
		return context.Children.Count;
	}

	public Control GetElementAt(FANonVirtualizingLayoutContext context, int index)
	{
		return context.Children[index];
	}

	protected internal override Size MeasureOverride(FANonVirtualizingLayoutContext context, Size availableSize)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_availableSize = availableSize;
		double num = 0.0;
		double num2 = 0.0;
		for (int i = 0; i < GetItemCount(context); i++)
		{
			Control elementAt = GetElementAt(context, i);
			((Layoutable)elementAt).Measure(availableSize);
			if (i != 0)
			{
				double num3 = num;
				Size desiredSize = ((Layoutable)elementAt).DesiredSize;
				num = num3 + ((Size)(ref desiredSize)).Width;
				double val = num2;
				desiredSize = ((Layoutable)elementAt).DesiredSize;
				num2 = Math.Max(val, ((Size)(ref desiredSize)).Height);
			}
		}
		if (GetItemCount(context) > 0 && GetElementAt(context, 0) is FABreadcrumbBarItem ellipsisButton)
		{
			_ellipsisButton = ellipsisButton;
		}
		if (num > ((Size)(ref availableSize)).Width)
		{
			_ellipsisIsRendered = true;
		}
		else
		{
			_ellipsisIsRendered = false;
		}
		return new Size(num, num2);
	}

	protected internal override Size ArrangeOverride(FANonVirtualizingLayoutContext context, Size finalSize)
	{
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		int itemCount = GetItemCount(context);
		int num = 0;
		_firstRenderedItemIndexAfterEllipsis = itemCount - 1;
		_visibleItemsCount = 0;
		if (_ellipsisIsRendered)
		{
			num = (_firstRenderedItemIndexAfterEllipsis = GetFirstBreadcrumbBarItemToArrange(context));
		}
		double accumWidth = 0.0;
		double breadcrumbBarItemsHeight = GetBreadcrumbBarItemsHeight(context, num);
		if (itemCount > 0)
		{
			FABreadcrumbBarItem ellipsisButton = _ellipsisButton;
			if (_ellipsisIsRendered)
			{
				ArrangeItem((Control)(object)ellipsisButton, ref accumWidth, breadcrumbBarItemsHeight);
			}
			else
			{
				HideItem((Control)(object)ellipsisButton);
			}
		}
		for (int i = 1; i < itemCount; i++)
		{
			if (i < num)
			{
				HideItem(context, i);
				continue;
			}
			ArrangeItem(context, i, ref accumWidth, breadcrumbBarItemsHeight);
			_visibleItemsCount++;
		}
		if (_breadcrumb.TryGetTarget(out var target))
		{
			target.ReIndexVisibleElementsForAccessibility();
		}
		return finalSize;
	}

	private void ArrangeItem(Control breadcrumbItem, ref double accumWidth, double maxElementHeight)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		Size desiredSize = ((Layoutable)breadcrumbItem).DesiredSize;
		Rect val = default(Rect);
		((Rect)(ref val))._002Ector(accumWidth, 0.0, ((Size)(ref desiredSize)).Width, maxElementHeight);
		((Layoutable)breadcrumbItem).Arrange(val);
		accumWidth += ((Size)(ref desiredSize)).Width;
	}

	private void ArrangeItem(FANonVirtualizingLayoutContext context, int index, ref double accumWidth, double maxElementHeight)
	{
		Control elementAt = GetElementAt(context, index);
		ArrangeItem(elementAt, ref accumWidth, maxElementHeight);
	}

	private void HideItem(Control item)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		((Layoutable)item).Arrange(default(Rect));
	}

	private void HideItem(FANonVirtualizingLayoutContext context, int index)
	{
		Control elementAt = GetElementAt(context, index);
		HideItem(elementAt);
	}

	private int GetFirstBreadcrumbBarItemToArrange(FANonVirtualizingLayoutContext context)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		int itemCount = GetItemCount(context);
		Size desiredSize = ((Layoutable)GetElementAt(context, itemCount - 1)).DesiredSize;
		double width = ((Size)(ref desiredSize)).Width;
		desiredSize = ((Layoutable)_ellipsisButton).DesiredSize;
		double num = width + ((Size)(ref desiredSize)).Width;
		for (int num2 = itemCount - 2; num2 >= 0; num2--)
		{
			double num3 = num;
			desiredSize = ((Layoutable)GetElementAt(context, num2)).DesiredSize;
			double num4 = num3 + ((Size)(ref desiredSize)).Width;
			if (num4 > ((Size)(ref _availableSize)).Width)
			{
				return num2 + 1;
			}
			num = num4;
		}
		return 0;
	}

	private double GetBreadcrumbBarItemsHeight(FANonVirtualizingLayoutContext context, int firstItemToRender)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		double num = 0.0;
		Size desiredSize;
		if (_ellipsisIsRendered)
		{
			desiredSize = ((Layoutable)_ellipsisButton).DesiredSize;
			num = ((Size)(ref desiredSize)).Height;
		}
		for (int i = firstItemToRender; i < GetItemCount(context); i++)
		{
			double val = num;
			desiredSize = ((Layoutable)GetElementAt(context, i)).DesiredSize;
			num = Math.Max(val, ((Size)(ref desiredSize)).Height);
		}
		return num;
	}
}
