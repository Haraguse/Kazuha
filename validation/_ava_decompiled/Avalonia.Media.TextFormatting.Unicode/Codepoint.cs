using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.TextFormatting.Unicode;

public readonly record struct Codepoint
{
	/// <summary>
	/// The replacement codepoint that is used for non supported values.
	/// </summary>
	public static Codepoint ReplacementCodepoint
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return new Codepoint(65533u);
		}
	}

	/// <summary>
	/// Get the codepoint's value.
	/// </summary>
	public uint Value => _value;

	/// <summary>
	/// Gets the <see cref="T:Avalonia.Media.TextFormatting.Unicode.GeneralCategory" />.
	/// </summary>
	public GeneralCategory GeneralCategory => UnicodeData.GetGeneralCategory(_value);

	/// <summary>
	/// Gets the <see cref="T:Avalonia.Media.TextFormatting.Unicode.Script" />.
	/// </summary>
	public Script Script => UnicodeData.GetScript(_value);

	/// <summary>
	/// Gets the <see cref="T:Avalonia.Media.TextFormatting.Unicode.BidiClass" />.
	/// </summary>
	public BidiClass BiDiClass => UnicodeData.GetBiDiClass(_value);

	/// <summary>
	/// Gets the <see cref="T:Avalonia.Media.TextFormatting.Unicode.BidiPairedBracketType" />.
	/// </summary>
	public BidiPairedBracketType PairedBracketType => UnicodeData.GetBiDiPairedBracketType(_value);

	/// <summary>
	/// Gets the <see cref="T:Avalonia.Media.TextFormatting.Unicode.LineBreakClass" />.
	/// </summary>
	public LineBreakClass LineBreakClass => UnicodeData.GetLineBreakClass(_value);

	/// <summary>
	/// Gets the <see cref="T:Avalonia.Media.TextFormatting.Unicode.WordBreakClass" />.
	/// </summary>
	public WordBreakClass WordBreakClass => UnicodeData.GetWordBreakClass(_value);

	/// <summary>
	/// Gets the <see cref="P:Avalonia.Media.TextFormatting.Unicode.Codepoint.GraphemeBreakClass" />.
	/// </summary>
	public GraphemeBreakClass GraphemeBreakClass => UnicodeData.GetGraphemeClusterBreak(_value);

	/// <summary>
	/// Gets the <see cref="P:Avalonia.Media.TextFormatting.Unicode.Codepoint.EastAsianWidthClass" />.
	/// </summary>
	public EastAsianWidthClass EastAsianWidthClass => UnicodeData.GetEastAsianWidthClass(_value);

	/// <summary>
	/// Determines whether this <see cref="T:Avalonia.Media.TextFormatting.Unicode.Codepoint" /> is an east asian char.
	/// </summary>
	/// <returns>
	/// <c>true</c> if [is an east asian character]; otherwise, <c>false</c>.
	/// </returns>
	public bool IsEastAsian
	{
		get
		{
			EastAsianWidthClass eastAsianWidthClass = EastAsianWidthClass;
			if ((uint)(eastAsianWidthClass - 1) <= 1u || eastAsianWidthClass == EastAsianWidthClass.Wide)
			{
				return true;
			}
			return false;
		}
	}

	/// <summary>
	/// Determines whether this <see cref="T:Avalonia.Media.TextFormatting.Unicode.Codepoint" /> is a break char.
	/// </summary>
	/// <returns>
	/// <c>true</c> if [is break character]; otherwise, <c>false</c>.
	/// </returns>
	public bool IsBreakChar
	{
		get
		{
			uint value = _value;
			if (value - 10 <= 3 || value == 133 || value - 8232 <= 1)
			{
				return true;
			}
			return false;
		}
	}

	/// <summary>
	/// Determines whether this <see cref="T:Avalonia.Media.TextFormatting.Unicode.Codepoint" /> is white space.
	/// </summary>
	/// <returns>
	/// <c>true</c> if [is whitespace]; otherwise, <c>false</c>.
	/// </returns>
	public bool IsWhiteSpace
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ((1L << (int)GeneralCategory) & 0x2000000006L) != 0;
		}
	}

	private readonly uint _value;

	/// <summary>
	/// Creates a new instance of <see cref="T:Avalonia.Media.TextFormatting.Unicode.Codepoint" /> with the specified value.
	/// </summary>
	/// <param name="value">The codepoint value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Codepoint(uint value)
	{
		_value = value;
	}

	/// <summary>
	/// Determines whether the codepoint's Unicode Script_Extensions property contains
	/// <paramref name="script" />.
	/// </summary>
	/// <param name="script">The script to test.</param>
	/// <returns>
	/// <c>true</c> when the codepoint participates in <paramref name="script" /> per UAX #24
	/// (Script_Extensions); <c>false</c> otherwise.
	/// </returns>
	/// <remarks>
	/// Backed by the UCD <c>ScriptExtensions.txt</c> data baked into <see cref="T:Avalonia.Media.TextFormatting.Unicode.UnicodeData" />.
	/// Codepoints without an explicit Script_Extensions entry fall back to the singleton set
	/// of their primary <see cref="P:Avalonia.Media.TextFormatting.Unicode.Codepoint.Script" /> property.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool HasScriptExtension(Script script)
	{
		return UnicodeData.HasScriptExtension(_value, script);
	}

	/// <summary>
	/// Gets the canonical representation of a given codepoint.
	/// <see href="https://www.unicode.org/L2/L2013/13123-norm-and-bpa.pdf" />
	/// </summary>
	/// <param name="codePoint">The code point to be mapped.</param>
	/// <returns>The mapped canonical code point, or the passed <paramref name="codePoint" />.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Codepoint GetCanonicalType(Codepoint codePoint)
	{
		if (codePoint._value == 12296)
		{
			return new Codepoint(9001u);
		}
		if (codePoint._value == 12297)
		{
			return new Codepoint(9002u);
		}
		return codePoint;
	}

	/// <summary>
	/// Gets the codepoint representing the bracket pairing for this instance.
	/// </summary>
	/// <param name="codepoint">
	/// When this method returns, contains the codepoint representing the bracket pairing for this instance;
	/// otherwise, the default value for the type of the <paramref name="codepoint" /> parameter.
	/// This parameter is passed uninitialized.
	/// .</param>
	/// <returns><see langword="true" /> if this instance has a bracket pairing; otherwise, <see langword="false" /></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryGetPairedBracket(out Codepoint codepoint)
	{
		if (PairedBracketType == BidiPairedBracketType.None)
		{
			codepoint = default(Codepoint);
			return false;
		}
		codepoint = UnicodeData.GetBiDiPairedBracket(_value);
		return true;
	}

	public static implicit operator int(Codepoint codepoint)
	{
		return (int)codepoint._value;
	}

	public static implicit operator uint(Codepoint codepoint)
	{
		return codepoint._value;
	}

	/// <summary>
	/// Reads the <see cref="T:Avalonia.Media.TextFormatting.Unicode.Codepoint" /> at specified position.
	/// </summary>
	/// <param name="text">The buffer to read from.</param>
	/// <param name="index">The index to read at.</param>
	/// <param name="count">The count of character that were read.</param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static Codepoint ReadAt(ReadOnlySpan<char> text, int index, out int count)
	{
		count = 1;
		if ((uint)index >= (uint)text.Length)
		{
			return ReplacementCodepoint;
		}
		uint num = text[index];
		if (IsInRangeInclusive(num, 55296u, 57343u))
		{
			if (num <= 56319)
			{
				if ((uint)(index + 1) < (uint)text.Length)
				{
					uint num2 = num;
					uint num3 = text[index + 1];
					if (IsInRangeInclusive(num3, 56320u, 57343u))
					{
						count = 2;
						return new Codepoint((num2 << 10) + num3 - 56613888);
					}
				}
			}
			else if (index > 0)
			{
				uint num3 = num;
				uint num2 = text[index - 1];
				if (IsInRangeInclusive(num2, 55296u, 56319u))
				{
					count = 2;
					return new Codepoint((num2 << 10) + num3 - 56613888);
				}
			}
			return ReplacementCodepoint;
		}
		return new Codepoint(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsInRangeInclusive(uint value, uint lowerBound, uint upperBound)
	{
		return value - lowerBound <= upperBound - lowerBound;
	}

	/// <summary>
	/// Returns <see langword="true" /> if <paramref name="cp" /> is between
	/// <paramref name="lowerBound" /> and <paramref name="upperBound" />, inclusive.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInRangeInclusive(Codepoint cp, uint lowerBound, uint upperBound)
	{
		return IsInRangeInclusive(cp._value, lowerBound, upperBound);
	}
}
