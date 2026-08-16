using System.Globalization;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.Fonts;

/// <summary>
/// Script-aware hints used by <see cref="M:Avalonia.Media.Fonts.FontCollectionBase.TryMatchCharacter(System.Int32,Avalonia.Media.FontStyle,Avalonia.Media.FontWeight,Avalonia.Media.FontStretch,System.String,System.Globalization.CultureInfo,Avalonia.Media.Typeface@)" /> to
/// disambiguate locale-sensitive scripts (e.g. CJK), to provide deterministic probe
/// codepoints for scoring candidate fonts, and to decide whether a candidate font is
/// compatible with the caller's culture.
/// </summary>
internal static class FontFallbackScriptHints
{
	private static readonly OpenTypeTag Arab = OpenTypeTag.Parse("arab");

	private static readonly OpenTypeTag Syrc = OpenTypeTag.Parse("syrc");

	private static readonly OpenTypeTag Mong = OpenTypeTag.Parse("mong");

	private static readonly OpenTypeTag Thaa = OpenTypeTag.Parse("thaa");

	private static readonly OpenTypeTag Khmr = OpenTypeTag.Parse("khmr");

	private static readonly OpenTypeTag Tibt = OpenTypeTag.Parse("tibt");

	private static readonly OpenTypeTag Sinh = OpenTypeTag.Parse("sinh");

	private static readonly OpenTypeTag Dev2 = OpenTypeTag.Parse("dev2");

	private static readonly OpenTypeTag Deva = OpenTypeTag.Parse("deva");

	private static readonly OpenTypeTag Bng2 = OpenTypeTag.Parse("bng2");

	private static readonly OpenTypeTag Beng = OpenTypeTag.Parse("beng");

	private static readonly OpenTypeTag Gur2 = OpenTypeTag.Parse("gur2");

	private static readonly OpenTypeTag Guru = OpenTypeTag.Parse("guru");

	private static readonly OpenTypeTag Gjr2 = OpenTypeTag.Parse("gjr2");

	private static readonly OpenTypeTag Gujr = OpenTypeTag.Parse("gujr");

	private static readonly OpenTypeTag Ory2 = OpenTypeTag.Parse("ory2");

	private static readonly OpenTypeTag Orya = OpenTypeTag.Parse("orya");

	private static readonly OpenTypeTag Tml2 = OpenTypeTag.Parse("tml2");

	private static readonly OpenTypeTag Taml = OpenTypeTag.Parse("taml");

	private static readonly OpenTypeTag Tel2 = OpenTypeTag.Parse("tel2");

	private static readonly OpenTypeTag Telu = OpenTypeTag.Parse("telu");

	private static readonly OpenTypeTag Knd2 = OpenTypeTag.Parse("knd2");

	private static readonly OpenTypeTag Knda = OpenTypeTag.Parse("knda");

	private static readonly OpenTypeTag Mlm2 = OpenTypeTag.Parse("mlm2");

	private static readonly OpenTypeTag Mlym = OpenTypeTag.Parse("mlym");

	private static readonly OpenTypeTag Mym2 = OpenTypeTag.Parse("mym2");

	private static readonly OpenTypeTag Mymr = OpenTypeTag.Parse("mymr");

	/// <summary>
	/// Returns true when the script's preferred font typically depends on the user's
	/// culture (CJK Han is the canonical example: identical codepoints render with
	/// different fonts in zh-CN vs. ja-JP vs. ko-KR).
	/// </summary>
	public static bool IsLocaleSensitive(Script script)
	{
		return GetProbeCodepoint(script) != 0;
	}

