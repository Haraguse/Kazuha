using System;
using Avalonia.Logging;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// A text run that holds text characters.
/// </summary>
public class TextCharacters : TextRun
{
	private const char WordJoiner = '\u2060';

	private static readonly string s_wordJoinerRun = new string('\u2060', 8);

	/// <inheritdoc />
	public override int Length => Text.Length;

	/// <inheritdoc />
	public override ReadOnlyMemory<char> Text { get; }

	/// <inheritdoc />
	public override TextRunProperties Properties { get; }

	/// <summary>
	/// Constructs a run for text content from a string.
	/// </summary>
	public TextCharacters(string text, TextRunProperties textRunProperties)
		: this(text.AsMemory(), textRunProperties)
	{
	}

	/// <summary>
	/// Constructs a run for text content from a memory region.
	/// </summary>
	public TextCharacters(ReadOnlyMemory<char> text, TextRunProperties textRunProperties)
	{
		if (textRunProperties.FontRenderingEmSize <= 0.0)
		{
			throw new ArgumentOutOfRangeException("textRunProperties", textRunProperties.FontRenderingEmSize, "Invalid FontRenderingEmSize");
		}
		Text = text;
		Properties = textRunProperties;
	}

	/// <summary>
	/// Gets a list of <see cref="T:Avalonia.Media.TextFormatting.UnshapedTextRun" />.
	/// </summary>
	/// <returns>The shapeable text characters.</returns>
	internal void GetShapeableCharacters(ReadOnlyMemory<char> text, sbyte biDiLevel, FontManager fontManager, ref TextRunProperties? previousProperties, FormattingObjectPool.RentedList<TextRun> results)
	{
		TextRunProperties properties = Properties;
		while (!text.IsEmpty)
		{
			UnshapedTextRun unshapedTextRun = CreateShapeableRun(text, properties, biDiLevel, fontManager, ref previousProperties);
			results.Add(unshapedTextRun);
			text = text.Slice(unshapedTextRun.Length);
			previousProperties = unshapedTextRun.Properties;
		}
	}

