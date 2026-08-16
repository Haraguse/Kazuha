using System;

namespace Avalonia.Utilities;

/// <summary>
/// A simple class to handle a list with 3 items.  Perf analysis showed
/// that this yielded better memory locality and perf than an object and an array.
/// </summary>
internal sealed class ThreeItemList<T> : FrugalListBase<T>
{
	private const int SIZE = 3;

	private T _entry0;

	private T _entry1;

	private T _entry2;

	public override int Capacity => 3;

	public override FrugalListStoreState Add(T value)
	{
		switch (_count)
		{
		case 0:
			_entry0 = value;
			break;
		case 1:
			_entry1 = value;
			break;
		case 2:
			_entry2 = value;
			break;
		default:
			return FrugalListStoreState.SixItemList;
		}
		_count++;
		return FrugalListStoreState.Success;
	}

	public override void Clear()
	{
		_entry0 = default(T);
		_entry1 = default(T);
		_entry2 = default(T);
		_count = 0;
	}

	public override bool Contains(T value)
	{
		return -1 != IndexOf(value);
	}

	public override int IndexOf(T value)
	{
		if (_entry0.Equals(value))
		{
			return 0;
		}
		if (_count > 1)
		{
			if (_entry1.Equals(value))
			{
				return 1;
			}
			if (3 == _count && _entry2.Equals(value))
			{
				return 2;
			}
		}
		return -1;
	}

	public override void Insert(int index, T value)
	{
		if (_count < 3)
		{
			switch (index)
			{
			case 0:
				_entry2 = _entry1;
				_entry1 = _entry0;
				_entry0 = value;
				break;
			case 1:
				_entry2 = _entry1;
				_entry1 = value;
				break;
			case 2:
				_entry2 = value;
				break;
			default:
				throw new ArgumentOutOfRangeException("index");
			}
			_count++;
			return;
		}
		throw new ArgumentOutOfRangeException("index");
	}

	public override void SetAt(int index, T value)
	{
		switch (index)
		{
		case 0:
			_entry0 = value;
			break;
		case 1:
			_entry1 = value;
			break;
		case 2:
			_entry2 = value;
			break;
		default:
			throw new ArgumentOutOfRangeException("index");
		}
	}

	public override bool Remove(T value)
	{
		if (_entry0.Equals(value))
		{
			RemoveAt(0);
			return true;
		}
		if (_count > 1)
		{
			if (_entry1.Equals(value))
			{
				RemoveAt(1);
				return true;
			}
			if (3 == _count && _entry2.Equals(value))
			{
				RemoveAt(2);
				return true;
			}
		}
		return false;
	}

	public override void RemoveAt(int index)
	{
		switch (index)
		{
		case 0:
			_entry0 = _entry1;
			_entry1 = _entry2;
			break;
		case 1:
			_entry1 = _entry2;
			break;
		default:
			throw new ArgumentOutOfRangeException("index");
		case 2:
			break;
		}
		_entry2 = default(T);
		_count--;
	}

	public override T EntryAt(int index)
	{
		return index switch
		{
			0 => _entry0, 
			1 => _entry1, 
			2 => _entry2, 
			_ => throw new ArgumentOutOfRangeException("index"), 
		};
	}

	public override void Promote(FrugalListBase<T> oldList)
	{
		int count = oldList.Count;
		if (3 >= count)
		{
			SetCount(oldList.Count);
			switch (count)
			{
			case 3:
				SetAt(0, oldList.EntryAt(0));
				SetAt(1, oldList.EntryAt(1));
				SetAt(2, oldList.EntryAt(2));
				break;
			case 2:
				SetAt(0, oldList.EntryAt(0));
				SetAt(1, oldList.EntryAt(1));
				break;
			case 1:
				SetAt(0, oldList.EntryAt(0));
				break;
			default:
				throw new ArgumentOutOfRangeException("oldList");
			case 0:
				break;
			}
			return;
		}
		throw new ArgumentException($"Cannot promote from '{oldList}' to '{ToString()}' because the target map is too small.", "oldList");
	}

	public void Promote(SingleItemList<T> oldList)
	{
		SetCount(oldList.Count);
		SetAt(0, oldList.EntryAt(0));
	}

	public void Promote(ThreeItemList<T> oldList)
	{
		int count = oldList.Count;
		SetCount(oldList.Count);
		switch (count)
		{
		case 3:
			SetAt(0, oldList.EntryAt(0));
			SetAt(1, oldList.EntryAt(1));
			SetAt(2, oldList.EntryAt(2));
			break;
		case 2:
			SetAt(0, oldList.EntryAt(0));
			SetAt(1, oldList.EntryAt(1));
			break;
		case 1:
			SetAt(0, oldList.EntryAt(0));
			break;
		default:
			throw new ArgumentOutOfRangeException("oldList");
		case 0:
			break;
		}
	}

	public override T[] ToArray()
	{
		T[] array = new T[_count];
		array[0] = _entry0;
		if (_count >= 2)
		{
			array[1] = _entry1;
			if (_count == 3)
			{
				array[2] = _entry2;
			}
		}
		return array;
	}

	public override void CopyTo(T[] array, int index)
	{
		array[index] = _entry0;
		if (_count >= 2)
		{
			array[index + 1] = _entry1;
			if (_count == 3)
			{
				array[index + 2] = _entry2;
			}
		}
	}

	public override object Clone()
	{
		ThreeItemList<T> threeItemList = new ThreeItemList<T>();
		threeItemList.Promote(this);
		return threeItemList;
	}

	private void SetCount(int value)
	{
		if (value >= 0 && value <= 3)
		{
			_count = value;
			return;
		}
		throw new ArgumentOutOfRangeException("value");
	}
}
