using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace Avalonia.Collections.Pooled;

/// <summary>
/// A simple stack of objects.  Internally it is implemented as an array,
/// so Push can be O(n).  Pop is O(1).
/// </summary>
[Serializable]
[DebuggerTypeProxy(typeof(StackDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
internal class PooledStack<T> : IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>, IDisposable, IDeserializationCallback
{
	public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
	{
		private readonly PooledStack<T> _stack;

		private readonly int _version;

		private int _index;

		private T? _currentElement;

		public T Current
		{
			get
			{
				if (_index < 0)
				{
					ThrowEnumerationNotStartedOrEnded();
				}
				return _currentElement;
			}
		}

		object? IEnumerator.Current => Current;

		internal Enumerator(PooledStack<T> stack)
		{
			_stack = stack;
			_version = stack._version;
			_index = -2;
			_currentElement = default(T);
		}

		public void Dispose()
		{
			_index = -1;
		}

		public bool MoveNext()
		{
			if (_version != _stack._version)
			{
				throw new InvalidOperationException("Collection was modified during enumeration.");
			}
			if (_index == -2)
			{
				_index = _stack._size - 1;
				bool num = _index >= 0;
				if (num)
				{
					_currentElement = _stack._array[_index];
				}
				return num;
			}
			if (_index == -1)
			{
				return false;
			}
			bool num2 = --_index >= 0;
			if (num2)
			{
				_currentElement = _stack._array[_index];
				return num2;
			}
			_currentElement = default(T);
			return num2;
		}

		private void ThrowEnumerationNotStartedOrEnded()
		{
			throw new InvalidOperationException((_index == -2) ? "Enumeration was not started." : "Enumeration has ended.");
		}

		void IEnumerator.Reset()
		{
			if (_version != _stack._version)
			{
				throw new InvalidOperationException("Collection was modified during enumeration.");
			}
			_index = -2;
			_currentElement = default(T);
		}
	}

	[NonSerialized]
	private ArrayPool<T> _pool;

	[NonSerialized]
	private object? _syncRoot;

	private T[] _array;

	private int _size;

	private int _version;

	private readonly bool _clearOnFree;

	private const int DefaultCapacity = 4;

	/// <summary>
	/// The number of items in the stack.
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
	/// Create a stack with the default initial capacity. 
	/// </summary>
	public PooledStack()
		: this(ClearMode.Auto, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Create a stack with the default initial capacity. 
	/// </summary>
	public PooledStack(ClearMode clearMode)
		: this(clearMode, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Create a stack with the default initial capacity. 
	/// </summary>
	public PooledStack(ArrayPool<T> customPool)
		: this(ClearMode.Auto, customPool)
	{
	}

	/// <summary>
	/// Create a stack with the default initial capacity and a custom ArrayPool.
	/// </summary>
	public PooledStack(ClearMode clearMode, ArrayPool<T> customPool)
	{
		_pool = customPool ?? ArrayPool<T>.Shared;
		_array = Array.Empty<T>();
		_clearOnFree = ShouldClear(clearMode);
	}

	/// <summary>
	/// Create a stack with a specific initial capacity.  The initial capacity
	/// must be a non-negative number.
	/// </summary>
	public PooledStack(int capacity)
		: this(capacity, ClearMode.Auto, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Create a stack with a specific initial capacity.  The initial capacity
	/// must be a non-negative number.
	/// </summary>
	public PooledStack(int capacity, ClearMode clearMode)
		: this(capacity, clearMode, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Create a stack with a specific initial capacity.  The initial capacity
	/// must be a non-negative number.
	/// </summary>
	public PooledStack(int capacity, ArrayPool<T> customPool)
		: this(capacity, ClearMode.Auto, customPool)
	{
	}

	/// <summary>
	/// Create a stack with a specific initial capacity.  The initial capacity
	/// must be a non-negative number.
	/// </summary>
	public PooledStack(int capacity, ClearMode clearMode, ArrayPool<T> customPool)
	{
		if (capacity < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		_pool = customPool ?? ArrayPool<T>.Shared;
		_array = _pool.Rent(capacity);
		_clearOnFree = ShouldClear(clearMode);
	}

	/// <summary>
	/// Fills a Stack with the contents of a particular collection.  The items are
	/// pushed onto the stack in the same order they are read by the enumerator.
	/// </summary>
	public PooledStack(IEnumerable<T> enumerable)
		: this(enumerable, ClearMode.Auto, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Fills a Stack with the contents of a particular collection.  The items are
	/// pushed onto the stack in the same order they are read by the enumerator.
	/// </summary>
	public PooledStack(IEnumerable<T> enumerable, ClearMode clearMode)
		: this(enumerable, clearMode, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Fills a Stack with the contents of a particular collection.  The items are
	/// pushed onto the stack in the same order they are read by the enumerator.
	/// </summary>
	public PooledStack(IEnumerable<T> enumerable, ArrayPool<T> customPool)
		: this(enumerable, ClearMode.Auto, customPool)
	{
	}

	/// <summary>
	/// Fills a Stack with the contents of a particular collection.  The items are
	/// pushed onto the stack in the same order they are read by the enumerator.
	/// </summary>
	public PooledStack(IEnumerable<T> enumerable, ClearMode clearMode, ArrayPool<T> customPool)
	{
		_pool = customPool ?? ArrayPool<T>.Shared;
		_clearOnFree = ShouldClear(clearMode);
		if (enumerable != null)
		{
			if (!(enumerable is ICollection<T> collection))
			{
				using (PooledList<T> pooledList = new PooledList<T>(enumerable))
				{
					_array = _pool.Rent(pooledList.Count);
					pooledList.Span.CopyTo(_array);
					_size = pooledList.Count;
					return;
				}
			}
			if (collection.Count == 0)
			{
				_array = Array.Empty<T>();
				return;
			}
			_array = _pool.Rent(collection.Count);
			collection.CopyTo(_array, 0);
			_size = collection.Count;
		}
		else
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.enumerable);
		}
	}

	/// <summary>
	/// Fills a Stack with the contents of a particular collection.  The items are
	/// pushed onto the stack in the same order they are read by the enumerator.
	/// </summary>
	public PooledStack(T[] array)
		: this((ReadOnlySpan<T>)array.AsSpan(), ClearMode.Auto, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Fills a Stack with the contents of a particular collection.  The items are
	/// pushed onto the stack in the same order they are read by the enumerator.
	/// </summary>
	public PooledStack(T[] array, ClearMode clearMode)
		: this((ReadOnlySpan<T>)array.AsSpan(), clearMode, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Fills a Stack with the contents of a particular collection.  The items are
	/// pushed onto the stack in the same order they are read by the enumerator.
	/// </summary>
	public PooledStack(T[] array, ArrayPool<T> customPool)
		: this((ReadOnlySpan<T>)array.AsSpan(), ClearMode.Auto, customPool)
	{
	}

	/// <summary>
	/// Fills a Stack with the contents of a particular collection.  The items are
	/// pushed onto the stack in the same order they are read by the enumerator.
	/// </summary>
	public PooledStack(T[] array, ClearMode clearMode, ArrayPool<T> customPool)
		: this((ReadOnlySpan<T>)array.AsSpan(), clearMode, customPool)
	{
	}

	/// <summary>
	/// Fills a Stack with the contents of a particular collection.  The items are
	/// pushed onto the stack in the same order they are read by the enumerator.
	/// </summary>
	public PooledStack(ReadOnlySpan<T> span)
		: this(span, ClearMode.Auto, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Fills a Stack with the contents of a particular collection.  The items are
	/// pushed onto the stack in the same order they are read by the enumerator.
	/// </summary>
	public PooledStack(ReadOnlySpan<T> span, ClearMode clearMode)
		: this(span, clearMode, ArrayPool<T>.Shared)
	{
	}

	/// <summary>
	/// Fills a Stack with the contents of a particular collection.  The items are
	/// pushed onto the stack in the same order they are read by the enumerator.
	/// </summary>
	public PooledStack(ReadOnlySpan<T> span, ArrayPool<T> customPool)
		: this(span, ClearMode.Auto, customPool)
	{
	}

	/// <summary>
	/// Fills a Stack with the contents of a particular collection.  The items are
	/// pushed onto the stack in the same order they are read by the enumerator.
	/// </summary>
	public PooledStack(ReadOnlySpan<T> span, ClearMode clearMode, ArrayPool<T> customPool)
	{
		_pool = customPool ?? ArrayPool<T>.Shared;
		_clearOnFree = ShouldClear(clearMode);
		_array = _pool.Rent(span.Length);
		span.CopyTo(_array);
		_size = span.Length;
	}

	/// <summary>
	/// Removes all Objects from the Stack.
	/// </summary>
	public void Clear()
	{
		if (_clearOnFree)
		{
			Array.Clear(_array, 0, _size);
		}
		_size = 0;
		_version++;
	}

	/// <summary>
	/// Compares items using the default equality comparer
	/// </summary>
	public bool Contains(T item)
	{
		if (_size != 0)
		{
			return Array.LastIndexOf(_array, item, _size - 1) != -1;
		}
		return false;
	}

	/// <summary>
	/// This method removes all items which match the predicate.
	/// The complexity is O(n).
	/// </summary>
	public int RemoveWhere(Func<T, bool> match)
	{
		if (match == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
		}
		int i;
		for (i = 0; i < _size && !match(_array[i]); i++)
		{
		}
		if (i >= _size)
		{
			return 0;
		}
		int j = i + 1;
		while (j < _size)
		{
			for (; j < _size && match(_array[j]); j++)
			{
			}
			if (j < _size)
			{
				_array[i++] = _array[j++];
			}
		}
		if (_clearOnFree)
		{
			Array.Clear(_array, i, _size - i);
		}
		int result = _size - i;
		_size = i;
		_version++;
		return result;
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (arrayIndex < 0 || arrayIndex > array.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.arrayIndex);
		}
		if (array.Length - arrayIndex < _size)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
		}
		int num = 0;
		int num2 = arrayIndex + _size;
		while (num < _size)
		{
			array[--num2] = _array[num++];
		}
	}

	public void CopyTo(Span<T> span)
	{
		if (span.Length < _size)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		int num = 0;
		int num2 = _size;
		while (num < _size)
		{
			span[--num2] = _array[num++];
		}
	}

	void ICollection.CopyTo(Array array, int arrayIndex)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (array.Rank != 1)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
		}
		if (array.GetLowerBound(0) != 0)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound, ExceptionArgument.array);
		}
		if (arrayIndex < 0 || arrayIndex > array.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.arrayIndex);
		}
		if (array.Length - arrayIndex < _size)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
		}
		try
		{
			Array.Copy(_array, 0, array, arrayIndex, _size);
			Array.Reverse(array, arrayIndex, _size);
		}
		catch (ArrayTypeMismatchException)
		{
			ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
		}
	}

	/// <summary>
	/// Returns an IEnumerator for this PooledStack.
	/// </summary>
	/// <returns></returns>
	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	/// <internalonly />
	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this);
	}

	public void TrimExcess()
	{
		if (_size == 0)
		{
			ReturnArray(Array.Empty<T>());
			_version++;
			return;
		}
		int num = (int)((double)_array.Length * 0.9);
		if (_size < num)
		{
			T[] array = _pool.Rent(_size);
			if (array.Length < _array.Length)
			{
				Array.Copy(_array, array, _size);
				ReturnArray(array);
				_version++;
			}
			else
			{
				_pool.Return(array);
			}
		}
	}

	/// <summary>
	/// Returns the top object on the stack without removing it.  If the stack
	/// is empty, Peek throws an InvalidOperationException.
	/// </summary>
	public T Peek()
	{
		int num = _size - 1;
		T[] array = _array;
		if ((uint)num >= (uint)array.Length)
		{
			ThrowForEmptyStack();
		}
		return array[num];
	}

	public bool TryPeek([MaybeNullWhen(false)] out T result)
	{
		int num = _size - 1;
		T[] array = _array;
		if ((uint)num >= (uint)array.Length)
		{
			result = default(T);
			return false;
		}
		result = array[num];
		return true;
	}

	/// <summary>
	/// Pops an item from the top of the stack.  If the stack is empty, Pop
	/// throws an InvalidOperationException.
	/// </summary>
	public T Pop()
	{
		int num = _size - 1;
		T[] array = _array;
		if ((uint)num >= (uint)array.Length)
		{
			ThrowForEmptyStack();
		}
		_version++;
		_size = num;
		T result = array[num];
		if (_clearOnFree)
		{
			array[num] = default(T);
		}
		return result;
	}

	public bool TryPop([MaybeNullWhen(false)] out T result)
	{
		int num = _size - 1;
		T[] array = _array;
		if ((uint)num >= (uint)array.Length)
		{
			result = default(T);
			return false;
		}
		_version++;
		_size = num;
		result = array[num];
		if (_clearOnFree)
		{
			array[num] = default(T);
		}
		return true;
	}

	/// <summary>
	/// Pushes an item to the top of the stack.
	/// </summary>
	public void Push(T item)
	{
		int size = _size;
		T[] array = _array;
		if ((uint)size < (uint)array.Length)
		{
			array[size] = item;
			_version++;
			_size = size + 1;
		}
		else
		{
			PushWithResize(item);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void PushWithResize(T item)
	{
		T[] array = _pool.Rent((_array.Length == 0) ? 4 : (2 * _array.Length));
		Array.Copy(_array, array, _size);
		ReturnArray(array);
		_array[_size] = item;
		_version++;
		_size++;
	}

	/// <summary>
	/// Copies the Stack to an array, in the same order Pop would return the items.
	/// </summary>
	public T[] ToArray()
	{
		if (_size == 0)
		{
			return Array.Empty<T>();
		}
		T[] array = new T[_size];
		for (int i = 0; i < _size; i++)
		{
			array[i] = _array[_size - i - 1];
		}
		return array;
	}

	private void ThrowForEmptyStack()
	{
		throw new InvalidOperationException("Stack was empty.");
	}

	private void ReturnArray(T[]? replaceWith = null)
	{
		T[] array = _array;
		if (array != null && array.Length != 0)
		{
			try
			{
				_pool.Return(_array, _clearOnFree);
			}
			catch (ArgumentException)
			{
			}
		}
		if (replaceWith != null)
		{
			_array = replaceWith;
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

	public void Dispose()
	{
		ReturnArray(Array.Empty<T>());
		_size = 0;
		_version++;
	}

	void IDeserializationCallback.OnDeserialization(object? sender)
	{
		_pool = ArrayPool<T>.Shared;
	}
}
