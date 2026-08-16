using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Diagnostics;

namespace Avalonia.Collections;

/// <summary>
/// A notifying list.
/// </summary>
/// <typeparam name="T">The type of the list items.</typeparam>
/// <remarks>
/// <para>
/// AvaloniaList is similar to <see cref="T:System.Collections.ObjectModel.ObservableCollection`1" />
/// with a few added features:
/// </para>
///
/// <list type="bullet">
/// <item>
/// It can be configured to notify the <see cref="E:Avalonia.Collections.AvaloniaList`1.CollectionChanged" /> event with a
/// <see cref="F:System.Collections.Specialized.NotifyCollectionChangedAction.Remove" /> action instead of a
/// <see cref="F:System.Collections.Specialized.NotifyCollectionChangedAction.Reset" /> when the list is cleared by
/// setting <see cref="P:Avalonia.Collections.AvaloniaList`1.ResetBehavior" /> to <see cref="F:Avalonia.Collections.ResetBehavior.Remove" />.
/// </item>
/// <item>
/// A <see cref="P:Avalonia.Collections.AvaloniaList`1.Validate" /> function can be used to validate each item before insertion.
/// </item>
/// </list>
/// </remarks>
public class AvaloniaList<T> : IAvaloniaList<T>, IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IAvaloniaReadOnlyList<T>, IReadOnlyList<T>, IReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged, IList, ICollection, INotifyCollectionChangedDebug
{
	/// <summary>
	/// Enumerates the elements of a <see cref="T:Avalonia.Collections.AvaloniaList`1" />.
	/// </summary>
	public struct Enumerator(List<T> inner) : IEnumerator<T>, IEnumerator, IDisposable
	{
		private List<T>.Enumerator _innerEnumerator = inner.GetEnumerator();

		public T Current => _innerEnumerator.Current;

		object? IEnumerator.Current => Current;

		public bool MoveNext()
		{
			return _innerEnumerator.MoveNext();
		}

		void IEnumerator.Reset()
		{
			((IEnumerator)_innerEnumerator).Reset();
		}

		public void Dispose()
		{
			_innerEnumerator.Dispose();
		}
	}

	private sealed class ItemValidator : IAvaloniaListItemValidator<T>
	{
		public Action<T> Validate { get; set; }

		public ItemValidator(Action<T> validate)
		{
			Validate = validate;
		}

		void IAvaloniaListItemValidator<T>.Validate(T item)
		{
			Validate(item);
		}
	}

	private readonly List<T> _inner;

	private NotifyCollectionChangedEventHandler? _collectionChanged;

	/// <summary>
	/// Gets the number of items in the collection.
	/// </summary>
	public int Count => _inner.Count;

	/// <summary>
	/// Gets or sets the reset behavior of the list.
	/// </summary>
	public ResetBehavior ResetBehavior { get; set; }

	/// <summary>
	/// Gets or sets a validation routine that can be used to validate items before they are
	/// added.
	/// </summary>
	public Action<T>? Validate
	{
		get
		{
			IAvaloniaListItemValidator<T> validator = Validator;
			if (validator != null)
			{
				if (validator is ItemValidator itemValidator)
				{
					return itemValidator.Validate;
				}
				return validator.Validate;
			}
			return null;
		}
		set
		{
			if (value == null)
			{
				Validator = null;
			}
			else if (Validator is ItemValidator itemValidator)
			{
				itemValidator.Validate = value;
			}
			else
			{
				Validator = new ItemValidator(value);
			}
		}
	}

	internal IAvaloniaListItemValidator<T>? Validator { get; set; }

	/// <inheritdoc />
	bool IList.IsFixedSize => false;

	/// <inheritdoc />
	bool IList.IsReadOnly => false;

	/// <inheritdoc />
	int ICollection.Count => _inner.Count;

	/// <inheritdoc />
	bool ICollection.IsSynchronized => false;

	/// <inheritdoc />
	object ICollection.SyncRoot => this;

	/// <inheritdoc />
	bool ICollection<T>.IsReadOnly => false;