	/// <summary>
	/// Creates a shapeable text run with unique properties.
	/// </summary>
	/// <param name="text">The characters to create text runs from.</param>
	/// <param name="defaultProperties">The default text run properties.</param>
	/// <param name="biDiLevel">The bidi level of the run.</param>
	/// <param name="fontManager">The font manager to use.</param>
	/// <param name="previousProperties"></param>
	/// <returns>A list of shapeable text runs.</returns>
	private static UnshapedTextRun CreateShapeableRun(ReadOnlyMemory<char> text, TextRunProperties defaultProperties, sbyte biDiLevel, FontManager fontManager, ref TextRunProperties? previousProperties)
	{
		Typeface typeface = defaultProperties.Typeface;
		GlyphTypeface cachedGlyphTypeface = defaultProperties.CachedGlyphTypeface;
		Typeface? typeface2 = previousProperties?.Typeface;
		GlyphTypeface glyphTypeface = previousProperties?.CachedGlyphTypeface;
		ReadOnlySpan<char> span = text.Span;
		int length = 0;
		CodepointEnumerator codepointEnumerator = new CodepointEnumerator(span);
		Codepoint codepoint;
		while (codepointEnumerator.MoveNext(out codepoint) && codepoint.Value == 0)
		{
			length++;
		}
		if (length > 0)
		{
			return new UnshapedTextRun((length <= s_wordJoinerRun.Length) ? s_wordJoinerRun.AsMemory(0, length) : new string('\u2060', length).AsMemory(), defaultProperties, biDiLevel);
		}
		Script script = Codepoint.ReadAt(span, 0, out var _).Script;
		bool flag = true;
		if (glyphTypeface != null && !object.Equals(previousProperties.CultureInfo, defaultProperties.CultureInfo) && FontFallbackScriptHints.IsLocaleSensitive(script))
		{
			flag = false;
		}
		int num = ((!FontFallbackScriptHints.TryGetComplexShapingTags(script, out var _, out var _)) ? 1 : 2);
		for (int i = 0; i < num; i++)
		{
			bool flag2 = num == 2 && i == 0;
			Script shapingScript = (flag2 ? script : Script.Unknown);
			Typeface typeface3 = default(Typeface);
			GlyphTypeface glyphTypeface2 = null;
			bool flag3 = false;
			bool flag4 = !flag2 || cachedGlyphTypeface.CanShapeScript(script);
			GlyphTypeface defaultGlyphTypeface = (flag4 ? cachedGlyphTypeface : null);
			for (int j = 0; j < 2; j++)
			{
				bool requireFullCluster = j == 0;
				if (flag4 && TryGetShapeableLength(span, cachedGlyphTypeface, null, requireFullCluster, out length))
				{
					return new UnshapedTextRun(text.Slice(0, length), defaultProperties, biDiLevel);
				}
				if (flag && glyphTypeface != null && (!flag2 || glyphTypeface.CanShapeScript(script)) && TryGetShapeableLength(span, glyphTypeface, defaultGlyphTypeface, requireFullCluster, out length))
				{
					return new UnshapedTextRun(text.Slice(0, length), defaultProperties.WithTypeface(typeface2.Value), biDiLevel);
				}
				if (!flag3)
				{
					flag3 = true;
					Codepoint fallbackCodepoint = GetFallbackCodepoint(span, cachedGlyphTypeface);
					if (fontManager.TryMatchCharacter(fallbackCodepoint, typeface.Style, typeface.Weight, typeface.Stretch, typeface.FontFamily, defaultProperties.CultureInfo, shapingScript, out typeface3) && !fontManager.TryGetGlyphTypeface(typeface3, out glyphTypeface2))
					{
						Logger.TryGet(LogEventLevel.Warning, "Fonts")?.Log(null, "Matched fallback typeface {FamilyName} for codepoint U+{Codepoint} but could not load its glyph typeface.", typeface3.FontFamily.Name, ((uint)fallbackCodepoint).ToString("X4"));
					}
				}
				if (glyphTypeface2 != null && TryGetShapeableLength(span, glyphTypeface2, defaultGlyphTypeface, requireFullCluster, out length))
				{
					return new UnshapedTextRun(text.Slice(0, length), defaultProperties.WithTypeface(typeface3), biDiLevel);
				}
			}
		}
		GraphemeEnumerator graphemeEnumerator = new GraphemeEnumerator(span);
		Grapheme grapheme;
		while (graphemeEnumerator.MoveNext(out grapheme))
		{
			Codepoint firstCodepoint = grapheme.FirstCodepoint;
			if (!firstCodepoint.IsWhiteSpace && (cachedGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(firstCodepoint, out var _) || (length > 0 && fontManager.TryMatchCharacter(firstCodepoint, typeface.Style, typeface.Weight, typeface.Stretch, typeface.FontFamily, defaultProperties.CultureInfo, out var _))))
			{
				break;
			}
			length += grapheme.Length;
		}
		return new UnshapedTextRun(text.Slice(0, length), defaultProperties, biDiLevel);
	}

