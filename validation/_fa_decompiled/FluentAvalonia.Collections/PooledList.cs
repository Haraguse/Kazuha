using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FluentAvalonia.Collections;

/// <summary>
/// Implements a variable-size list that uses a pooled array to store the
/// elements. A PooledList has a capacity, which is the allocated length
/// of the internal array. As elements are added to a PooledList, the capacity
/// of the PooledList is automatically increased as required by reallocating the
/// internal array.
/// </summary>
/// <remarks>
/// This class is based on the code for <see cref="T:System.Collections.Generic.List`1" /> but it supports <see cref="T:System.Span`1" />
/// and uses <see cref="T:System.Buffers.ArrayPool`1" /> when allocating internal arrays.
/// </remarks>
[Serializable]
[DebuggerDisplay("Count = {Count}")]
internal class PooledList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList, ICollection, IDisposable
{
	public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
	{
		private readonly PooledList<T> _list;

		private int _index;

		private readonly int _version;

		private T _current;

		public T Current => _current;

		object IEnumerator.Current => Current;

		internal Enumerator(PooledList<T> list)
		{
			_list = list;
			_index = 0;
			_version = list._version;
			_current = default(T);
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			PooledList<T> list = _list;
			if (_version == list._version && (uint)_index < (uint)list._size)
			{
				_current = list._items[_index];
				_index++;
				return true;
			}
			return MoveNextRare();
		}

		private bool MoveNextRare()
		{
			_index = _list._size + 1;
			_current = default(T);
			return false;
		}

		void IEnumerator.Reset()
		{
			_index = 0;
			_current = default(T);
		}
	}

	private readonly struct Comparer(Func<T, T, int> comparison) : IComparer<T>
	{
		private readonly Func<T, T, int> _comparison = comparison;

		public int Compare(T x, T y)
		{
			return _comparison(x, y);
		}
	}

	private const int MaxArrayLength = 2146435071;

	private const int DefaultCapacity = 4;

	private static readonly T[] s_emptyArray = Array.Empty<T>();

	[NonSerialized]
	private ArrayPool<T> _pool;

	[NonSerialized]
	private object _syncRoot;

	private T[] _items;

	private int _size;

	private int _version;

	private readonly bool _clearOnFree;

	/// <summary>
	/// Gets a <see cref="T:System.Span`1" /> for the items currently in the collection.
	/// </summary>
	public Span<T> Span => _items.AsSpan(0, _size);

	/// <summary>
	/// Gets and sets the capacity of this list.  The capacity is the size of
	/// the internal array used to hold items.  When set, the internal 
	/// Memory of the list is reallocated to the given capacity.
	/// Note that the return value for this property may be larger than the property was set to.
	/// </summary>
	public int Capacity
	{
		get
		{
			return _items.Length;
		}
		set
		{
			if (value == _items.Length)
			{
				return;
			}
			if (value > 0)
			{
				T[] array = _pool.Rent(value);
				if (_size > 0)
				{
					Array.Copy(_items, array, _size);
				}
				ReturnArray();
				_items = array;
			}
			else
			{
				ReturnArray();
				_size = 0;
			}
		}
	}

	/// <summary>
	/// Read-only property describing how many elements are in the List.
	/// </summary>
	public int Count => _size;

	/// <summary>
	/// Returns the ClearMode behavior for the collection, denoting whether values are
	/// cleared from internal arrays before returning them to the pool.
	/// </summary>
	public ClearMode ClearMode
	{
		get
		{
			if (!_clearOnFree)
			{
				return ClearMode.Never;
			}
			return ClearMode.Always;
		}
	}

	bool IList.IsFixedSize => false;

	bool ICollection<T>.IsReadOnly => false;

	bool IList.IsReadOnly => false;

	int ICollection.Count => _size;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot
	{
		get
		{
			if (_syncRoot == null)
			{
				Interlocked.CompareExchange<object>(ref _syncRoot, new object(), (object)null);
			}
			return _syncRoot;
		}
	}

	/// <summary>
	/// Gets or sets the element at the given index.
	/// </summary>
	public T this[int index]
	{
		get
		{
			return _items[index];
		}
		set
		{
			_items[index] = value;
			_version++;
		}
	}

