using System;
using System.Collections.Generic;

namespace Avalonia.Utilities;

/// <summary>
/// Helpers for splitting strings.
/// </summary>
internal static class StringSplitter
{
	private const char DefaultOpeningParenthesis = '(';

	private const char DefaultClosingParenthesis = ')';

	/// <summary>
	/// Splits the provided string by the specified separators, but ignores separators that
	/// appear inside matching bracket pairs (<paramref name="openingBracket" /> / <paramref name="closingBracket" />).
	/// </summary>
	/// <param name="s">The input string to split. If <c>null</c>, an empty array is returned.</param>
	/// <param name="separator">The separator character to split on.</param>
	/// <param name="openingBracket">The character that opens a bracketed section. <c>(</c> by default.</param>
	/// <param name="closingBracket">The character that closes a bracketed section. <c>)</c> by default.</param>
	/// <param name="options">Options for trimming entries and removing empty entries.</param>
	/// <returns>An array of split segments. Returns an empty array if the input is null or only whitespace.</returns>
	public static string[] SplitRespectingBrackets(string? s, char separator, char openingBracket = '(', char closingBracket = ')', StringSplitOptions options = StringSplitOptions.None)
	{
		return SplitRespectingBrackets(s, new ReadOnlySpan<char>((char)separator), openingBracket, closingBracket, options);
	}

	/// <summary>
	/// Splits the provided string by the specified separator, but ignores separators that
	/// appear inside matching bracket pairs (<paramref name="openingBracket" /> / <paramref name="closingBracket" />).
	/// </summary>
	/// <param name="s">The input string to split. If <c>null</c>, an empty array is returned.</param>
	/// <param name="separators">The separator characters to split on.</param>
	/// <param name="openingBracket">The character that opens a bracketed section. <c>(</c> by default.</param>
	/// <param name="closingBracket">The character that closes a bracketed section. <c>)</c> by default.</param>
	/// <param name="options">Options for trimming entries and removing empty entries.</param>
	/// <returns>An array of split segments. Returns an empty array if the input is null or only whitespace.</returns>
	public static string[] SplitRespectingBrackets(string? s, ReadOnlySpan<char> separators, char openingBracket = '(', char closingBracket = ')', StringSplitOptions options = StringSplitOptions.None)
	{
		if (openingBracket == closingBracket)
		{
			throw new ArgumentException($"Opening bracket and closing bracket cannot be the same character '{openingBracket}'.", "closingBracket");
		}
		if (s == null)
		{
			return Array.Empty<string>();
		}
		ReadOnlySpan<char> readOnlySpan = s.AsSpan();
		List<(int start, int length)> ranges = new List<(int, int)>();
		int num = 0;
		int start = 0;
		bool removeEmptyEntries = options.HasFlag(StringSplitOptions.RemoveEmptyEntries);
		bool trimEntries = options.HasFlag(StringSplitOptions.TrimEntries);
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			if (c == openingBracket)
			{
				num++;
			}
			else if (c == closingBracket)
			{
				if (num <= 0)
				{
					throw new FormatException($"Unmatched closing bracket '{closingBracket}' at position {i}.");
				}
				num--;
			}
			else if (separators.Contains(c) && num == 0)
			{
				ProcessSegment(start, i - 1);
				start = i + 1;
			}
		}
		if (num != 0)
		{
			throw new FormatException($"Unmatched opening bracket '{openingBracket}' in input string.");
		}
		ProcessSegment(start, readOnlySpan.Length - 1);
		if (ranges.Count == 0)
		{
			return Array.Empty<string>();
		}
		string[] array = new string[ranges.Count];
		for (int j = 0; j < ranges.Count; j++)
		{
			(int, int) tuple = ranges[j];
			array[j] = new string(readOnlySpan.Slice(tuple.Item1, tuple.Item2));
		}
		return array;
		void ProcessSegment(int num2, int end)
		{
			if (trimEntries)
			{
				while (num2 <= end && char.IsWhiteSpace(s[num2]))
				{
					num2++;
				}
				while (end >= num2 && char.IsWhiteSpace(s[end]))
				{
					end--;
				}
			}
			int num3 = end - num2 + 1;
			if (num3 > 0 || !removeEmptyEntries)
			{
				ranges.Add((num2, num3));
			}
		}
	}
}