	/// <summary>
	/// Refines an ambiguous script using the supplied codepoint's
	/// <see cref="M:Avalonia.Media.TextFormatting.Unicode.Codepoint.HasScriptExtension(Avalonia.Media.TextFormatting.Unicode.Script)" /> data and the requested culture.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The rules mirror what major shaping/font-matching stacks (DirectWrite/UniscribeExtensions,
	/// CoreText, HarfBuzz + ICU likelySubtags) do:
	/// </para>
	/// <list type="bullet">
	/// <item><description>
	/// <see cref="F:Avalonia.Media.TextFormatting.Unicode.Script.KatakanaOrHiragana" /> (Hrkt) is resolved using the codepoint's
	/// <c>Script_Extensions</c> set per UAX #24 — if it contains Hiragana the codepoint
	/// is treated as Hiragana, otherwise Katakana.
	/// </description></item>
	/// <item><description>
	/// <see cref="F:Avalonia.Media.TextFormatting.Unicode.Script.Han" /> is refined to the user's regional CJK variant via
	/// <see cref="M:Avalonia.Media.Fonts.Bcp47ScriptResolver.GetScriptSubtag(System.Globalization.CultureInfo)" />: <c>"Jpan"</c> selects Hiragana
	/// (which forces matching against a Japanese font), <c>"Kore"</c> selects Hangul,
	/// and <c>"Hans"/"Hant"</c> leave the script as Han.
	/// </description></item>
	/// <item><description>
	/// Any other script is returned unchanged.
	/// </description></item>
	/// </list>
	/// </remarks>
	public static Script RefineWithCulture(Codepoint codepoint, CultureInfo? culture)
	{
		Script script = codepoint.Script;
		switch (script)
		{
		case Script.KatakanaOrHiragana:
			if (!codepoint.HasScriptExtension(Script.Hiragana))
			{
				return Script.Katakana;
			}
			return Script.Hiragana;
		default:
			return script;
		case Script.Han:
		{
			string scriptSubtag = Bcp47ScriptResolver.GetScriptSubtag(culture);
			if (scriptSubtag == null)
			{
				return script;
			}
			if (!(scriptSubtag == "Jpan"))
			{
				if (scriptSubtag == "Kore")
				{
					return Script.Hangul;
				}
				return script;
			}
			return Script.Hiragana;
		}
		}
	}

	/// <summary>
	/// Determines whether <paramref name="candidate" /> self-declares coverage for the supplied
	/// culture's writing system. Used as a positive signal in font fallback scoring.
	/// </summary>
	/// <returns>
	/// <c>true</c> when the font advertises the culture's BCP-47 tag in its OpenType <c>meta</c>
	/// table (<c>dlng</c>/<c>slng</c>), or when its OS/2 codepage range bits cover the
	/// resolved script subtag's canonical codepage. Returns <c>false</c> when no positive
	/// signal exists; this is a non-negative test and should be combined with the probe-based
	/// fallback in <see cref="T:Avalonia.Media.Fonts.FontCollectionBase" />.
	/// </returns>
	public static bool IsFontCompatibleWithCulture(GlyphTypeface candidate, CultureInfo? culture)
	{
		if (culture == null || culture == CultureInfo.InvariantCulture)
		{
			return false;
		}
		if (candidate.DeclaresLanguageCoverage(culture))
		{
			return true;
		}
		FontCodePageCoverage codePageCoverage = candidate.CodePageCoverage;
		if (codePageCoverage == FontCodePageCoverage.None)
		{
			return false;
		}
		return Bcp47ScriptResolver.GetScriptSubtag(culture) switch
		{
			"Jpan" => (codePageCoverage & FontCodePageCoverage.JapaneseJis) != 0, 
			"Kore" => (codePageCoverage & (FontCodePageCoverage.KoreanWansung | FontCodePageCoverage.KoreanJohab)) != 0, 
			"Hans" => (codePageCoverage & FontCodePageCoverage.ChineseSimplified) != 0, 
			"Hant" => (codePageCoverage & FontCodePageCoverage.ChineseTraditional) != 0, 
			"Cyrl" => (codePageCoverage & FontCodePageCoverage.Cyrillic) != 0, 
			"Grek" => (codePageCoverage & FontCodePageCoverage.Greek) != 0, 
			"Arab" => (codePageCoverage & FontCodePageCoverage.Arabic) != 0, 
			"Hebr" => (codePageCoverage & FontCodePageCoverage.Hebrew) != 0, 
			"Thai" => (codePageCoverage & FontCodePageCoverage.Thai) != 0, 
			"Latn" => (codePageCoverage & FontCodePageCoverage.Latin1) != 0, 
			_ => false, 
		};
	}

