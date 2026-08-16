using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Avalonia.Collections;

/// <summary>
/// A notifying dictionary.
/// </summary>
/// <typeparam name="TKey">The type of the dictionary key.</typeparam>
/// <typeparam name="TValue">The type of the dictionary value.</typeparam>
public class AvaloniaDictionary<TKey, TValue> : IAvaloniaDictionary<TKey, TValue>, IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IAvaloniaReadOnlyDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, INotifyCollectionChanged, INotifyPropertyChanged, IDictionary, ICollection where TKey : notnull
{
	private Dictionary<TKey, TValue> _inner;

	/// <inheritdoc cref="P:System.Collections.Generic.ICollection`1.Count" />
	public int Count => _inner.Count;

	/// <inheritdoc cref="P:System.Collections.Generic.ICollection`1.IsReadOnly" />
	public bool IsReadOnly => false;

	/// <inheritdoc />
	public ICollection<TKey> Keys => _inner.Keys;

	/// <inheritdoc />
	public ICollection<TValue> Values => _inner.Values;

	bool IDictionary.IsFixedSize => ((IDictionary)_inner).IsFixedSize;

	ICollection IDictionary.Keys => ((IDictionary)_inner).Keys;

	ICollection IDictionary.Values => ((IDictionary)_inner).Values;

	bool ICollection.IsSynchronized => ((ICollection)_inner).IsSynchronized;

	object ICollection.SyncRoot => ((ICollection)_inner).SyncRoot;

	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _inner.Keys;

	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _inner.Values;

	/// <summary>
	/// Gets or sets the named resource.
	/// </summary>
	/// <param name="key">The resource key.</param>
	/// <returns>The resource, or null if not found.</returns>
	public TValue this[TKey key]
	{
		get
		{
			return _inner[key];
		}
		set
		{
			bool num = _inner.TryGetValue(key, out var value2);
			_inner[key] = value;
			if (num)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"{"Item"}[{key}]"));
				if (CollectionChanged != null)
				{
					NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new KeyValuePair<TKey, TValue>(key, value), new KeyValuePair<TKey, TValue>(key, value2));
					CollectionChanged(this, e);
				}
			}
			else
			{
				NotifyAdd(key, value);
			}
		}
	}

	object? IDictionary.this[object key]
	{
		get
		{
			return ((IDictionary)_inner)[key];
		}
		set
		{
			((IDictionary)_inner)[key] = value;
		}
	}

	/// <summary>
	/// Occurs when the collection changes.
	/// </summary>
	public event NotifyCollectionChangedEventHandler? CollectionChanged;

	/// <summary>
	/// Raised when a property on the collection changes.
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Collections.AvaloniaDictionary`2" /> class.
	/// </summary>
	public AvaloniaDictionary()
	{
		_inner = new Dictionary<TKey, TValue>();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Collections.AvaloniaDictionary`2" /> class.
	/// </summary>
	public AvaloniaDictionary(int capacity)
	{
		_inner = new Dictionary<TKey, TValue>(capacity);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Collections.AvaloniaDictionary`2" /> class using an IDictionary.
	/// </summary>
	public AvaloniaDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer = null)
	{
		if (dictionary != null)
		{
			_inner = new Dictionary<TKey, TValue>(dictionary, comparer ?? EqualityComparer<TKey>.Default);
			return;
		}
		throw new ArgumentNullException("dictionary");
	}

	/// <inheritdoc />
	public void Add(TKey key, TValue value)
	{
		_inner.Add(key, value);
		NotifyAdd(key, value);
	}

	/// <inheritdoc cref="M:System.Collections.Generic.ICollection`1.Clear" />
	public void Clear()
	{
		Dictionary<TKey, TValue> inner = _inner;
		_inner = new Dictionary<TKey, TValue>();
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
		if (CollectionChanged != null)
		{
			NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, inner.ToArray(), -1);
			CollectionChanged(this, e);
		}
	}

	/// <inheritdoc cref="M:System.Collections.Generic.IDictionary`2.ContainsKey(`0)" />
	public bool ContainsKey(TKey key)
	{
		return _inner.ContainsKey(key);
	}

	/// <inheritdoc />
	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		((ICollection<KeyValuePair<TKey, TValue>>)_inner).CopyTo(array, arrayIndex);
	}

	/// <inheritdoc />
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return _inner.GetEnumerator();
	}

	/// <inheritdoc />
	public bool Remove(TKey key)
	{
		if (_inner.Remove(key, out var value))
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"{"Item"}[{key}]"));
			if (CollectionChanged != null)
			{
				NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>[1]
				{
					new KeyValuePair<TKey, TValue>(key, value)
				}, -1);
				CollectionChanged(this, e);
			}
			return true;
		}
		return false;
	}

	/// <inheritdoc cref="M:System.Collections.Generic.IDictionary`2.TryGetValue(`0,`1@)" />
	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		return _inner.TryGetValue(key, out value);
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return _inner.GetEnumerator();
	}

	/// <inheritdoc />
	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)_inner).CopyTo(array, index);
	}

	/// <inheritdoc />
	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
	{
		Add(item.Key, item.Value);
	}

	/// <inheritdoc />
	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
	{
		return _inner.Contains(item);
	}

	/// <inheritdoc />
	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
	{
		return Remove(item.Key);
	}

	/// <inheritdoc />
	void IDictionary.Add(object key, object? value)
	{
		Add((TKey)key, (TValue)value);
	}

	/// <inheritdoc />
	bool IDictionary.Contains(object key)
	{
		return ((IDictionary)_inner).Contains(key);
	}

	/// <inheritdoc />
	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return ((IDictionary)_inner).GetEnumerator();
	}

	/// <inheritdoc />
	void IDictionary.Remove(object key)
	{
		Remove((TKey)key);
	}

	private void NotifyAdd(TKey key, TValue value)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"{"Item"}[{key}]"));
		if (CollectionChanged != null)
		{
			NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>[1]
			{
				new KeyValuePair<TKey, TValue>(key, value)
			}, -1);
			CollectionChanged(this, e);
		}
	}
}
