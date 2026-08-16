using System;
using System.Diagnostics;

namespace Avalonia.Media;

/// <summary>
///     Represents information about a character hit within a glyph run.
/// </summary>
/// <remarks>
///     The CharacterHit structure provides information about the index of the first
///     character that got hit as well as information about leading or trailing edge.
/// </remarks>
[DebuggerDisplay("CharacterHit({FirstCharacterIndex}, {TrailingLength})")]
public readonly struct CharacterHit : IEquatable<CharacterHit>
{
	/// <summary>
	///     Gets the index of the first character that got hit.
	/// </summary>
	public int FirstCharacterIndex { get; }

	/// <summary>
	///     Gets the trailing length value for the character that got hit.
	/// </summary>
	/// <remarks>
	/// In the case of a leading edge, this value is 0. In the case of a trailing edge,
	/// this value is the number of code points until the next valid caret position.
	/// </remarks>
	public int TrailingLength { get; }

	/// <summary>
	///     Initializes a new instance of the <see cref="T:Avalonia.Media.CharacterHit" /> structure.
	/// </summary>
	/// <param name="firstCharacterIndex">Index of the first character that got hit.</param>
	/// <param name="trailingLength">In the case of a leading edge, this value is 0. In the case of a trailing edge,
	/// this value is the number of code points until the next valid caret position.</param>
	[DebuggerStepThrough]
	public CharacterHit(int firstCharacterIndex, int trailingLength = 0)
	{
		FirstCharacterIndex = firstCharacterIndex;
		TrailingLength = trailingLength;
	}

	public bool Equals(CharacterHit other)
	{
		if (FirstCharacterIndex == other.FirstCharacterIndex)
		{
			return TrailingLength == other.TrailingLength;
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj is CharacterHit other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (FirstCharacterIndex * 397) ^ TrailingLength;
	}

	public static bool operator ==(CharacterHit left, CharacterHit right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CharacterHit left, CharacterHit right)
	{
		return !left.Equals(right);
	}
}
