using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using FluentAvalonia.Collections;
using FluentAvalonia.UI.Controls.Primitives;

namespace FluentAvalonia.UI.Controls;

internal class LiveReorderHelper
{
	private readonly FATabViewListView _owner;

	private LiveReorderIndices _liveReorderIndices = new LiveReorderIndices(-1, -1, -1);

	private DispatcherTimer _liveReorderTimer;

	private readonly MovedItems _movedItems = new MovedItems();

	private List<Rect> _cachedContainerBounds;

	private int _firstCachedContainerIndex = -1;

	public Panel ItemsPanelRoot => ((ItemsControl)_owner).ItemsPanelRoot;

	public LiveReorderHelper(FATabViewListView owner)
	{
		_owner = owner;
	}

	public void ProcessLiveReorder(DragEventArgs args, int dragItemIndex)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		Orientation? logicalOrientation = _owner.GetLogicalOrientation();
		if (!logicalOrientation.HasValue)
		{
			return;
		}
		if (ShouldCacheContainerBounds())
		{
			((Layoutable)_owner).UpdateLayout();
			CacheContainerBounds();
		}
		Point dragPoint = AdjustDragPoint(args.GetPosition((Visual)(object)_owner), _owner.Scroller.Offset, logicalOrientation);
		int num = dragItemIndex;
		int num2 = -1;
		int closestElement = GetClosestElement(dragPoint);
		int num3 = _liveReorderIndices.draggedOverIndex;
		int itemCount = ((ItemsControl)_owner).ItemCount;
		if (num == -1)
		{
			num = itemCount;
		}
		if (num3 == -1)
		{
			num3 = num;
		}
		num2 = GetClosestElement(dragPoint, requestingInsertionIndex: true);
		if (num == itemCount && num2 == itemCount - 1)
		{
			Control val = ((ItemsControl)_owner).ContainerFromIndex(num2);
			if (val is FATabViewItem)
			{
				Point position = args.GetPosition((Visual)(object)val);
				Rect bounds = ((Visual)val).Bounds;
				if (IsInBottomHalf(position, new Rect(((Rect)(ref bounds)).Size), logicalOrientation.Value))
				{
					num2 = itemCount;
				}
			}
		}
		closestElement = ((num2 != itemCount) ? GetDragOverIndex(closestElement, num2, num3) : itemCount);
		_liveReorderIndices = new LiveReorderIndices(num, closestElement, itemCount);
		if (num3 == num || num3 != closestElement)
		{
			StartLiveReorderTimer();
		}
		static int GetDragOverIndex(int closestElementIndex, int insertionIndex, int previousDragOverIndex)
		{
			int num4 = closestElementIndex;
			if (insertionIndex == closestElementIndex)
			{
				if (previousDragOverIndex < insertionIndex)
				{
					num4--;
				}
			}
			else if (previousDragOverIndex >= insertionIndex)
			{
				num4++;
			}
			return num4;
		}
		static bool IsInBottomHalf(Point pt, Rect rc, Orientation orientation)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			if ((int)orientation == 0)
			{
				return ((Point)(ref pt)).X - ((Rect)(ref rc)).Left >= ((Rect)(ref rc)).Width * 0.5;
			}
			return ((Point)(ref pt)).Y - ((Rect)(ref rc)).Top >= ((Rect)(ref rc)).Height * 0.5;
		}
	}

	public void ResetAllItemsForLiveReorder()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		StopLiveReorderTimer();
		ReadOnlySpan<MovedItem> readOnlySpan = _movedItems.AsSpan();
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			MovedItem movedItem = readOnlySpan[i];
			if (movedItem.destinationIndex != -1)
			{
				Control val = ((ItemsControl)_owner).ContainerFromIndex(movedItem.sourceIndex);
				if (val != null)
				{
					Control val2 = val;
					((Layoutable)val2).Arrange(movedItem.sourceRect);
				}
			}
		}
		_movedItems.Clear();
		_liveReorderIndices = new LiveReorderIndices(-1, -1, -1);
		ClearContainerBoundsCache();
	}

	public int GetInsertionIndexForLiveReorder()
	{
		int draggedItemIndex = _liveReorderIndices.draggedItemIndex;
		int num = _liveReorderIndices.draggedOverIndex;
		if (draggedItemIndex < num)
		{
			num++;
		}
		if (num > _liveReorderIndices.itemsCount)
		{
			num = _liveReorderIndices.itemsCount;
		}
		return num;
	}

	public int GetClosestElement(Point dragPoint, bool requestingInsertionIndex = false)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Invalid comparison between Unknown and I4
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		Panel itemsPanelRoot = ItemsPanelRoot;
		VirtualizingStackPanel val = (VirtualizingStackPanel)(object)((itemsPanelRoot is VirtualizingStackPanel) ? itemsPanelRoot : null);
		if (val != null)
		{
			int firstCachedContainerIndex = _firstCachedContainerIndex;
			int num = firstCachedContainerIndex + _cachedContainerBounds.Count - 1;
			Orientation orientation = val.Orientation;
			_movedItems.AsSpan();
			int num2 = -1;
			double num3 = double.PositiveInfinity;
			Rect val2 = default(Rect);
			for (int i = firstCachedContainerIndex; i <= num; i++)
			{
				Rect val3 = _cachedContainerBounds[i - firstCachedContainerIndex];
				double num5;
				if ((int)orientation == 0)
				{
					double num4 = double.Clamp(((Point)(ref dragPoint)).X, ((Rect)(ref val3)).X, ((Rect)(ref val3)).Right);
					num5 = double.Abs(((Point)(ref dragPoint)).X - num4);
				}
				else
				{
					double num6 = double.Clamp(((Point)(ref dragPoint)).Y, ((Rect)(ref val3)).Y, ((Rect)(ref val3)).Bottom);
					num5 = double.Abs(((Point)(ref dragPoint)).Y - num6);
				}
				if (num5 < num3)
				{
					num3 = num5;
					num2 = i;
					val2 = val3;
				}
			}
			if (requestingInsertionIndex)
			{
				if ((int)orientation == 0)
				{
					if (((Point)(ref dragPoint)).X - ((Rect)(ref val2)).X >= ((Rect)(ref val2)).Width * 0.5)
					{
						num2++;
					}
				}
				else if ((int)orientation == 1 && ((Point)(ref dragPoint)).Y - ((Rect)(ref val2)).Y >= ((Rect)(ref val2)).Height * 0.5)
				{
					num2++;
				}
			}
			return num2;
		}
		_ = itemsPanelRoot is StackPanel;
		return -1;
	}

	private void StartLiveReorderTimer()
	{
		StopLiveReorderTimer();
		EnsureLiveReorderTimer();
		_liveReorderTimer.Interval = TimeSpan.FromMilliseconds(200L);
		_liveReorderTimer.Start();
	}

	private void EnsureLiveReorderTimer()
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		if (_liveReorderTimer == null)
		{
			_liveReorderTimer = new DispatcherTimer();
			_liveReorderTimer.Tick += LiveReorderTimerTickHandler;
		}
	}

	private void LiveReorderTimerTickHandler(object sender, EventArgs e)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Invalid comparison between Unknown and I4
		StopLiveReorderTimer();
		Orientation value = _owner.GetLogicalOrientation().Value;
		using PooledList<MovedItem> newItems = new PooledList<MovedItem>();
		using PooledList<MovedItem> newItemsToMove = new PooledList<MovedItem>();
		using PooledList<MovedItem> pooledList = new PooledList<MovedItem>();
		GetNewMovedItemsForLiveReorder(newItems);
		_movedItems.Update((int)value == 1, newItems, newItemsToMove, pooledList);
		MoveItemsForLiveReorder(areNewItems: false, pooledList);
		MoveItemsForLiveReorder(areNewItems: true, newItemsToMove);
	}

	private void GetNewMovedItemsForLiveReorder(IList<MovedItem> newItems)
	{
		int draggedItemIndex = _liveReorderIndices.draggedItemIndex;
		int draggedOverIndex = _liveReorderIndices.draggedOverIndex;
		int num = ((draggedItemIndex < draggedOverIndex) ? 1 : (-1));
		newItems.Clear();
		for (int i = draggedItemIndex; i != draggedOverIndex; i += num)
		{
			int targetIndex = i - num;
			if (i == draggedItemIndex)
			{
				targetIndex = -1;
			}
			AddNewItemForLiveReorder(i, targetIndex, newItems, _liveReorderIndices.itemsCount, this);
		}
		AddNewItemForLiveReorder(draggedOverIndex, draggedOverIndex - num, newItems, _liveReorderIndices.itemsCount, this);
		static void AddNewItemForLiveReorder(int sourceIndex, int num2, IList<MovedItem> list, int itemsCount, LiveReorderHelper host)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			Rect srcRc = default(Rect);
			Rect dstRc = default(Rect);
			if (sourceIndex != num2)
			{
				srcRc = GetLayoutSlot(host, sourceIndex);
			}
			if (num2 != -1 && num2 != itemsCount)
			{
				dstRc = GetLayoutSlot(host, num2);
			}
			list.Add(new MovedItem(sourceIndex, num2, srcRc, dstRc));
		}
		static Rect GetLayoutSlot(LiveReorderHelper host, int index)
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			int num2 = ((host.ItemsPanelRoot is VirtualizingStackPanel) ? (index - host._firstCachedContainerIndex) : index);
			if (num2 < 0 || num2 >= host._cachedContainerBounds.Count)
			{
				return default(Rect);
			}
			return host._cachedContainerBounds[num2];
		}
	}

	private void MoveItemsForLiveReorder(bool areNewItems, PooledList<MovedItem> newItemsToMove)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		Span<MovedItem> span = newItemsToMove.AsSpan();
		for (int i = 0; i < span.Length; i++)
		{
			MovedItem movedItem = span[i];
			Control val = ((ItemsControl)_owner).ContainerFromIndex(movedItem.sourceIndex);
			if (val != null)
			{
				Control val2 = val;
				Rect val3 = ((!areNewItems) ? movedItem.sourceRect : movedItem.destinationRect);
				((Layoutable)val2).Arrange(val3);
			}
		}
	}

	private void StopLiveReorderTimer()
	{
		DispatcherTimer liveReorderTimer = _liveReorderTimer;
		if (liveReorderTimer != null)
		{
			liveReorderTimer.Stop();
		}
	}

	private bool ShouldCacheContainerBounds()
	{
		if (_cachedContainerBounds != null)
		{
			return _cachedContainerBounds.Count == 0;
		}
		return true;
	}

	private void CacheContainerBounds()
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		Panel itemsPanelRoot = ItemsPanelRoot;
		VirtualizingStackPanel val = (VirtualizingStackPanel)(object)((itemsPanelRoot is VirtualizingStackPanel) ? itemsPanelRoot : null);
		if (val != null)
		{
			int firstRealizedIndex = val.FirstRealizedIndex;
			int lastRealizedIndex = val.LastRealizedIndex;
			_firstCachedContainerIndex = firstRealizedIndex;
			if (_cachedContainerBounds == null)
			{
				_cachedContainerBounds = new List<Rect>(lastRealizedIndex - firstRealizedIndex + 1);
			}
			for (int i = firstRealizedIndex; i <= lastRealizedIndex; i++)
			{
				Control val2 = ((ItemsControl)_owner).ContainerFromIndex(i);
				_cachedContainerBounds.Add(((Visual)val2).Bounds);
			}
		}
		else if (itemsPanelRoot is StackPanel)
		{
			_firstCachedContainerIndex = 0;
			int itemCount = ((ItemsControl)_owner).ItemCount;
			if (_cachedContainerBounds == null)
			{
				_cachedContainerBounds = new List<Rect>(itemCount);
			}
			for (int j = 0; j < itemCount; j++)
			{
				_cachedContainerBounds.Add(((Visual)((AvaloniaList<Control>)(object)itemsPanelRoot.Children)[j]).Bounds);
			}
		}
	}

	public void ClearContainerBoundsCache(bool clearCompletely = false)
	{
		_cachedContainerBounds?.Clear();
		if (clearCompletely)
		{
			_cachedContainerBounds = null;
		}
		_firstCachedContainerIndex = -1;
	}

	private static Point AdjustDragPoint(Point rawPoint, Vector offset, Orientation? orientation)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (orientation.HasValue)
		{
			if ((int)orientation.Value == 0)
			{
				return new Point(((Point)(ref rawPoint)).X + ((Vector)(ref offset)).X, ((Point)(ref rawPoint)).Y);
			}
			return new Point(((Point)(ref rawPoint)).X, ((Point)(ref rawPoint)).Y + ((Vector)(ref offset)).Y);
		}
		return rawPoint + offset;
	}
}
