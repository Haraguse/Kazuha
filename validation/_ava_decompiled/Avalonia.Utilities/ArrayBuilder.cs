using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities;

/// <summary>
/// A helper type for avoiding allocations while building arrays.
/// </summary>
/// <typeparam name="T">The type of item contained in the array.</typeparam>
internal struct ArrayBuilder<T>
{
	private const int DefaultCapacity = 4;

	private const int MaxCoreClrArrayLength = 2146435071;

	private T[]? _data;

	private int _size;

	/// <summary>
	/// Gets or sets the number of items in the array.
	/// </summary>
	public int Length
	{
		get
		{
			return _size;
		}
		set
		{
			if (value != _size)
			{
				if (value > 0)
				{
					EnsureCapacity(value);
					_size = value;
				}
				else
				{
					_size = 0;
				}
			}
		}
	}

	/// <summary>
	/// Gets the current capacity of the array.
	/// </summary>
	public int Capacity
	{
		get
		{
			T[]? data = _data;
			if (data == null)
			{
				return 0;
			}
			return data.Length;
		}
	}

	/// <summary>
	/// Returns a reference to specified element of the array.
	/// </summary>
	/// <param name="index">The index of the element to return.</param>
	/// <returns>The <typeparamref name="T" />.</returns>
	/// <exception cref="T:System.IndexOutOfRangeException">
	/// Thrown when index less than 0 or index greater than or equal to <see cref="P:Avalonia.Utilities.ArrayBuilder`1.Length" />.
	/// </exception>
	public ref T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ref _data[index];
		}
	}

	/// <summary>
	/// Appends a given number of empty items to the array returning
	/// the items as a slice.
	/// </summary>
	/// <param name="length">The number of items in the slice.</param>
	/// <param name="clear">Whether to clear the new slice, Defaults to <see langword="true" />.</param>
	/// <returns>The <see cref="T:Avalonia.Utilities.ArraySlice`1" />.</returns>
	public ArraySlice<T> Add(int length, bool clear = true)
	{
		int size = _size;
		Length += length;
		ArraySlice<T> result = AsSlice(size, Length - size);
		if (clear)
		{
			result.Span.Clear();
		}
		return result;
	}

	/// <summary>
	/// Appends the slice to the array copying the data across.
	/// </summary>
	/// <param name="value">The array slice.</param>
	/// <returns>The <see cref="T:Avalonia.Utilities.ArraySlice`1" />.</returns>
	public ArraySlice<T> Add(in ArraySlice<T> value)
	{
		int size = _size;
		Length += value.Length;
		ArraySlice<T> result = AsSlice(size, Length - size);
		value.Span.CopyTo(result.Span);
		return result;
	}

	/// <summary>
	/// Appends an item.
	/// </summary>
	/// <param name="value">The item to append.</param>
	public void AddItem(T value)
	{
		int num = Length++;
		_data[num] = value;
	}

	/// <summary>
	/// Clears the array.
	/// Allocated memory is left intact for future usage.
	/// </summary>
	public void Clear()
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			ClearArray();
		}
		else
		{
			_size = 0;
		}
	}

	private void ClearArray()
	{
		int size = _size;
		_size = 0;
		if (size > 0)
		{
			Array.Clear(_data, 0, size);
		}
	}

	private void EnsureCapacity(int min)
	{
		T[]? data = _data;
		int num = ((data != null) ? data.Length : 0);
		if (num < min)
		{
			uint num2 = ((num == 0) ? 4u : ((uint)(num * 2)));
			if (num2 > 2146435071)
			{
				num2 = 2146435071u;
			}
			if (num2 < min)
			{
				num2 = (uint)min;
			}
			T[] array = new T[num2];
			if (_size > 0)
			{
				Array.Copy(_data, array, _size);
			}
			_data = array;
		}
	}

	/// <summary>
	/// Returns the current state of the array as a slice.
	/// </summary>
	/// <returns>The <see cref="T:Avalonia.Utilities.ArraySlice`1" />.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ArraySlice<T> AsSlice()
	{
		return AsSlice(Length);
	}

	/// <summary>
	/// Returns the current state of the array as a slice.
	/// </summary>
	/// <param name="length">The number of items in the slice.</param>
	/// <returns>The <see cref="T:Avalonia.Utilities.ArraySlice`1" />.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ArraySlice<T> AsSlice(int length)
	{
		return new ArraySlice<T>(_data, 0, length);
	}

	/// <summary>
	/// Returns the current state of the array as a slice.
	/// </summary>
	/// <param name="start">The index at which to begin the slice.</param>
	/// <param name="length">The number of items in the slice.</param>
	/// <returns>The <see cref="T:Avalonia.Utilities.ArraySlice`1" />.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ArraySlice<T> AsSlice(int start, int length)
	{
		return new ArraySlice<T>(_data, start, length);
	}

	/// <summary>
	/// Returns the current state of the array as a span.
	/// </summary>
	/// <returns>The <see cref="T:System.Span`1" />.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<T> AsSpan()
	{
		return _data.AsSpan(0, _size);
	}
}
