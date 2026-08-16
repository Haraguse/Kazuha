using System;
using System.Globalization;

namespace Avalonia.Media.Fonts;

/// <summary>
/// Resolves a BCP-47 culture identifier to the ISO 15924 script subtag that best represents
/// the writing system the user is requesting. Used by the font fallback algorithm to refine
/// ambiguous Unicode scripts (for example <see cref="F:Avalonia.Media.TextFormatting.Unicode.Script.Han" />)
/// into the regional variant the candidate font should advertise.
/// </summary>
/// <remarks>
/// The mapping follows the well-formed subset of
/// <see href="https://www.unicode.org/cldr/charts/latest/supplemental/likely_subtags.html">
/// CLDR likely subtags
/// </see>: explicit script subtags pass through, common region/language pairs are mapped to
/// their canonical script, and unrecognised inputs return <c>null</c>.
/// </remarks>
internal static class Bcp47ScriptResolver
{
	/// <summary>
	/// Returns the ISO 15924 script subtag (e.g. <c>"Jpan"</c>, <c>"Hans"</c>, <c>"Latn"</c>)
	/// implied by the supplied culture, or <c>null</c> when no script can be inferred.
	/// </summary>
	public static string? GetScriptSubtag(CultureInfo? culture)
	{
		if (culture == null || culture == CultureInfo.InvariantCulture)
		{
			return null;
		}
		string name = culture.Name;
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (TryExtractScriptSubtag(name, out string script))
		{
			return script;
		}
		ReadOnlySpan<char> readOnlySpan = name.AsSpan();
		int num = readOnlySpan.IndexOfAny('-', '_');
		ReadOnlySpan<char> a = ((num < 0) ? readOnlySpan : readOnlySpan.Slice(0, num));
		ReadOnlySpan<char> rest = ((num < 0) ? ReadOnlySpan<char>.Empty : readOnlySpan.Slice(num + 1));
		if (Eq(a, "ja"))
		{
			return "Jpan";
		}
		if (Eq(a, "ko"))
		{
			return "Kore";
		}
		if (Eq(a, "zh"))
		{
			if (IsTraditionalChineseRegion(rest))
			{
				return "Hant";
			}
			return "Hans";
		}
		if (Eq(a, "ru") || Eq(a, "uk") || Eq(a, "bg") || Eq(a, "be") || Eq(a, "sr") || Eq(a, "mk"))
		{
			return "Cyrl";
		}
		if (Eq(a, "en") || Eq(a, "de") || Eq(a, "fr") || Eq(a, "es") || Eq(a, "it") || Eq(a, "pt") || Eq(a, "nl") || Eq(a, "sv") || Eq(a, "no") || Eq(a, "da") || Eq(a, "fi") || Eq(a, "pl") || Eq(a, "cs") || Eq(a, "tr"))
		{
			return "Latn";
		}
		if (Eq(a, "ar") || Eq(a, "fa") || Eq(a, "ur"))
		{
			return "Arab";
		}
		if (Eq(a, "he") || Eq(a, "yi"))
		{
			return "Hebr";
		}
		if (Eq(a, "th"))
		{
			return "Thai";
		}
		if (Eq(a, "el"))
		{
			return "Grek";
		}
		return null;
	}

	private static bool TryExtractScriptSubtag(string name, out string? script)
	{
		ReadOnlySpan<char> span = name.AsSpan();
		int num = span.IndexOfAny('-', '_');
		while (num >= 0 && num + 1 < span.Length)
		{
			int num2 = num + 1;
			int num3 = span.Slice(num2).IndexOfAny('-', '_');
			if (((num3 < 0) ? (span.Length - num2) : num3) == 4 && IsAllAsciiLetters(span.Slice(num2, 4)))
			{
				Span<char> span2 = stackalloc char[4];
				span2[0] = char.ToUpperInvariant(span[num2]);
				span2[1] = char.ToLowerInvariant(span[num2 + 1]);
				span2[2] = char.ToLowerInvariant(span[num2 + 2]);
				span2[3] = char.ToLowerInvariant(span[num2 + 3]);
				script = new string(span2);
				return true;
			}
			if (num3 < 0)
			{
				break;
			}
			num = num2 + num3;
		}
		script = null;
		return false;
	}

	private static bool IsAllAsciiLetters(ReadOnlySpan<char> span)
	{
		ReadOnlySpan<char> readOnlySpan = span;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			if (!char.IsAsciiLetter(readOnlySpan[i]))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsTraditionalChineseRegion(ReadOnlySpan<char> rest)
	{
		while (!rest.IsEmpty)
		{
			int num = rest.IndexOfAny('-', '_');
			ReadOnlySpan<char> a = ((num < 0) ? rest : rest.Slice(0, num));
			if (Eq(a, "Hant"))
			{
				return true;
			}
			if (Eq(a, "Hans"))
			{
				return false;
			}
			if (Eq(a, "TW") || Eq(a, "HK") || Eq(a, "MO"))
			{
				return true;
			}
			if (Eq(a, "CN") || Eq(a, "SG") || Eq(a, "MY"))
			{
				return false;
			}
			if (num < 0)
			{
				break;
			}
			rest = rest.Slice(num + 1);
		}
		return false;
	}

	private static bool Eq(ReadOnlySpan<char> a, string b)
	{
		return a.Equals(b.AsSpan(), StringComparison.OrdinalIgnoreCase);
	}
}
