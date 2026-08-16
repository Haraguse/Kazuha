using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.TextFormatting.Unicode;

/// <summary>
///     Helper for looking up unicode character class information.
///     This file contains only the packing-layout constants; the trie-backed
///     lookup helpers live in <c>UnicodeData.Lookups.cs</c>. The split lets
///     the trie generator tool consume the constants without needing the
///     generated <c>*Trie</c> classes (which it produces).
/// </summary>
internal static class UnicodeData
{
	internal const int CATEGORY_BITS = 6;

	internal const int SCRIPT_BITS = 8;

	internal const int LINEBREAK_BITS = 6;

	internal const int WORDBREAK_BITS = 5;

	internal const int GRAPHEMEBREAK_BITS = 5;

	internal const int INDICCONJUNCTBREAK_BITS = 2;

	internal const int SCRIPTEXTENSIONS_BITS = 7;

	internal const int BIDIPAIREDBRACKED_BITS = 16;

	internal const int BIDIPAIREDBRACKEDTYPE_BITS = 2;

	internal const int BIDICLASS_BITS = 5;

	internal const int SCRIPT_SHIFT = 6;

	internal const int LINEBREAK_SHIFT = 14;

	internal const int WORDBREAK_SHIFT = 20;

	internal const int SCRIPTEXTENSIONS_SHIFT = 25;

	internal const int INDICCONJUNCTBREAK_SHIFT = 5;

	internal const int BIDIPAIREDBRACKEDTYPE_SHIFT = 16;

	internal const int BIDICLASS_SHIFT = 18;

	internal const int CATEGORY_MASK = 63;

	internal const int SCRIPT_MASK = 255;

	internal const int LINEBREAK_MASK = 63;

	internal const int WORDBREAK_MASK = 31;

	internal const int GRAPHEMEBREAK_MASK = 31;

	internal const int INDICCONJUNCTBREAK_MASK = 3;

	internal const int SCRIPTEXTENSIONS_MASK = 127;

	internal const int BIDIPAIREDBRACKED_MASK = 65535;

	internal const int BIDIPAIREDBRACKEDTYPE_MASK = 3;

	internal const int BIDICLASS_MASK = 31;

	private static ReadOnlySpan<ushort> s_scriptExtensionSetOffsets => new ushort[120]
	{
		0, 0, 16, 23, 25, 27, 29, 37, 45, 49,
		54, 65, 71, 76, 85, 96, 98, 101, 105, 108,
		110, 112, 115, 119, 125, 129, 131, 134, 137, 144,
		145, 147, 150, 151, 153, 155, 157, 159, 162, 169,
		172, 180, 189, 191, 194, 196, 211, 224, 245, 268,
		272, 275, 277, 279, 281, 284, 287, 290, 291, 295,
		297, 301, 302, 305, 310, 313, 316, 320, 322, 328,
		330, 333, 336, 339, 341, 345, 356, 358, 362, 363,
		364, 367, 368, 371, 373, 379, 383, 386, 388, 390,
		397, 398, 401, 403, 410, 418, 423, 424, 432, 441,
		447, 449, 451, 454, 456, 472, 487, 498, 510, 513,
		515, 518, 520, 522, 524, 527, 529, 532, 534, 536
	};

