using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Utilities;

internal struct FrugalStructList<T>
{
	internal FrugalListBase<T> _listStore;

	public int Capacity
	{
		get
		{
			if (_listStore != null)
			{
				return _listStore.Capacity;
			}
			return 0;
		}
		set
		{
			int num = 0;
			if (_listStore != null)
			{
				num = _listStore.Capacity;
			}
			if (num < value)
			{
				FrugalListBase<T> frugalListBase = ((value == 1) ? new SingleItemList<T>() : ((value <= 3) ? new ThreeItemList<T>() : ((value > 6) ? ((FrugalListBase<T>)new ArrayItemList<T>(value)) : ((FrugalListBase<T>)new SixItemList<T>()))));
				if (_listStore != null)
				{
					frugalListBase.Promote(_listStore);
				}
				_listStore = frugalListBase;
			}
		}
	}

	public int Count => _listStore?.Count ?? 0;

	public T this[int index]
	{
		get
		{
			if (_listStore != null && index < _listStore.Count && index >= 0)
			{
				return _listStore.EntryAt(index);
			}
			throw new ArgumentOutOfRangeException("index");
		}
		set
		{
			if (_listStore != null && index < _listStore.Count && index >= 0)
			{
				_listStore.SetAt(index, value);
				return;
			}
			throw new ArgumentOutOfRangeException("index");
		}
	}

	public FrugalStructList(int size)
	{
		_listStore = null;
		Capacity = size;
	}

	public FrugalStructList(ICollection collection)
	{
		if (collection.Count > 6)
		{
			_listStore = new ArrayItemList<T>(collection);
			return;
		}
		_listStore = null;
		Capacity = collection.Count;
		foreach (T item in collection)
		{
			Add(item);
		}
	}

	public FrugalStructList(ICollection<T> collection)
	{
		if (collection.Count > 6)
		{
			_listStore = new ArrayItemList<T>(collection);
			return;
		}
		_listStore = null;
		Capacity = collection.Count;
		foreach (T item in collection)
		{
			Add(item);
		}
	}

	public int Add(T value)
	{
		if (_listStore == null)
		{
			_listStore = new SingleItemList<T>();
		}
		switch (_listStore.Add(value))
		{
		case FrugalListStoreState.ThreeItemList:
		{
			ThreeItemList<T> threeItemList = new ThreeItemList<T>();
			threeItemList.Promote(_listStore);
			threeItemList.Add(value);
			_listStore = threeItemList;
			break;
		}
		case FrugalListStoreState.SixItemList:
		{
			SixItemList<T> sixItemList = new SixItemList<T>();
			sixItemList.Promote(_listStore);
			_listStore = sixItemList;
			sixItemList.Add(value);
			_listStore = sixItemList;
			break;
		}
		case FrugalListStoreState.Array:
		{
			ArrayItemList<T> arrayItemList = new ArrayItemList<T>(_listStore.Count + 1);
			arrayItemList.Promote(_listStore);
			_listStore = arrayItemList;
			arrayItemList.Add(value);
			_listStore = arrayItemList;
			break;
		}
		default:
			throw new InvalidOperationException("Cannot promote from Array.");
		case FrugalListStoreState.Success:
			break;
		}
		return _listStore.Count - 1;
	}

	public void Clear()
	{
		_listStore?.Clear();
	}

	public bool Contains(T value)
	{
		if (_listStore != null && _listStore.Count > 0)
		{
			return _listStore.Contains(value);
		}
		return false;
	}

	public int IndexOf(T value)
	{
		if (_listStore != null && _listStore.Count > 0)
		{
			return _listStore.IndexOf(value);
		}
		return -1;
	}

	public void Insert(int index, T value)
	{
		if (index == 0 || (_listStore != null && index <= _listStore.Count && index >= 0))
		{
			int capacity = 1;
			if (_listStore != null && _listStore.Count == _listStore.Capacity)
			{
				capacity = Capacity + 1;
			}
			Capacity = capacity;
			_listStore.Insert(index, value);
			return;
		}
		throw new ArgumentOutOfRangeException("index");
	}

	public bool Remove(T value)
	{
		if (_listStore != null && _listStore.Count > 0)
		{
			return _listStore.Remove(value);
		}
		return false;
	}

	public void RemoveAt(int index)
	{
		if (_listStore != null && index < _listStore.Count && index >= 0)
		{
			_listStore.RemoveAt(index);
			return;
		}
		throw new ArgumentOutOfRangeException("index");
	}

	public void EnsureIndex(int index)
	{
		if (index >= 0)
		{
			int num = index + 1 - Count;
			if (num > 0)
			{
				Capacity = index + 1;
				T value = default(T);
				for (int i = 0; i < num; i++)
				{
					_listStore.Add(value);
				}
			}
			return;
		}
		throw new ArgumentOutOfRangeException("index");
	}

	public T[] ToArray()
	{
		if (_listStore != null && _listStore.Count > 0)
		{
			return _listStore.ToArray();
		}
		return null;
	}

	public void CopyTo(T[] array, int index)
	{
		if (_listStore != null && _listStore.Count > 0)
		{
			_listStore.CopyTo(array, index);
		}
	}

	public FrugalStructList<T> Clone()
	{
		FrugalStructList<T> result = default(FrugalStructList<T>);
		if (_listStore != null)
		{
			result._listStore = (FrugalListBase<T>)_listStore.Clone();
		}
		return result;
	}
}
