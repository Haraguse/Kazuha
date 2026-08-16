using System;

namespace Avalonia.Utilities;

internal static class KeywordParser
{
	public static bool CheckKeyword(this ref CharacterReader r, string keyword)
	{
		return r.CheckKeywordInternal(keyword) >= 0;
	}

	private static int CheckKeywordInternal(this ref CharacterReader r, string keyword)
	{
		ReadOnlySpan<char> readOnlySpan = r.PeekWhitespace();
		ReadOnlySpan<char> readOnlySpan2 = r.TryPeek(readOnlySpan.Length + keyword.Length);
		if (readOnlySpan2.IsEmpty)
		{
			return -1;
		}
		if (SpanEquals(readOnlySpan2.Slice(readOnlySpan.Length), keyword.AsSpan()))
		{
			return readOnlySpan2.Length;
		}
		return -1;
	}

	private static bool SpanEquals(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
	{
		if (left.Length != right.Length)
		{
			return false;
		}
		for (int i = 0; i < left.Length; i++)
		{
			if (left[i] != right[i])
			{
				return false;
			}
		}
		return true;
	}

	public static bool TakeIfKeyword(this ref CharacterReader r, string keyword)
	{
		int num = r.CheckKeywordInternal(keyword);
		if (num < 0)
		{
			return false;
		}
		r.Skip(num);
		return true;
	}
}
