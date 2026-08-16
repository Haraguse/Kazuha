using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities;

internal struct PooledInlineList<T> : IDisposable, IEnumerable<T>, IEnumerable where T : class
{
	private class SimplePooledList : IDisposable
	{
		public int Count;

		public T[]? Items;

		public void Add(T item)
		{
			if (Items == null)
			{
				Items = ArrayPool<T>.Shared.Rent(4);
			}
			else if (Count == Items.Length)
			{
				GrowItems(Count * 2);
			}
			Items[Count] = item;
			Count++;
		}

		private void ReturnToPool(T[] items)
		{
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
			{
				Array.Clear(items, 0, Count);
			}
			ArrayPool<T>.Shared.Return(items);
		}

		private void GrowItems(int count)
		{
			if (count >= Count)
			{
				T[] array = ArrayPool<T>.Shared.Rent(count);
				Array.Copy(Items, array, Count);
				ReturnToPool(Items);
				Items = array;
			}
		}

		public void EnsureCapacity(int count)
		{
			if (Items == null)
			{
				Items = ArrayPool<T>.Shared.Rent(count);
			}
			else if (Items.Length < count)
			{
				GrowItems(count);
			}
		}

		public void Dispose()
		{
			if (Items != null)
			{
				ReturnToPool(Items);
				Items = null;
				Count = 0;
			}
		}

		public bool Remove(T item)
		{
			for (int i = 0; i < Count; i++)
			{
				if (item == Items[i])
				{
					Items[i] = null;
					Count--;
					if (i < Count)
					{
						Array.Copy(Items, i + 1, Items, i, Count - i);
					}
					return true;
				}
			}
			return false;
		}
	}

	public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
	{
		private readonly T? _singleItem;

		private int _index;

		private readonly SimplePooledList? _list;

		object IEnumerator.Current => Current;

		public T Current
		{
			get
			{
				if (_list != null)
				{
					return _list.Items[_index];
				}
				return _singleItem;
			}
		}

		public Enumerator(object? item)
		{
			_singleItem = null;
			_index = -1;
			_list = item as SimplePooledList;
			if (_list == null)
			{
				_singleItem = (T)item;
			}
		}

		public bool MoveNext()
		{
			if (_singleItem != null)
			{
				if (_index >= 0)
				{
					return false;
				}
				_index = 0;
				return true;
			}
			if (_list != null)
			{
				if (_index >= _list.Count - 1)
				{
					return false;
				}
				_index++;
				return true;
			}
			return false;
		}

		public void Reset()
		{
			throw new NotSupportedException();
		}

		public void Dispose()
		{
		}
	}

	private object? _item;

	public int Count
	{
		get
		{
			if (_item != null)
			{
				if (!(_item is SimplePooledList simplePooledList))
				{
					return 1;
				}
				return simplePooledList.Count;
			}
			return 0;
		}
	}

	public PooledInlineList()
	{
		_item = null;
	}

	public void Add(T item)
	{
		if (_item == null)
		{
			_item = item;
			return;
		}
		if (_item is SimplePooledList simplePooledList)
		{
			simplePooledList.Add(item);
			return;
		}
		ConvertToList();
		Add(item);
	}

	public bool Remove(T item)
	{
		if (item == _item)
		{
			_item = null;
			return true;
		}
		if (item is SimplePooledList simplePooledList)
		{
			return simplePooledList.Remove(item);
		}
		return false;
	}

	private void ConvertToList()
	{
		if (!(_item is SimplePooledList))
		{
			SimplePooledList simplePooledList = new SimplePooledList();
			if (_item != null)
			{
				simplePooledList.Add((T)_item);
			}
			_item = simplePooledList;
		}
	}

	public void EnsureCapacity(int count)
	{
		if (count >= 2)
		{
			ConvertToList();
			((SimplePooledList)_item).EnsureCapacity(count);
		}
	}

	public void Dispose()
	{
		if (_item is SimplePooledList simplePooledList)
		{
			simplePooledList.Dispose();
		}
		_item = null;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return GetEnumerator();
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_item);
	}

	/// <summary>
	///  For compositor serialization purposes only, takes the ownership of previously transferred state
	/// </summary>
	public PooledInlineList(object? rawState)
	{
		_item = rawState;
	}

	/// <summary>
	///  For compositor serialization purposes only, gives up the ownership of the internal state and returns it
	/// </summary>
	public object? TransferRawState()
	{
		object? item = _item;
		_item = null;
		return item;
	}
}
