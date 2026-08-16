using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Utilities;

internal struct RefCountingSmallDictionary<TKey> : IEnumerable<KeyValuePair<TKey, int>>, IEnumerable where TKey : class
{
	private InlineDictionary<TKey, int> _counts;

	public bool Add(TKey key)
	{
		_counts.GetValueRefOrAddDefault(key, out var exists)++;
		return !exists;
	}

	public bool Remove(TKey key)
	{
		ref int valueRefOrNullRef = ref _counts.GetValueRefOrNullRef(key);
		valueRefOrNullRef--;
		if (valueRefOrNullRef == 0)
		{
			_counts.Remove(key);
			return true;
		}
		return false;
	}

	public InlineDictionary<TKey, int>.Enumerator GetEnumerator()
	{
		return _counts.GetEnumerator();
	}

	IEnumerator<KeyValuePair<TKey, int>> IEnumerable<KeyValuePair<TKey, int>>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
