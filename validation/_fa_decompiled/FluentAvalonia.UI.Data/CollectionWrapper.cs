using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Data;

internal class CollectionWrapper : IAvaloniaList<object>, IList<object>, ICollection<object>, IEnumerable<object>, IEnumerable, IAvaloniaReadOnlyList<object>, IReadOnlyList<object>, IReadOnlyCollection<object>, INotifyCollectionChanged, INotifyPropertyChanged, IList, ICollection
{
	private IEnumerable _collection;

	public object this[int index]
	{
		get
		{
			return _collection.ElementAt(index);
		}
		set
		{
			ThrowIfNotMutable();
			if (_collection is IList list)
			{
				list[index] = value;
			}
		}
	}

	public int Count => _collection.Count();

	public bool IsReadOnly
	{
		get
		{
			if (_collection is ICollection<object> collection)
			{
				return collection.IsReadOnly;
			}
			return false;
		}
	}

	object IReadOnlyList<object>.this[int index] => this[index];

	bool IList.IsFixedSize => false;

	bool IList.IsReadOnly => IsReadOnly;

	int ICollection.Count => Count;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => false;

	object IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			this[index] = value;
		}
	}

	public event NotifyCollectionChangedEventHandler CollectionChanged;

	public event PropertyChangedEventHandler PropertyChanged;

	public CollectionWrapper(IEnumerable collection)
	{
		_collection = collection;
		if (collection is INotifyCollectionChanged notifyCollectionChanged)
		{
			notifyCollectionChanged.CollectionChanged += OnBackingCollectionChanged;
		}
	}

	private void OnBackingCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		CollectionChanged?.Invoke(this, e);
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
	}

	public void Add(object item)
	{
		ThrowIfNotMutable();
		if (_collection is IList list)
		{
			list.Add(item);
		}
	}

	public void AddRange(IEnumerable<object> items)
	{
		ThrowIfNotMutable();
		if (!(_collection is IList list))
		{
			return;
		}
		foreach (object item in items)
		{
			list.Add(item);
		}
	}

	public void Clear()
	{
		ThrowIfNotMutable();
		if (_collection is IList list)
		{
			list.Clear();
		}
	}

	public bool Contains(object item)
	{
		return _collection.Contains(item);
	}

	public void CopyTo(object[] array, int arrayIndex)
	{
		if (_collection is ICollection collection)
		{
			collection.CopyTo(array, arrayIndex);
		}
		else
		{
			_collection.Cast<object>().ToList().CopyTo(array, arrayIndex);
		}
	}

	public IEnumerator<object> GetEnumerator()
	{
		if (_collection is IEnumerable<object> enumerable)
		{
			return enumerable.GetEnumerator();
		}
		return _collection.Cast<object>().GetEnumerator();
	}

	public int IndexOf(object item)
	{
		return _collection.IndexOf(item);
	}

	public void Insert(int index, object item)
	{
		ThrowIfNotMutable();
		if (_collection is IList list)
		{
			list.Insert(index, item);
		}
	}

	public void InsertRange(int index, IEnumerable<object> items)
	{
		ThrowIfNotMutable();
		if (!(_collection is IList list))
		{
			return;
		}
		int num = index;
		foreach (object item in items)
		{
			list.Insert(num++, item);
		}
	}

	public void Move(int oldIndex, int newIndex)
	{
		throw new NotImplementedException();
	}

	public void MoveRange(int oldIndex, int count, int newIndex)
	{
		throw new NotImplementedException();
	}

	public bool Remove(object item)
	{
		ThrowIfNotMutable();
		if (_collection is IList list)
		{
			list.Remove(item);
		}
		return false;
	}

	public void RemoveAll(IEnumerable<object> items)
	{
		throw new NotImplementedException();
	}

	public void RemoveAt(int index)
	{
		ThrowIfNotMutable();
		if (_collection is IList list)
		{
			list.RemoveAt(index);
		}
	}

	public void RemoveRange(int index, int count)
	{
		throw new NotImplementedException();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _collection.GetEnumerator();
	}

	private void ThrowIfNotMutable()
	{
		if (!IsReadOnly || _collection is INotifyCollectionChanged)
		{
			return;
		}
		throw new NotSupportedException("Collection is not mutable. Collection groups must implement INotifyCollectionChanged");
	}

	int IList.Add(object value)
	{
		Add(value);
		return _collection.Count();
	}

	void IList.Clear()
	{
		Clear();
	}

	bool IList.Contains(object value)
	{
		return Contains(value);
	}

	int IList.IndexOf(object value)
	{
		return IndexOf(value);
	}

	void IList.Insert(int index, object value)
	{
		Insert(index, value);
	}

	void IList.Remove(object value)
	{
		Remove(value);
	}

	void IList.RemoveAt(int index)
	{
		RemoveAt(index);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		CopyTo((object[])array, index);
	}
}
