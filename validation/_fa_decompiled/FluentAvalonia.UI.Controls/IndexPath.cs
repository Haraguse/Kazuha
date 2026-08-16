using System;
using System.Collections.Generic;

namespace FluentAvalonia.UI.Controls;

internal struct IndexPath : IComparable<IndexPath>, IEquatable<IndexPath>
{
	public static readonly IndexPath Unselected;

	private IList<int> _path;

	public IndexPath(int index)
	{
		_path = new List<int> { index };
	}

	public IndexPath(int groupIndex, int itemIndex)
	{
		_path = new List<int> { groupIndex, itemIndex };
	}

	public IndexPath(IEnumerable<int> indices)
	{
		_path = ((indices != null) ? new List<int>(indices) : new List<int>());
	}

	public static IndexPath CreateFrom(int index)
	{
		return new IndexPath(index);
	}

	public static IndexPath CreateFrom(int groupIndex, int itemIndex)
	{
		return new IndexPath(groupIndex, itemIndex);
	}

	public static IndexPath CreateFromIndices(IList<int> indices)
	{
		return new IndexPath(indices);
	}

	public int GetSize()
	{
		return _path?.Count ?? 0;
	}

	public int GetAt(int index)
	{
		return (_path ?? throw new IndexOutOfRangeException())[index];
	}

	public int CompareTo(IndexPath rhs)
	{
		int num = 0;
		int size = GetSize();
		int size2 = rhs.GetSize();
		if (size == 0 || size2 == 0)
		{
			num = size - size2;
		}
		else
		{
			for (int i = 0; i < Math.Min(size, size2); i++)
			{
				if (_path[i] < rhs._path[i])
				{
					num = -1;
					break;
				}
				if (_path[i] > rhs._path[i])
				{
					num = 1;
					break;
				}
			}
			num = ((num == 0) ? (size - size2) : num);
		}
		if (num != 0)
		{
			num = ((num > 0) ? 1 : (-1));
		}
		return num;
	}

	public override string ToString()
	{
		string text = "R";
		foreach (int item in _path)
		{
			text += $".{item}";
		}
		return text;
	}

	public bool IsValid()
	{
		for (int i = 0; i < _path.Count; i++)
		{
			if (_path[i] < 0)
			{
				return false;
			}
		}
		return true;
	}

	public IndexPath CloneWithChildIndex(int childIndex)
	{
		return new IndexPath(new List<int>(_path) { childIndex });
	}

	public override int GetHashCode()
	{
		int num = -504981047;
		foreach (int item in _path)
		{
			num = num * -1521134295 + item.GetHashCode();
		}
		return num;
	}

	public override bool Equals(object obj)
	{
		if (obj is IndexPath other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(IndexPath other)
	{
		return CompareTo(other) == 0;
	}

	public static bool operator <(IndexPath x, IndexPath y)
	{
		return x.CompareTo(y) < 0;
	}

	public static bool operator >(IndexPath x, IndexPath y)
	{
		return x.CompareTo(y) > 0;
	}

	public static bool operator <=(IndexPath x, IndexPath y)
	{
		return x.CompareTo(y) <= 0;
	}

	public static bool operator >=(IndexPath x, IndexPath y)
	{
		return x.CompareTo(y) >= 0;
	}

	public static bool operator ==(IndexPath x, IndexPath y)
	{
		return x.CompareTo(y) == 0;
	}

	public static bool operator !=(IndexPath x, IndexPath y)
	{
		return x.CompareTo(y) != 0;
	}

	public static bool operator ==(IndexPath? x, IndexPath? y)
	{
		return x.GetValueOrDefault().CompareTo(y.GetValueOrDefault()) == 0;
	}

	public static bool operator !=(IndexPath? x, IndexPath? y)
	{
		return x.GetValueOrDefault().CompareTo(y.GetValueOrDefault()) != 0;
	}
}
