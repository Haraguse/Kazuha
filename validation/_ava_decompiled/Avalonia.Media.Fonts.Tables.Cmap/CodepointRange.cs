using System;

namespace Avalonia.Media.Fonts.Tables.Cmap;

/// <summary>
/// Represents a range of Unicode code points, defined by inclusive start and end values.
/// </summary>
public readonly struct CodepointRange(int start, int end)
{
	/// <summary>
	/// Gets the start of the range.
	/// </summary>
	public readonly int Start = start;

	/// <summary>
	/// Gets the end of the range.
	/// </summary>
	public readonly int End = end;

	public override bool Equals(object? obj)
	{
		if (obj is CodepointRange codepointRange && Start == codepointRange.Start)
		{
			return End == codepointRange.End;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Start, End);
	}

	public static bool operator ==(CodepointRange left, CodepointRange right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CodepointRange left, CodepointRange right)
	{
		return !(left == right);
	}
}
