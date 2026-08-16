using System.Collections.Generic;

namespace Avalonia.Utilities;

/// <summary>
/// Extension methods for binary searching an IReadOnlyList collection
/// </summary>
internal static class BinarySearchExtension
{
	private static int GetMedian(int low, int hi)
	{
		return low + (hi - low >> 1);
	}

	/// <summary>
	/// Performs a binary search on the entire contents of an IReadOnlyList
	/// </summary>
	/// <typeparam name="T">The list element type</typeparam>
	/// <param name="list">The list to be searched</param>
	/// <param name="value">The value to search for</param>
	/// <param name="comparer">The comparer</param>
	/// <returns>The index of the found item; otherwise the bitwise complement of the index of the next larger item</returns>
	public static int BinarySearch<T>(this IReadOnlyList<T> list, T value, IComparer<T> comparer)
	{
		return list.BinarySearch(0, list.Count, value, comparer);
	}

	/// <summary>
	/// Performs a binary search on a subset of an IReadOnlyList
	/// </summary>
	/// <typeparam name="T">The list element type</typeparam>
	/// <param name="list">The list to be searched</param>
	/// <param name="index">The start of the range to be searched</param>
	/// <param name="length">The length of the range to be searched</param>
	/// <param name="value">The value to search for</param>
	/// <param name="comparer">A comparer</param>
	/// <returns>The index of the found item; otherwise the bitwise complement of the index of the next larger item</returns>
	public static int BinarySearch<T>(this IReadOnlyList<T> list, int index, int length, T value, IComparer<T> comparer)
	{
		int num = index;
		int num2 = index + length - 1;
		while (num <= num2)
		{
			int median = GetMedian(num, num2);
			int num3 = comparer.Compare(list[median], value);
			if (num3 == 0)
			{
				return median;
			}
			if (num3 < 0)
			{
				num = median + 1;
			}
			else
			{
				num2 = median - 1;
			}
		}
		return ~num;
	}
}
