using System;
using System.Collections.Generic;

namespace Avalonia.Media;

/// <summary>
/// The <see cref="T:Avalonia.Media.UnicodeRange" /> descripes a set of Unicode characters.
/// </summary>
public readonly record struct UnicodeRange
{
	internal UnicodeRangeSegment Single => _single;

	internal IReadOnlyList<UnicodeRangeSegment>? Segments => _segments;

	public static readonly UnicodeRange Default = Parse("0-10FFFD");

	private readonly UnicodeRangeSegment _single;

	private readonly IReadOnlyList<UnicodeRangeSegment>? _segments;

	public UnicodeRange(int start, int end)
	{
		_segments = null;
		_single = new UnicodeRangeSegment(start, end);
	}

	public UnicodeRange(UnicodeRangeSegment single)
	{
		_segments = null;
		_single = single;
	}

	public UnicodeRange(IReadOnlyList<UnicodeRangeSegment> segments)
	{
		_segments = null;
		if (segments == null || segments.Count == 0)
		{
			throw new ArgumentException("segments");
		}
		_single = segments[0];
		_segments = segments;
	}

	/// <summary>
	/// Determines if given value is inside the range.
	/// </summary>
	/// <param name="value">The value to verify.</param>
	/// <returns>
	/// <c>true</c> If given value is inside the range, <c>false</c> otherwise.
	/// </returns>
	public bool IsInRange(int value)
	{
		if (_segments == null)
		{
			return _single.IsInRange(value);
		}
		foreach (UnicodeRangeSegment segment in _segments)
		{
			if (segment.IsInRange(value))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.Media.UnicodeRange" />.
	/// </summary>
	/// <param name="s">The string to parse.</param>
	/// <returns>The parsed <see cref="T:Avalonia.Media.UnicodeRange" />.</returns>
	/// <exception cref="T:System.FormatException"></exception>
	public static UnicodeRange Parse(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			throw new FormatException("Could not parse specified Unicode range.");
		}
		string[] array = s.Split(',');
		int num = array.Length;
		switch (num)
		{
		case 0:
			throw new FormatException("Could not parse specified Unicode range.");
		case 1:
			return new UnicodeRange(UnicodeRangeSegment.Parse(array[0]));
		default:
		{
			UnicodeRangeSegment[] array2 = new UnicodeRangeSegment[num];
			for (int i = 0; i < num; i++)
			{
				array2[i] = UnicodeRangeSegment.Parse(array[i].Trim());
			}
			return new UnicodeRange(array2);
		}
		}
	}
}
