using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Utilities;

/// <summary>
/// Stores values with <see cref="T:Avalonia.AvaloniaProperty" /> as key.
/// </summary>
/// <typeparam name="TValue">Stored value type.</typeparam>
/// <remarks>
/// This struct implements the most commonly-used part of the dictionary API, but does
/// not implement <see cref="T:System.Collections.Generic.IDictionary`2" />. In particular, this struct
/// is not enumerable. Enumeration is intended to be done by index for better performance.
/// </remarks>
internal struct AvaloniaPropertyDictionary<TValue>
{
	private readonly struct Entry(AvaloniaProperty property, TValue value)
	{
		public readonly int Id = property.Id;

		public readonly TValue Value = value;
	}

	private const int DefaultInitialCapacity = 4;

	private Entry[]? _entries;

	private int _entryCount;

	/// <summary>
	/// Gets the number of key/value pairs contained in the collection.
	/// </summary>
	public int Count => _entryCount;

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <param name="property">The key to get or set.</param>
	/// <returns>
	/// The value associated with the specified key. If the key is not found, a get operation
	/// throws a <see cref="T:System.Collections.Generic.KeyNotFoundException" />, and a set operation creates a
	/// new element for the specified key.
	/// </returns>
	/// <exception cref="T:System.Collections.Generic.KeyNotFoundException">
	/// The key does not exist in the collection.
	/// </exception>
	public TValue this[AvaloniaProperty property]
	{
		get
		{
			int num = FindEntry(property.Id);
			if (num < 0)
			{
				ThrowNotFound();
			}
			return UnsafeGetEntryRef(num).Value;
		}
		set
		{
			int num = FindEntry(property.Id);
			if (num >= 0)
			{
				UnsafeGetEntryRef(num) = new Entry(property, value);
			}
			else
			{
				InsertEntry(new Entry(property, value), ~num);
			}
		}
	}