	private static ReadOnlySpan<byte> s_scriptExtensionSets => new byte[536]
	{
		9, 24, 28, 36, 38, 42, 43, 44, 46, 48,
		53, 77, 84, 85, 118, 132, 14, 31, 32, 77,
		82, 158, 164, 17, 77, 77, 82, 77, 158, 26,
		28, 31, 48, 77, 118, 143, 148, 26, 31, 48,
		77, 113, 143, 148, 162, 26, 31, 77, 155, 43,
		77, 143, 145, 158, 4, 26, 28, 31, 46, 48,
		77, 113, 145, 155, 162, 28, 38, 43, 46, 66,
		77, 31, 48, 77, 118, 155, 28, 36, 56, 77,
		118, 145, 148, 155, 162, 8, 31, 36, 46, 48,
		56, 77, 118, 145, 148, 155, 77, 155, 36, 77,
		145, 26, 31, 77, 113, 26, 77, 148, 77, 143,
		40, 77, 31, 77, 162, 48, 77, 118, 162, 26,
		36, 66, 77, 145, 155, 26, 36, 77, 145, 77,
		145, 77, 143, 145, 26, 77, 145, 4, 26, 46,
		77, 143, 145, 158, 48, 77, 113, 4, 77, 162,
		77, 28, 48, 31, 118, 31, 43, 31, 77, 8,
		42, 43, 6, 41, 106, 126, 145, 157, 173, 6,
		145, 157, 3, 6, 41, 106, 126, 145, 157, 173,
		3, 6, 87, 88, 115, 121, 126, 138, 145, 6,
		145, 6, 157, 173, 6, 126, 14, 32, 47, 49,
		51, 72, 77, 94, 102, 105, 112, 133, 150, 154,
		160, 14, 32, 47, 49, 51, 72, 77, 94, 105,
		112, 150, 154, 160, 14, 32, 34, 44, 45, 47,
		49, 51, 72, 85, 94, 102, 110, 112, 136, 137,
		144, 147, 150, 154, 160, 14, 32, 34, 44, 45,
		47, 49, 50, 51, 72, 79, 85, 94, 102, 110,
		112, 136, 137, 144, 147, 150, 154, 160, 32, 34,
		74, 85, 14, 22, 144, 51, 99, 49, 70, 47,
		150, 72, 102, 165, 22, 100, 148, 42, 43, 77,
		127, 21, 54, 146, 156, 96, 119, 14, 32, 47,
		72, 32, 32, 47, 72, 14, 32, 105, 154, 160,
		14, 32, 154, 32, 105, 133, 14, 32, 105, 154,
		32, 133, 32, 72, 94, 112, 150, 154, 14, 32,
		32, 105, 160, 32, 102, 105, 14, 32, 133, 32,
		105, 14, 32, 105, 133, 14, 32, 47, 72, 94,
		102, 112, 137, 154, 160, 165, 32, 47, 32, 47,
		72, 165, 14, 102, 31, 77, 145, 145, 77, 96,
		119, 3, 6, 24, 42, 43, 62, 83, 111, 24,
		48, 62, 93, 32, 47, 77, 28, 77, 9, 111,
		9, 24, 42, 62, 74, 84, 128, 36, 3, 6,
		62, 53, 151, 17, 52, 53, 57, 66, 96, 174,
		17, 52, 53, 57, 66, 96, 119, 174, 17, 52,
		53, 57, 66, 53, 17, 52, 53, 57, 66, 96,
		159, 174, 17, 52, 53, 57, 66, 82, 96, 159,
		174, 17, 52, 53, 57, 66, 174, 17, 53, 57,
		66, 53, 57, 66, 53, 77, 32, 34, 49, 51,
		70, 72, 74, 85, 94, 95, 102, 133, 136, 147,
		160, 165, 32, 34, 49, 51, 70, 72, 74, 85,
		95, 102, 133, 136, 147, 160, 165, 32, 34, 49,
		51, 70, 74, 85, 95, 136, 147, 160, 32, 34,
		49, 51, 70, 74, 85, 95, 133, 136, 147, 160,
		14, 32, 165, 32, 150, 65, 77, 100, 20, 64,
		6, 106, 6, 157, 29, 30, 81, 30, 81, 30,
		80, 81, 6, 28, 88, 115
	};

	/// <summary>
	/// Gets the <see cref="T:Avalonia.Media.TextFormatting.Unicode.GeneralCategory" /> for a Unicode codepoint.
	/// </summary>
	/// <param name="codepoint">The codepoint in question.</param>
	/// <returns>The code point's general category.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static GeneralCategory GetGeneralCategory(uint codepoint)
	{
		return (GeneralCategory)(UnicodeDataTrie.Trie.Get(codepoint) & 0x3F);
	}

	/// <summary>
	/// Gets the <see cref="T:Avalonia.Media.TextFormatting.Unicode.Script" /> for a Unicode codepoint.
	/// </summary>
	/// <param name="codepoint">The codepoint in question.</param>
	/// <returns>The code point's script.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Script GetScript(uint codepoint)
	{
		return (Script)((UnicodeDataTrie.Trie.Get(codepoint) >> 6) & 0xFF);
	}

	/// <summary>
	/// Gets the <see cref="T:Avalonia.Media.TextFormatting.Unicode.BidiClass" /> for a Unicode codepoint.
	/// </summary>
	/// <param name="codepoint">The codepoint in question.</param>
	/// <returns>The code point's biDi class.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BidiClass GetBiDiClass(uint codepoint)
	{
		return (BidiClass)((BiDiTrie.Trie.Get(codepoint) >> 18) & 0x1F);
	}

