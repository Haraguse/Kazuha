using System;

namespace Avalonia.Media.TextFormatting.Unicode;

/// <summary>
/// Enumerates Unicode word-boundary segments.
/// </summary>
public ref struct WordBreakEnumerator(ReadOnlySpan<char> text)
{
	private readonly struct WordBreakUnit(Codepoint codepoint, int start, int end)
	{
		public Codepoint Codepoint { get; } = codepoint;

		public WordBreakClass WordBreakClass { get; } = codepoint.WordBreakClass;

		public int Start { get; } = start;

		public int End { get; } = end;
	}

	private readonly ReadOnlySpan<char> _text = text;

	private int _offset = 0;

	private int _codepointOffset = 0;

	/// <summary>
	/// Moves to the next <see cref="T:Avalonia.Media.TextFormatting.Unicode.WordSegment" />.
	/// </summary>
	/// <param name="segment">The current word-boundary segment.</param>
	/// <returns><see langword="true" /> if a segment was found; otherwise, <see langword="false" />.</returns>
	public bool MoveNext(out WordSegment segment)
	{
		if (_offset >= _text.Length)
		{
			segment = default(WordSegment);
			return false;
		}
		int offset = _offset;
		int codepointOffset = _codepointOffset;
		WordBreakUnit current = ReadForward(_offset);
		int end = current.End;
		int num = _codepointOffset + 1;
		while (end < _text.Length)
		{
			WordBreakUnit next = ReadForward(end);
			if (IsBoundary(in current, in next))
			{
				break;
			}
			current = next;
			end = current.End;
			num++;
		}
		segment = new WordSegment(offset, end - offset, codepointOffset, num - codepointOffset);
		_offset = end;
		_codepointOffset = num;
		return true;
	}

	private readonly bool IsBoundary(in WordBreakUnit current, in WordBreakUnit next)
	{
		if (current.WordBreakClass == WordBreakClass.CarriageReturn && next.WordBreakClass == WordBreakClass.LineFeed)
		{
			return false;
		}
		if (IsNewline(in current) || IsNewline(in next))
		{
			return true;
		}
		if (current.WordBreakClass == WordBreakClass.ZWJ && next.Codepoint.GraphemeBreakClass == GraphemeBreakClass.ExtendedPictographic)
		{
			return false;
		}
		if (current.WordBreakClass == WordBreakClass.WSegSpace && next.WordBreakClass == WordBreakClass.WSegSpace)
		{
			return false;
		}
		if (IsIgnored(next.WordBreakClass))
		{
			return false;
		}
		WordBreakUnit effectivePrevious = GetEffectivePrevious(in current);
		WordBreakClass wordBreakClass = next.WordBreakClass;
		if (IsAHLetter(effectivePrevious.WordBreakClass) && IsAHLetter(wordBreakClass))
		{
			return false;
		}
		if (IsAHLetter(effectivePrevious.WordBreakClass) && IsMidLetterMidNumLetQ(wordBreakClass) && TryGetNextSignificant(next.End, out var codepoint) && IsAHLetter(codepoint.WordBreakClass))
		{
			return false;
		}
		if (IsAHLetter(wordBreakClass) && IsMidLetterMidNumLetQ(effectivePrevious.WordBreakClass) && TryGetPreviousSignificant(effectivePrevious.Start, out var codepoint2) && IsAHLetter(codepoint2.WordBreakClass))
		{
			return false;
		}
		if (effectivePrevious.WordBreakClass == WordBreakClass.HebrewLetter && wordBreakClass == WordBreakClass.SingleQuote)
		{
			return false;
		}
		if (effectivePrevious.WordBreakClass == WordBreakClass.HebrewLetter && wordBreakClass == WordBreakClass.DoubleQuote && TryGetNextSignificant(next.End, out codepoint) && codepoint.WordBreakClass == WordBreakClass.HebrewLetter)
		{
			return false;
		}
		if (wordBreakClass == WordBreakClass.HebrewLetter && effectivePrevious.WordBreakClass == WordBreakClass.DoubleQuote && TryGetPreviousSignificant(effectivePrevious.Start, out codepoint2) && codepoint2.WordBreakClass == WordBreakClass.HebrewLetter)
		{
			return false;
		}
		if (effectivePrevious.WordBreakClass == WordBreakClass.Numeric && wordBreakClass == WordBreakClass.Numeric)
		{
			return false;
		}
		if (IsAHLetter(effectivePrevious.WordBreakClass) && wordBreakClass == WordBreakClass.Numeric)
		{
			return false;
		}
		if (effectivePrevious.WordBreakClass == WordBreakClass.Numeric && IsAHLetter(wordBreakClass))
		{
			return false;
		}
		if (wordBreakClass == WordBreakClass.Numeric && IsMidNumMidNumLetQ(effectivePrevious.WordBreakClass) && TryGetPreviousSignificant(effectivePrevious.Start, out codepoint2) && codepoint2.WordBreakClass == WordBreakClass.Numeric)
		{
			return false;
		}
		if (effectivePrevious.WordBreakClass == WordBreakClass.Numeric && IsMidNumMidNumLetQ(wordBreakClass) && TryGetNextSignificant(next.End, out codepoint) && codepoint.WordBreakClass == WordBreakClass.Numeric)
		{
			return false;
		}
		if (effectivePrevious.WordBreakClass == WordBreakClass.Katakana && wordBreakClass == WordBreakClass.Katakana)
		{
			return false;
		}
		if (IsAHLetterNumericKatakanaExtendNumLet(effectivePrevious.WordBreakClass) && wordBreakClass == WordBreakClass.ExtendNumLet)
		{
			return false;
		}
		if (effectivePrevious.WordBreakClass == WordBreakClass.ExtendNumLet && IsAHLetterNumericKatakana(wordBreakClass))
		{
			return false;
		}
		if (effectivePrevious.WordBreakClass == WordBreakClass.RegionalIndicator && wordBreakClass == WordBreakClass.RegionalIndicator && (CountRegionalIndicatorsBefore(next.Start) & 1) == 1)
		{
			return false;
		}
		return true;
	}

	private readonly WordBreakUnit GetEffectivePrevious(in WordBreakUnit current)
	{
		if (!IsIgnored(current.WordBreakClass))
		{
			return current;
		}
		int start = current.Start;
		WordBreakUnit codepoint;
		while (TryReadBackward(start, out codepoint))
		{
			if (!IsIgnored(codepoint.WordBreakClass))
			{
				if (!IsNewline(in codepoint))
				{
					return codepoint;
				}
				return current;
			}
			start = codepoint.Start;
		}
		return current;
	}

	private readonly int CountRegionalIndicatorsBefore(int end)
	{
		int num = 0;
		int end2 = end;
		WordBreakUnit codepoint;
		while (TryGetPreviousSignificant(end2, out codepoint) && codepoint.WordBreakClass == WordBreakClass.RegionalIndicator)
		{
			num++;
			end2 = codepoint.Start;
		}
		return num;
	}

	private readonly bool TryGetPreviousSignificant(int end, out WordBreakUnit codepoint)
	{
		int end2 = end;
		while (TryReadBackward(end2, out codepoint))
		{
			if (!IsIgnored(codepoint.WordBreakClass))
			{
				return true;
			}
			end2 = codepoint.Start;
		}
		codepoint = default(WordBreakUnit);
		return false;
	}

	private readonly bool TryGetNextSignificant(int start, out WordBreakUnit codepoint)
	{
		int start2 = start;
		while (TryReadForward(start2, out codepoint))
		{
			if (!IsIgnored(codepoint.WordBreakClass))
			{
				return true;
			}
			start2 = codepoint.End;
		}
		codepoint = default(WordBreakUnit);
		return false;
	}

	private readonly WordBreakUnit ReadForward(int start)
	{
		int count;
		return new WordBreakUnit(Codepoint.ReadAt(_text, start, out count), start, start + count);
	}

	private readonly bool TryReadForward(int start, out WordBreakUnit codepoint)
	{
		if (start >= _text.Length)
		{
			codepoint = default(WordBreakUnit);
			return false;
		}
		codepoint = ReadForward(start);
		return true;
	}

	private readonly bool TryReadBackward(int end, out WordBreakUnit codepoint)
	{
		if (end <= 0)
		{
			codepoint = default(WordBreakUnit);
			return false;
		}
		int num = end - 1;
		if (num > 0 && char.IsLowSurrogate(_text[num]) && char.IsHighSurrogate(_text[num - 1]))
		{
			num--;
		}
		codepoint = ReadForward(num);
		return true;
	}

	private static bool IsAHLetter(WordBreakClass wordBreakClass)
	{
		if ((uint)(wordBreakClass - 9) <= 1u)
		{
			return true;
		}
		return false;
	}

	private static bool IsAHLetterNumericKatakana(WordBreakClass wordBreakClass)
	{
		bool flag = IsAHLetter(wordBreakClass);
		if (!flag)
		{
			bool flag2 = ((wordBreakClass == WordBreakClass.Katakana || wordBreakClass == WordBreakClass.Numeric) ? true : false);
			flag = flag2;
		}
		return flag;
	}

	private static bool IsAHLetterNumericKatakanaExtendNumLet(WordBreakClass wordBreakClass)
	{
		if (!IsAHLetterNumericKatakana(wordBreakClass))
		{
			return wordBreakClass == WordBreakClass.ExtendNumLet;
		}
		return true;
	}

	private static bool IsIgnored(WordBreakClass wordBreakClass)
	{
		if ((uint)(wordBreakClass - 4) <= 1u || wordBreakClass == WordBreakClass.Format)
		{
			return true;
		}
		return false;
	}

	private static bool IsMidLetterMidNumLetQ(WordBreakClass wordBreakClass)
	{
		if (wordBreakClass == WordBreakClass.SingleQuote || (uint)(wordBreakClass - 13) <= 1u)
		{
			return true;
		}
		return false;
	}

	private static bool IsMidNumMidNumLetQ(WordBreakClass wordBreakClass)
	{
		switch (wordBreakClass)
		{
		case WordBreakClass.SingleQuote:
		case WordBreakClass.MidNumLet:
		case WordBreakClass.MidNum:
			return true;
		default:
			return false;
		}
	}

	private static bool IsNewline(in WordBreakUnit codepoint)
	{
		WordBreakClass wordBreakClass = codepoint.WordBreakClass;
		if ((uint)(wordBreakClass - 1) <= 2u)
		{
			return true;
		}
		return false;
	}
}
