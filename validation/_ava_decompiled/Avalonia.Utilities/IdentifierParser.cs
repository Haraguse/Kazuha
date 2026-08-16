using System;
using System.Globalization;

namespace Avalonia.Utilities;

internal static class IdentifierParser
{
	public static ReadOnlySpan<char> ParseIdentifier(this scoped ref CharacterReader r)
	{
		if (IsValidIdentifierStart(r.Peek))
		{
			return r.TakeWhile((char c) => IsValidIdentifierChar(c));
		}
		return ReadOnlySpan<char>.Empty;
	}

	private static bool IsValidIdentifierStart(char c)
	{
		if (!char.IsLetter(c))
		{
			return c == '_';
		}
		return true;
	}

	private static bool IsValidIdentifierChar(char c)
	{
		if (IsValidIdentifierStart(c))
		{
			return true;
		}
		UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
		if (unicodeCategory != UnicodeCategory.NonSpacingMark && unicodeCategory != UnicodeCategory.SpacingCombiningMark && unicodeCategory != UnicodeCategory.ConnectorPunctuation && unicodeCategory != UnicodeCategory.Format)
		{
			return unicodeCategory == UnicodeCategory.DecimalDigitNumber;
		}
		return true;
	}

	internal static ReadOnlySpan<char> ParseNumber(this ref CharacterReader r)
	{
		return r.TakeWhile((char c) => IsValidNumberChar(c));
	}

	private static bool IsValidNumberChar(char c)
	{
		return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.DecimalDigitNumber;
	}
}
