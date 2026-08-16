using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;

namespace FluentAvalonia.UI.Controls;

internal class MovedItems : IEnumerable<MovedItem>, IEnumerable
{
	private readonly List<MovedItem> _items = new List<MovedItem>();

	public void Update(in bool isOrientationVertical, IList<MovedItem> newItems, IList<MovedItem> newItemsToMove, IList<MovedItem> oldItemsToMoveBack)
	{
		int count = newItems.Count;
		int count2 = _items.Count;
		int sourceIndex = newItems[0].sourceIndex;
		int sourceIndex2 = newItems[newItems.Count - 1].sourceIndex;
		int num = ((sourceIndex < sourceIndex2) ? 1 : (-1));
		int num2 = -1;
		newItemsToMove.Clear();
		oldItemsToMoveBack.Clear();
		if (count2 > 0)
		{
			int sourceIndex3 = _items[0].sourceIndex;
			List<MovedItem> items = _items;
			int sourceIndex4 = items[items.Count - 1].sourceIndex;
			int num3 = ((sourceIndex3 < sourceIndex4) ? 1 : (-1));
			RemoveMovedItems((num != num3) ? 1 : count, count2 - 1, oldItemsToMoveBack);
			num2 = ((num != num3) ? 1 : count2);
		}
		else
		{
			num2 = 0;
		}
		if (num2 < count)
		{
			AddMovedItems(isOrientationVertical, num2, newItems, newItemsToMove);
		}
	}

	public void RemoveMovedItems(int from, int to, IList<MovedItem> oldItemsToMoveBack)
	{
		for (int num = to; num >= from; num--)
		{
			oldItemsToMoveBack.Add(_items[num]);
			_items.RemoveAt(num);
		}
	}

	public void AddMovedItems(bool isOrientationVertical, int from, IList<MovedItem> newItems, IList<MovedItem> newItemsToMove)
	{
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0393: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		for (int i = from; i < newItems.Count; i++)
		{
			MovedItem movedItem = newItems[i];
			if (_items.Count > 0)
			{
				List<MovedItem> items = _items;
				MovedItem movedItem2 = items[items.Count - 1];
				bool flag = movedItem.sourceIndex > movedItem.destinationIndex;
				if (((Rect)(ref movedItem.sourceRect)).Width == ((Rect)(ref movedItem2.sourceRect)).Width && ((Rect)(ref movedItem.sourceRect)).Height == ((Rect)(ref movedItem2.sourceRect)).Height)
				{
					movedItem.destinationRect = new Rect(((Rect)(ref movedItem2.sourceRect)).X, ((Rect)(ref movedItem2.sourceRect)).Y, ((Rect)(ref movedItem.destinationRect)).Width, ((Rect)(ref movedItem.destinationRect)).Height);
				}
				else if (movedItem2.sourceIndex == _items[0].sourceIndex)
				{
					movedItem.destinationRect = new Rect(((Rect)(ref movedItem2.sourceRect)).X, ((Rect)(ref movedItem2.sourceRect)).Y, ((Rect)(ref movedItem.destinationRect)).Width, ((Rect)(ref movedItem.destinationRect)).Height);
					if (!flag)
					{
						if (isOrientationVertical)
						{
							movedItem.destinationRect = ((Rect)(ref movedItem.destinationRect)).WithY(((Rect)(ref movedItem.destinationRect)).Y - ((Rect)(ref movedItem.sourceRect)).Height - ((Rect)(ref movedItem2.sourceRect)).Height);
							if (((Rect)(ref movedItem.destinationRect)).Y < 0.0)
							{
								movedItem.destinationRect = new Rect(((Rect)(ref movedItem.sourceRect)).X, ((Rect)(ref movedItem.sourceRect)).Y + ((Rect)(ref movedItem.sourceRect)).Height, ((Rect)(ref movedItem.destinationRect)).Width, ((Rect)(ref movedItem.destinationRect)).Height);
							}
						}
						else
						{
							movedItem.destinationRect = ((Rect)(ref movedItem.destinationRect)).WithX(((Rect)(ref movedItem.destinationRect)).X - ((Rect)(ref movedItem.sourceRect)).Width - ((Rect)(ref movedItem2.sourceRect)).Width);
							if (((Rect)(ref movedItem.destinationRect)).X < 0.0)
							{
								movedItem.destinationRect = new Rect(((Rect)(ref movedItem.sourceRect)).X + ((Rect)(ref movedItem.sourceRect)).Width, ((Rect)(ref movedItem.sourceRect)).Y, ((Rect)(ref movedItem.destinationRect)).Width, ((Rect)(ref movedItem.destinationRect)).Height);
							}
						}
					}
				}
				else
				{
					movedItem.destinationRect = new Rect(((Rect)(ref movedItem2.destinationRect)).X, ((Rect)(ref movedItem2.destinationRect)).Y, ((Rect)(ref movedItem.destinationRect)).Width, ((Rect)(ref movedItem.destinationRect)).Height);
					if (isOrientationVertical)
					{
						if (flag)
						{
							movedItem.destinationRect = ((Rect)(ref movedItem.destinationRect)).WithY(((Rect)(ref movedItem.destinationRect)).Y + ((Rect)(ref movedItem2.sourceRect)).Height);
						}
						else
						{
							movedItem.destinationRect = ((Rect)(ref movedItem.destinationRect)).WithY(((Rect)(ref movedItem.destinationRect)).Y - ((Rect)(ref movedItem.sourceRect)).Height);
						}
					}
					else if (flag)
					{
						movedItem.destinationRect = ((Rect)(ref movedItem.destinationRect)).WithX(((Rect)(ref movedItem.destinationRect)).X + ((Rect)(ref movedItem2.sourceRect)).Width);
					}
					else
					{
						movedItem.destinationRect = ((Rect)(ref movedItem.destinationRect)).WithX(((Rect)(ref movedItem.destinationRect)).X - ((Rect)(ref movedItem.sourceRect)).Width);
					}
				}
				movedItem.destinationRect = new Rect(((Rect)(ref movedItem.destinationRect)).X, ((Rect)(ref movedItem.destinationRect)).Y, ((Rect)(ref movedItem.sourceRect)).Width, ((Rect)(ref movedItem.sourceRect)).Height);
				newItemsToMove.Add(movedItem);
				newItems[i] = movedItem;
			}
			_items.Add(movedItem);
		}
	}

	public void Clear()
	{
		_items.Clear();
	}

	public ReadOnlySpan<MovedItem> AsSpan()
	{
		return CollectionsMarshal.AsSpan(_items);
	}

	public IEnumerator<MovedItem> GetEnumerator()
	{
		return _items.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _items.GetEnumerator();
	}
}
