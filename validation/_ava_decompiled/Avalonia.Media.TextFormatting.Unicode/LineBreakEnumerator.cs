using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.TextFormatting.Unicode;

public ref struct LineBreakEnumerator(ReadOnlySpan<char> text)
{
	private enum RuleResult
	{
		Pass,
		NoBreak,
		MayBreak,
		MustBreak
	}

	private readonly struct BreakUnit
	{
		public int Start { get; }

		public int Length { get; }

		public Codepoint Codepoint { get; }

		public bool EndOfText { get; init; }

		public bool StartOfText { get; init; }

		public LineBreakClass LineBreakClass { get; init; }

		public bool Ignored { get; init; }

		public bool Inherited { get; init; }

		public BreakUnit()
		{
			Start = 0;
			Length = 0;
			Codepoint = default(Codepoint);
			EndOfText = false;
			StartOfText = false;
			Ignored = false;
			Inherited = false;
			LineBreakClass = LineBreakClass.Unknown;
		}

		public BreakUnit(BreakUnit other, LineBreakClass lineBreakClass)
		{
			StartOfText = false;
			Ignored = false;
			Inherited = false;
			Codepoint = other.Codepoint;
			Start = other.Start;
			Length = other.Length;
			LineBreakClass = lineBreakClass;
			EndOfText = other.EndOfText;
		}

		public BreakUnit(Codepoint codepoint, int start, int length)
		{
			EndOfText = false;
			StartOfText = false;
			Ignored = false;
			Inherited = false;
			Codepoint = codepoint;
			Start = start;
			Length = length;
			LineBreakClass = MapClass(codepoint);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static LineBreakClass MapClass(Codepoint cp)
		{
			if (cp.Value == 327685)
			{
				return LineBreakClass.Alphabetic;
			}
			LineBreakClass lineBreakClass = cp.LineBreakClass;
			if (((1L << (int)lineBreakClass) & 0x308600000000L) != 0L)
			{
				switch (lineBreakClass)
				{
				case LineBreakClass.Unknown:
				case LineBreakClass.Ambiguous:
				case LineBreakClass.Surrogate:
					return LineBreakClass.Alphabetic;
				case LineBreakClass.ComplexContext:
				{
					GeneralCategory generalCategory = cp.GeneralCategory;
					if ((generalCategory != GeneralCategory.SpacingMark && generalCategory != GeneralCategory.NonspacingMark) || 1 == 0)
					{
						return LineBreakClass.Alphabetic;
					}
					return LineBreakClass.CombiningMark;
				}
				case LineBreakClass.ConditionalJapaneseStarter:
					return LineBreakClass.Nonstarter;
				}
			}
			return lineBreakClass;
		}
	}

	private ref struct LineBreakState
	{
		private BreakUnit _next = default(BreakUnit);

		private bool _hasNext = false;

		private LineBreakClass _nextClass = LineBreakClass.Unknown;

		private BreakUnit _previous = s_sot;

		private LineBreakClass _previousClass = s_sot.LineBreakClass;

		public BreakUnit Current { get; set; } = s_sot;

		/// <summary>
		/// Cached LineBreakClass of the previous BreakUnit. Updated whenever
		/// <see cref="P:Avalonia.Media.TextFormatting.Unicode.LineBreakEnumerator.LineBreakState.Previous" /> would be reassigned (in <see cref="M:Avalonia.Media.TextFormatting.Unicode.LineBreakEnumerator.LineBreakState.Read(System.ReadOnlySpan{System.Char})" />
		/// or via the Ignored/Inherited fall-through in the getter).
		/// </summary>
		public LineBreakClass PreviousClass
		{
			get
			{
				if (_previous.Ignored || _previous.Inherited)
				{
					_previous = LastBeforeWhitespace;
					_previousClass = LastBeforeWhitespace.LineBreakClass;
				}
				return _previousClass;
			}
		}

		public BreakUnit Previous
		{
			get
			{
				if (_previous.Ignored || _previous.Inherited)
				{
					_previous = LastBeforeWhitespace;
					_previousClass = LastBeforeWhitespace.LineBreakClass;
				}
				return _previous;
			}
		}

		/// <summary>
		/// Cached LineBreakClass of the next BreakUnit. Faster than
		/// <c>state.NextClass</c> because it avoids the
		/// BreakUnit struct copy. Read is responsible for ensuring _hasNext
		/// is set before any rule sees this property.
		/// </summary>
		public LineBreakClass NextClass => _nextClass;

		public int Position { get; private set; } = 0;

		public int RegionalIndicator { get; set; } = 0;

		public BreakUnit LastBeforeWhitespace { get; set; } = s_sot;

		public BreakUnit LastBeforeSpace { get; set; } = s_sot;

		public LineBreakState()
		{
		}

		public BreakUnit Next(ReadOnlySpan<char> text)
		{
			if (!_hasNext)
			{
				_next = Peek(text);
				_nextClass = _next.LineBreakClass;
				_hasNext = true;
			}
			return _next;
		}

		public static BreakUnit After(ReadOnlySpan<char> text, BreakUnit current)
		{
			if (current.EndOfText)
			{
				return s_eot;
			}
			return PeekAt(text, current.Start + current.Length);
		}

		public static BreakUnit Before(ReadOnlySpan<char> text, BreakUnit current)
		{
			if (current.StartOfText)
			{
				return s_sot;
			}
			int index = current.Start - 1;
			return PeekAt(text, index);
		}

		public void IgnoreNext(ReadOnlySpan<char> text)
		{
			BreakUnit next = Next(text);
			next.Ignored = true;
			BreakUnit breakUnit = (_next = next);
			_nextClass = breakUnit.LineBreakClass;
			_hasNext = true;
		}

		public void ReplaceNext(BreakUnit next)
		{
			_next = next;
			_nextClass = next.LineBreakClass;
			_hasNext = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static BreakUnit PeekAt(ReadOnlySpan<char> text, int index)
		{
			if (text.Length == 0)
			{
				return new BreakUnit(Codepoint.ReplacementCodepoint, index, 0)
				{
					StartOfText = true
				};
			}
			if (index >= text.Length)
			{
				return s_eot;
			}
			Codepoint codepoint = Codepoint.ReadAt(text, index, out var count);
			return new BreakUnit(codepoint, index, count)
			{
				EndOfText = (index + count == text.Length),
				StartOfText = (index == 0)
			};
		}

		public BreakUnit Peek(ReadOnlySpan<char> text)
		{
			return PeekAt(text, Position);
		}

		public BreakUnit Read(ReadOnlySpan<char> text)
		{
			_previous = Current;
			_previousClass = Current.LineBreakClass;
			BreakUnit result = (Current = Next(text));
			Position += result.Length;
			_next = Peek(text);
			_nextClass = _next.LineBreakClass;
			_hasNext = true;
			if (_previous.Ignored || _previous.Inherited)
			{
				_previous = LastBeforeWhitespace;
				_previousClass = LastBeforeWhitespace.LineBreakClass;
			}
			if (Current.Ignored)
			{
				BreakUnit current = Current;
				current.LineBreakClass = Previous.LineBreakClass;
				current.Inherited = true;
				Current = current;
			}
			BreakUnit breakUnit2 = (Current.Inherited ? Previous : Current);
			if (!Current.Codepoint.IsWhiteSpace)
			{
				LastBeforeWhitespace = breakUnit2;
			}
			if (Current.LineBreakClass != LineBreakClass.Space)
			{
				LastBeforeSpace = breakUnit2;
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static LineBreakClass ClassAfterSpaces(ReadOnlySpan<char> text, BreakUnit current)
		{
			int num = current.Start + current.Length;
			if (num >= text.Length)
			{
				return current.LineBreakClass;
			}
			CodepointEnumerator codepointEnumerator = new CodepointEnumerator(text.Slice(num));
			Codepoint codepoint;
			while (codepointEnumerator.MoveNext(out codepoint) && codepoint.LineBreakClass == LineBreakClass.Space)
			{
			}
			return codepoint.LineBreakClass;
		}
	}

	private delegate RuleResult BreakUnitDelegate(ReadOnlySpan<char> text, ref LineBreakState state);

	private const char DotCircle = '◌';

	private static readonly BreakUnit s_sot = new BreakUnit
	{
		StartOfText = true
	};

	private static readonly BreakUnit s_eot = new BreakUnit
	{
		EndOfText = true
	};

	public readonly ReadOnlySpan<char> _text = text;

	private LineBreakState _state = new LineBreakState();

	public bool MoveNext([NotNullWhen(true)] out LineBreak lineBreak)
	{
		lineBreak = default(LineBreak);
		if (_text.IsEmpty)
		{
			return false;
		}
		if (_state.Current.EndOfText)
		{
			return false;
		}
		LineBreak? lineBreak2 = null;
		while (!lineBreak2.HasValue)
		{
			_state.Read(_text);
			lineBreak2 = ExecuteRules(_text, ref _state);
		}
		if (!lineBreak2.HasValue)
		{
			return false;
		}
		lineBreak = lineBreak2.Value;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsBreakClass(LineBreakClass cls)
	{
		return ((1L << (int)cls) & 0xD4000000000L) != 0;
	}

	private static LineBreak GetLineBreak(ReadOnlySpan<char> text, LineBreakState state, bool isRequired)
	{
		int num = state.Current.Start + state.Current.Length;
		int positionWrap = num;
		LineBreakClass lineBreakClass = state.Current.LineBreakClass;
		if (lineBreakClass == LineBreakClass.CarriageReturn || lineBreakClass == LineBreakClass.LineFeed || lineBreakClass == LineBreakClass.Space)
		{
			num = ((state.PreviousClass != LineBreakClass.CarriageReturn) ? FindPriorNonWhitespace(text, num) : FindPriorNonWhitespace(text, state.Previous.Start));
		}
		return new LineBreak(num, positionWrap, isRequired);
	}

	private static int FindPriorNonWhitespace(ReadOnlySpan<char> text, int from)
	{
		if (from > 0 && IsBreakClass(Codepoint.ReadAt(text, from - 1, out var count).LineBreakClass))
		{
			from -= count;
		}
		int count2;
		while (from > 0 && Codepoint.ReadAt(text, from - 1, out count2).LineBreakClass == LineBreakClass.Space)
		{
			from -= count2;
		}
		return from;
	}

	private static LineBreak? ExecuteRules(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		RuleResult ruleResult;
		if ((ruleResult = RegionalIndicator(text, ref state)) == RuleResult.Pass && (ruleResult = LB03(text, ref state)) == RuleResult.Pass && (ruleResult = LB04(text, ref state)) == RuleResult.Pass && (ruleResult = LB05(text, ref state)) == RuleResult.Pass && (ruleResult = LB06(text, ref state)) == RuleResult.Pass && (ruleResult = LB07(text, ref state)) == RuleResult.Pass && (ruleResult = LB08(text, ref state)) == RuleResult.Pass && (ruleResult = LB08a(text, ref state)) == RuleResult.Pass && (ruleResult = LB09(text, ref state)) == RuleResult.Pass && (ruleResult = LB10(text, ref state)) == RuleResult.Pass && (ruleResult = LB11(text, ref state)) == RuleResult.Pass && (ruleResult = LB12(text, ref state)) == RuleResult.Pass && (ruleResult = LB12a(text, ref state)) == RuleResult.Pass && (ruleResult = LB13(text, ref state)) == RuleResult.Pass && (ruleResult = LB14(text, ref state)) == RuleResult.Pass && (ruleResult = LB15a(text, ref state)) == RuleResult.Pass && (ruleResult = LB15b(text, ref state)) == RuleResult.Pass && (ruleResult = LB15c(text, ref state)) == RuleResult.Pass && (ruleResult = LB15d(text, ref state)) == RuleResult.Pass && (ruleResult = LB16(text, ref state)) == RuleResult.Pass && (ruleResult = LB17(text, ref state)) == RuleResult.Pass && (ruleResult = LB18(text, ref state)) == RuleResult.Pass && (ruleResult = LB19(text, ref state)) == RuleResult.Pass && (ruleResult = LB20(text, ref state)) == RuleResult.Pass && (ruleResult = LB20a(text, ref state)) == RuleResult.Pass && (ruleResult = LB21a(text, ref state)) == RuleResult.Pass && (ruleResult = LB21(text, ref state)) == RuleResult.Pass && (ruleResult = LB21b(text, ref state)) == RuleResult.Pass && (ruleResult = LB22(text, ref state)) == RuleResult.Pass && (ruleResult = LB23(text, ref state)) == RuleResult.Pass && (ruleResult = LB23a(text, ref state)) == RuleResult.Pass && (ruleResult = LB24(text, ref state)) == RuleResult.Pass && (ruleResult = LB25(text, ref state)) == RuleResult.Pass && (ruleResult = LB26(text, ref state)) == RuleResult.Pass && (ruleResult = LB27(text, ref state)) == RuleResult.Pass && (ruleResult = LB28(text, ref state)) == RuleResult.Pass && (ruleResult = LB28a(text, ref state)) == RuleResult.Pass && (ruleResult = LB29(text, ref state)) == RuleResult.Pass && (ruleResult = LB30(text, ref state)) == RuleResult.Pass && (ruleResult = LB30a(text, ref state)) == RuleResult.Pass && (ruleResult = LB30b(text, ref state)) == RuleResult.Pass)
		{
			ruleResult = LB31(text, ref state);
		}
		switch (ruleResult)
		{
		case RuleResult.NoBreak:
			return null;
		case RuleResult.MayBreak:
		case RuleResult.MustBreak:
			return GetLineBreak(text, state, IsBreakClass(state.Current.LineBreakClass));
		default:
			return null;
		}
	}

	private static RuleResult RegionalIndicator(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.Current.Inherited)
		{
			return RuleResult.Pass;
		}
		if (state.Current.LineBreakClass == LineBreakClass.RegionalIndicator && ++state.RegionalIndicator % 2 == 0)
		{
			state.RegionalIndicator = 0;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB3: Always break at the end of text.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB03(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.Current.EndOfText)
		{
			return RuleResult.MustBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB4: Always break after hard line breaks.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB04(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.Current.LineBreakClass == LineBreakClass.MandatoryBreak)
		{
			return RuleResult.MustBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB5: Treat CR followed by LF, as well as CR, LF, and NL as hard line
	/// </summary>
	private static RuleResult LB05(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		switch (state.Current.LineBreakClass)
		{
		case LineBreakClass.CarriageReturn:
			if (state.NextClass == LineBreakClass.LineFeed)
			{
				return RuleResult.NoBreak;
			}
			return RuleResult.MustBreak;
		case LineBreakClass.LineFeed:
		case LineBreakClass.NextLine:
			return RuleResult.MustBreak;
		default:
			return RuleResult.Pass;
		}
	}

	/// <summary>
	/// LB6: Do not break before hard line breaks.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB06(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (IsBreakClass(state.NextClass))
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB7: Do not break before spaces or zero width space.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB07(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		LineBreakClass nextClass = state.NextClass;
		if (nextClass == LineBreakClass.ZWSpace || nextClass == LineBreakClass.Space)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB8: Break before any character following a zero-width space, even if one or more spaces intervene.
	/// </summary>
	private static RuleResult LB08(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.LastBeforeSpace.LineBreakClass == LineBreakClass.ZWSpace && state.NextClass != LineBreakClass.Space)
		{
			return RuleResult.MayBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB8a: Do not break after a zero width joiner.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB08a(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.Current.LineBreakClass == LineBreakClass.ZWJ)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB9: Do not break a combining character sequence;
	/// treat it as if it has the line breaking class of the base character in all of the following rules.
	/// Treat ZWJ as if it were CM.
	/// </summary>
	private static RuleResult LB09(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		LineBreakClass lineBreakClass = state.Current.LineBreakClass;
		if (IsBreakClass(lineBreakClass) || lineBreakClass == LineBreakClass.Space || lineBreakClass == LineBreakClass.ZWSpace)
		{
			return RuleResult.Pass;
		}
		LineBreakClass nextClass = state.NextClass;
		if (nextClass == LineBreakClass.CombiningMark || nextClass == LineBreakClass.ZWJ)
		{
			state.IgnoreNext(text);
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB10: Treat any remaining combining mark or ZWJ as AL.
	/// </summary>
	private static RuleResult LB10(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.Current.LineBreakClass == LineBreakClass.CombiningMark)
		{
			BreakUnit current = state.Current;
			current.LineBreakClass = LineBreakClass.Alphabetic;
			current.Inherited = true;
			state.Current = current;
		}
		BreakUnit breakUnit = state.Next(text);
		if (breakUnit.LineBreakClass == LineBreakClass.CombiningMark)
		{
			BreakUnit current = breakUnit;
			current.LineBreakClass = LineBreakClass.Alphabetic;
			current.Inherited = true;
			state.ReplaceNext(current);
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB11: Do not break before or after Word joiner and related characters.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB11(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.NextClass == LineBreakClass.WordJoiner || state.Current.LineBreakClass == LineBreakClass.WordJoiner)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB12: Do not break after NBSP and related characters.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB12(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.Current.LineBreakClass == LineBreakClass.Glue)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB12a: Do not break before NBSP and related characters, except after spaces and hyphens.
	/// </summary>
	private static RuleResult LB12a(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.NextClass == LineBreakClass.Glue)
		{
			LineBreakClass lineBreakClass = state.Current.LineBreakClass;
			if ((uint)(lineBreakClass - 16) <= 1u || lineBreakClass == LineBreakClass.UnambiguousHyphen || lineBreakClass == LineBreakClass.Space)
			{
				return RuleResult.Pass;
			}
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB13: Do not break before ‘]’ or ‘!’ or ‘;’ or ‘/’, even after spaces.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB13(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		LineBreakClass nextClass = state.NextClass;
		if ((uint)(nextClass - 1) <= 1u || (uint)(nextClass - 6) <= 1u)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB14: Do not break after ‘[’, even after spaces.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB14(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.LastBeforeWhitespace.LineBreakClass == LineBreakClass.OpenPunctuation)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB15a: Do not break after an unresolved initial punctuation that lies at the start of the line,
	/// after a space, after opening punctuation, or after an unresolved quotation mark, even after spaces.
	/// </summary>
	private static RuleResult LB15a(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.LastBeforeWhitespace.Codepoint.GeneralCategory == GeneralCategory.InitialPunctuation && state.LastBeforeWhitespace.LineBreakClass == LineBreakClass.Quotation && IsStartLike(LineBreakState.Before(text, state.LastBeforeWhitespace)))
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
		static bool IsStartLike(BreakUnit unit)
		{
			if ((unit.StartOfText && unit.Length == 0) || IsBreakClass(unit.LineBreakClass))
			{
				return true;
			}
			switch (unit.LineBreakClass)
			{
			case LineBreakClass.OpenPunctuation:
			case LineBreakClass.Quotation:
			case LineBreakClass.Glue:
			case LineBreakClass.ZWSpace:
			case LineBreakClass.Space:
				return true;
			default:
				return false;
			}
		}
	}

	/// <summary>
	/// LB15b: Do not break before an unresolved final punctuation that lies at the end of the line,
	/// before a space, before a prohibited break, or before an unresolved quotation mark, even after spaces.
	/// </summary>
	private static RuleResult LB15b(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.Next(text).Codepoint.GeneralCategory == GeneralCategory.FinalPunctuation && state.NextClass == LineBreakClass.Quotation)
		{
			BreakUnit breakUnit = LineBreakState.After(text, state.Next(text));
			if (breakUnit.EndOfText)
			{
				return RuleResult.NoBreak;
			}
			if (IsBreakClass(breakUnit.LineBreakClass))
			{
				return RuleResult.NoBreak;
			}
			switch (breakUnit.LineBreakClass)
			{
			case LineBreakClass.ClosePunctuation:
			case LineBreakClass.CloseParenthesis:
			case LineBreakClass.Quotation:
			case LineBreakClass.Glue:
			case LineBreakClass.Exclamation:
			case LineBreakClass.BreakSymbols:
			case LineBreakClass.InfixNumeric:
			case LineBreakClass.ZWSpace:
			case LineBreakClass.WordJoiner:
			case LineBreakClass.Space:
				return RuleResult.NoBreak;
			}
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB15c: Break before a decimal mark that follows a space, for instance, in ‘subtract .5’.
	/// </summary>
	private static RuleResult LB15c(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.Current.LineBreakClass == LineBreakClass.Space && state.NextClass == LineBreakClass.InfixNumeric && LineBreakState.After(text, state.Next(text)).LineBreakClass == LineBreakClass.Numeric)
		{
			return RuleResult.MayBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB15d: Otherwise, do not break before ‘;’, ‘,’, or ‘.’, even after spaces.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB15d(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.NextClass == LineBreakClass.InfixNumeric)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB16: Do not break between closing punctuation and a nonstarter (lb=NS),
	/// even with intervening spaces.
	/// </summary>
	private static RuleResult LB16(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		LineBreakClass lineBreakClass = state.LastBeforeWhitespace.LineBreakClass;
		if ((uint)(lineBreakClass - 1) <= 1u)
		{
			LineBreakClass lineBreakClass2 = LineBreakState.ClassAfterSpaces(text, state.Current);
			if (lineBreakClass2 == LineBreakClass.Nonstarter || lineBreakClass2 == LineBreakClass.ConditionalJapaneseStarter)
			{
				return RuleResult.NoBreak;
			}
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB17: Do not break within ‘——’, even with intervening spaces.
	/// </summary>
	private static RuleResult LB17(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.LastBeforeWhitespace.LineBreakClass == LineBreakClass.BreakBoth && LineBreakState.ClassAfterSpaces(text, state.Current) == LineBreakClass.BreakBoth)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB18: Break after spaces.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB18(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.Current.LineBreakClass == LineBreakClass.Space)
		{
			return RuleResult.MayBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB19: Do not break before or after quotation marks.
	/// </summary>
	private static RuleResult LB19(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		BreakUnit current = state.Next(text);
		if (current.LineBreakClass == LineBreakClass.Quotation && current.Codepoint.GeneralCategory != GeneralCategory.InitialPunctuation)
		{
			return RuleResult.NoBreak;
		}
		if (state.Current.LineBreakClass == LineBreakClass.Quotation && state.Current.Codepoint.GeneralCategory != GeneralCategory.FinalPunctuation)
		{
			return RuleResult.NoBreak;
		}
		if (!state.Current.Codepoint.IsEastAsian && current.LineBreakClass == LineBreakClass.Quotation)
		{
			return RuleResult.NoBreak;
		}
		if (current.LineBreakClass == LineBreakClass.Quotation)
		{
			BreakUnit breakUnit = LineBreakState.After(text, current);
			if (breakUnit.EndOfText || !breakUnit.Codepoint.IsEastAsian)
			{
				return RuleResult.NoBreak;
			}
		}
		if (state.Current.LineBreakClass == LineBreakClass.Quotation && !current.Codepoint.IsEastAsian)
		{
			return RuleResult.NoBreak;
		}
		if (((state.Previous.StartOfText && state.Previous.Length == 0) || !state.Previous.Codepoint.IsEastAsian) && state.Current.LineBreakClass == LineBreakClass.Quotation)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB20: Break before and after unresolved CB.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB20(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.Current.LineBreakClass == LineBreakClass.ContingentBreak || state.NextClass == LineBreakClass.ContingentBreak)
		{
			return RuleResult.MayBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB20a: Do not break after a word-initial hyphen.
	/// </summary>
	private static RuleResult LB20a(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		BreakUnit current = (state.Current.Inherited ? state.LastBeforeWhitespace : state.Current);
		bool flag = IsMatch(state.Current.Inherited ? LineBreakState.Before(text, current) : state.Previous);
		if (flag)
		{
			LineBreakClass nextClass = state.NextClass;
			bool flag2 = (uint)(nextClass - 12) <= 1u;
			flag = flag2;
		}
		if (flag)
		{
			LineBreakClass nextClass = current.LineBreakClass;
			if ((nextClass == LineBreakClass.Hyphen || nextClass == LineBreakClass.UnambiguousHyphen) ? true : false)
			{
				return RuleResult.NoBreak;
			}
		}
		return RuleResult.Pass;
		static bool IsMatch(BreakUnit unit)
		{
			if (unit.StartOfText && unit.Length == 0)
			{
				return true;
			}
			if (IsBreakClass(unit.LineBreakClass))
			{
				return true;
			}
			switch (unit.LineBreakClass)
			{
			case LineBreakClass.Glue:
			case LineBreakClass.ZWSpace:
			case LineBreakClass.ContingentBreak:
			case LineBreakClass.Space:
				return true;
			default:
				return false;
			}
		}
	}

	/// <summary>
	/// LB21: Do not break before hyphen-minus, other hyphens, fixed-width spaces, small kana, and other non-starters, or after acute accents.
	/// </summary>
	private static RuleResult LB21(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		LineBreakClass nextClass = state.NextClass;
		if (nextClass == LineBreakClass.Nonstarter || (uint)(nextClass - 16) <= 1u || nextClass == LineBreakClass.UnambiguousHyphen)
		{
			return RuleResult.NoBreak;
		}
		if (state.Current.LineBreakClass == LineBreakClass.BreakBefore)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB21a: Don't break after Hebrew + Hyphen.
	/// </summary>
	private static RuleResult LB21a(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.NextClass != LineBreakClass.HebrewLetter)
		{
			bool flag = state.PreviousClass == LineBreakClass.HebrewLetter;
			if (flag)
			{
				LineBreakClass lineBreakClass = state.Current.LineBreakClass;
				bool flag2 = ((lineBreakClass == LineBreakClass.Hyphen || lineBreakClass == LineBreakClass.UnambiguousHyphen) ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				return RuleResult.NoBreak;
			}
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB21b: Don’t break between Solidus and Hebrew letters.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB21b(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.Current.LineBreakClass == LineBreakClass.BreakSymbols && state.NextClass == LineBreakClass.HebrewLetter)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB22: Do not break before ellipses.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB22(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.NextClass == LineBreakClass.Inseparable)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB23: Do not break between digits and letters.
	/// </summary>
	private static RuleResult LB23(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		switch (state.Current.LineBreakClass)
		{
		case LineBreakClass.Alphabetic:
		case LineBreakClass.HebrewLetter:
			if (state.NextClass == LineBreakClass.Numeric)
			{
				return RuleResult.NoBreak;
			}
			break;
		case LineBreakClass.Numeric:
		{
			LineBreakClass nextClass = state.NextClass;
			if ((uint)(nextClass - 12) <= 1u)
			{
				return RuleResult.NoBreak;
			}
			break;
		}
		}
		return RuleResult.Pass;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsIdEbEm(LineBreakClass cls)
	{
		if (cls != LineBreakClass.Ideographic && cls != LineBreakClass.EBase)
		{
			return cls == LineBreakClass.EModifier;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsPrPo(LineBreakClass cls)
	{
		if (cls != LineBreakClass.PrefixNumeric)
		{
			return cls == LineBreakClass.PostfixNumeric;
		}
		return true;
	}

	/// <summary>
	/// LB23a: Do not break between numeric prefixes and ideographs, or between
	/// ideographs and numeric postfixes.
	/// </summary>
	private static RuleResult LB23a(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		LineBreakClass lineBreakClass = state.Current.LineBreakClass;
		LineBreakClass nextClass = state.NextClass;
		if (lineBreakClass == LineBreakClass.PrefixNumeric && IsIdEbEm(nextClass))
		{
			return RuleResult.NoBreak;
		}
		if (nextClass == LineBreakClass.PostfixNumeric && IsIdEbEm(lineBreakClass))
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB24: Do not break between numeric prefix/postfix and letters, or between
	/// letters and prefix/postfix.
	/// </summary>
	private static RuleResult LB24(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		LineBreakClass lineBreakClass = state.Current.LineBreakClass;
		LineBreakClass nextClass = state.NextClass;
		if (IsPrPo(lineBreakClass) && (nextClass == LineBreakClass.Alphabetic || nextClass == LineBreakClass.HebrewLetter))
		{
			return RuleResult.NoBreak;
		}
		if ((lineBreakClass == LineBreakClass.Alphabetic || lineBreakClass == LineBreakClass.HebrewLetter) && IsPrPo(nextClass))
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB25: Do not break between the following pairs of classes relevant to numbers
	/// </summary>
	private static RuleResult LB25(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		switch (state.NextClass)
		{
		case LineBreakClass.PrefixNumeric:
			switch (state.Current.LineBreakClass)
			{
			case LineBreakClass.CloseParenthesis:
				switch (state.PreviousClass)
				{
				case LineBreakClass.Numeric:
					return RuleResult.NoBreak;
				case LineBreakClass.BreakSymbols:
				case LineBreakClass.InfixNumeric:
					if (LineBreakState.Before(text, state.Previous).LineBreakClass == LineBreakClass.Numeric)
					{
						return RuleResult.NoBreak;
					}
					break;
				}
				break;
			case LineBreakClass.Numeric:
				return RuleResult.NoBreak;
			case LineBreakClass.BreakSymbols:
			case LineBreakClass.InfixNumeric:
				if (state.PreviousClass == LineBreakClass.Numeric)
				{
					return RuleResult.NoBreak;
				}
				break;
			case LineBreakClass.ClosePunctuation:
				switch (state.PreviousClass)
				{
				case LineBreakClass.Numeric:
					return RuleResult.NoBreak;
				case LineBreakClass.BreakSymbols:
				case LineBreakClass.InfixNumeric:
					if (state.PreviousClass == LineBreakClass.Numeric)
					{
						return RuleResult.NoBreak;
					}
					break;
				}
				break;
			}
			break;
		case LineBreakClass.Numeric:
			switch (state.Current.LineBreakClass)
			{
			case LineBreakClass.Numeric:
				return RuleResult.NoBreak;
			case LineBreakClass.BreakSymbols:
			case LineBreakClass.InfixNumeric:
				if (state.PreviousClass == LineBreakClass.Numeric)
				{
					return RuleResult.NoBreak;
				}
				break;
			}
			break;
		case LineBreakClass.PostfixNumeric:
			switch (state.Current.LineBreakClass)
			{
			case LineBreakClass.ClosePunctuation:
				switch (state.PreviousClass)
				{
				case LineBreakClass.Numeric:
					return RuleResult.NoBreak;
				case LineBreakClass.BreakSymbols:
				case LineBreakClass.InfixNumeric:
					if (state.PreviousClass == LineBreakClass.Numeric)
					{
						return RuleResult.NoBreak;
					}
					break;
				}
				break;
			case LineBreakClass.Numeric:
				return RuleResult.NoBreak;
			case LineBreakClass.BreakSymbols:
			case LineBreakClass.InfixNumeric:
				if (state.PreviousClass == LineBreakClass.Numeric)
				{
					return RuleResult.NoBreak;
				}
				break;
			}
			break;
		}
		if (state.Current.LineBreakClass == LineBreakClass.PrefixNumeric)
		{
			switch (state.NextClass)
			{
			case LineBreakClass.OpenPunctuation:
			{
				BreakUnit current = LineBreakState.After(text, state.Next(text));
				if (current.LineBreakClass == LineBreakClass.Numeric)
				{
					return RuleResult.NoBreak;
				}
				if (current.LineBreakClass == LineBreakClass.InfixNumeric && LineBreakState.After(text, current).LineBreakClass == LineBreakClass.Numeric)
				{
					return RuleResult.NoBreak;
				}
				break;
			}
			case LineBreakClass.Numeric:
				return RuleResult.NoBreak;
			}
		}
		if (state.Current.LineBreakClass == LineBreakClass.PostfixNumeric)
		{
			switch (state.NextClass)
			{
			case LineBreakClass.OpenPunctuation:
			{
				BreakUnit current2 = LineBreakState.After(text, state.Next(text));
				if (current2.LineBreakClass == LineBreakClass.Numeric)
				{
					return RuleResult.NoBreak;
				}
				if (current2.LineBreakClass == LineBreakClass.InfixNumeric && LineBreakState.After(text, current2).LineBreakClass == LineBreakClass.Numeric)
				{
					return RuleResult.NoBreak;
				}
				break;
			}
			case LineBreakClass.Numeric:
				return RuleResult.NoBreak;
			}
		}
		LineBreakClass lineBreakClass = state.Current.LineBreakClass;
		if ((lineBreakClass == LineBreakClass.InfixNumeric || lineBreakClass == LineBreakClass.Hyphen) && state.NextClass == LineBreakClass.Numeric)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB26: Do not break a Korean syllable.
	/// </summary>
	private static RuleResult LB26(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		switch (state.Current.LineBreakClass)
		{
		case LineBreakClass.JL:
		{
			LineBreakClass nextClass = state.NextClass;
			if ((uint)(nextClass - 23) <= 3u)
			{
				return RuleResult.NoBreak;
			}
			break;
		}
		case LineBreakClass.H2:
		case LineBreakClass.JV:
		{
			LineBreakClass nextClass = state.NextClass;
			if ((uint)(nextClass - 26) <= 1u)
			{
				return RuleResult.NoBreak;
			}
			break;
		}
		case LineBreakClass.H3:
		case LineBreakClass.JT:
			if (state.NextClass == LineBreakClass.JT)
			{
				return RuleResult.NoBreak;
			}
			break;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB27: Treat a Korean Syllable Block the same as ID.
	/// </summary>
	private static RuleResult LB27(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		switch (state.Current.LineBreakClass)
		{
		case LineBreakClass.H2:
		case LineBreakClass.H3:
		case LineBreakClass.JL:
		case LineBreakClass.JV:
		case LineBreakClass.JT:
			if (state.NextClass == LineBreakClass.PostfixNumeric)
			{
				return RuleResult.NoBreak;
			}
			break;
		case LineBreakClass.PrefixNumeric:
		{
			LineBreakClass nextClass = state.NextClass;
			if ((uint)(nextClass - 23) <= 4u)
			{
				return RuleResult.NoBreak;
			}
			break;
		}
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB28: Do not break between alphabetics (“at”).
	/// </summary>
	private static RuleResult LB28(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		LineBreakClass lineBreakClass = state.Current.LineBreakClass;
		if ((uint)(lineBreakClass - 12) <= 1u)
		{
			LineBreakClass nextClass = state.NextClass;
			if ((uint)(nextClass - 12) <= 1u)
			{
				return RuleResult.NoBreak;
			}
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB28a: Do not break inside the orthographic syllables of Brahmic scripts.
	/// </summary>
	private static RuleResult LB28a(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		BreakUnit breakUnit = (state.Current.Inherited ? state.LastBeforeWhitespace : state.Current);
		BreakUnit chr = (state.Current.Inherited ? LineBreakState.Before(text, breakUnit) : state.Previous);
		if (breakUnit.LineBreakClass == LineBreakClass.AksaraPrebase && isMatch(state.Next(text)))
		{
			return RuleResult.NoBreak;
		}
		if (isMatch(breakUnit) && (state.NextClass == LineBreakClass.ViramaFinal || state.NextClass == LineBreakClass.Virama))
		{
			return RuleResult.NoBreak;
		}
		if (isMatch(chr) && breakUnit.LineBreakClass == LineBreakClass.Virama && (state.NextClass == LineBreakClass.Aksara || (int)state.Next(text).Codepoint == 9676))
		{
			return RuleResult.NoBreak;
		}
		if (isMatch(breakUnit) && isMatch(state.Next(text)) && LineBreakState.After(text, state.Next(text)).LineBreakClass == LineBreakClass.ViramaFinal)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
		static bool isMatch(BreakUnit breakUnit2)
		{
			if (breakUnit2.LineBreakClass != LineBreakClass.Aksara && (int)breakUnit2.Codepoint != 9676)
			{
				return breakUnit2.LineBreakClass == LineBreakClass.AksaraStart;
			}
			return true;
		}
	}

	/// <summary>
	/// LB29: Do not break between numeric punctuation and alphabetics (“e.g.”).
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB29(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.Current.LineBreakClass == LineBreakClass.InfixNumeric && (state.NextClass == LineBreakClass.Alphabetic || state.NextClass == LineBreakClass.HebrewLetter))
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB30: Do not break between letters, numbers, or ordinary symbols and opening or closing parentheses.
	/// </summary>
	private static RuleResult LB30(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		switch (state.Current.LineBreakClass)
		{
		case LineBreakClass.Numeric:
		case LineBreakClass.Alphabetic:
		case LineBreakClass.HebrewLetter:
		{
			BreakUnit breakUnit = state.Next(text);
			if (breakUnit.LineBreakClass == LineBreakClass.OpenPunctuation && !breakUnit.Codepoint.IsEastAsian)
			{
				return RuleResult.NoBreak;
			}
			break;
		}
		case LineBreakClass.CloseParenthesis:
			if (!state.Current.Codepoint.IsEastAsian)
			{
				LineBreakClass nextClass = state.NextClass;
				if ((uint)(nextClass - 11) <= 2u)
				{
					return RuleResult.NoBreak;
				}
			}
			break;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB30a: Break between two regional indicator symbols if and only if there
	/// are an even number of regional indicators preceding the position of the
	/// break.
	/// </summary>
	/// <returns></returns>
	private static RuleResult LB30a(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		if (state.RegionalIndicator > 0 && state.NextClass == LineBreakClass.RegionalIndicator && state.RegionalIndicator + 1 == 2)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB30b: Do not break between an emoji base (or potential emoji) and an emoji modifier.
	/// </summary>
	/// <returns></returns>
	private static RuleResult LB30b(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		BreakUnit breakUnit = (state.Current.Inherited ? state.Previous : state.Current);
		if (breakUnit.LineBreakClass == LineBreakClass.EBase && state.NextClass == LineBreakClass.EModifier)
		{
			return RuleResult.NoBreak;
		}
		if (state.NextClass == LineBreakClass.EModifier && breakUnit.Codepoint.GraphemeBreakClass == GraphemeBreakClass.ExtendedPictographic && breakUnit.Codepoint.GeneralCategory == GeneralCategory.Unassigned)
		{
			return RuleResult.NoBreak;
		}
		return RuleResult.Pass;
	}

	/// <summary>
	/// LB31: Break everywhere else.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static RuleResult LB31(ReadOnlySpan<char> text, ref LineBreakState state)
	{
		return RuleResult.MayBreak;
	}
}
