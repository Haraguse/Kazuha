using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities;

/// <summary>
/// ArraySlice represents a contiguous region of arbitrary memory similar
/// to <see cref="T:System.Memory`1" /> and <see cref="T:System.Span`1" /> though constrained
/// to arrays.
/// Unlike <see cref="T:System.Span`1" />, it is not a byref-like type.
/// </summary>
/// <typeparam name="T">The type of item contained in the slice.</typeparam>
internal readonly struct ArraySlice<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
{
	private readonly T[] _data;

	/// <summary>
	/// Gets an empty <see cref="T:Avalonia.Utilities.ArraySlice`1" />
	/// </summary>
	public static ArraySlice<T> Empty => new ArraySlice<T>(Array.Empty<T>());

	/// <summary>
	///     Gets a value that indicates whether this instance of <see cref="T:Avalonia.Utilities.ArraySlice`1" /> is Empty.
	/// </summary>
	public bool IsEmpty => Length == 0;

	/// <summary>
	/// Gets the offset position in the underlying buffer this slice was created from.
	/// </summary>
	public int Start { get; }

	/// <summary>
	/// Gets the number of items in the slice.
	/// </summary>
	public int Length { get; }

	/// <summary>
	/// Gets a <see cref="T:System.Span`1" /> representing this slice.
	/// </summary>
	public Span<T> Span => new Span<T>(_data, Start, Length);

	/// <summary>
	/// Returns a reference to specified element of the slice.
	/// </summary>
	/// <param name="index">The index of the element to return.</param>
	/// <returns>The <typeparamref name="T" />.</returns>
	/// <exception cref="T:System.IndexOutOfRangeException">
	/// Thrown when index less than 0 or index greater than or equal to <see cref="P:Avalonia.Utilities.ArraySlice`1.Length" />.
	/// </exception>
	public ref T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			int num = index + Start;
			return ref _data[num];
		}
	}

	/// <inheritdoc />
	T IReadOnlyList<T>.this[int index] => this[index];

	/// <inheritdoc />
	int IReadOnlyCollection<T>.Count => Length;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Utilities.ArraySlice`1" /> struct.
	/// </summary>
	/// <param name="data">The underlying data buffer.</param>
	public ArraySlice(T[] data)
		: this(data, 0, data.Length)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Utilities.ArraySlice`1" /> struct.
	/// </summary>
	/// <param name="data">The underlying data buffer.</param>
	/// <param name="start">The offset position in the underlying buffer this slice was created from.</param>
	/// <param name="length">The number of items in the slice.</param>
	public ArraySlice(T[] data, int start, int length)
	{
		_data = data;
		Start = start;
		Length = length;
	}

	/// <summary>
	/// Defines an implicit conversion of an array to a <see cref="T:Avalonia.Utilities.ArraySlice`1" />
	/// </summary>
	public static implicit operator ArraySlice<T>(T[] array)
	{
		return new ArraySlice<T>(array, 0, array.Length);
	}

	/// <summary>
	/// Fills the contents of this slice with the given value.
	/// </summary>
	public void Fill(T value)
	{
		Span.Fill(value);
	}

	/// <summary>
	/// Forms a slice out of the given slice, beginning at 'start', of given length
	/// </summary>
	/// <param name="start">The index at which to begin this slice.</param>
	/// <param name="length">The desired length for the slice (exclusive).</param>
	/// <exception cref="T:System.ArgumentOutOfRangeException">
	/// Thrown when the specified <paramref name="start" /> or end index is not in range (&lt;0 or &gt;Length).
	/// </exception>
	public ArraySlice<T> Slice(int start, int length)
	{
		return new ArraySlice<T>(_data, start, length);
	}

	/// <summary>
	///     Returns a specified number of contiguous elements from the start of the slice.
	/// </summary>
	/// <param name="length">The number of elements to return.</param>
	/// <returns>A <see cref="T:Avalonia.Utilities.ArraySlice`1" /> that contains the specified number of elements from the start of this slice.</returns>
	public ArraySlice<T> Take(int length)
	{
		if (IsEmpty)
		{
			return this;
		}
		if (length > Length)
		{
			throw new ArgumentOutOfRangeException("length");
		}
		return new ArraySlice<T>(_data, Start, length);
	}

	/// <summary>
	///     Bypasses a specified number of elements in the slice and then returns the remaining elements.
	/// </summary>
	/// <param name="length">The number of elements to skip before returning the remaining elements.</param>
	/// <returns>A <see cref="T:Avalonia.Utilities.ArraySlice`1" /> that contains the elements that occur after the specified index in this slice.</returns>
	public ArraySlice<T> Skip(int length)
	{
		if (IsEmpty)
		{
			return this;
		}
		if (length > Length)
		{
			throw new ArgumentOutOfRangeException("length");
		}
		return new ArraySlice<T>(_data, Start + length, Length - length);
	}

	public ImmutableReadOnlyListStructEnumerator<T> GetEnumerator()
	{
		return new ImmutableReadOnlyListStructEnumerator<T>(this);
	}

	/// <inheritdoc />
	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