	/// <summary>
	/// Gets or sets the item at the specified index.
	/// </summary>
	/// <param name="index">The index.</param>
	/// <returns>The item.</returns>
	public T this[int index]
	{
		get
		{
			return _inner[index];
		}
		set
		{
			Validator?.Validate(value);
			T val = _inner[index];
			if (!EqualityComparer<T>.Default.Equals(val, value))
			{
				_inner[index] = value;
				if (_collectionChanged != null)
				{
					NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, val, index);
					_collectionChanged(this, e);
				}
			}
		}
	}

	/// <summary>
	/// Gets or sets the item at the specified index.
	/// </summary>
	/// <param name="index">The index.</param>
	/// <returns>The item.</returns>
	object? IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			this[index] = (T)value;
		}
	}

	/// <summary>
	/// Gets or sets the total number of elements the internal data structure can hold without resizing.
	/// </summary>
	public int Capacity
	{
		get
		{
			return _inner.Capacity;
		}
		set
		{
			_inner.Capacity = value;
		}
	}

	/// <summary>
	/// Raised when a change is made to the collection's items.
	/// </summary>
	public event NotifyCollectionChangedEventHandler? CollectionChanged
	{
		add
		{
			_collectionChanged = (NotifyCollectionChangedEventHandler)Delegate.Combine(_collectionChanged, value);
		}
		remove
		{
			_collectionChanged = (NotifyCollectionChangedEventHandler)Delegate.Remove(_collectionChanged, value);
		}
	}

	/// <summary>
	/// Raised when a property on the collection changes.
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Collections.AvaloniaList`1" /> class.
	/// </summary>
	public AvaloniaList()
	{
		_inner = new List<T>();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Collections.AvaloniaList`1" />.
	/// </summary>
	/// <param name="capacity">Initial list capacity.</param>
	public AvaloniaList(int capacity)
	{
		_inner = new List<T>(capacity);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Collections.AvaloniaList`1" /> class.
	/// </summary>
	/// <param name="items">The initial items for the collection.</param>
	public AvaloniaList(IEnumerable<T> items)
	{
		_inner = new List<T>(items);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Collections.AvaloniaList`1" /> class.
	/// </summary>
	/// <param name="items">The initial items for the collection.</param>
	public AvaloniaList(params T[] items)
	{
		_inner = new List<T>(items);
	}

	/// <summary>
	/// Adds an item to the collection.
	/// </summary>
	/// <param name="item">The item.</param>
	public virtual void Add(T item)
	{
		Validator?.Validate(item);
		int count = _inner.Count;
		_inner.Add(item);
		NotifyAdd(item, count);
	}

	/// <summary>
	/// Adds multiple items to the collection.
	/// </summary>
	/// <param name="items">The items.</param>
	public virtual void AddRange(IEnumerable<T> items)
	{
		InsertRange(_inner.Count, items);
	}

	/// <summary>
	/// Removes all items from the collection.
	/// </summary>
	public virtual void Clear()
	{
		if (Count > 0)
		{
			if (_collectionChanged != null)
			{
				NotifyCollectionChangedEventArgs e = ((ResetBehavior == ResetBehavior.Reset) ? EventArgsCache.ResetCollectionChanged : new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, _inner.ToArray(), 0));
				_inner.Clear();
				_collectionChanged(this, e);
			}
			else
			{
				_inner.Clear();
			}
			NotifyCountChanged();
		}
	}

	/// <summary>
	/// Tests if the collection contains the specified item.
	/// </summary>
	/// <param name="item">The item.</param>
	/// <returns>True if the collection contains the item; otherwise false.</returns>
	public bool Contains(T item)
	{
		return _inner.Contains(item);
	}

	/// <summary>
	/// Copies the collection's contents to an array.
	/// </summary>
	/// <param name="array">The array.</param>
	/// <param name="arrayIndex">The first index of the array to copy to.</param>
	public void CopyTo(T[] array, int arrayIndex)
	{
		_inner.CopyTo(array, arrayIndex);
	}

	/// <summary>
	/// Returns an enumerator that enumerates the items in the collection.
	/// </summary>
	/// <returns>An <see cref="T:System.Collections.Generic.IEnumerator`1" />.</returns>
	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(_inner);
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(_inner);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_inner);
	}

	/// <summary>
	/// Gets a range of items from the collection.
	/// </summary>
	/// <param name="index">The zero-based <see cref="T:Avalonia.Collections.AvaloniaList`1" /> index at which the range starts.</param>
	/// <param name="count">The number of elements in the range.</param>
	public IEnumerable<T> GetRange(int index, int count)
	{
		return _inner.GetRange(index, count);
	}

	/// <summary>
	/// Gets the index of the specified item in the collection.
	/// </summary>
	/// <param name="item">The item.</param>
	/// <returns>
	/// The index of the item or -1 if the item is not contained in the collection.
	/// </returns>
	public int IndexOf(T item)
	{
		return _inner.IndexOf(item);
	}

	/// <summary>
	/// Inserts an item at the specified index.
	/// </summary>
	/// <param name="index">The index.</param>
	/// <param name="item">The item.</param>
	public virtual void Insert(int index, T item)
	{
		Validator?.Validate(item);
		_inner.Insert(index, item);
		NotifyAdd(item, index);
	}

	/// <summary>
	/// Inserts multiple items at the specified index.
	/// </summary>
	/// <param name="index">The index.</param>
	/// <param name="items">The items.</param>
	public virtual void InsertRange(int index, IEnumerable<T> items)
	{
		if (items == null)
		{
			throw new ArgumentNullException("items");
		}
		bool flag = _collectionChanged != null;
		bool flag2 = Validator != null;
		if (items is IList list)
		{
			if (list.Count <= 0)
			{
				return;
			}
			if (list is ICollection<T> collection)
			{
				if (flag2)
				{
					foreach (T item in collection)
					{
						Validator.Validate(item);
					}
				}
				_inner.InsertRange(index, collection);
				NotifyAdd(list, index);
				return;
			}
			EnsureCapacity(_inner.Count + list.Count);
			using (IEnumerator<T> enumerator2 = items.GetEnumerator())
			{
				int num = index;
				while (enumerator2.MoveNext())
				{
					T current2 = enumerator2.Current;
					if (flag2)
					{
						Validator.Validate(current2);
					}
					_inner.Insert(num++, current2);
				}
			}
			NotifyAdd(list, index);
			return;
		}
		using IEnumerator<T> enumerator3 = items.GetEnumerator();
		if (!enumerator3.MoveNext())
		{
			return;
		}
		List<T> list2 = (flag ? new List<T>() : null);
		int num2 = index;
		do
		{
			T current3 = enumerator3.Current;
			if (flag2)
			{
				Validator.Validate(current3);
			}
			_inner.Insert(num2++, current3);
			list2?.Add(current3);
		}
		while (enumerator3.MoveNext());
		if (list2 != null)
		{
			NotifyAdd(list2, index);
		}
		else
		{
			NotifyCountChanged();
		}
	}

	/// <summary>
	/// Moves an item to a new index.
	/// </summary>
	/// <param name="oldIndex">The index of the item to move.</param>
	/// <param name="newIndex">The index to move the item to.</param>
	public void Move(int oldIndex, int newIndex)
	{
		T val = this[oldIndex];
		_inner.RemoveAt(oldIndex);
		_inner.Insert(newIndex, val);
		if (_collectionChanged != null)
		{
			NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, val, newIndex, oldIndex);
			_collectionChanged(this, e);
		}
	}

	/// <summary>
	/// Moves multiple items to a new index.
	/// </summary>
	/// <param name="oldIndex">The first index of the items to move.</param>
	/// <param name="count">The number of items to move.</param>
	/// <param name="newIndex">The index to move the items to.</param>
	public void MoveRange(int oldIndex, int count, int newIndex)
	{
		List<T> range = _inner.GetRange(oldIndex, count);
		int num = newIndex;
		_inner.RemoveRange(oldIndex, count);
		if (newIndex > oldIndex)
		{
			num -= count - 1;
		}
		_inner.InsertRange(num, range);
		if (_collectionChanged != null)
		{
			NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, range, newIndex, oldIndex);
			_collectionChanged(this, e);
		}
	}

	/// <summary>
	/// Ensures that the capacity of the list is at least <see cref="P:Avalonia.Collections.AvaloniaList`1.Capacity" />.
	/// </summary>
	/// <param name="capacity">The capacity.</param>
	public void EnsureCapacity(int capacity)
	{
		int capacity2 = _inner.Capacity;
		if (capacity2 < capacity)
		{
			int num = ((capacity2 == 0) ? 4 : (capacity2 * 2));
			if (num < capacity)
			{
				num = capacity;
			}
			_inner.Capacity = num;
		}
	}

	/// <summary>
	/// Removes an item from the collection.
	/// </summary>
	/// <param name="item">The item.</param>
	/// <returns>True if the item was found and removed, otherwise false.</returns>
	public virtual bool Remove(T item)
	{
		int num = _inner.IndexOf(item);
		if (num != -1)
		{
			_inner.RemoveAt(num);
			NotifyRemove(item, num);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Removes multiple items from the collection.
	/// </summary>
	/// <param name="items">The items.</param>
	public virtual void RemoveAll(IEnumerable<T> items)
	{
		if (items == null)
		{
			throw new ArgumentNullException("items");
		}
		HashSet<T> hashSet = new HashSet<T>(items);
		int num = 0;
		for (int num2 = _inner.Count - 1; num2 >= 0; num2--)
		{
			if (hashSet.Contains(_inner[num2]))
			{
				num++;
			}
			else if (num > 0)
			{
				RemoveRange(num2 + 1, num);
				num = 0;
			}
		}
		if (num > 0)
		{
			RemoveRange(0, num);
		}
	}

	/// <summary>
	/// Removes the item at the specified index.
	/// </summary>
	/// <param name="index">The index.</param>
	public virtual void RemoveAt(int index)
	{
		T item = _inner[index];
		_inner.RemoveAt(index);
		NotifyRemove(item, index);
	}

	/// <summary>
	/// Removes a range of elements from the collection.
	/// </summary>
	/// <param name="index">The first index to remove.</param>
	/// <param name="count">The number of items to remove.</param>
	public virtual void RemoveRange(int index, int count)
	{
		if (count > 0)
		{
			List<T> range = _inner.GetRange(index, count);
			_inner.RemoveRange(index, count);
			NotifyRemove(range, index);
		}
	}

	/// <inheritdoc />
	int IList.Add(object? value)
	{
		int count = Count;
		Add((T)value);
		return count;
	}

	/// <inheritdoc />
	bool IList.Contains(object? value)
	{
		return Contains((T)value);
	}

	/// <inheritdoc />
	void IList.Clear()
	{
		Clear();
	}

	/// <inheritdoc />
	int IList.IndexOf(object? value)
	{
		return IndexOf((T)value);
	}

	/// <inheritdoc />
	void IList.Insert(int index, object? value)
	{
		Insert(index, (T)value);
	}

	/// <inheritdoc />
	void IList.Remove(object? value)
	{
		Remove((T)value);
	}

	/// <inheritdoc />
	void IList.RemoveAt(int index)
	{
		RemoveAt(index);
	}

	/// <inheritdoc />
	void ICollection.CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException("Multi-dimensional arrays are not supported.");
		}
		if (array.GetLowerBound(0) != 0)
		{
			throw new ArgumentException("Non-zero lower bounds are not supported.");
		}
		if (index < 0)
		{
			throw new ArgumentException("Invalid index.");
		}
		if (array.Length - index < Count)
		{
			throw new ArgumentException("The target array is too small.");
		}
		if (array is T[] array2)
		{
			_inner.CopyTo(array2, index);
			return;
		}
		Type elementType = array.GetType().GetElementType();
		Type typeFromHandle = typeof(T);
		if (!elementType.IsAssignableFrom(typeFromHandle) && !typeFromHandle.IsAssignableFrom(elementType))
		{
			throw new ArgumentException("Invalid array type");
		}
		if (!(array is object[] array3))
		{
			throw new ArgumentException("Invalid array type");
		}
		int count = _inner.Count;
		try
		{
			for (int i = 0; i < count; i++)
			{
				array3[index++] = _inner[i];
			}
		}
		catch (ArrayTypeMismatchException)
		{
			throw new ArgumentException("Invalid array type");
		}
	}

	/// <inheritdoc />
	Delegate[]? INotifyCollectionChangedDebug.GetCollectionChangedSubscribers()
	{
		return _collectionChanged?.GetInvocationList();
	}

	/// <summary>
	/// Raises the <see cref="E:Avalonia.Collections.AvaloniaList`1.CollectionChanged" /> event with an add action.
	/// </summary>
	/// <param name="t">The items that were added.</param>
	/// <param name="index">The starting index.</param>
	private void NotifyAdd(IList t, int index)
	{
		if (_collectionChanged != null)
		{
			NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, t, index);
			_collectionChanged(this, e);
		}
		NotifyCountChanged();
	}

	/// <summary>
	/// Raises the <see cref="E:Avalonia.Collections.AvaloniaList`1.CollectionChanged" /> event with a add action.
	/// </summary>
	/// <param name="item">The item that was added.</param>
	/// <param name="index">The starting index.</param>
	private void NotifyAdd(T item, int index)
	{
		if (_collectionChanged != null)
		{
			NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new T[1] { item }, index);
			_collectionChanged(this, e);
		}
		NotifyCountChanged();
	}

	/// <summary>
	/// Raises the <see cref="E:Avalonia.Collections.AvaloniaList`1.PropertyChanged" /> event when the <see cref="P:Avalonia.Collections.AvaloniaList`1.Count" /> property
	/// changes.
	/// </summary>
	private void NotifyCountChanged()
	{
		PropertyChanged?.Invoke(this, EventArgsCache.CountPropertyChanged);
	}

	/// <summary>
	/// Raises the <see cref="E:Avalonia.Collections.AvaloniaList`1.CollectionChanged" /> event with a remove action.
	/// </summary>
	/// <param name="t">The items that were removed.</param>
	/// <param name="index">The starting index.</param>
	private void NotifyRemove(IList t, int index)
	{
		if (_collectionChanged != null)
		{
			NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, t, index);
			_collectionChanged(this, e);
		}
		NotifyCountChanged();
	}

	/// <summary>
	/// Raises the <see cref="E:Avalonia.Collections.AvaloniaList`1.CollectionChanged" /> event with a remove action.
	/// </summary>
	/// <param name="item">The item that was removed.</param>
	/// <param name="index">The starting index.</param>
	private void NotifyRemove(T item, int index)
	{
		if (_collectionChanged != null)
		{
			NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new T[1] { item }, index);
			_collectionChanged(this, e);
		}
		NotifyCountChanged();
	}
}
