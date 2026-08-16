using System;
using System.Collections.Generic;

namespace FluentAvalonia.UI.Controls;

internal readonly struct IndexRange : IEquatable<IndexRange>
{
	private static readonly IndexRange s_invalid = new IndexRange(int.MinValue, int.MinValue);

	public int Begin { get; }

	public int End { get; }

	public int Count => End - Begin + 1;

	public IndexRange(int begin, int end)
	{
		if (begin > end)
		{
			int num = begin;
			begin = end;
			end = num;
		}
		Begin = begin;
		End = end;
	}

	public bool Contains(int index)
	{
		if (index >= Begin)
		{
			return index <= End;
		}
		return false;
	}

	public bool Split(int splitIndex, out IndexRange before, out IndexRange after)
	{
		before = new IndexRange(Begin, splitIndex);
		if (splitIndex < End)
		{
			after = new IndexRange(splitIndex + 1, End);
			return true;
		}
		after = default(IndexRange);
		return false;
	}

	public bool Intersects(IndexRange other)
	{
		if (Begin <= other.End)
		{
			return End >= other.Begin;
		}
		return false;
	}

	public bool Adjacent(IndexRange other)
	{
		if (Begin != other.End + 1)
		{
			return End == other.Begin - 1;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (obj is IndexRange other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(IndexRange other)
	{
		if (Begin == other.Begin)
		{
			return End == other.End;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (unchecked(1903003160 * -1521134295) + Begin.GetHashCode()) * -1521134295 + End.GetHashCode();
	}

	public override string ToString()
	{
		return $"[{Begin}..{End}]";
	}

	public static bool operator ==(IndexRange left, IndexRange right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(IndexRange left, IndexRange right)
	{
		return !(left == right);
	}

	public static int Add(IList<IndexRange> ranges, IndexRange range, IList<IndexRange> added = null)
	{
		int num = 0;
		for (int i = 0; i < ranges.Count; i++)
		{
			if (!(range != s_invalid))
			{
				break;
			}
			IndexRange other = ranges[i];
			if (range.Intersects(other) || range.Adjacent(other))
			{
				if (range.Begin < other.Begin)
				{
					IndexRange item = new IndexRange(range.Begin, other.Begin - 1);
					ranges[i] = new IndexRange(range.Begin, other.End);
					added?.Add(item);
					num += item.Count;
				}
				range = ((range.End <= other.End) ? s_invalid : new IndexRange(other.End + 1, range.End));
			}
			else if (range.End < other.Begin)
			{
				ranges.Insert(i, range);
				added?.Add(range);
				num += range.Count;
				range = s_invalid;
			}
		}
		if (range != s_invalid)
		{
			ranges.Add(range);
			added?.Add(range);
			num += range.Count;
		}
		MergeRanges(ranges);
		return num;
	}

	public static int Intersect(IList<IndexRange> ranges, IndexRange range, IList<IndexRange> removed = null)
	{
		int num = 0;
		for (int i = 0; i < ranges.Count; i++)
		{
			if (!(range != s_invalid))
			{
				break;
			}
			IndexRange item = ranges[i];
			if (item.End < range.Begin || item.Begin > range.End)
			{
				removed?.Add(item);
				ranges.RemoveAt(i--);
				num += item.Count;
				continue;
			}
			if (item.Begin < range.Begin)
			{
				IndexRange item2 = new IndexRange(item.Begin, range.Begin - 1);
				removed?.Add(item2);
				item = (ranges[i] = new IndexRange(range.Begin, item.End));
				num += item2.Count;
			}
			if (item.End > range.End)
			{
				IndexRange item3 = new IndexRange(range.End + 1, item.End);
				removed?.Add(item3);
				ranges[i] = new IndexRange(item.Begin, range.End);
				num += item3.Count;
			}
		}
		MergeRanges(ranges);
		if (removed != null)
		{
			MergeRanges(removed);
		}
		return num;
	}

	public static int Remove(IList<IndexRange> ranges, IndexRange range, IList<IndexRange> removed = null)
	{
		int num = 0;
		for (int i = 0; i < ranges.Count; i++)
		{
			IndexRange indexRange = ranges[i];
			if (range.Intersects(indexRange))
			{
				if (range.Begin <= indexRange.Begin && range.End >= indexRange.End)
				{
					ranges.RemoveAt(i--);
					removed?.Add(indexRange);
					num += indexRange.Count;
				}
				else if (range.Begin > indexRange.Begin && range.End >= indexRange.End)
				{
					ranges[i] = new IndexRange(indexRange.Begin, range.Begin - 1);
					removed?.Add(new IndexRange(range.Begin, indexRange.End));
					num += indexRange.End - (range.Begin - 1);
				}
				else if (range.Begin > indexRange.Begin && range.End < indexRange.End)
				{
					ranges[i] = new IndexRange(indexRange.Begin, range.Begin - 1);
					ranges.Insert(++i, new IndexRange(range.End + 1, indexRange.End));
					removed?.Add(range);
					num += range.Count;
				}
				else if (range.End <= indexRange.End)
				{
					IndexRange item = new IndexRange(indexRange.Begin, range.End);
					ranges[i] = new IndexRange(range.End + 1, indexRange.End);
					removed?.Add(item);
					num += item.Count;
				}
			}
		}
		return num;
	}

	public static IEnumerable<IndexRange> Subtract(IndexRange lhs, IEnumerable<IndexRange> rhs)
	{
		List<IndexRange> list = new List<IndexRange> { lhs };
		foreach (IndexRange rh in rhs)
		{
			Remove(list, rh);
		}
		return list;
	}

	public static IEnumerable<int> EnumerateIndices(IEnumerable<IndexRange> ranges)
	{
		foreach (IndexRange range in ranges)
		{
			int i = range.Begin;
			while (i <= range.End)
			{
				yield return i;
				int num = i + 1;
				i = num;
			}
		}
	}

	public static int GetCount(IEnumerable<IndexRange> ranges)
	{
		int num = 0;
		foreach (IndexRange range in ranges)
		{
			num += range.End - range.Begin + 1;
		}
		return num;
	}

	private static void MergeRanges(IList<IndexRange> ranges)
	{
		for (int num = ranges.Count - 2; num >= 0; num--)
		{
			IndexRange indexRange = ranges[num];
			IndexRange other = ranges[num + 1];
			if (indexRange.Intersects(other) || indexRange.End == other.Begin - 1)
			{
				ranges[num] = new IndexRange(indexRange.Begin, other.End);
				ranges.RemoveAt(num + 1);
			}
		}
	}
}
