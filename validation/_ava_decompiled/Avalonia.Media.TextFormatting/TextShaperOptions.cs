using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Options to customize text shaping.
/// </summary>
public readonly record struct TextShaperOptions
{
	/// <summary>
	/// Get the typeface.
	/// </summary>
	public GlyphTypeface GlyphTypeface { get; }

	/// <summary>
	/// Get the font rendering em size.
	/// </summary>
	public double FontRenderingEmSize { get; }

	/// <summary>
	/// Get the bidi level of the text.
	/// </summary>
	public sbyte BidiLevel { get; }

	/// <summary>
	/// Get the culture.
	/// </summary>
	public CultureInfo? Culture { get; }

	/// <summary>
	/// Get the incremental tab width.
	/// </summary>
	public double IncrementalTabWidth { get; }

	/// <summary>
	/// Get the letter spacing.
	/// </summary>
	public double LetterSpacing { get; }

	/// <summary>
	/// Get features.
	/// </summary>
	public IReadOnlyList<FontFeature>? FontFeatures { get; }

	public TextShaperOptions(GlyphTypeface typeface, double fontRenderingEmSize = 12.0, sbyte bidiLevel = 0, CultureInfo? culture = null, double incrementalTabWidth = 0.0, double letterSpacing = 0.0, IReadOnlyList<FontFeature>? fontFeatures = null)
	{
		GlyphTypeface = typeface;
		FontRenderingEmSize = fontRenderingEmSize;
		BidiLevel = bidiLevel;
		Culture = culture;
		IncrementalTabWidth = incrementalTabWidth;
		LetterSpacing = letterSpacing;
		FontFeatures = fontFeatures;
	}
}