	object IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			try
			{
				this[index] = (T)value;
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// Constructs a PooledList. The list is initially empty and has a capacity
	/// of zero. Upon adding the first element to the list the capacity is
	/// increased to DefaultCapacity, and then increased in multiples of two
	/// as required.
	/// </summary>
	public PooledList()
		: this(ClearMode.Auto, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Constructs a PooledList. The list is initially empty and has a capacity
	/// of zero. Upon adding the first element to the list the capacity is
	/// increased to DefaultCapacity, and then increased in multiples of two
	/// as required.
	/// </summary>
	public PooledList(ClearMode clearMode)
		: this(clearMode, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Constructs a PooledList. The list is initially empty and has a capacity
	/// of zero. Upon adding the first element to the list the capacity is
	/// increased to DefaultCapacity, and then increased in multiples of two
	/// as required.
	/// </summary>
	public PooledList(ArrayPool<T> customPool)
		: this(ClearMode.Auto, customPool)
	{
	}

	/// <summary>
	/// Constructs a PooledList. The list is initially empty and has a capacity
	/// of zero. Upon adding the first element to the list the capacity is
	/// increased to DefaultCapacity, and then increased in multiples of two
	/// as required.
	/// </summary>
	public PooledList(ClearMode clearMode, ArrayPool<T> customPool)
	{
		_items = s_emptyArray;
		_pool = customPool ?? ArrayPool<T>.Shared;
		_clearOnFree = ShouldClear(clearMode);
	}

	/// <summary>
	/// Constructs a List with a given initial capacity. The list is
	/// initially empty, but will have room for the given number of elements
	/// before any reallocations are required.
	/// </summary>
	public PooledList(int capacity)
		: this(capacity, ClearMode.Auto, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Constructs a List with a given initial capacity. The list is
	/// initially empty, but will have room for the given number of elements
	/// before any reallocations are required.
	/// </summary>
	public PooledList(int capacity, bool sizeToCapacity)
		: this(capacity, ClearMode.Auto, ArrayPool<T>.Shared, sizeToCapacity)
	{
	}

	/// <summary>
	/// Constructs a List with a given initial capacity. The list is
	/// initially empty, but will have room for the given number of elements
	/// before any reallocations are required.
	/// </summary>
	public PooledList(int capacity, ClearMode clearMode)
		: this(capacity, clearMode, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Constructs a List with a given initial capacity. The list is
	/// initially empty, but will have room for the given number of elements
	/// before any reallocations are required.
	/// </summary>
	public PooledList(int capacity, ClearMode clearMode, bool sizeToCapacity)
		: this(capacity, clearMode, ArrayPool<T>.Shared, sizeToCapacity)
	{
	}

	/// <summary>
	/// Constructs a List with a given initial capacity. The list is
	/// initially empty, but will have room for the given number of elements
	/// before any reallocations are required.
	/// </summary>
	public PooledList(int capacity, ArrayPool<T> customPool)
		: this(capacity, ClearMode.Auto, customPool)
	{
	}

	/// <summary>
	/// Constructs a List with a given initial capacity. The list is
	/// initially empty, but will have room for the given number of elements
	/// before any reallocations are required.
	/// </summary>
	public PooledList(int capacity, ArrayPool<T> customPool, bool sizeToCapacity)
		: this(capacity, ClearMode.Auto, customPool, sizeToCapacity)
	{
	}

	/// <summary>
	/// Constructs a List with a given initial capacity. The list is
	/// initially empty, but will have room for the given number of elements
	/// before any reallocations are required.
	/// </summary>
	public PooledList(int capacity, ClearMode clearMode, ArrayPool<T> customPool)
		: this(capacity, clearMode, customPool, false)
	{
	}

	/// <summary>
	/// Constructs a List with a given initial capacity. The list is
	/// initially empty, but will have room for the given number of elements
	/// before any reallocations are required.
	/// </summary>
	public PooledList(int capacity, ClearMode clearMode, ArrayPool<T> customPool, bool sizeToCapacity)
	{
		_pool = customPool ?? ArrayPool<T>.Shared;
		_clearOnFree = ShouldClear(clearMode);
		if (capacity == 0)
		{
			_items = s_emptyArray;
		}
		else
		{
			_items = _pool.Rent(capacity);
		}
		if (sizeToCapacity)
		{
			_size = capacity;
			if (clearMode != ClearMode.Never)
			{
				Array.Clear(_items, 0, _size);
			}
		}
	}

	/// <summary>
	/// Constructs a PooledList, copying the contents of the given collection. The
	/// size and capacity of the new list will both be equal to the size of the
	/// given collection.
	/// </summary>
	public PooledList(T[] array)
		: this((ReadOnlySpan<T>)array.AsSpan(), ClearMode.Auto, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Constructs a PooledList, copying the contents of the given collection. The
	/// size and capacity of the new list will both be equal to the size of the
	/// given collection.
	/// </summary>
	public PooledList(T[] array, ClearMode clearMode)
		: this((ReadOnlySpan<T>)array.AsSpan(), clearMode, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Constructs a PooledList, copying the contents of the given collection. The
	/// size and capacity of the new list will both be equal to the size of the
	/// given collection.
	/// </summary>
	public PooledList(T[] array, ArrayPool<T> customPool)
		: this((ReadOnlySpan<T>)array.AsSpan(), ClearMode.Auto, customPool)
	{
	}

	/// <summary>
	/// Constructs a PooledList, copying the contents of the given collection. The
	/// size and capacity of the new list will both be equal to the size of the
	/// given collection.
	/// </summary>
	public PooledList(T[] array, ClearMode clearMode, ArrayPool<T> customPool)
		: this((ReadOnlySpan<T>)array.AsSpan(), clearMode, customPool)
	{
	}

	/// <summary>
	/// Constructs a PooledList, copying the contents of the given collection. The
	/// size and capacity of the new list will both be equal to the size of the
	/// given collection.
	/// </summary>
	public PooledList(ReadOnlySpan<T> span)
		: this(span, ClearMode.Auto, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Constructs a PooledList, copying the contents of the given collection. The
	/// size and capacity of the new list will both be equal to the size of the
	/// given collection.
	/// </summary>
	public PooledList(ReadOnlySpan<T> span, ClearMode clearMode)
		: this(span, clearMode, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Constructs a PooledList, copying the contents of the given collection. The
	/// size and capacity of the new list will both be equal to the size of the
	/// given collection.
	/// </summary>
	public PooledList(ReadOnlySpan<T> span, ArrayPool<T> customPool)
		: this(span, ClearMode.Auto, customPool)
	{
	}

	/// <summary>
	/// Constructs a PooledList, copying the contents of the given collection. The
	/// size and capacity of the new list will both be equal to the size of the
	/// given collection.
	/// </summary>
	public PooledList(ReadOnlySpan<T> span, ClearMode clearMode, ArrayPool<T> customPool)
	{
		_pool = customPool ?? ArrayPool<T>.Shared;
		_clearOnFree = ShouldClear(clearMode);
		int length = span.Length;
		if (length == 0)
		{
			_items = s_emptyArray;
			return;
		}
		_items = _pool.Rent(length);
		span.CopyTo(_items);
		_size = length;
	}

	/// <summary>
	/// Constructs a PooledList, copying the contents of the given collection. The
	/// size and capacity of the new list will both be equal to the size of the
	/// given collection.
	/// </summary>
	public PooledList(IEnumerable<T> collection)
		: this(collection, ClearMode.Auto, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Constructs a PooledList, copying the contents of the given collection. The
	/// size and capacity of the new list will both be equal to the size of the
	/// given collection.
	/// </summary>
	public PooledList(IEnumerable<T> collection, ClearMode clearMode)
		: this(collection, clearMode, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Constructs a PooledList, copying the contents of the given collection. The
	/// size and capacity of the new list will both be equal to the size of the
	/// given collection.
	/// </summary>
	public PooledList(IEnumerable<T> collection, ArrayPool<T> customPool)
		: this(collection, ClearMode.Auto, customPool)
	{
	}

	/// <summary>
	/// Constructs a PooledList, copying the contents of the given collection. The
	/// size and capacity of the new list will both be equal to the size of the
	/// given collection.
	/// </summary>
	public PooledList(IEnumerable<T> collection, ClearMode clearMode, ArrayPool<T> customPool)
	{
		_pool = customPool ?? ArrayPool<T>.Shared;
		_clearOnFree = ShouldClear(clearMode);
		if (collection is ICollection<T> { Count: var count } collection2)
		{
			if (count == 0)
			{
				_items = s_emptyArray;
				return;
			}
			_items = _pool.Rent(count);
			collection2.CopyTo(_items, 0);
			_size = count;
			return;
		}
		_size = 0;
		_items = s_emptyArray;
		foreach (T item in collection)
		{
			Add(item);
		}
	}

	private static bool IsCompatibleObject(object value)
	{
		if (!(value is T))
		{
			if (value == null)
			{
				return default(T) == null;
			}
			return false;
		}
		return true;
	}

	/// <summary>
	/// Adds the given object to the end of this list. The size of the list is
	/// increased by one. If required, the capacity of the list is doubled
	/// before adding the new element.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(T item)
	{
		_version++;
		int size = _size;
		if ((uint)size < (uint)_items.Length)
		{
			_size = size + 1;
			_items[size] = item;
		}
		else
		{
			AddWithResize(item);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void AddWithResize(T item)
	{
		int size = _size;
		EnsureCapacity(size + 1);
		_size = size + 1;
		_items[size] = item;
	}

	int IList.Add(object item)
	{
		Add((T)item);
		return Count - 1;
	}

	/// <summary>
	/// Adds the elements of the given collection to the end of this list. If
	/// required, the capacity of the list is increased to twice the previous
	/// capacity or the new size, whichever is larger.
	/// </summary>
	public void AddRange(IEnumerable<T> collection)
	{
		InsertRange(_size, collection);
	}

	/// <summary>
	/// Adds the elements of the given array to the end of this list. If
	/// required, the capacity of the list is increased to twice the previous
	/// capacity or the new size, whichever is larger.
	/// </summary>
	public void AddRange(T[] array)
	{
		AddRange(array.AsSpan());
	}

	/// <summary>
	/// Adds the elements of the given <see cref="T:System.ReadOnlySpan`1" /> to the end of this list. If
	/// required, the capacity of the list is increased to twice the previous
	/// capacity or the new size, whichever is larger.
	/// </summary>
	public void AddRange(ReadOnlySpan<T> span)
	{
		Span<T> destination = InsertSpan(_size, span.Length, clearOutput: false);
		span.CopyTo(destination);
	}

	/// <summary>
	/// Advances the <see cref="P:FluentAvalonia.Collections.PooledList`1.Count" /> by the number of items specified,
	/// increasing the capacity if required, then returns a Span representing
	/// the set of items to be added, allowing direct writes to that section
	/// of the collection.
	/// </summary>
	/// <param name="count">The number of items to add.</param>
	public Span<T> AddSpan(int count)
	{
		return InsertSpan(_size, count);
	}

	public ReadOnlyCollection<T> AsReadOnly()
	{
		return new ReadOnlyCollection<T>(this);
	}

	/// <summary>Gets a <see cref="T:System.Span`1" /> view over the data in a list.
	/// Items should not be added or removed from the list while the returned span is in use.</summary>
	public Span<T> AsSpan()
	{
		return new Span<T>(_items, 0, _size);
	}

	/// <summary>
	/// Searches a section of the list for a given element using a binary search
	/// algorithm. 
	/// </summary>
	///
	/// <remarks><para>Elements of the list are compared to the search value using
	/// the given IComparer interface. If comparer is null, elements of
	/// the list are compared to the search value using the IComparable
	/// interface, which in that case must be implemented by all elements of the
	/// list and the given search value. This method assumes that the given
	/// section of the list is already sorted; if this is not the case, the
	/// result will be incorrect.</para>
	///
	/// <para>The method returns the index of the given value in the list. If the
	/// list does not contain the given value, the method returns a negative
	/// integer. The bitwise complement operator (~) can be applied to a
	/// negative result to produce the index of the first element (if any) that
	/// is larger than the given search value. This is also the index at which
	/// the search value should be inserted into the list in order for the list
	/// to remain sorted.
	/// </para></remarks>
	public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
	{
		return Array.BinarySearch(_items, index, count, item, comparer);
	}

	/// <summary>
	/// Searches the list for a given element using a binary search
	/// algorithm. If the item implements <see cref="T:System.IComparable`1" />
	/// then that is used for comparison, otherwise <see cref="P:System.Collections.Generic.Comparer`1.Default" /> is used.
	/// </summary>
	public int BinarySearch(T item)
	{
		return BinarySearch(0, Count, item, null);
	}

	/// <summary>
	/// Searches the list for a given element using a binary search
	/// algorithm. If the item implements <see cref="T:System.IComparable`1" />
	/// then that is used for comparison, otherwise <see cref="P:System.Collections.Generic.Comparer`1.Default" /> is used.
	/// </summary>
	public int BinarySearch(T item, IComparer<T> comparer)
	{
		return BinarySearch(0, Count, item, comparer);
	}

	/// <summary>
	/// Clears the contents of the PooledList.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Clear()
	{
		_version++;
		int size = _size;
		_size = 0;
		if (size > 0 && _clearOnFree)
		{
			Array.Clear(_items, 0, size);
		}
	}

	/// <summary>
	/// Contains returns true if the specified element is in the List.
	/// It does a linear, O(n) search.  Equality is determined by calling
	/// EqualityComparer{T}.Default.Equals.
	/// </summary>
	public bool Contains(T item)
	{
		if (_size != 0)
		{
			return IndexOf(item) != -1;
		}
		return false;
	}

	bool IList.Contains(object item)
	{
		if (IsCompatibleObject(item))
		{
			return Contains((T)item);
		}
		return false;
	}

	public PooledList<TOutput> ConvertAll<TOutput>(Func<T, TOutput> converter)
	{
		PooledList<TOutput> pooledList = new PooledList<TOutput>(_size);
		for (int i = 0; i < _size; i++)
		{
			pooledList._items[i] = converter(_items[i]);
		}
		pooledList._size = _size;
		return pooledList;
	}

	/// <summary>
	/// Copies this list to the given span.
	/// </summary>
	public void CopyTo(Span<T> span)
	{
		if (span.Length < Count)
		{
			throw new ArgumentException("Destination span is shorter than the list to be copied.");
		}
		Span.CopyTo(span);
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		Array.Copy(_items, 0, array, arrayIndex, _size);
	}

	void ICollection.CopyTo(Array array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		try
		{
			Array.Copy(_items, 0, array, arrayIndex, _size);
		}
		catch (ArrayTypeMismatchException)
		{
		}
	}

	/// <summary>
	/// Ensures that the capacity of this list is at least the given minimum
	/// value. If the current capacity of the list is less than min, the
	/// capacity is increased to twice the current capacity or to min,
	/// whichever is larger.
	/// </summary>
	private void EnsureCapacity(int min)
	{
		if (_items.Length < min)
		{
			int num = ((_items.Length == 0) ? 4 : (_items.Length * 2));
			if ((uint)num > 2146435071u)
			{
				num = 2146435071;
			}
			if (num < min)
			{
				num = min;
			}
			Capacity = num;
		}
	}

	public bool Exists(Func<T, bool> match)
	{
		return FindIndex(match) != -1;
	}

	public bool TryFind(Func<T, bool> match, out T result)
	{
		for (int i = 0; i < _size; i++)
		{
			if (match(_items[i]))
			{
				result = _items[i];
				return true;
			}
		}
		result = default(T);
		return false;
	}

	public PooledList<T> FindAll(Func<T, bool> match)
	{
		PooledList<T> pooledList = new PooledList<T>();
		for (int i = 0; i < _size; i++)
		{
			if (match(_items[i]))
			{
				pooledList.Add(_items[i]);
			}
		}
		return pooledList;
	}

	public int FindIndex(Func<T, bool> match)
	{
		return FindIndex(0, _size, match);
	}

	public int FindIndex(int startIndex, Func<T, bool> match)
	{
		return FindIndex(startIndex, _size - startIndex, match);
	}

	public int FindIndex(int startIndex, int count, Func<T, bool> match)
	{
		int num = startIndex + count;
		for (int i = startIndex; i < num; i++)
		{
			if (match(_items[i]))
			{
				return i;
			}
		}
		return -1;
	}

	public bool TryFindLast(Func<T, bool> match, out T result)
	{
		for (int num = _size - 1; num >= 0; num--)
		{
			if (match(_items[num]))
			{
				result = _items[num];
				return true;
			}
		}
		result = default(T);
		return false;
	}

	public int FindLastIndex(Func<T, bool> match)
	{
		return FindLastIndex(_size - 1, _size, match);
	}

	public int FindLastIndex(int startIndex, Func<T, bool> match)
	{
		return FindLastIndex(startIndex, startIndex + 1, match);
	}

	public int FindLastIndex(int startIndex, int count, Func<T, bool> match)
	{
		if (_size == 0)
		{
			if (startIndex != -1)
			{
				throw new ArgumentOutOfRangeException("startIndex");
			}
		}
		else if ((uint)startIndex >= (uint)_size)
		{
			throw new ArgumentOutOfRangeException("startIndex");
		}
		if (count < 0 || startIndex - count + 1 < 0)
		{
			throw new ArgumentOutOfRangeException("startIndex");
		}
		int num = startIndex - count;
		for (int num2 = startIndex; num2 > num; num2--)
		{
			if (match(_items[num2]))
			{
				return num2;
			}
		}
		return -1;
	}

	/// <summary>
	/// Returns an enumerator for this list with the given
	/// permission for removal of elements. If modifications made to the list 
	/// while an enumeration is in progress, the MoveNext and 
	/// GetObject methods of the enumerator will throw an exception.
	/// </summary>
	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this);
	}

	/// <summary>
	/// Equivalent to PooledList.Span.Slice(index, count).
	/// </summary>
	public Span<T> GetRange(int index, int count)
	{
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (_size - index < count)
		{
			throw new ArgumentException();
		}
		return Span.Slice(index, count);
	}

	/// <summary>
	/// Returns the index of the first occurrence of a given value in
	/// this list. The list is searched forwards from beginning to end.
	/// </summary>
	public int IndexOf(T item)
	{
		return Array.IndexOf(_items, item, 0, _size);
	}

	int IList.IndexOf(object item)
	{
		if (IsCompatibleObject(item))
		{
			return IndexOf((T)item);
		}
		return -1;
	}

	/// <summary>
	/// Returns the index of the first occurrence of a given value in a range of
	/// this list. The list is searched forwards, starting at index
	/// index and ending at count number of elements. 
	/// </summary>
	public int IndexOf(T item, int index)
	{
		if (index > _size)
		{
			throw new ArgumentOutOfRangeException();
		}
		return Array.IndexOf(_items, item, index, _size - index);
	}

	/// <summary>
	/// Returns the index of the first occurrence of a given value in a range of
	/// this list. The list is searched forwards, starting at index
	/// index and upto count number of elements. 
	/// </summary>
	public int IndexOf(T item, int index, int count)
	{
		if (index > _size)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (count < 0 || index > _size - count)
		{
			throw new ArgumentOutOfRangeException();
		}
		return Array.IndexOf(_items, item, index, count);
	}

	/// <summary>
	/// Inserts an element into this list at a given index. The size of the list
	/// is increased by one. If required, the capacity of the list is doubled
	/// before inserting the new element.
	/// </summary>
	public void Insert(int index, T item)
	{
		if ((uint)index > (uint)_size)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (_size == _items.Length)
		{
			EnsureCapacity(_size + 1);
		}
		if (index < _size)
		{
			Array.Copy(_items, index, _items, index + 1, _size - index);
		}
		_items[index] = item;
		_size++;
		_version++;
	}

	void IList.Insert(int index, object item)
	{
		try
		{
			Insert(index, (T)item);
		}
		catch (InvalidCastException)
		{
		}
	}

	/// <summary>
	/// Inserts the elements of the given collection at a given index. If
	/// required, the capacity of the list is increased to twice the previous
	/// capacity or the new size, whichever is larger.  Ranges may be added
	/// to the end of the list by setting index to the List's size.
	/// </summary>
	public void InsertRange(int index, IEnumerable<T> collection)
	{
		if ((uint)index > (uint)_size)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (collection != null)
		{
			if (collection is ICollection<T> { Count: var count } collection2)
			{
				if (count > 0)
				{
					EnsureCapacity(_size + count);
					if (index < _size)
					{
						Array.Copy(_items, index, _items, index + count, _size - index);
					}
					if (this == collection2)
					{
						Array.Copy(_items, 0, _items, index, index);
						Array.Copy(_items, index + count, _items, index * 2, _size - index);
					}
					else
					{
						collection2.CopyTo(_items, index);
					}
					_size += count;
				}
			}
			else
			{
				using IEnumerator<T> enumerator = collection.GetEnumerator();
				while (enumerator.MoveNext())
				{
					Insert(index++, enumerator.Current);
				}
			}
		}
		_version++;
	}

	/// <summary>
	/// Inserts the elements of the given collection at a given index. If
	/// required, the capacity of the list is increased to twice the previous
	/// capacity or the new size, whichever is larger.  Ranges may be added
	/// to the end of the list by setting index to the List's size.
	/// </summary>
	public void InsertRange(int index, ReadOnlySpan<T> span)
	{
		Span<T> destination = InsertSpan(index, span.Length, clearOutput: false);
		span.CopyTo(destination);
	}

	/// <summary>
	/// Inserts the elements of the given collection at a given index. If
	/// required, the capacity of the list is increased to twice the previous
	/// capacity or the new size, whichever is larger.  Ranges may be added
	/// to the end of the list by setting index to the List's size.
	/// </summary>
	public void InsertRange(int index, T[] array)
	{
		InsertRange(index, array.AsSpan());
	}

	/// <summary>
	/// Advances the <see cref="P:FluentAvalonia.Collections.PooledList`1.Count" /> by the number of items specified,
	/// increasing the capacity if required, then returns a Span representing
	/// the set of items to be added, allowing direct writes to that section
	/// of the collection.
	/// </summary>
	public Span<T> InsertSpan(int index, int count)
	{
		return InsertSpan(index, count, clearOutput: true);
	}

	private Span<T> InsertSpan(int index, int count, bool clearOutput)
	{
		EnsureCapacity(_size + count);
		if (index < _size)
		{
			Array.Copy(_items, index, _items, index + count, _size - index);
		}
		_size += count;
		_version++;
		Span<T> result = _items.AsSpan(index, count);
		if (clearOutput && _clearOnFree)
		{
			result.Clear();
		}
		return result;
	}

	/// <summary>
	/// Returns the index of the last occurrence of a given value in a range of
	/// this list. The list is searched backwards, starting at the end 
	/// and ending at the first element in the list.
	/// </summary>
	public int LastIndexOf(T item)
	{
		if (_size == 0)
		{
			return -1;
		}
		return LastIndexOf(item, _size - 1, _size);
	}

	/// <summary>
	/// Returns the index of the last occurrence of a given value in a range of
	/// this list. The list is searched backwards, starting at index
	/// index and ending at the first element in the list.
	/// </summary>
	public int LastIndexOf(T item, int index)
	{
		if (index >= _size)
		{
			throw new ArgumentOutOfRangeException();
		}
		return LastIndexOf(item, index, index + 1);
	}

	/// <summary>
	/// Returns the index of the last occurrence of a given value in a range of
	/// this list. The list is searched backwards, starting at index
	/// index and upto count elements
	/// </summary>
	public int LastIndexOf(T item, int index, int count)
	{
		if (Count != 0 && index < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (Count != 0 && count < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (_size == 0)
		{
			return -1;
		}
		if (index >= _size)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (count > index + 1)
		{
			throw new ArgumentOutOfRangeException();
		}
		return Array.LastIndexOf(_items, item, index, count);
	}

	public bool Remove(T item)
	{
		int num = IndexOf(item);
		if (num >= 0)
		{
			RemoveAt(num);
			return true;
		}
		return false;
	}

	void IList.Remove(object item)
	{
		if (IsCompatibleObject(item))
		{
			Remove((T)item);
		}
	}

	/// <summary>
	/// This method removes all items which match the predicate.
	/// The complexity is O(n).
	/// </summary>
	public int RemoveAll(Func<T, bool> match)
	{
		int i;
		for (i = 0; i < _size && !match(_items[i]); i++)
		{
		}
		if (i >= _size)
		{
			return 0;
		}
		int j = i + 1;
		while (j < _size)
		{
			for (; j < _size && match(_items[j]); j++)
			{
			}
			if (j < _size)
			{
				_items[i++] = _items[j++];
			}
		}
		if (_clearOnFree)
		{
			Array.Clear(_items, i, _size - i);
		}
		int result = _size - i;
		_size = i;
		_version++;
		return result;
	}

	/// <summary>
	/// Removes the element at the given index. The size of the list is
	/// decreased by one.
	/// </summary>
	public void RemoveAt(int index)
	{
		if ((uint)index >= (uint)_size)
		{
			throw new ArgumentOutOfRangeException();
		}
		_size--;
		if (index < _size)
		{
			Array.Copy(_items, index + 1, _items, index, _size - index);
		}
		_version++;
		if (_clearOnFree)
		{
			_items[_size] = default(T);
		}
	}

	/// <summary>
	/// Removes a range of elements from this list.
	/// </summary>
	public void RemoveRange(int index, int count)
	{
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (_size - index < count)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (count > 0)
		{
			_size -= count;
			if (index < _size)
			{
				Array.Copy(_items, index + count, _items, index, _size - index);
			}
			_version++;
			if (_clearOnFree)
			{
				Array.Clear(_items, _size, count);
			}
		}
	}

	/// <summary>
	/// Reverses the elements in this list.
	/// </summary>
	public void Reverse()
	{
		Reverse(0, _size);
	}

	/// <summary>
	/// Reverses the elements in a range of this list. Following a call to this
	/// method, an element in the range given by index and count
	/// which was previously located at index i will now be located at
	/// index + (index + count - i - 1).
	/// </summary>
	public void Reverse(int index, int count)
	{
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (_size - index < count)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (count > 1)
		{
			Array.Reverse(_items, index, count);
		}
		_version++;
	}

	/// <summary>
	/// Sorts the elements in this list.  Uses the default comparer and 
	/// Array.Sort.
	/// </summary>
	public void Sort()
	{
		Sort(0, Count, null);
	}

	/// <summary>
	/// Sorts the elements in this list.  Uses Array.Sort with the
	/// provided comparer.
	/// </summary>
	/// <param name="comparer"></param>
	public void Sort(IComparer<T> comparer)
	{
		Sort(0, Count, comparer);
	}

	/// <summary>
	/// Sorts the elements in a section of this list. The sort compares the
	/// elements to each other using the given IComparer interface. If
	/// comparer is null, the elements are compared to each other using
	/// the IComparable interface, which in that case must be implemented by all
	/// elements of the list.
	///
	/// This method uses the Array.Sort method to sort the elements.
	/// </summary>
	public void Sort(int index, int count, IComparer<T> comparer)
	{
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (_size - index < count)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (count > 1)
		{
			Array.Sort(_items, index, count, comparer);
		}
		_version++;
	}

	public void Sort(Func<T, T, int> comparison)
	{
		if (_size > 1)
		{
			Array.Sort(_items, 0, _size, new Comparer(comparison));
		}
		_version++;
	}

	/// <summary>
	/// ToArray returns an array containing the contents of the List.
	/// This requires copying the List, which is an O(n) operation.
	/// </summary>
	public T[] ToArray()
	{
		if (_size == 0)
		{
			return s_emptyArray;
		}
		return Span.ToArray();
	}

	/// <summary>
	/// Sets the capacity of this list to the size of the list. This method can
	/// be used to minimize a list's memory overhead once it is known that no
	/// new elements will be added to the list. To completely clear a list and
	/// release all memory referenced by the list, execute the following
	/// statements:
	/// <code>
	/// list.Clear();
	/// list.TrimExcess();
	/// </code>
	/// </summary>
	public void TrimExcess()
	{
		int num = (int)((double)_items.Length * 0.9);
		if (_size < num)
		{
			Capacity = _size;
		}
	}

	private void ReturnArray()
	{
		if (_items.Length != 0)
		{
			try
			{
				_pool.Return(_items, _clearOnFree);
			}
			catch (ArgumentException)
			{
			}
			_items = s_emptyArray;
		}
	}

	private static bool ShouldClear(ClearMode mode)
	{
		return mode switch
		{
			ClearMode.Auto => RuntimeHelpers.IsReferenceOrContainsReferences<T>(), 
			ClearMode.Always => true, 
			_ => false, 
		};
	}

	/// <summary>
	/// Returns the internal buffers to the ArrayPool.
	/// </summary>
	public virtual void Dispose()
	{
		ReturnArray();
		_size = 0;
		_version++;
	}
}
