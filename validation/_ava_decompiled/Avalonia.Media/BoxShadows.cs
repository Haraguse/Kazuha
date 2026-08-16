using System;
using System.ComponentModel;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// Represents a collection of <see cref="T:Avalonia.Media.BoxShadow" />s.
/// </summary>
public struct BoxShadows
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public struct BoxShadowsEnumerator(BoxShadows shadows)
	{
		private int _index = -1;

		private readonly BoxShadows _shadows = shadows;

		public BoxShadow Current => _shadows[_index];

		public bool MoveNext()
		{
			_index++;
			return _index < _shadows.Count;
		}
	}

	private const char Separator = ',';

	private const char OpeningParenthesis = '(';

	private const char ClosingParenthesis = ')';

	private readonly BoxShadow _first;

	private readonly BoxShadow[]? _list;

	/// <summary>
	/// Gets the number of <see cref="T:Avalonia.Media.BoxShadow" />s in the collection.
	/// </summary>
	public int Count { get; }

	/// <summary>
	/// Gets the <see cref="T:Avalonia.Media.BoxShadow" /> at the specified index.
	/// </summary>
	/// <param name="index">The index of the <see cref="T:Avalonia.Media.BoxShadow" /> to return.</param>
	/// <returns>The <see cref="T:Avalonia.Media.BoxShadow" /> at the specified index.</returns>
	/// <exception cref="T:System.IndexOutOfRangeException">
	/// Thrown when index less than 0 or index greater than or equal to <see cref="P:Avalonia.Media.BoxShadows.Count" />.
	/// </exception>
	public BoxShadow this[int index]
	{
		get
		{
			if (index < 0 || index >= Count)
			{
				throw new IndexOutOfRangeException();
			}
			if (index == 0)
			{
				return _first;
			}
			return _list[index - 1];
		}
	}

	/// <summary>
	/// Gets a value indicating whether any <see cref="T:Avalonia.Media.BoxShadow" /> in the collection has
	/// <see cref="P:Avalonia.Media.BoxShadow.IsInset" /> set to <c>true</c>.
	/// </summary>
	public bool HasInsetShadows
	{
		get
		{
			BoxShadowsEnumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				BoxShadow current = enumerator.Current;
				if (current != default(BoxShadow) && current.IsInset)
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.BoxShadows" /> struct.
	/// </summary>
	/// <param name="shadow">The first <see cref="T:Avalonia.Media.BoxShadow" /> to add to the collection.</param>
	public BoxShadows(BoxShadow shadow)
	{
		_first = shadow;
		_list = null;
		Count = ((!(_first == default(BoxShadow))) ? 1 : 0);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.BoxShadows" /> struct.
	/// </summary>
	/// <param name="first">The first <see cref="T:Avalonia.Media.BoxShadow" /> to add to the collection.</param>
	/// <param name="rest">All remaining <see cref="T:Avalonia.Media.BoxShadow" />s to add to the collection.</param>
	public BoxShadows(BoxShadow first, BoxShadow[] rest)
	{
		_first = first;
		_list = rest;
		Count = 1 + ((rest != null) ? rest.Length : 0);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		if (Count == 0)
		{
			return "none";
		}
		StringBuilder stringBuilder = StringBuilderCache.Acquire();
		BoxShadowsEnumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			enumerator.Current.ToString(stringBuilder);
			stringBuilder.Append(',');
			stringBuilder.Append(' ');
		}
		stringBuilder.Remove(stringBuilder.Length - 2, 2);
		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public BoxShadowsEnumerator GetEnumerator()
	{
		return new BoxShadowsEnumerator(this);
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.Media.BoxShadows" /> string representing one or more <see cref="T:Avalonia.Media.BoxShadow" />s.
	/// </summary>
	/// <param name="s">The input string to parse.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.BoxShadows" /> collection.</returns>
	public static BoxShadows Parse(string s)
	{
		string[] array = StringSplitter.SplitRespectingBrackets(s, ',', '(', ')', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0 || (array.Length == 1 && (string.IsNullOrWhiteSpace(array[0]) || array[0] == "none")))
		{
			return default(BoxShadows);
		}
		BoxShadow boxShadow = BoxShadow.Parse(array[0]);
		if (array.Length == 1)
		{
			return new BoxShadows(boxShadow);
		}
		BoxShadow[] array2 = new BoxShadow[array.Length - 1];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = BoxShadow.Parse(array[i + 1]);
		}
		return new BoxShadows(boxShadow, array2);
	}

	/// <summary>
	/// Transforms the specified bounding rectangle to account for all shadow's offset, spread, and blur.
	/// </summary>
	/// <param name="rect">The original bounding <see cref="T:Avalonia.Rect" /> to transform.</param>
	/// <returns>
	/// A new <see cref="T:Avalonia.Rect" /> that includes all shadow's offset, spread, and blur in the collection.
	/// </returns>
	public Rect TransformBounds(in Rect rect)
	{
		Rect result = rect;
		BoxShadowsEnumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = result.Union(enumerator.Current.TransformBounds(in rect));
		}
		return result;
	}

	/// <summary>
	/// Indicates whether the current object is equal to another object of the same type.
	/// </summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>
	/// <c>true</c> if the current object is equal to the other parameter; otherwise, <c>false</c>.
	/// </returns>
	public bool Equals(BoxShadows other)
	{
		if (other.Count != Count)
		{
			return false;
		}
		for (int i = 0; i < Count; i++)
		{
			if (!this[i].Equals(other[i]))
			{
				return false;
			}
		}
		return true;
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		if (obj is BoxShadows other)
		{
			return Equals(other);
		}
		return false;
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		int num = 0;
		BoxShadowsEnumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			num = (num * 397) ^ enumerator.Current.GetHashCode();
		}
		return num;
	}

	/// <summary>
	/// Determines whether two <see cref="T:Avalonia.Media.BoxShadows" /> collections are equal.
	/// </summary>
	/// <param name="left">The first <see cref="T:Avalonia.Media.BoxShadows" /> collection to compare.</param>
	/// <param name="right">The second <see cref="T:Avalonia.Media.BoxShadows" /> collection to compare.</param>
	/// <returns>
	/// <c>true</c> if the two <see cref="T:Avalonia.Media.BoxShadows" /> collections are equal; otherwise, <c>false</c>.
	/// </returns>
	public static bool operator ==(BoxShadows left, BoxShadows right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Determines whether two <see cref="T:Avalonia.Media.BoxShadows" /> collections are not equal.
	/// </summary>
	/// <param name="left">The first <see cref="T:Avalonia.Media.BoxShadows" /> collection to compare.</param>
	/// <param name="right">The second <see cref="T:Avalonia.Media.BoxShadows" /> collection to compare.</param>
	/// <returns>
	/// <c>true</c> if the two <see cref="T:Avalonia.Media.BoxShadows" /> collections are not equal; otherwise, <c>false</c>.
	/// </returns>
	public static bool operator !=(BoxShadows left, BoxShadows right)
	{
		return !(left == right);
	}
}
