using System;

namespace Avalonia.Media.Fonts;

/// <summary>
/// Represents a unique key for identifying a font inside a font collection based on style, weight, and stretch attributes.
/// </summary>
/// <remarks>Use this key to efficiently look up or group fonts in a collection by their style, weight,
/// and stretch characteristics. Keys are ordered lexicographically by <see cref="P:Avalonia.Media.Fonts.FontCollectionKey.Style" />, then
/// <see cref="P:Avalonia.Media.Fonts.FontCollectionKey.Weight" />, then <see cref="P:Avalonia.Media.Fonts.FontCollectionKey.Stretch" />.</remarks>
/// <param name="Style">The font style to use when constructing the key.</param>
/// <param name="Weight">The font weight to use when constructing the key.</param>
/// <param name="Stretch">The font stretch to use when constructing the key.</param>
public readonly record struct FontCollectionKey(FontStyle Style, FontWeight Weight, FontStretch Stretch) : IComparable<FontCollectionKey>, IComparable
{
	/// <inheritdoc />
	public int CompareTo(FontCollectionKey other)
	{
		int num = ((int)Style).CompareTo((int)other.Style);
		if (num != 0)
		{
			return num;
		}
		num = ((int)Weight).CompareTo((int)other.Weight);
		if (num != 0)
		{
			return num;
		}
		return ((int)Stretch).CompareTo((int)other.Stretch);
	}

	/// <inheritdoc />
	public int CompareTo(object? obj)
	{
		if (obj == null)
		{
			return 1;
		}
		if (obj is FontCollectionKey other)
		{
			return CompareTo(other);
		}
		throw new ArgumentException("Object must be of type FontCollectionKey.", "obj");
	}

	public static bool operator <(FontCollectionKey left, FontCollectionKey right)
	{
		return left.CompareTo(right) < 0;
	}

	public static bool operator <=(FontCollectionKey left, FontCollectionKey right)
	{
		return left.CompareTo(right) <= 0;
	}

	public static bool operator >(FontCollectionKey left, FontCollectionKey right)
	{
		return left.CompareTo(right) > 0;
	}

	public static bool operator >=(FontCollectionKey left, FontCollectionKey right)
	{
		return left.CompareTo(right) >= 0;
	}
}
