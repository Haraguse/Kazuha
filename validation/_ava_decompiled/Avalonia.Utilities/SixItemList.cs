using System;

namespace Avalonia.Utilities;

/// <summary>
/// A simple class to handle a list with 6 items.
/// </summary>
internal sealed class SixItemList<T> : FrugalListBase<T>
{
	private const int SIZE = 6;

	private T _entry0;

	private T _entry1;

	private T _entry2;

	private T _entry3;

	private T _entry4;

	private T _entry5;

	public override int Capacity => 6;

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
		case 3:
			_entry3 = value;
			break;
		case 4:
			_entry4 = value;
			break;
		case 5:
			_entry5 = value;
			break;
		default:
			return FrugalListStoreState.Array;
		}
		_count++;
		return FrugalListStoreState.Success;
	}

	public override void Clear()
	{
		_entry0 = default(T);
		_entry1 = default(T);
		_entry2 = default(T);
		_entry3 = default(T);
		_entry4 = default(T);
		_entry5 = default(T);
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
			if (_count > 2)
			{
				if (_entry2.Equals(value))
				{
					return 2;
				}
				if (_count > 3)
				{
					if (_entry3.Equals(value))
					{
						return 3;
					}
					if (_count > 4)
					{
						if (_entry4.Equals(value))
						{
							return 4;
						}
						if (6 == _count && _entry5.Equals(value))
						{
							return 5;
						}
					}
				}
			}
		}
		return -1;
	}

	public override void Insert(int index, T value)
	{
		if (_count < 6)
		{
			switch (index)
			{
			case 0:
				_entry5 = _entry4;
				_entry4 = _entry3;
				_entry3 = _entry2;
				_entry2 = _entry1;
				_entry1 = _entry0;
				_entry0 = value;
				break;
			case 1:
				_entry5 = _entry4;
				_entry4 = _entry3;
				_entry3 = _entry2;
				_entry2 = _entry1;
				_entry1 = value;
				break;
			case 2:
				_entry5 = _entry4;
				_entry4 = _entry3;
				_entry3 = _entry2;
				_entry2 = value;
				break;
			case 3:
				_entry5 = _entry4;
				_entry4 = _entry3;
				_entry3 = value;
				break;
			case 4:
				_entry5 = _entry4;
				_entry4 = value;
				break;
			case 5:
				_entry5 = value;
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
		case 3:
			_entry3 = value;
			break;
		case 4:
			_entry4 = value;
			break;
		case 5:
			_entry5 = value;
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
			if (_count > 2)
			{
				if (_entry2.Equals(value))
				{
					RemoveAt(2);
					return true;
				}
				if (_count > 3)
				{
					if (_entry3.Equals(value))
					{
						RemoveAt(3);
						return true;
					}
					if (_count > 4)
					{
						if (_entry4.Equals(value))
						{
							RemoveAt(4);
							return true;
						}
						if (6 == _count && _entry5.Equals(value))
						{
							RemoveAt(5);
							return true;
						}
					}
				}
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
			_entry2 = _entry3;
			_entry3 = _entry4;
			_entry4 = _entry5;
			break;
		case 1:
			_entry1 = _entry2;
			_entry2 = _entry3;
			_entry3 = _entry4;
			_entry4 = _entry5;
			break;
		case 2:
			_entry2 = _entry3;
			_entry3 = _entry4;
			_entry4 = _entry5;
			break;
		case 3:
			_entry3 = _entry4;
			_entry4 = _entry5;
			break;
		case 4:
			_entry4 = _entry5;
			break;
		default:
			throw new ArgumentOutOfRangeException("index");
		case 5:
			break;
		}
		_entry5 = default(T);
		_count--;
	}

	public override T EntryAt(int index)
	{
		return index switch
		{
			0 => _entry0, 
			1 => _entry1, 
			2 => _entry2, 
			3 => _entry3, 
			4 => _entry4, 
			5 => _entry5, 
			_ => throw new ArgumentOutOfRangeException("index"), 
		};
	}

	public override void Promote(FrugalListBase<T> oldList)
	{
		int count = oldList.Count;
		if (6 >= count)
		{
			SetCount(oldList.Count);
			switch (count)
			{
			case 6:
				SetAt(0, oldList.EntryAt(0));
				SetAt(1, oldList.EntryAt(1));
				SetAt(2, oldList.EntryAt(2));
				SetAt(3, oldList.EntryAt(3));
				SetAt(4, oldList.EntryAt(4));
				SetAt(5, oldList.EntryAt(5));
				break;
			case 5:
				SetAt(0, oldList.EntryAt(0));
				SetAt(1, oldList.EntryAt(1));
				SetAt(2, oldList.EntryAt(2));
				SetAt(3, oldList.EntryAt(3));
				SetAt(4, oldList.EntryAt(4));
				break;
			case 4:
				SetAt(0, oldList.EntryAt(0));
				SetAt(1, oldList.EntryAt(1));
				SetAt(2, oldList.EntryAt(2));
				SetAt(3, oldList.EntryAt(3));
				break;
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

	public void Promote(ThreeItemList<T> oldList)
	{
		int count = oldList.Count;
		if (count <= 6)
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

	public void Promote(SixItemList<T> oldList)
	{
		int count = oldList.Count;
		SetCount(oldList.Count);
		switch (count)
		{
		case 6:
			SetAt(0, oldList.EntryAt(0));
			SetAt(1, oldList.EntryAt(1));
			SetAt(2, oldList.EntryAt(2));
			SetAt(3, oldList.EntryAt(3));
			SetAt(4, oldList.EntryAt(4));
			SetAt(5, oldList.EntryAt(5));
			break;
		case 5:
			SetAt(0, oldList.EntryAt(0));
			SetAt(1, oldList.EntryAt(1));
			SetAt(2, oldList.EntryAt(2));
			SetAt(3, oldList.EntryAt(3));
			SetAt(4, oldList.EntryAt(4));
			break;
		case 4:
			SetAt(0, oldList.EntryAt(0));
			SetAt(1, oldList.EntryAt(1));
			SetAt(2, oldList.EntryAt(2));
			SetAt(3, oldList.EntryAt(3));
			break;
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
		if (_count >= 1)
		{
			array[0] = _entry0;
			if (_count >= 2)
			{
				array[1] = _entry1;
				if (_count >= 3)
				{
					array[2] = _entry2;
					if (_count >= 4)
					{
						array[3] = _entry3;
						if (_count >= 5)
						{
							array[4] = _entry4;
							if (_count == 6)
							{
								array[5] = _entry5;
							}
						}
					}
				}
			}
		}
		return array;
	}

	public override void CopyTo(T[] array, int index)
	{
		if (_count < 1)
		{
			return;
		}
		array[index] = _entry0;
		if (_count < 2)
		{
			return;
		}
		array[index + 1] = _entry1;
		if (_count < 3)
		{
			return;
		}
		array[index + 2] = _entry2;
		if (_count < 4)
		{
			return;
		}
		array[index + 3] = _entry3;
		if (_count >= 5)
		{
			array[index + 4] = _entry4;
			if (_count == 6)
			{
				array[index + 5] = _entry5;
			}
		}
	}

	public override object Clone()
	{
		SixItemList<T> sixItemList = new SixItemList<T>();
		sixItemList.Promote(this);
		return sixItemList;
	}

	private void SetCount(int value)
	{
		if (value >= 0 && value <= 6)
		{
			_count = value;
			return;
		}
		throw new ArgumentOutOfRangeException("value");
	}
}