	/// <summary>
	/// Returns a representative codepoint for the script that can be used to probe a
	/// candidate font's character-to-glyph map. Returns 0 for scripts without a probe
	/// (in which case the script is considered locale-insensitive).
	/// </summary>
	public static int GetProbeCodepoint(Script script)
	{
		return script switch
		{
			Script.Han => 20013, 
			Script.Hiragana => 12354, 
			Script.Katakana => 12450, 
			Script.KatakanaOrHiragana => 12354, 
			Script.Hangul => 44032, 
			Script.Bopomofo => 12549, 
			Script.Arabic => 1575, 
			Script.Hebrew => 1488, 
			Script.Devanagari => 2325, 
			Script.Bengali => 2453, 
			Script.Thai => 3585, 
			Script.Tibetan => 3904, 
			Script.Cyrillic => 1040, 
			Script.Greek => 913, 
			_ => 0, 
		};
	}

	/// <summary>
	/// Returns the bit index into the OS/2 ulUnicodeRange bitfield (UnicodeRange1..4) that the
	/// OpenType specification assigns to the supplied script, or -1 if the script has no
	/// canonical OS/2 bit. Bits 0..31 belong to UnicodeRange1, 32..63 to UnicodeRange2, etc.
	/// </summary>
	public static bool TryGetOS2Bit(Script script, out int bit)
	{
		bit = script switch
		{
			Script.Greek => 7, 
			Script.Cyrillic => 9, 
			Script.Hebrew => 11, 
			Script.Arabic => 13, 
			Script.Devanagari => 15, 
			Script.Bengali => 16, 
			Script.Thai => 24, 
			Script.Hiragana => 49, 
			Script.Katakana => 50, 
			Script.KatakanaOrHiragana => 49, 
			Script.Bopomofo => 51, 
			Script.Hangul => 56, 
			Script.Han => 59, 
			Script.Tibetan => 70, 
			_ => -1, 
		};
		return bit >= 0;
	}

	/// <summary>
	/// For scripts that require OpenType complex shaping (cursive joining, reordering, conjunct
	/// formation, dependent-vowel positioning, …), returns the GSUB/GPOS script tag(s) a font
	/// must declare in order to shape them. A font that maps the codepoints through cmap but
	/// declares none of these tags cannot shape the script and should be skipped during fallback.
	/// </summary>
	/// <returns>
	/// <c>true</c> with one or two acceptable script tags (Indic and Myanmar expose both a modern
	/// "dev2"-style tag and a legacy "deva"-style tag; for single-tag scripts
	/// <paramref name="secondary" /> equals <paramref name="primary" />). <c>false</c> for scripts
	/// that render acceptably from cmap alone (Latin, CJK, Hangul, Hebrew, Thai, …), for which no
	/// layout-table gate should apply.
	/// </returns>
	public static bool TryGetComplexShapingTags(Script script, out OpenTypeTag primary, out OpenTypeTag secondary)
	{
		switch (script)
		{
		case Script.Arabic:
			primary = (secondary = Arab);
			return true;
		case Script.Syriac:
			primary = (secondary = Syrc);
			return true;
		case Script.Mongolian:
			primary = (secondary = Mong);
			return true;
		case Script.Thaana:
			primary = (secondary = Thaa);
			return true;
		case Script.Khmer:
			primary = (secondary = Khmr);
			return true;
		case Script.Tibetan:
			primary = (secondary = Tibt);
			return true;
		case Script.Sinhala:
			primary = (secondary = Sinh);
			return true;
		case Script.Devanagari:
			primary = Dev2;
			secondary = Deva;
			return true;
		case Script.Bengali:
			primary = Bng2;
			secondary = Beng;
			return true;
		case Script.Gurmukhi:
			primary = Gur2;
			secondary = Guru;
			return true;
		case Script.Gujarati:
			primary = Gjr2;
			secondary = Gujr;
			return true;
		case Script.Oriya:
			primary = Ory2;
			secondary = Orya;
			return true;
		case Script.Tamil:
			primary = Tml2;
			secondary = Taml;
			return true;
		case Script.Telugu:
			primary = Tel2;
			secondary = Telu;
			return true;
		case Script.Kannada:
			primary = Knd2;
			secondary = Knda;
			return true;
		case Script.Malayalam:
			primary = Mlm2;
			secondary = Mlym;
			return true;
		case Script.Myanmar:
			primary = Mym2;
			secondary = Mymr;
			return true;
		default:
			primary = default(OpenTypeTag);
			secondary = default(OpenTypeTag);
			return false;
		}
	}
}
