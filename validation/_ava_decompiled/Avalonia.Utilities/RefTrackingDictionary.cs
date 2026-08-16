using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Utilities;

/// <summary>
/// Maintains a set of objects with reference counts
/// </summary>
internal class RefTrackingDictionary<TKey> : Dictionary<TKey, int> where TKey : class
{
	/// <summary>
	/// Increase reference count for a key by 1.
	/// </summary>
	/// <returns>true if key was added to the dictionary, false otherwise</returns>
	public bool AddRef(TKey key)
	{
		ref int valueRefOrAddDefault = ref CollectionsMarshal.GetValueRefOrAddDefault(this, key, out var _);
		valueRefOrAddDefault++;
		return valueRefOrAddDefault == 1;
	}

	/// <summary>
	/// Decrease reference count for a key by 1.
	/// </summary>
	/// <returns>true if key was removed to the dictionary, false otherwise</returns>
	public bool ReleaseRef(TKey key)
	{
		ref int valueRefOrNullRef = ref CollectionsMarshal.GetValueRefOrNullRef(this, key);
		if (Unsafe.IsNullRef(in valueRefOrNullRef))
		{
			return false;
		}
		valueRefOrNullRef--;
		if (valueRefOrNullRef == 0)
		{
			Remove(key);
			return true;
		}
		return false;
	}
}