	/// <summary>
	/// Gets the <see cref="T:Avalonia.Media.TextFormatting.Unicode.BidiPairedBracketType" /> for a Unicode codepoint.
	/// </summary>
	/// <param name="codepoint">The codepoint in question.</param>
	/// <returns>The code point's paired bracket type.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BidiPairedBracketType GetBiDiPairedBracketType(uint codepoint)
	{
		return (BidiPairedBracketType)((BiDiTrie.Trie.Get(codepoint) >> 16) & 3);
	}

	/// <summary>
	/// Gets the paired bracket for a Unicode codepoint.
	/// </summary>
	/// <param name="codepoint">The codepoint in question.</param>
	/// <returns>The code point's paired bracket.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Codepoint GetBiDiPairedBracket(uint codepoint)
	{
		return new Codepoint(BiDiTrie.Trie.Get(codepoint) & 0xFFFF);
	}

	/// <summary>
	/// Gets the line break class for a Unicode codepoint.
	/// </summary>
	/// <param name="codepoint">The codepoint in question.</param>
	/// <returns>The code point's line break class.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static LineBreakClass GetLineBreakClass(uint codepoint)
	{
		return (LineBreakClass)((UnicodeDataTrie.Trie.Get(codepoint) >> 14) & 0x3F);
	}

	/// <summary>
	/// Gets the word break class for a Unicode codepoint.
	/// </summary>
	/// <param name="codepoint">The codepoint in question.</param>
	/// <returns>The code point's word break class.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static WordBreakClass GetWordBreakClass(uint codepoint)
	{
		return (WordBreakClass)((UnicodeDataTrie.Trie.Get(codepoint) >> 20) & 0x1F);
	}

	/// <summary>
	/// Gets the grapheme break type for the Unicode codepoint.
	/// </summary>
	/// <param name="codepoint">The codepoint in question.</param>
	/// <returns>The code point's grapheme break type.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static GraphemeBreakClass GetGraphemeClusterBreak(uint codepoint)
	{
		return (GraphemeBreakClass)(GraphemeBreakTrie.Trie.Get(codepoint) & 0x1F);
	}

	/// <summary>
	/// Gets the Indic conjunct break class for the Unicode codepoint.
	/// </summary>
	/// <param name="codepoint">The codepoint in question.</param>
	/// <returns>The code point's Indic conjunct break class.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static IndicConjunctBreakClass GetIndicConjunctBreakClass(uint codepoint)
	{
		return (IndicConjunctBreakClass)((GraphemeBreakTrie.Trie.Get(codepoint) >> 5) & 3);
	}

	/// <summary>
	/// Gets the EastAsianWidth class for the Unicode codepoint.
	/// </summary>
	/// <param name="codepoint">The codepoint in question.</param>
	/// <returns>The code point's EastAsianWidth class.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EastAsianWidthClass GetEastAsianWidthClass(uint codepoint)
	{
		return (EastAsianWidthClass)EastAsianWidthTrie.Trie.Get(codepoint);
	}

	/// <summary>
	/// Determines whether the given codepoint's Script_Extensions property (UAX #24) contains
	/// the supplied script. When the codepoint has no explicit Script_Extensions entry the
	/// extensions set is taken to be the singleton of the codepoint's primary
	/// <see cref="T:Avalonia.Media.TextFormatting.Unicode.Script" /> property.
	/// </summary>
	/// <param name="codepoint">The codepoint in question.</param>
	/// <param name="script">The script being tested.</param>
	public static bool HasScriptExtension(uint codepoint, Script script)
	{
		if (script == Script.Unknown)
		{
			return false;
		}
		uint num = UnicodeDataTrie.Trie.Get(codepoint);
		int num2 = (int)((num >> 25) & 0x7F);
		if (num2 == 0)
		{
			return ((num >> 6) & 0xFF) == (uint)script;
		}
		ReadOnlySpan<ushort> readOnlySpan = s_scriptExtensionSetOffsets;
		if ((uint)(num2 + 1) >= (uint)readOnlySpan.Length)
		{
			return false;
		}
		ushort num3 = readOnlySpan[num2];
		ushort num4 = readOnlySpan[num2 + 1];
		ReadOnlySpan<byte> readOnlySpan2 = s_scriptExtensionSets;
		if (num4 > readOnlySpan2.Length || num3 > num4)
		{
			return false;
		}
		byte value = (byte)script;
		return readOnlySpan2.Slice(num3, num4 - num3).Contains(value);
	}
}
