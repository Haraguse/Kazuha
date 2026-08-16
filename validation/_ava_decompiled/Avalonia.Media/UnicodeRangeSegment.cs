using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Avalonia.Media;

public readonly record struct UnicodeRangeSegment
{
	/// <summary>
	/// Get the start of the segment.
	/// </summary>
	public int Start { get; }

	/// <summary>
	/// Get the end of the segment.
	/// </summary>
	public int End { get; }

	private static readonly Regex s_regex = new Regex("^(?:[uU]\\+)?(?:([0-9a-fA-F](?:[0-9a-fA-F?]{1,5})?))$", RegexOptions.Compiled);

	public UnicodeRangeSegment(int start, int end)
	{
		Start = start;
		End = end;
	}

	/// <summary>
	/// Determines if given value is inside the range segment.
	/// </summary>
	/// <param name="value">The value to verify.</param>
	/// <returns>
	/// <c>true</c> If given value is inside the range segment, <c>false</c> otherwise.
	/// </returns>
	public bool IsInRange(int value)
	{
		if (Start <= value)
		{
			return value <= End;
		}
		return false;
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.Media.UnicodeRangeSegment" />.
	/// </summary>
	/// <param name="s">The string to parse.</param>
	/// <returns>The parsed <see cref="T:Avalonia.Media.UnicodeRangeSegment" />.</returns>
	/// <exception cref="T:System.FormatException"></exception>
	public static UnicodeRangeSegment Parse(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			throw new FormatException("Could not parse specified Unicode range segment.");
		}
		string[] array = s.Split('-');
		int num;
		int end;
		switch (array.Length)
		{
		case 1:
		{
			Match match3 = s_regex.Match(array[0]);
			if (!match3.Success)
			{
				throw new FormatException("Could not parse specified Unicode range segment.");
			}
			if (!match3.Value.Contains('?'))
			{
				num = int.Parse(match3.Groups[1].Value, NumberStyles.HexNumber);
				end = num;
			}
			else
			{
				num = int.Parse(match3.Groups[1].Value.Replace('?', '0'), NumberStyles.HexNumber);
				end = int.Parse(match3.Groups[1].Value.Replace('?', 'F'), NumberStyles.HexNumber);
			}
			break;
		}
		case 2:
		{
			Match match = s_regex.Match(array[0]);
			Match match2 = s_regex.Match(array[1]);
			if (!match.Success || !match2.Success)
			{
				throw new FormatException("Could not parse specified Unicode range segment.");
			}
			num = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
			end = int.Parse(match2.Groups[1].Value, NumberStyles.HexNumber);
			break;
		}
		default:
			throw new FormatException("Could not parse specified Unicode range segment.");
		}
		return new UnicodeRangeSegment(num, end);
	}
}