	/// <summary>
	/// Gets the value at the specified index.
	/// </summary>
	/// <param name="index">
	/// The index of the entry, between 0 and <see cref="P:Avalonia.Utilities.AvaloniaPropertyDictionary`1.Count" /> - 1.
	/// </param>
	public TValue this[int index]
	{
		get
		{
			if (index >= _entryCount)
			{
				ThrowOutOfRange();
			}
			return UnsafeGetEntryRef(index).Value;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Utilities.AvaloniaPropertyDictionary`1" />
	/// class that is empty and has the default initial capacity.
	/// </summary>
	public AvaloniaPropertyDictionary()
	{
		_entries = null;
		_entryCount = 0;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Utilities.AvaloniaPropertyDictionary`1" />
	/// class that is empty and has the specified initial capacity.
	/// </summary>
	/// <param name="capactity">
	/// The initial number of elements that the collection can contain.
	/// </param>
	public AvaloniaPropertyDictionary(int capactity)
	{
		_entries = new Entry[capactity];
		_entryCount = 0;
	}

	/// <summary>
	/// Adds the specified key and value to the dictionary.
	/// </summary>
	/// <param name="property">The key.</param>
	/// <param name="value">The value of the element to add.</param>
	public void Add(AvaloniaProperty property, TValue value)
	{
		int num = FindEntry(property.Id);
		if (num >= 0)
		{
			ThrowDuplicate();
		}
		InsertEntry(new Entry(property, value), ~num);
	}

	/// <summary>
	/// Removes all keys and values from the collection.
	/// </summary>
	/// <remarks>
	/// The Count property is set to 0, and references to other objects from elements of the
	/// collection are also released. The capacity remains unchanged.
	/// </remarks>
	public void Clear()
	{
		if (_entries != null)
		{
			Array.Clear(_entries, 0, _entries.Length);
			_entryCount = 0;
		}
	}

	/// <summary>
	/// Determines whether the collection contains the specified key.
	/// </summary>
	/// <param name="property">The key.</param>
	public bool ContainsKey(AvaloniaProperty property)
	{
		return FindEntry(property.Id) >= 0;
	}

	/// <summary>
	/// Gets value at the specified index.
	/// </summary>
	/// <param name="index">The index of the entry, between 0 and <see cref="P:Avalonia.Utilities.AvaloniaPropertyDictionary`1.Count" /> - 1.</param>
	/// <returns>The value at the specified index.</returns>
	public TValue GetValue(int index)
	{
		if (index >= _entryCount)
		{
			ThrowOutOfRange();
		}
		return UnsafeGetEntryRef(index).Value;
	}

	/// <summary>
	/// Removes the value of the specified key from the collection.
	/// </summary>
	/// <param name="property">The key.</param>
	/// <returns>
	/// true if the element is successfully found and removed; otherwise, false. This method
	/// returns false if key is not found in the collection.
	/// </returns>
	public bool Remove(AvaloniaProperty property)
	{
		int num = FindEntry(property.Id);
		if (num >= 0)
		{
			RemoveAt(num);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Removes the value of the specified key from the collection, and copies the element to
	/// the value parameter.
	/// </summary>
	/// <param name="property">The key.</param>
	/// <param name="value">The removed element.</param>
	/// <returns>
	/// true if the element is successfully found and removed; otherwise, false. This method
	/// returns false if key is not found in the collection.
	/// </returns>
	public bool Remove(AvaloniaProperty property, [MaybeNullWhen(false)] out TValue value)
	{
		int num = FindEntry(property.Id);
		if (num >= 0)
		{
			value = UnsafeGetEntryRef(num).Value;
			RemoveAt(num);
			return true;
		}
		value = default(TValue);
		return false;
	}

	/// <summary>
	/// Removes the element at the specified index from the collection.
	/// </summary>
	/// <param name="index">The index.</param>
	public void RemoveAt(int index)
	{
		if (_entries == null)
		{
			ThrowOutOfRange();
		}
		Array.Copy(_entries, index + 1, _entries, index, _entryCount - index - 1);
		_entryCount--;
		UnsafeGetEntryRef(_entryCount) = default(Entry);
	}

	/// <summary>
	/// Attempts to add the specified key and value to the collection.
	/// </summary>
	/// <param name="property">The key.</param>
	/// <param name="value">The value of the element to add.</param>
	/// <returns></returns>
	public bool TryAdd(AvaloniaProperty property, TValue value)
	{
		int num = FindEntry(property.Id);
		if (num >= 0)
		{
			return false;
		}
		InsertEntry(new Entry(property, value), ~num);
		return true;
	}

	/// <summary>
	/// Gets the value associated with the specified key.
	/// </summary>
	/// <param name="property">The property key.</param>
	/// <param name="value">
	/// When this method returns, contains the value associated with the specified key,
	/// if the property is found; otherwise, null. This parameter is passed uninitialized.
	/// </param>
	/// <returns></returns>
	public bool TryGetValue(AvaloniaProperty property, [MaybeNullWhen(false)] out TValue value)
	{
		int num = 0;
		int num2 = _entryCount - 1;
		if (num2 >= 0)
		{
			int id = property.Id;
			ref Entry source = ref UnsafeGetEntryRef(0);
			do
			{
				int num3 = num2 + num >>> 1;
				ref Entry reference = ref Unsafe.Add(ref source, (nuint)num3);
				int id2 = reference.Id;
				if (id2 == id)
				{
					value = reference.Value;
					return true;
				}
				if (id2 < id)
				{
					num = num3 + 1;
				}
				else
				{
					num2 = num3 - 1;
				}
			}
			while (num <= num2);
		}
		value = default(TValue);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int FindEntry(int propertyId)
	{
		int num = 0;
		int num2 = _entryCount - 1;
		if (num2 >= 0)
		{
			ref Entry source = ref UnsafeGetEntryRef(0);
			do
			{
				int num3 = num2 + num >>> 1;
				int id = Unsafe.Add(ref source, (nuint)num3).Id;
				if (id == propertyId)
				{
					return num3;
				}
				if (id < propertyId)
				{
					num = num3 + 1;
				}
				else
				{
					num2 = num3 - 1;
				}
			}
			while (num <= num2);
		}
		return ~num;
	}

	[MemberNotNull("_entries")]
	private void InsertEntry(Entry entry, int entryIndex)
	{
		if (_entryCount > 0)
		{
			if (_entryCount == _entries.Length)
			{
				Entry[] array = new Entry[(_entryCount == 4) ? 8 : ((int)((double)_entryCount * 1.5))];
				Array.Copy(_entries, 0, array, 0, entryIndex);
				array[entryIndex] = entry;
				Array.Copy(_entries, entryIndex, array, entryIndex + 1, _entryCount - entryIndex);
				_entries = array;
			}
			else
			{
				Array.Copy(_entries, entryIndex, _entries, entryIndex + 1, _entryCount - entryIndex);
				UnsafeGetEntryRef(entryIndex) = entry;
			}
		}
		else
		{
			if (_entries == null)
			{
				_entries = new Entry[4];
			}
			UnsafeGetEntryRef(0) = entry;
		}
		_entryCount++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref Entry UnsafeGetEntryRef(int index)
	{
		return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), (uint)index);
	}

	[DoesNotReturn]
	private static void ThrowOutOfRange()
	{
		throw new IndexOutOfRangeException();
	}

	[DoesNotReturn]
	private static void ThrowDuplicate()
	{
		throw new ArgumentException("An item with the same key has already been added.");
	}

	[DoesNotReturn]
	private static void ThrowNotFound()
	{
		throw new KeyNotFoundException();
	}
}