	/// <summary>
	/// Tries to get a shapeable length that is supported by the specified typeface.
	/// </summary>
	/// <param name="text">The characters to shape.</param>
	/// <param name="glyphTypeface">The typeface that is used to find matching characters.</param>
	/// <param name="defaultGlyphTypeface">The default typeface.</param>
	/// <param name="requireFullCluster">
	/// When <c>true</c>, a grapheme cluster only counts as supported when the typeface has a glyph
	/// for every scalar it contains (base plus combining marks); when <c>false</c>, only the base
	/// scalar is tested.
	/// </param>
	/// <param name="length">The shapeable length.</param>
	/// <returns></returns>
	internal static bool TryGetShapeableLength(ReadOnlySpan<char> text, GlyphTypeface glyphTypeface, GlyphTypeface? defaultGlyphTypeface, bool requireFullCluster, out int length)
	{
		length = 0;
		Script script = Script.Unknown;
		if (text.IsEmpty)
		{
			return false;
		}
		GraphemeEnumerator graphemeEnumerator = new GraphemeEnumerator(text);
		Grapheme grapheme;
		while (graphemeEnumerator.MoveNext(out grapheme))
		{
			Codepoint firstCodepoint = grapheme.FirstCodepoint;
			Script script2 = firstCodepoint.Script;
			if (firstCodepoint.Value == 0)
			{
				break;
			}
			ReadOnlySpan<char> clusterText = text.Slice(grapheme.Offset, grapheme.Length);
			if ((!firstCodepoint.IsWhiteSpace && defaultGlyphTypeface != null && ClusterIsCovered(clusterText, firstCodepoint, defaultGlyphTypeface, requireFullCluster)) || (!firstCodepoint.IsBreakChar && firstCodepoint.GeneralCategory != GeneralCategory.Control && !ClusterIsCovered(clusterText, firstCodepoint, glyphTypeface, requireFullCluster)))
			{
				break;
			}
			if (script2 != script)
			{
				bool flag = script == Script.Unknown;
				if (!flag)
				{
					bool flag2 = script2 != Script.Common;
					if (flag2)
					{
						bool flag3 = (uint)(script - 1) <= 1u;
						flag2 = flag3;
					}
					flag = flag2;
				}
				if (flag)
				{
					script = script2;
				}
				else if (script2 != Script.Inherited && script2 != Script.Common)
				{
					break;
				}
			}
			length += grapheme.Length;
		}
		return length > 0;
	}

	/// <summary>
	/// Determines whether <paramref name="glyphTypeface" /> can render the first grapheme cluster in
	/// <paramref name="clusterText" />. For a single-scalar cluster, or when
	/// <paramref name="requireFullCluster" /> is <c>false</c>, only the base scalar is tested.
	/// Otherwise every scalar that needs a glyph (excluding break chars and control/format
	/// codepoints) must be present, so a base+mark cluster is only covered by a font that has the
	/// marks too.
	/// </summary>
	private static bool ClusterIsCovered(ReadOnlySpan<char> clusterText, Codepoint firstCodepoint, GlyphTypeface glyphTypeface, bool requireFullCluster)
	{
		int num = ((firstCodepoint.Value <= 65535) ? 1 : 2);
		ushort glyphId;
		if (!requireFullCluster || clusterText.Length <= num)
		{
			return glyphTypeface.CharacterToGlyphMap.TryGetGlyph(firstCodepoint, out glyphId);
		}
		CodepointEnumerator codepointEnumerator = new CodepointEnumerator(clusterText);
		Codepoint codepoint;
		while (codepointEnumerator.MoveNext(out codepoint))
		{
			bool flag = codepoint.IsBreakChar;
			if (!flag)
			{
				GeneralCategory generalCategory = codepoint.GeneralCategory;
				bool flag2 = (uint)(generalCategory - 1) <= 1u;
				flag = flag2;
			}
			if (!flag && !glyphTypeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out glyphId))
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Returns the first scalar of the run's first grapheme cluster that
	/// <paramref name="defaultGlyphTypeface" /> cannot render - the base for an unsupported script,
	/// or a combining mark for an otherwise-supported cluster. Keying the fallback search on this
	/// lets it find a font for the mark, not just the base. Falls back to the cluster's first
	/// scalar when every scalar is already covered.
	/// </summary>
	private static Codepoint GetFallbackCodepoint(ReadOnlySpan<char> text, GlyphTypeface defaultGlyphTypeface)
	{
		if (!new GraphemeEnumerator(text).MoveNext(out var grapheme))
		{
			return Codepoint.ReplacementCodepoint;
		}
		CodepointEnumerator codepointEnumerator = new CodepointEnumerator(text.Slice(grapheme.Offset, grapheme.Length));
		Codepoint codepoint;
		while (codepointEnumerator.MoveNext(out codepoint))
		{
			bool flag = codepoint.IsBreakChar;
			if (!flag)
			{
				GeneralCategory generalCategory = codepoint.GeneralCategory;
				bool flag2 = (uint)(generalCategory - 1) <= 1u;
				flag = flag2;
			}
			if (!flag && !defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out var _))
			{
				return codepoint;
			}
		}
		return grapheme.FirstCodepoint;
	}
}
