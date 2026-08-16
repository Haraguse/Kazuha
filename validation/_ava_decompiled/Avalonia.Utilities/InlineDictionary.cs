using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Utilities;

internal struct InlineDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable where TKey : class
{
	private struct KeyValuePair(TKey key, TValue? value)
	{
		public TKey? Key = key;

		public TValue? Value = value;

		public static implicit operator KeyValuePair<TKey?, TValue?>(KeyValuePair kvp)
		{
			return new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value);
		}
	}

	public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator, IDisposable
	{
		private enum Type
		{
			Empty,
			Single,
			Array,
			Dictionary
		}

		private Dictionary<TKey, TValue>.Enumerator _inner = default(Dictionary<TKey, TValue>.Enumerator);

		private readonly KeyValuePair[]? _arr = null;

		private KeyValuePair<TKey, TValue> _first = default(KeyValuePair<TKey, TValue>);

		private int _index = -1;

		private readonly Type _type;

		public KeyValuePair<TKey, TValue> Current
		{
			get
			{
				if (_type == Type.Single)
				{
					return _first;
				}
				if (_type == Type.Array)
				{
					return _arr[_index];
				}
				if (_type == Type.Dictionary)
				{
					return _inner.Current;
				}
				throw new InvalidOperationException();
			}
		}

		object IEnumerator.Current => Current;

		public Enumerator(InlineDictionary<TKey, TValue> parent)
		{
			if (parent._data is Dictionary<TKey, TValue> dictionary)
			{
				_inner = dictionary.GetEnumerator();
				_type = Type.Dictionary;
			}
			else if (parent._data is KeyValuePair[] arr)
			{
				_type = Type.Array;
				_arr = arr;
			}
			else if (parent._data != null)
			{
				_type = Type.Single;
				_first = new KeyValuePair<TKey, TValue>((TKey)parent._data, parent._value);
			}
			else
			{
				_type = Type.Empty;
			}
		}

		public bool MoveNext()
		{
			if (_type == Type.Single)
			{
				if (_index != -1)
				{
					return false;
				}
				_index = 0;
				return true;
			}
			if (_type == Type.Array)
			{
				for (int i = _index + 1; i < _arr.Length; i++)
				{
					if (_arr[i].Key != null)
					{
						_index = i;
						return true;
					}
				}
				return false;
			}
			if (_type == Type.Dictionary)
			{
				return _inner.MoveNext();
			}
			return false;
		}

		public void Reset()
		{
			_index = -1;
			if (_type == Type.Dictionary)
			{
				((IEnumerator)_inner).Reset();
			}
		}

		public void Dispose()
		{
		}
	}

	private object? _data;

	private TValue? _value;

	public TValue this[TKey key]
	{
		get
		{
			if (TryGetValue(key, out var value))
			{
				return value;
			}
			throw new KeyNotFoundException();
		}
		set
		{
			Set(key, value);
		}
	}

	public bool HasEntries => _data != null;

	private void SetCore(TKey key, TValue value, bool overwrite)
	{
		if (key == null)
		{
			throw new ArgumentNullException();
		}
		if (_data == null)
		{
			_data = key;
			_value = value;
		}
		else if (_data is KeyValuePair[] array)
		{
			int num = -1;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Key == key)
				{
					if (overwrite)
					{
						array[i] = new KeyValuePair(key, value);
						return;
					}
					throw new ArgumentException("Key already exists in dictionary");
				}
				if (array[i].Key == null && num == -1)
				{
					num = i;
				}
			}
			if (num != -1)
			{
				array[num] = new KeyValuePair(key, value);
				return;
			}
			Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
			KeyValuePair[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				KeyValuePair keyValuePair = array2[j];
				dictionary.Add(keyValuePair.Key, keyValuePair.Value);
			}
			dictionary.Add(key, value);
			_data = dictionary;
		}
		else if (_data is Dictionary<TKey, TValue> dictionary2)
		{
			if (overwrite)
			{
				dictionary2[key] = value;
			}
			else
			{
				dictionary2.Add(key, value);
			}
		}
		else
		{
			TKey val = (TKey)_data;
			if ((val == key) & overwrite)
			{
				_value = value;
				return;
			}
			_data = new KeyValuePair[6]
			{
				new KeyValuePair(val, _value),
				new KeyValuePair(key, value),
				default(KeyValuePair),
				default(KeyValuePair),
				default(KeyValuePair),
				default(KeyValuePair)
			};
			_value = default(TValue);
		}
	}

	public void Add(TKey key, TValue value)
	{
		SetCore(key, value, overwrite: false);
	}

	public void Set(TKey key, TValue value)
	{
		SetCore(key, value, overwrite: true);
	}

	public bool Remove(TKey key)
	{
		if (_data == key)
		{
			_data = null;
			_value = default(TValue);
			return true;
		}
		if (_data is KeyValuePair[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Key == key)
				{
					array[i] = default(KeyValuePair);
					return true;
				}
			}
			return false;
		}
		if (_data is Dictionary<TKey, TValue> dictionary)
		{
			return dictionary.Remove(key);
		}
		return false;
	}

	public void Clear()
	{
		if (_data != null)
		{
			if (_data is KeyValuePair[] array)
			{
				Array.Clear(array, 0, array.Length);
			}
			else if (_data is Dictionary<TKey, TValue> dictionary)
			{
				dictionary.Clear();
			}
			else
			{
				_data = null;
			}
		}
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (_data == key)
		{
			value = _value;
			return true;
		}
		if (_data is KeyValuePair[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Key == key)
				{
					value = array[i].Value;
					return true;
				}
			}
			value = default(TValue);
			return false;
		}
		if (_data is Dictionary<TKey, TValue> dictionary)
		{
			return dictionary.TryGetValue(key, out value);
		}
		value = default(TValue);
		return false;
	}

	[UnscopedRef]
	public ref TValue GetValueRefOrNullRef(TKey key)
	{
		if (_data == key)
		{
			return ref _value;
		}
		if (_data is KeyValuePair[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Key == key)
				{
					return ref array[i].Value;
				}
			}
			return ref Unsafe.NullRef<TValue>();
		}
		if (_data is Dictionary<TKey, TValue> dictionary)
		{
			return ref CollectionsMarshal.GetValueRefOrNullRef(dictionary, key);
		}
		return ref Unsafe.NullRef<TValue>();
	}

	[UnscopedRef]
	public ref TValue GetValueRefOrAddDefault(TKey key, [UnscopedRef] out bool exists)
	{
		if (_data == null)
		{
			exists = false;
			_data = key;
			return ref _value;
		}
		if (_data == key)
		{
			exists = true;
			return ref _value;
		}
		if (_data is KeyValuePair[] array)
		{
			int num = -1;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Key == key)
				{
					exists = true;
					return ref array[i].Value;
				}
				if (num == -1 && array[i].Key == null)
				{
					num = i;
				}
			}
			if (num != -1)
			{
				array[num] = new KeyValuePair(key, default(TValue));
				exists = false;
				return ref array[num].Value;
			}
			Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
			KeyValuePair[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				KeyValuePair keyValuePair = array2[j];
				dictionary.Add(keyValuePair.Key, keyValuePair.Value);
			}
			_data = dictionary;
			return ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out exists);
		}
		if (_data is Dictionary<TKey, TValue> dictionary2)
		{
			return ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary2, key, out exists);
		}
		KeyValuePair[] array3 = (KeyValuePair[])(_data = new KeyValuePair[6]
		{
			new KeyValuePair((TKey)_data, _value),
			new KeyValuePair(key, default(TValue)),
			default(KeyValuePair),
			default(KeyValuePair),
			default(KeyValuePair),
			default(KeyValuePair)
		});
		_value = default(TValue);
		exists = false;
		return ref array3[1].Value;
	}

	public bool TryGetAndRemoveValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (_data == key)
		{
			value = _value;
			_value = default(TValue);
			_data = null;
			return true;
		}
		if (_data is KeyValuePair[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Key == key)
				{
					value = array[i].Value;
					array[i] = default(KeyValuePair);
					return true;
				}
			}
			value = default(TValue);
			return false;
		}
		if (_data is Dictionary<TKey, TValue> dictionary && !dictionary.Remove(key, out value))
		{
			return false;
		}
		value = default(TValue);
		return false;
	}

	public TValue GetAndRemove(TKey key)
	{
		if (TryGetAndRemoveValue(key, out var value))
		{
			return value;
		}
		throw new KeyNotFoundException();
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
