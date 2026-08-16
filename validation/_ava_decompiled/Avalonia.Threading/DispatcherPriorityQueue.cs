using System;
using System.Collections.Generic;

namespace Avalonia.Threading;

internal class DispatcherPriorityQueue
{
	private readonly SortedList<int, PriorityChain> _priorityChains;

	private readonly Stack<PriorityChain> _cacheReusableChains;

	private DispatcherOperation? _head;

	private DispatcherOperation? _tail;

	public DispatcherPriority MaxPriority
	{
		get
		{
			int count = _priorityChains.Count;
			if (count > 0)
			{
				return _priorityChains.Keys[count - 1];
			}
			return DispatcherPriority.Invalid;
		}
	}

	public DispatcherPriorityQueue()
	{
		_priorityChains = new SortedList<int, PriorityChain>();
		_cacheReusableChains = new Stack<PriorityChain>(10);
		_head = (_tail = null);
	}

	public DispatcherOperation Enqueue(DispatcherPriority priority, DispatcherOperation item)
	{
		PriorityChain chain = GetChain(priority);
		InsertItemInSequentialChain(item, _tail);
		InsertItemInPriorityChain(item, chain, chain.Tail);
		return item;
	}

	public DispatcherOperation Dequeue()
	{
		int count = _priorityChains.Count;
		if (count > 0)
		{
			DispatcherOperation head = _priorityChains.Values[count - 1].Head;
			RemoveItem(head);
			return head;
		}
		throw new InvalidOperationException();
	}

	public DispatcherOperation? Peek()
	{
		int count = _priorityChains.Count;
		if (count > 0)
		{
			return _priorityChains.Values[count - 1].Head;
		}
		return null;
	}

	public void RemoveItem(DispatcherOperation item)
	{
		RemoveItemFromPriorityChain(item);
		RemoveItemFromSequentialChain(item);
	}

	public void ChangeItemPriority(DispatcherOperation item, DispatcherPriority priority)
	{
		RemoveItemFromPriorityChain(item);
		PriorityChain chain = GetChain(priority);
		InsertItemInPriorityChain(item, chain);
	}

	private PriorityChain GetChain(DispatcherPriority priority)
	{
		PriorityChain value = null;
		int count = _priorityChains.Count;
		if (count > 0)
		{
			if (priority == _priorityChains.Keys[0])
			{
				value = _priorityChains.Values[0];
			}
			else if (priority == _priorityChains.Keys[count - 1])
			{
				value = _priorityChains.Values[count - 1];
			}
			else if (priority > _priorityChains.Keys[0] && priority < _priorityChains.Keys[count - 1])
			{
				_priorityChains.TryGetValue(priority, out value);
			}
		}
		if (value == null)
		{
			if (_cacheReusableChains.Count > 0)
			{
				value = _cacheReusableChains.Pop();
				value.Priority = priority;
			}
			else
			{
				value = new PriorityChain(priority);
			}
			_priorityChains.Add(priority, value);
		}
		return value;
	}

	private void InsertItemInPriorityChain(DispatcherOperation item, PriorityChain chain)
	{
		if (chain.Head == null)
		{
			InsertItemInPriorityChain(item, chain, null);
			return;
		}
		DispatcherOperation sequentialPrev = item.SequentialPrev;
		while (sequentialPrev != null && sequentialPrev.Chain != chain)
		{
			sequentialPrev = sequentialPrev.SequentialPrev;
		}
		InsertItemInPriorityChain(item, chain, sequentialPrev);
	}

	internal void InsertItemInPriorityChain(DispatcherOperation item, PriorityChain chain, DispatcherOperation? after)
	{
		item.Chain = chain;
		if (after == null)
		{
			if (chain.Head != null)
			{
				chain.Head.PriorityPrev = item;
				item.PriorityNext = chain.Head;
				chain.Head = item;
			}
			else
			{
				DispatcherOperation head = (chain.Tail = item);
				chain.Head = head;
			}
		}
		else
		{
			item.PriorityPrev = after;
			if (after.PriorityNext != null)
			{
				item.PriorityNext = after.PriorityNext;
				after.PriorityNext.PriorityPrev = item;
				after.PriorityNext = item;
			}
			else
			{
				after.PriorityNext = item;
				chain.Tail = item;
			}
		}
		chain.Count++;
	}

	private void RemoveItemFromPriorityChain(DispatcherOperation item)
	{
		if (item.PriorityPrev != null)
		{
			item.PriorityPrev.PriorityNext = item.PriorityNext;
		}
		else
		{
			item.Chain.Head = item.PriorityNext;
		}
		if (item.PriorityNext != null)
		{
			item.PriorityNext.PriorityPrev = item.PriorityPrev;
		}
		else
		{
			item.Chain.Tail = item.PriorityPrev;
		}
		DispatcherOperation priorityPrev = (item.PriorityNext = null);
		item.PriorityPrev = priorityPrev;
		item.Chain.Count--;
		if (item.Chain.Count == 0)
		{
			if (item.Chain.Priority == _priorityChains.Keys[_priorityChains.Count - 1])
			{
				_priorityChains.RemoveAt(_priorityChains.Count - 1);
			}
			else
			{
				_priorityChains.Remove(item.Chain.Priority);
			}
			if (_cacheReusableChains.Count < 10)
			{
				item.Chain.Priority = DispatcherPriority.Invalid;
				_cacheReusableChains.Push(item.Chain);
			}
		}
		item.Chain = null;
	}

	internal void InsertItemInSequentialChain(DispatcherOperation item, DispatcherOperation? after)
	{
		if (after == null)
		{
			if (_head != null)
			{
				_head.SequentialPrev = item;
				item.SequentialNext = _head;
				_head = item;
			}
			else
			{
				_head = (_tail = item);
			}
			return;
		}
		item.SequentialPrev = after;
		if (after.SequentialNext != null)
		{
			item.SequentialNext = after.SequentialNext;
			after.SequentialNext.SequentialPrev = item;
			after.SequentialNext = item;
		}
		else
		{
			after.SequentialNext = item;
			_tail = item;
		}
	}

	private void RemoveItemFromSequentialChain(DispatcherOperation item)
	{
		if (item.SequentialPrev != null)
		{
			item.SequentialPrev.SequentialNext = item.SequentialNext;
		}
		else
		{
			_head = item.SequentialNext;
		}
		if (item.SequentialNext != null)
		{
			item.SequentialNext.SequentialPrev = item.SequentialPrev;
		}
		else
		{
			_tail = item.SequentialPrev;
		}
		DispatcherOperation sequentialPrev = (item.SequentialNext = null);
		item.SequentialPrev = sequentialPrev;
	}

	public List<DispatcherOperation> PeekAll()
	{
		List<DispatcherOperation> list = new List<DispatcherOperation>();
		for (DispatcherOperation dispatcherOperation = _head; dispatcherOperation != null; dispatcherOperation = dispatcherOperation.SequentialNext)
		{
			list.Add(dispatcherOperation);
		}
		return list;
	}

	public void Clear()
	{
		_priorityChains.Clear();
		_cacheReusableChains.Clear();
		_head = (_tail = null);
	}
}
