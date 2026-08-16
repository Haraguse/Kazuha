using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

internal class ElementManager
{
	private bool _useLayoutBounds;

	private List<Control> _realizedElements = new List<Control>();

	private List<Rect> _realizedElementLayoutBounds = new List<Rect>();

	private int _firstRealizedDataIndex = -1;

	private FAVirtualizingLayoutContext _context;

	public int FirstRealizedIndex => _firstRealizedDataIndex;

	public int LastRealizedIndex => _firstRealizedDataIndex + _realizedElements.Count - 1;

	public ElementManager(bool useLayoutBounds = true)
	{
		_useLayoutBounds = useLayoutBounds;
	}

	public void SetContext(FAVirtualizingLayoutContext virtualContext)
	{
		_context = virtualContext;
	}

	public void OnBeginMeasure(ScrollOrientation orientation)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (_context == null)
		{
			return;
		}
		if (IsVirtualizingContext())
		{
			DiscardElementsOutsideWindow(_context.RealizationRect, orientation);
			return;
		}
		int itemCount = _context.ItemCount;
		if (_realizedElementLayoutBounds.Count != itemCount)
		{
			_realizedElementLayoutBounds.Capacity = itemCount;
		}
	}

	public int GetRealizedElementCount()
	{
		if (!IsVirtualizingContext())
		{
			return _context.ItemCount;
		}
		return _realizedElements.Count;
	}

	public Control GetAt(int realizedIndex)
	{
		Control val = null;
		if (IsVirtualizingContext())
		{
			if (_realizedElements[realizedIndex] == null)
			{
				int dataIndexFromRealizedRangeIndex = GetDataIndexFromRealizedRangeIndex(realizedIndex);
				val = _context.GetOrCreateElementAt(dataIndexFromRealizedRangeIndex, FAElementRealizationOptions.ForceCreate | FAElementRealizationOptions.SuppressAutoRecycle);
				_realizedElements[realizedIndex] = val;
			}
			else
			{
				val = _realizedElements[realizedIndex];
			}
		}
		else
		{
			val = _context.GetOrCreateElementAt(realizedIndex, FAElementRealizationOptions.ForceCreate | FAElementRealizationOptions.SuppressAutoRecycle);
		}
		return val;
	}

	public void Add(Control element, int dataIndex)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (_realizedElements.Count == 0)
		{
			_firstRealizedDataIndex = dataIndex;
		}
		_realizedElements.Add(element);
		if (_useLayoutBounds)
		{
			_realizedElementLayoutBounds.Add(default(Rect));
		}
	}

	public void Insert(int realizedIndex, int dataIndex, Control element)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		if (realizedIndex == 0)
		{
			_firstRealizedDataIndex = dataIndex;
		}
		_realizedElements.Insert(realizedIndex, element);
		if (_useLayoutBounds)
		{
			_realizedElementLayoutBounds.Insert(realizedIndex, new Rect(-1.0, -1.0, -1.0, -1.0));
		}
	}

	public void ClearRealizedRange(int realizedIndex, int count)
	{
		for (int i = 0; i < count; i++)
		{
			int index = ((realizedIndex == 0) ? (realizedIndex + i) : (realizedIndex + count - 1 - i));
			Control val = _realizedElements[index];
			if (val != null)
			{
				_context.RecycleElement(val);
			}
		}
		int num = realizedIndex + count;
		_realizedElements.RemoveRange(realizedIndex, num - realizedIndex);
		if (_useLayoutBounds)
		{
			_realizedElementLayoutBounds.RemoveRange(realizedIndex, num - realizedIndex);
		}
		if (realizedIndex == 0)
		{
			_firstRealizedDataIndex = ((_realizedElements.Count == 0) ? (-1) : (_firstRealizedDataIndex + count));
		}
	}

	public void DiscardElementsOutsideWindow(bool forward, int startIndex)
	{
		if (IsDataIndexRealized(startIndex))
		{
			int realizedRangeIndexFromDataIndex = GetRealizedRangeIndexFromDataIndex(startIndex);
			if (forward)
			{
				ClearRealizedRange(realizedRangeIndexFromDataIndex, GetRealizedElementCount() - realizedRangeIndexFromDataIndex);
			}
			else
			{
				ClearRealizedRange(0, realizedRangeIndexFromDataIndex + 1);
			}
		}
	}

	public void ClearRealizedRange()
	{
		ClearRealizedRange(0, GetRealizedElementCount());
	}

	public Rect GetLayoutBoundsForDataIndex(int dataIndex)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		int realizedRangeIndexFromDataIndex = GetRealizedRangeIndexFromDataIndex(dataIndex);
		return _realizedElementLayoutBounds[realizedRangeIndexFromDataIndex];
	}

	public void SetLayoutBoundsForDataIndex(int dataIndex, Rect bounds)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		int realizedRangeIndexFromDataIndex = GetRealizedRangeIndexFromDataIndex(dataIndex);
		_realizedElementLayoutBounds[realizedRangeIndexFromDataIndex] = bounds;
	}

	public Rect GetLayoutBoundsForRealizedIndex(int realizedIndex)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return _realizedElementLayoutBounds[realizedIndex];
	}

	public void SetLayoutBoundsForRealizedIndex(int realizedIndex, Rect bounds)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		_realizedElementLayoutBounds[realizedIndex] = bounds;
	}

	public bool IsDataIndexRealized(int index)
	{
		if (IsVirtualizingContext())
		{
			int realizedElementCount = GetRealizedElementCount();
			if (realizedElementCount > 0 && GetDataIndexFromRealizedRangeIndex(0) <= index)
			{
				return GetDataIndexFromRealizedRangeIndex(realizedElementCount - 1) >= index;
			}
			return false;
		}
		if (index >= 0)
		{
			return index < _context.ItemCount;
		}
		return false;
	}

	public bool IsIndexValidInData(int currentIndex)
	{
		if (currentIndex >= 0)
		{
			return currentIndex < _context.ItemCount;
		}
		return false;
	}

	public Control GetRealizedElement(int dataIndex)
	{
		if (!IsVirtualizingContext())
		{
			return _context.GetOrCreateElementAt(dataIndex, FAElementRealizationOptions.ForceCreate | FAElementRealizationOptions.SuppressAutoRecycle);
		}
		return GetAt(GetRealizedRangeIndexFromDataIndex(dataIndex));
	}

	public void EnsureElementRealized(bool forward, int dataIndex, string layoutId)
	{
		if (!IsDataIndexRealized(dataIndex))
		{
			Control orCreateElementAt = _context.GetOrCreateElementAt(dataIndex, FAElementRealizationOptions.ForceCreate | FAElementRealizationOptions.SuppressAutoRecycle);
			if (forward)
			{
				Add(orCreateElementAt, dataIndex);
			}
			else
			{
				Insert(0, dataIndex, orCreateElementAt);
			}
		}
	}

	public bool IsWindowConnected(Rect window, ScrollOrientation orientation, bool scrollOrientationSameAsFlow)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		bool result = false;
		if (_realizedElementLayoutBounds.Count > 0)
		{
			Rect layoutBoundsForRealizedIndex = GetLayoutBoundsForRealizedIndex(0);
			Rect layoutBoundsForRealizedIndex2 = GetLayoutBoundsForRealizedIndex(GetRealizedElementCount() - 1);
			int num = ((!scrollOrientationSameAsFlow) ? ((int)orientation) : ((orientation != ScrollOrientation.Vertical) ? 1 : 0));
			double num2 = ((num == 1) ? ((Rect)(ref window)).Y : ((Rect)(ref window)).X);
			double num3 = ((num == 1) ? (((Rect)(ref window)).Y + ((Rect)(ref window)).Height) : (((Rect)(ref window)).X + ((Rect)(ref window)).Width));
			double num4 = ((num == 1) ? ((Rect)(ref layoutBoundsForRealizedIndex)).Y : ((Rect)(ref layoutBoundsForRealizedIndex)).X);
			double num5 = ((num == 1) ? (((Rect)(ref layoutBoundsForRealizedIndex2)).Y + ((Rect)(ref layoutBoundsForRealizedIndex2)).Height) : (((Rect)(ref layoutBoundsForRealizedIndex2)).X + ((Rect)(ref layoutBoundsForRealizedIndex2)).Width));
			result = num4 <= num3 && num5 >= num2;
		}
		return result;
	}

	public void DataSourceChanged(object source, NotifyCollectionChangedEventArgs args)
	{
		if (_realizedElements.Count == 0)
		{
			return;
		}
		switch (args.Action)
		{
		case NotifyCollectionChangedAction.Add:
			OnItemsAdded(args.NewStartingIndex, args.NewItems.Count);
			break;
		case NotifyCollectionChangedAction.Replace:
		{
			int count2 = args.OldItems.Count;
			int count3 = args.NewItems.Count;
			int oldStartingIndex = args.OldStartingIndex;
			int newStartingIndex = args.NewStartingIndex;
			if (count2 == count3 && oldStartingIndex == newStartingIndex && IsDataIndexRealized(oldStartingIndex) && IsDataIndexRealized(oldStartingIndex + count2 - 1))
			{
				int realizedRangeIndexFromDataIndex = GetRealizedRangeIndexFromDataIndex(oldStartingIndex);
				for (int i = realizedRangeIndexFromDataIndex; i < realizedRangeIndexFromDataIndex + count2; i++)
				{
					Control val = _realizedElements[i];
					if (val != null)
					{
						_context.RecycleElement(val);
						_realizedElements[i] = null;
					}
				}
			}
			else
			{
				OnItemsRemoved(oldStartingIndex, count2);
				OnItemsAdded(newStartingIndex, count3);
			}
			break;
		}
		case NotifyCollectionChangedAction.Remove:
			OnItemsRemoved(args.OldStartingIndex, args.OldItems.Count);
			break;
		case NotifyCollectionChangedAction.Reset:
			ClearRealizedRange();
			break;
		case NotifyCollectionChangedAction.Move:
		{
			int count = args.OldItems?.Count ?? 1;
			OnItemsRemoved(args.OldStartingIndex, count);
			OnItemsAdded(args.NewStartingIndex, count);
			break;
		}
		}
	}

	public int GetElementDataIndex(Control suggestedAnchor)
	{
		int num = _realizedElements.IndexOf(suggestedAnchor);
		if (num == -1)
		{
			return -1;
		}
		return GetDataIndexFromRealizedRangeIndex(num);
	}

	public int GetDataIndexFromRealizedRangeIndex(int rangeIndex)
	{
		if (!IsVirtualizingContext())
		{
			return rangeIndex;
		}
		return rangeIndex + _firstRealizedDataIndex;
	}

	public int GetRealizedRangeIndexFromDataIndex(int dataIndex)
	{
		if (!IsVirtualizingContext())
		{
			return dataIndex;
		}
		return dataIndex - _firstRealizedDataIndex;
	}

	public void DiscardElementsOutsideWindow(Rect window, ScrollOrientation orientation)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		int realizedElementCount = GetRealizedElementCount();
		int num = -1;
		int num2 = realizedElementCount;
		for (int i = 0; i < realizedElementCount && !Intersects(window, _realizedElementLayoutBounds[i], orientation); i++)
		{
			num++;
		}
		int num3 = realizedElementCount - 1;
		while (num3 >= 0 && !Intersects(window, _realizedElementLayoutBounds[num3], orientation))
		{
			num2--;
			num3--;
		}
		if (num2 < realizedElementCount - 1)
		{
			ClearRealizedRange(num2 + 1, realizedElementCount - num2 - 1);
		}
		if (num > 0)
		{
			ClearRealizedRange(0, Math.Min(num, GetRealizedElementCount()));
		}
	}

	public static bool Intersects(Rect lhs, Rect rhs, ScrollOrientation orientation)
	{
		double num = ((orientation == ScrollOrientation.Vertical) ? ((Rect)(ref lhs)).Y : ((Rect)(ref lhs)).X);
		double num2 = ((orientation == ScrollOrientation.Vertical) ? (((Rect)(ref lhs)).Y + ((Rect)(ref lhs)).Height) : (((Rect)(ref lhs)).X + ((Rect)(ref lhs)).Width));
		double num3 = ((orientation == ScrollOrientation.Vertical) ? ((Rect)(ref rhs)).Y : ((Rect)(ref rhs)).X);
		double num4 = ((orientation == ScrollOrientation.Vertical) ? (((Rect)(ref rhs)).Y + ((Rect)(ref rhs)).Height) : (((Rect)(ref rhs)).X + ((Rect)(ref rhs)).Width));
		if (num2 >= num3)
		{
			return num <= num4;
		}
		return false;
	}

	private void OnItemsAdded(int index, int count)
	{
		int num = _firstRealizedDataIndex + GetRealizedElementCount() - 1;
		if (index >= _firstRealizedDataIndex && index <= num)
		{
			int num2 = index - _firstRealizedDataIndex;
			for (int i = 0; i < count; i++)
			{
				int realizedIndex = num2 + i;
				int dataIndex = index + i;
				Insert(realizedIndex, dataIndex, null);
			}
		}
		else if (index <= _firstRealizedDataIndex)
		{
			_firstRealizedDataIndex += count;
		}
	}

	private void OnItemsRemoved(int index, int count)
	{
		int val = _firstRealizedDataIndex + _realizedElements.Count - 1;
		int num = Math.Max(_firstRealizedDataIndex, index);
		int num2 = Math.Min(val, index + count - 1);
		bool num3 = index <= _firstRealizedDataIndex;
		if (num2 >= num)
		{
			ClearRealizedRange(GetRealizedRangeIndexFromDataIndex(num), num2 - num + 1);
		}
		if (num3 && _firstRealizedDataIndex != -1)
		{
			_firstRealizedDataIndex -= count;
		}
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
