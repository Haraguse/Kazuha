using System;

namespace Avalonia.Utilities;

/// <summary>
/// A simple class to handle a single item
/// </summary>
internal sealed class SingleItemList<T> : FrugalListBase<T>
{
	private const int SIZE = 1;

	private T _loneEntry;

	public override int Capacity => 1;

	public override FrugalListStoreState Add(T value)
	{
		if (_count == 0)
		{
			_loneEntry = value;
			_count++;
			return FrugalListStoreState.Success;
		}
		return FrugalListStoreState.ThreeItemList;
	}

	public override void Clear()
	{
		_loneEntry = default(T);
		_count = 0;
	}

	public override bool Contains(T value)
	{
		return _loneEntry.Equals(value);
	}

	public override int IndexOf(T value)
	{
		if (_loneEntry.Equals(value))
		{
			return 0;
		}
		return -1;
	}

	public override void Insert(int index, T value)
	{
		if (_count < 1 && index < 1)
		{
			_loneEntry = value;
			_count++;
			return;
		}
		throw new ArgumentOutOfRangeException("index");
	}

	public override void SetAt(int index, T value)
	{
		_loneEntry = value;
	}

	public override bool Remove(T value)
	{
		if (_loneEntry.Equals(value))
		{
			_loneEntry = default(T);
			_count--;
			return true;
		}
		return false;
	}

	public override void RemoveAt(int index)
	{
		if (index == 0)
		{
			_loneEntry = default(T);
			_count--;
			return;
		}
		throw new ArgumentOutOfRangeException("index");
	}

	public override T EntryAt(int index)
	{
		return _loneEntry;
	}

	public override void Promote(FrugalListBase<T> oldList)
	{
		if (1 == oldList.Count)
		{
			SetCount(1);
			SetAt(0, oldList.EntryAt(0));
			return;
		}
		throw new ArgumentException($"Cannot promote from '{oldList}' to '{ToString()}' because the target map is too small.", "oldList");
	}

	public void Promote(SingleItemList<T> oldList)
	{
		SetCount(oldList.Count);
		SetAt(0, oldList.EntryAt(0));
	}

	public override T[] ToArray()
	{
		return new T[1] { _loneEntry };
	}

	public override void CopyTo(T[] array, int index)
	{
		array[index] = _loneEntry;
	}

	public override object Clone()
	{
		SingleItemList<T> singleItemList = new SingleItemList<T>();
		singleItemList.Promote(this);
		return singleItemList;
	}

	private void SetCount(int value)
	{
		if (value >= 0 && value <= 1)
		{
			_count = value;
			return;
		}
		throw new ArgumentOutOfRangeException("value");
	}
}
