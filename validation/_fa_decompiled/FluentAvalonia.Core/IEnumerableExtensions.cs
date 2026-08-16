using System.Collections;
using System.Linq;

namespace FluentAvalonia.Core;

/// <summary>
/// <see cref="T:System.Collections.IEnumerable" /> extensions methods
/// </summary>
internal static class IEnumerableExtensions
{
	/// <summary>
	/// Gets the item count of the IEnumerable
	/// </summary>
	public static int Count(this IEnumerable items)
	{
		if (items == null)
		{
			return 0;
		}
		if (items is ICollection collection)
		{
			return collection.Count;
		}
		return Enumerable.Count(items.Cast<object>());
	}

	/// <summary>
	/// Gets the index of an item from an IEnumerable
	/// </summary>
	public static int IndexOf(this IEnumerable items, object item)
	{
		if (items is IList list)
		{
			return list.IndexOf(item);
		}
		int num = 0;
		foreach (object item2 in items)
		{
			if (item2 == item)
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	/// <summary>
	/// Retreives the element at the specified index from the IEnumerable
	/// </summary>
	/// <param name="items"></param>
	/// <param name="reqIndex"></param>
	/// <returns></returns>
	public static object ElementAt(this IEnumerable items, int reqIndex)
	{
		if (items.Count() == 0)
		{
			return null;
		}
		if (items is IList list)
		{
			return list[reqIndex];
		}
		return Enumerable.ElementAt(items.Cast<object>(), reqIndex);
	}

	/// <summary>
	/// Checks of the IEnumerable contains the given item
	/// </summary>
	public static bool Contains(this IEnumerable items, object item)
	{
		if (items is IList list)
		{
			return list.Contains(item);
		}
		foreach (object item2 in items)
		{
			if (item2 == item)
			{
				return true;
			}
		}
		return false;
	}
}
