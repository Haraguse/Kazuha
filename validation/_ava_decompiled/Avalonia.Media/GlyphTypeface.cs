using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Logging;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Cmap;
using Avalonia.Media.Fonts.Tables.Metrics;
using Avalonia.Media.Fonts.Tables.Name;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;

namespace Avalonia.Media;

/// <summary>
/// Represents a glyph typeface, providing access to font metrics, glyph mappings, and other font-related
/// properties.
/// </summary>
/// <remarks>The <see cref="T:Avalonia.Media.GlyphTypeface" /> class is used to encapsulate font data, including metrics,
/// character-to-glyph mappings, and supported OpenType features. It supports platform-specific typefaces and
/// applies optional font simulations such as bold or oblique. This class is typically used in text rendering and
/// shaping scenarios.</remarks>
public sealed class GlyphTypeface
{
	private static readonly IReadOnlyDictionary<CultureInfo, string> s_emptyStringDictionary = new Dictionary<CultureInfo, string>(0);

	private bool _isDisposed;

	private readonly NameTable? _nameTable;

	private readonly OS2Table _os2Table;

	private readonly CharacterToGlyphMap _cmapTable;

	private readonly HorizontalHeaderTable _hhTable;

	private readonly VerticalHeaderTable _vhTable;

	private readonly HorizontalMetricsTable? _hmTable;

	private readonly VerticalMetricsTable? _vmTable;

	private readonly bool _hasOs2Table;

	private readonly bool _hasHorizontalMetrics;

	private readonly bool _hasVerticalMetrics;

	private readonly string[] _designLanguages;

	private readonly string[] _supportedLanguages;

	private IReadOnlyList<OpenTypeTag>? _supportedFeatures;

	private ITextShaperTypeface? _textShaperTypeface;

	private UnicodeRange? _supportedUnicodeRange;

	private volatile HashSet<OpenTypeTag>? _shapingScriptTags;

	private bool _shapingScriptTagsUnknown;

	private readonly object _shapingScriptTagsLock = new object();

	/// <summary>
	/// Gets the family name of the font.
	/// </summary>
	public string FamilyName { get; }

	/// <summary>
	/// Gets the typographic family name of the font.
	/// </summary>
	public string TypographicFamilyName { get; }

	/// <summary>
	/// Gets a read-only mapping of localized culture-specific family names.
	/// </summary>
	/// <remarks>The dictionary contains entries for each supported culture, where the key is a <see cref="T:System.Globalization.CultureInfo" /> representing the culture, and the value is the corresponding localized family name. The
	/// dictionary may be empty if no family names are available.</remarks>
	public IReadOnlyDictionary<CultureInfo, string> FamilyNames { get; }

	/// <summary>
	/// Gets a read-only mapping of culture-specific face names.
	/// </summary>
	/// <remarks>Each entry in the dictionary maps a <see cref="T:System.Globalization.CultureInfo" /> to
	/// the corresponding localized face name. The dictionary is empty if no face names are defined.</remarks>
	public IReadOnlyDictionary<CultureInfo, string> FaceNames { get; }

	/// <summary>
	/// Gets a read-only mapping of Unicode character codes to glyph indices for the font.
	/// </summary>
	/// <remarks>This dictionary provides the correspondence between Unicode code points and the
	/// glyphs defined in the font. The mapping can be used to look up the glyph index for a given character when
	/// rendering or processing text. The set of mapped characters depends on the font's supported character
	/// set.</remarks>
	public CharacterToGlyphMap CharacterToGlyphMap => _cmapTable;

	/// <summary>
	/// Gets the font metrics associated with this font.
	/// </summary>
	public FontMetrics Metrics { get; }

	/// <summary>
	/// Gets the font weight.
	/// </summary>
	public FontWeight Weight { get; }

	/// <summary>
	/// Gets the font style.
	/// </summary>
	public FontStyle Style { get; }

	/// <summary>
	/// Gets the font stretch.
	/// </summary>
	public FontStretch Stretch { get; }

	/// <summary>
	/// Gets the font simulation settings applied to the <see cref="T:Avalonia.Media.GlyphTypeface" />.
	/// </summary>
	public FontSimulations FontSimulations { get; }

	/// <summary>
	/// Gets the number of glyphs held by this font.
	/// </summary>
	public int GlyphCount { get; }

	/// <summary>
	/// Gets the list of OpenType feature tags supported by the font.
	/// </summary>
	/// <remarks>The returned list reflects the features available in the underlying font and is
	/// read-only. The order of features in the list is not guaranteed. This property does not return null; if the
	/// font does not support any features, the list will be empty.</remarks>
	public IReadOnlyList<OpenTypeTag> SupportedFeatures
	{
		get
		{
			if (_supportedFeatures != null)
			{
				return _supportedFeatures;
			}
			_supportedFeatures = LoadSupportedFeatures();
			return _supportedFeatures;
		}
	}

	/// <summary>
	/// Gets the union of Unicode codepoint ranges covered by the font's character map.
	/// </summary>
	/// <remarks>
	/// The returned <see cref="T:Avalonia.Media.UnicodeRange" /> is derived from the cmap table and represents every
	/// codepoint for which the font defines a glyph. It is computed lazily on first access and cached
	/// for the lifetime of the <see cref="T:Avalonia.Media.GlyphTypeface" />. Prefer this property over enumerating
	/// <see cref="P:Avalonia.Media.GlyphTypeface.CharacterToGlyphMap" /> when only coverage information (not glyph IDs) is required.
	/// </remarks>
	public UnicodeRange SupportedUnicodeRange
	{
		get
		{
			if (_supportedUnicodeRange.HasValue)
			{
				return _supportedUnicodeRange.Value;
			}
			_supportedUnicodeRange = BuildSupportedUnicodeRange();
			return _supportedUnicodeRange.Value;
		}
	}

	/// <summary>
	/// Gets the codepage coverage advertised by the font via the OpenType
	/// <c>OS/2.ulCodePageRange1/2</c> bitfields.
	/// </summary>
	/// <remarks>
	/// Returns <see cref="F:Avalonia.Media.Fonts.FontCodePageCoverage.None" /> when the font does not ship an OS/2 table
	/// or only supplies an OS/2 version &lt; 1 (where the codepage range fields are not present).
	/// </remarks>
	public FontCodePageCoverage CodePageCoverage { get; }

	/// <summary>
	/// Gets the BCP-47 language tags the font's designer declared as the design target for the
	/// font (the <c>dlng</c> data tag in the OpenType <c>meta</c> table).
	/// </summary>
	/// <remarks>
	/// Returns an empty span when the font does not ship a <c>meta</c> table or omits the
	/// <c>dlng</c> data tag.
	/// </remarks>
	public ReadOnlySpan<string> DesignLanguages => _designLanguages;

	/// <summary>
	/// Gets the BCP-47 language tags the font advertises as supported (the <c>slng</c> data tag
	/// in the OpenType <c>meta</c> table).
	/// </summary>
	/// <remarks>
	/// Returns an empty span when the font does not ship a <c>meta</c> table or omits the
	/// <c>slng</c> data tag.
	/// </remarks>
	public ReadOnlySpan<string> SupportedLanguages => _supportedLanguages;

	/// <summary>
	/// Gets the platform-specific typeface associated with this font.
	/// </summary>
	public IPlatformTypeface PlatformTypeface { get; }

	/// <summary>
	/// Gets the typeface information used by the text shaper for this font.
	/// </summary>
	/// <remarks>The returned typeface is created on demand and cached for subsequent accesses. This
	/// property is typically used by text rendering components that require low-level font shaping
	/// details.</remarks>
	public ITextShaperTypeface TextShaperTypeface
	{
		get
		{
			if (_textShaperTypeface != null)
			{
				return _textShaperTypeface;
			}
			ITextShaperImpl requiredService = AvaloniaLocator.Current.GetRequiredService<ITextShaperImpl>();
			_textShaperTypeface = requiredService.CreateTypeface(this);
			return _textShaperTypeface;
		}
	}

	/// <summary>
	/// Gets whether the font should be used as a last resort, if no other fonts matched.
	/// </summary>
	internal bool IsLastResort { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.GlyphTypeface" /> class with the specified platform typeface and
	/// font simulations.
	/// </summary>
	/// <remarks>This constructor initializes the glyph typeface by loading various font tables,
	/// including OS/2, CMAP, and metrics tables, to calculate font metrics and other properties. It also determines
	/// font characteristics such as weight, style, stretch, and family names based on the provided typeface and
	/// font simulations.</remarks>
	/// <param name="typeface">The platform-specific typeface to be used for this <see cref="T:Avalonia.Media.GlyphTypeface" /> instance. This parameter
	/// cannot be <c>null</c>.</param>
	/// <param name="fontSimulations">The font simulations to apply, such as bold or oblique. The default is <see cref="F:Avalonia.Media.FontSimulations.None" />.</param>
	/// <exception cref="T:System.InvalidOperationException">Thrown if required font tables (e.g., 'maxp') cannot be loaded.</exception>
	public GlyphTypeface(IPlatformTypeface typeface, FontSimulations fontSimulations = FontSimulations.None)
	{
		PlatformTypeface = typeface;
		_hasOs2Table = OS2Table.TryLoad(this, out _os2Table);
		_cmapTable = CmapTable.Load(this);
		if (MetaTable.TryLoad(this, out var metaTable))
		{
			_designLanguages = metaTable.DesignLanguages;
			_supportedLanguages = metaTable.SupportedLanguages;
		}
		else
		{
			_designLanguages = Array.Empty<string>();
			_supportedLanguages = Array.Empty<string>();
		}
		if (_hasOs2Table && _os2Table.Version >= 1)
		{
			CodePageCoverage = (FontCodePageCoverage)(_os2Table.CodePageRange1 | ((ulong)_os2Table.CodePageRange2 << 32));
		}
		else
		{
			CodePageCoverage = FontCodePageCoverage.None;
		}
		GlyphCount = MaxpTable.Load(this).NumGlyphs;
		_hasHorizontalMetrics = HorizontalHeaderTable.TryLoad(this, out _hhTable);
		if (_hasHorizontalMetrics)
		{
			_hmTable = HorizontalMetricsTable.Load(this, _hhTable.NumberOfHMetrics, GlyphCount);
		}
		_hasVerticalMetrics = VerticalHeaderTable.TryLoad(this, out _vhTable);
		if (_hasVerticalMetrics)
		{
			_vmTable = VerticalMetricsTable.Load(this, _vhTable.NumberOfVMetrics, GlyphCount);
		}
		int num = 0;
		int num2 = 0;
		int lineGap = 0;
		if (_hasOs2Table && (_os2Table.Selection & OS2Table.FontSelectionFlags.USE_TYPO_METRICS) != 0)
		{
			num = -_os2Table.TypoAscender;
			num2 = -_os2Table.TypoDescender;
			lineGap = _os2Table.TypoLineGap;
		}
		else if (_hasHorizontalMetrics)
		{
			num = -_hhTable.Ascender;
			num2 = -_hhTable.Descender;
			lineGap = _hhTable.LineGap;
		}
		if (_hasOs2Table && (num == 0 || num2 == 0))
		{
			if (_os2Table.TypoAscender != 0 || _os2Table.TypoDescender != 0)
			{
				num = -_os2Table.TypoAscender;
				num2 = -_os2Table.TypoDescender;
				lineGap = _os2Table.TypoLineGap;
			}
			else
			{
				num = -_os2Table.WinAscent;
				num2 = _os2Table.WinDescent;
			}
		}
		HeadTable.TryLoad(this, out HeadTable headTable);
		IsLastResort = (headTable != null && (headTable.Flags & HeadFlags.LastResortFont) != 0) || _cmapTable.Format == CmapFormat.Format13;
		PostTable postTable = PostTable.Load(this);
		bool isFixedPitch = postTable.IsFixedPitch;
		short underlinePosition = postTable.UnderlinePosition;
		short underlineThickness = postTable.UnderlineThickness;
		ushort fontDesignEmHeight = GetFontDesignEmHeight(headTable);
		Metrics = new FontMetrics
		{
			DesignEmHeight = fontDesignEmHeight,
			Ascent = num,
			Descent = num2,
			LineGap = lineGap,
			UnderlinePosition = -underlinePosition,
			UnderlineThickness = underlineThickness,
			StrikethroughPosition = (_hasOs2Table ? (-_os2Table.StrikeoutPosition) : 0),
			StrikethroughThickness = (_hasOs2Table ? _os2Table.StrikeoutSize : 0),
			IsFixedPitch = isFixedPitch
		};
		FontSimulations = fontSimulations;
		FontWeight fontWeight = GetFontWeight(_hasOs2Table ? new OS2Table?(_os2Table) : ((OS2Table?)null), headTable);
		Weight = (((fontSimulations & FontSimulations.Bold) != FontSimulations.None) ? FontWeight.Bold : fontWeight);
		FontStyle fontStyle = GetFontStyle(_hasOs2Table ? new OS2Table?(_os2Table) : ((OS2Table?)null), headTable, postTable);
		Style = (((fontSimulations & FontSimulations.Oblique) != FontSimulations.None) ? FontStyle.Italic : fontStyle);
		Stretch = GetFontStretch(_hasOs2Table ? new OS2Table?(_os2Table) : ((OS2Table?)null));
		_nameTable = NameTable.Load(this);
		FamilyName = _nameTable?.FontFamilyName((ushort)CultureInfo.InvariantCulture.LCID) ?? "unknown";
		TypographicFamilyName = _nameTable?.GetNameById((ushort)CultureInfo.InvariantCulture.LCID, KnownNameIds.TypographicFamilyName) ?? FamilyName;
		if (_nameTable != null)
		{
			Dictionary<CultureInfo, string> dictionary = null;
			Dictionary<CultureInfo, string> dictionary2 = null;
			foreach (NameRecord item in _nameTable)
			{
				if (item.NameID == KnownNameIds.FontFamilyName)
				{
					if (item.Platform != Avalonia.Media.Fonts.Tables.PlatformID.Windows || item.LanguageID == 0)
					{
						continue;
					}
					CultureInfo key = GetCulture(item.LanguageID);
					if (dictionary == null)
					{
						dictionary = new Dictionary<CultureInfo, string>(1);
					}
					if (!dictionary.ContainsKey(key))
					{
						dictionary[key] = item.GetValue();
					}
				}
				if (item.NameID == KnownNameIds.FontSubfamilyName && item.Platform == Avalonia.Media.Fonts.Tables.PlatformID.Windows && item.LanguageID != 0)
				{
					CultureInfo key2 = GetCulture(item.LanguageID);
					if (dictionary2 == null)
					{
						dictionary2 = new Dictionary<CultureInfo, string>(1);
					}
					if (!dictionary2.ContainsKey(key2))
					{
						dictionary2[key2] = item.GetValue();
					}
				}
				static CultureInfo GetCulture(int lcid)
				{
					if (lcid == 65535)
					{
						return CultureInfo.InvariantCulture;
					}
					try
					{
						return CultureInfo.GetCultureInfo(lcid);
					}
					catch (CultureNotFoundException)
					{
						return CultureInfo.InvariantCulture;
					}
				}
			}
			IReadOnlyDictionary<CultureInfo, string> readOnlyDictionary = dictionary;
			FamilyNames = readOnlyDictionary ?? s_emptyStringDictionary;
			readOnlyDictionary = dictionary2;
			FaceNames = readOnlyDictionary ?? s_emptyStringDictionary;
		}
		else
		{
			FamilyNames = new Dictionary<CultureInfo, string> { 
			{
				CultureInfo.InvariantCulture,
				FamilyName
			} };
			FaceNames = new Dictionary<CultureInfo, string> { 
			{
				CultureInfo.InvariantCulture,
				Weight.ToString()
			} };
		}
	}

	private static ushort GetFontDesignEmHeight(HeadTable? headTable)
	{
		ushort num = headTable?.UnitsPerEm ?? 0;
		if (num == 0)
		{
			num = 2048;
		}
		return num;
	}

	internal static GlyphTypeface? TryCreate(IPlatformTypeface typeface, FontSimulations fontSimulations = FontSimulations.None)
	{
		try
		{
			return new GlyphTypeface(typeface, fontSimulations);
		}
		catch (Exception propertyValue)
		{
			Logger.TryGet(LogEventLevel.Warning, "Fonts")?.Log(null, "Could not create glyph typeface from platform typeface named {FamilyName} with simulations {Simulations}: {Exception}", typeface.FamilyName, fontSimulations, propertyValue);
			return null;
		}
	}

	/// <summary>
	/// Determines whether this font self-declares coverage for the supplied culture via its
	/// OpenType <c>meta</c> table <c>dlng</c> or <c>slng</c> tag list.
	/// </summary>
	/// <param name="culture">
	/// The culture to check. If <c>null</c> the method returns <c>false</c>.
	/// </param>
	/// <returns>
	/// <c>true</c> when one of the declared language tags is a BCP-47 prefix of the culture's
	/// <see cref="P:System.Globalization.CultureInfo.Name" /> (or vice versa, when the font specifies a narrower tag).
	/// </returns>
	/// <remarks>
	/// The match is case-insensitive and BCP-47-aware: the comparison succeeds when one tag is
	/// a prefix of the other up to a subtag boundary (e.g. <c>"ja"</c> matches <c>"ja-JP"</c>,
	/// and <c>"zh-Hans"</c> matches <c>"zh-Hans-CN"</c>). Returns <c>false</c> when the font
	/// declares no design or supported languages.
	/// </remarks>
	public bool DeclaresLanguageCoverage(CultureInfo? culture)
	{
		if (culture == null || culture == CultureInfo.InvariantCulture)
		{
			return false;
		}
		if (_designLanguages.Length == 0 && _supportedLanguages.Length == 0)
		{
			return false;
		}
		string name = culture.Name;
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		if (!MatchesAny(_designLanguages, name))
		{
			return MatchesAny(_supportedLanguages, name);
		}
		return true;
		static bool MatchesAny(string[] tags, string cultureName)
		{
			for (int i = 0; i < tags.Length; i++)
			{
				if (IsBcp47PrefixMatch(tags[i], cultureName))
				{
					return true;
				}
			}
			return false;
		}
	}

	private static bool IsBcp47PrefixMatch(string tag, string cultureName)
	{
		if (!IsPrefix(tag, cultureName))
		{
			return IsPrefix(cultureName, tag);
		}
		return true;
		static bool IsPrefix(string prefix, string candidate)
		{
			if (prefix.Length == 0 || prefix.Length > candidate.Length)
			{
				return false;
			}
			if (!candidate.AsSpan(0, prefix.Length).Equals(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (prefix.Length != candidate.Length && candidate[prefix.Length] != '-')
			{
				return candidate[prefix.Length] == '_';
			}
			return true;
		}
	}

	/// <summary>
	/// Determines whether the font advertises support for the supplied Unicode script.
	/// </summary>
	/// <remarks>
	/// When the font ships an OS/2 table the answer is taken from the OS/2 ulUnicodeRange bitfield
	/// (the font's own self-declaration of script coverage). When OS/2 is absent or the bit is unset,
	/// this falls back to probing the cmap with a representative codepoint for the script. Returns
	/// <c>true</c> for scripts that don't have a meaningful per-script signal (for example
	/// <see cref="F:Avalonia.Media.TextFormatting.Unicode.Script.Common" /> or <see cref="F:Avalonia.Media.TextFormatting.Unicode.Script.Unknown" />).
	/// </remarks>
	public bool SupportsScript(Script script)
	{
		if (!FontFallbackScriptHints.TryGetOS2Bit(script, out var bit) && FontFallbackScriptHints.GetProbeCodepoint(script) == 0)
		{
			return true;
		}
		if (_hasOs2Table && bit >= 0)
		{
			uint num = ((bit < 64) ? ((bit >= 32) ? _os2Table.UnicodeRange2 : _os2Table.UnicodeRange1) : ((bit >= 96) ? _os2Table.UnicodeRange4 : _os2Table.UnicodeRange3));
			if ((num & (uint)(1 << bit)) != 0)
			{
				return true;
			}
		}
		int probeCodepoint = FontFallbackScriptHints.GetProbeCodepoint(script);
		if (probeCodepoint != 0 && _cmapTable.TryGetGlyph(probeCodepoint, out var _))
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// Determines whether this font can <em>shape</em> the specified script, not merely map its
	/// codepoints. Scripts that need OpenType complex shaping (e.g. Arabic joining, Indic
	/// conjuncts) require the font to declare the script in its GSUB/GPOS tables; scripts that
	/// render acceptably from cmap alone always return <c>true</c>. Used by the fallback itemizer
	/// to avoid selecting a font that has the glyphs but cannot form them correctly.
	/// </summary>
	public bool CanShapeScript(Script script)
	{
		if (!FontFallbackScriptHints.TryGetComplexShapingTags(script, out var primary, out var secondary))
		{
			return true;
		}
		HashSet<OpenTypeTag> hashSet = EnsureShapingScriptTags();
		if (_shapingScriptTagsUnknown)
		{
			return true;
		}
		if (!hashSet.Contains(primary))
		{
			return hashSet.Contains(secondary);
		}
		return true;
	}

	private HashSet<OpenTypeTag> EnsureShapingScriptTags()
	{
		HashSet<OpenTypeTag> shapingScriptTags = _shapingScriptTags;
		if (shapingScriptTags != null)
		{
			return shapingScriptTags;
		}
		lock (_shapingScriptTagsLock)
		{
			if (_shapingScriptTags != null)
			{
				return _shapingScriptTags;
			}
			HashSet<OpenTypeTag> hashSet = new HashSet<OpenTypeTag>();
			_shapingScriptTagsUnknown = !ScriptListTable.TryReadScriptTags(this, hashSet);
			return _shapingScriptTags = hashSet;
		}
	}

	private UnicodeRange BuildSupportedUnicodeRange()
	{
		List<UnicodeRangeSegment> list = new List<UnicodeRangeSegment>();
		CodepointRangeEnumerator mappedRanges = _cmapTable.GetMappedRanges();
		while (mappedRanges.MoveNext())
		{
			CodepointRange current = mappedRanges.Current;
			list.Add(new UnicodeRangeSegment(current.Start, current.End));
		}
		if (list.Count == 0)
		{
			return new UnicodeRange(0, -1);
		}
		return new UnicodeRange(list);
	}

	/// <summary>
	/// Attempts to retrieve the horizontal advance width for the specified glyph.
	/// </summary>
	/// <remarks>Returns false if horizontal metrics are not available or if the specified glyph is
	/// not present in the metrics table.</remarks>
	/// <param name="glyphId">The identifier of the glyph for which to obtain the horizontal advance width.</param>
	/// <param name="advance">When this method returns, contains the horizontal advance width of the glyph if found; otherwise, zero. This
	/// parameter is passed uninitialized.</param>
	/// <returns>true if the horizontal advance width was successfully retrieved; otherwise, false.</returns>
	public bool TryGetHorizontalGlyphAdvance(ushort glyphId, out ushort advance)
	{
		advance = 0;
		if (!_hasHorizontalMetrics || _hmTable == null)
		{
			return false;
		}
		if (!_hmTable.TryGetAdvance(glyphId, out advance))
		{
			return false;
		}
		return true;
	}

	/// <summary>
	/// Attempts to retrieve horizontal advance widths for multiple glyphs in a single operation.
	/// </summary>
	/// <remarks>This method is significantly more efficient than calling <see cref="M:Avalonia.Media.GlyphTypeface.TryGetHorizontalGlyphAdvance(System.UInt16,System.UInt16@)" />
	/// multiple times as it minimizes memory access overhead and exploits data locality. This is the preferred method
	/// for batch glyph metrics retrieval in text layout and rendering scenarios. Returns false if horizontal metrics
	/// are not available.</remarks>
	/// <param name="glyphIds">Read-only span of glyph identifiers for which to retrieve advance widths.</param>
	/// <param name="advances">Output span to write the advance widths. Must be at least as long as <paramref name="glyphIds" />.</param>
	/// <returns>true if horizontal metrics are available and all advances were successfully retrieved; otherwise, false.</returns>
	public bool TryGetHorizontalGlyphAdvances(ReadOnlySpan<ushort> glyphIds, Span<ushort> advances)
	{
		if (!_hasHorizontalMetrics || _hmTable == null)
		{
			return false;
		}
		return _hmTable.TryGetAdvances(glyphIds, advances);
	}

	/// <summary>
	/// Attempts to retrieve the metrics for the specified glyph.
	/// </summary>
	/// <remarks>This method returns metrics only if horizontal or vertical metrics are available for
	/// the specified glyph. If neither is available, the method returns false and the output parameter is set to
	/// its default value.</remarks>
	/// <param name="glyph">The identifier of the glyph for which to obtain metrics.</param>
	/// <param name="metrics">When this method returns, contains the metrics for the specified glyph if found; otherwise, contains the
	/// default value.</param>
	/// <returns>true if metrics for the specified glyph are available; otherwise, false.</returns>
	public bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics)
	{
		metrics = default(GlyphMetrics);
		HorizontalGlyphMetric metric = default(HorizontalGlyphMetric);
		VerticalGlyphMetric metric2 = default(VerticalGlyphMetric);
		bool flag = false;
		bool flag2 = false;
		if (_hasHorizontalMetrics && _hmTable != null)
		{
			flag = _hmTable.TryGetMetrics(glyph, out metric);
		}
		if (_hasVerticalMetrics && _vmTable != null)
		{
			flag2 = _vmTable.TryGetMetrics(glyph, out metric2);
		}
		if (!flag && !flag2)
		{
			return false;
		}
		metrics = new GlyphMetrics
		{
			XBearing = metric.LeftSideBearing,
			YBearing = metric2.TopSideBearing,
			Width = metric.AdvanceWidth,
			Height = metric2.AdvanceHeight
		};
		return true;
	}

	/// <summary>
	/// Attempts to retrieve glyph metrics for multiple glyphs in a single operation.
	/// </summary>
	/// <remarks>This method is significantly more efficient than calling <see cref="M:Avalonia.Media.GlyphTypeface.TryGetGlyphMetrics(System.UInt16,Avalonia.Media.GlyphMetrics@)" />
	/// multiple times as it minimizes memory access overhead and exploits data locality. This is the preferred
	/// method for batch glyph metrics retrieval in text layout and rendering scenarios. Returns false if neither
	/// horizontal nor vertical metrics are available.</remarks>
	/// <param name="glyphIds">Read-only span of glyph identifiers for which to retrieve metrics.</param>
	/// <param name="metrics">Output span to write the glyph metrics. Must be at least as long as <paramref name="glyphIds" />.</param>
	/// <returns>true if metrics are available and all were successfully retrieved; otherwise, false.</returns>
	public bool TryGetGlyphMetrics(ReadOnlySpan<ushort> glyphIds, Span<GlyphMetrics> metrics)
	{
		if (metrics.Length < glyphIds.Length)
		{
			throw new ArgumentException("Output span must be at least as long as input span", "metrics");
		}
		if (!_hasHorizontalMetrics && !_hasVerticalMetrics)
		{
			return false;
		}
		Span<HorizontalGlyphMetric> span = ((glyphIds.Length > 256) ? ((Span<HorizontalGlyphMetric>)new HorizontalGlyphMetric[glyphIds.Length]) : stackalloc HorizontalGlyphMetric[glyphIds.Length]);
		Span<HorizontalGlyphMetric> metrics2 = span;
		Span<VerticalGlyphMetric> span2 = ((glyphIds.Length > 256) ? ((Span<VerticalGlyphMetric>)new VerticalGlyphMetric[glyphIds.Length]) : stackalloc VerticalGlyphMetric[glyphIds.Length]);
		Span<VerticalGlyphMetric> metrics3 = span2;
		bool flag = false;
		bool flag2 = false;
		if (_hasHorizontalMetrics && _hmTable != null)
		{
			flag = _hmTable.TryGetMetrics(glyphIds, metrics2);
		}
		if (_hasVerticalMetrics && _vmTable != null)
		{
			flag2 = _vmTable.TryGetMetrics(glyphIds, metrics3);
		}
		if (!flag && !flag2)
		{
			return false;
		}
		for (int i = 0; i < glyphIds.Length; i++)
		{
			metrics[i] = new GlyphMetrics
			{
				XBearing = (flag ? metrics2[i].LeftSideBearing : 0),
				YBearing = (flag2 ? metrics3[i].TopSideBearing : 0),
				Width = (ushort)(flag ? metrics2[i].AdvanceWidth : 0),
				Height = (ushort)(flag2 ? metrics3[i].AdvanceHeight : 0)
			};
		}
		return true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private IReadOnlyList<OpenTypeTag> LoadSupportedFeatures()
	{
		FeatureListTable featureListTable = FeatureListTable.LoadGPos(this);
		FeatureListTable featureListTable2 = FeatureListTable.LoadGSub(this);
		int num = (featureListTable?.Features.Count ?? 0) + (featureListTable2?.Features.Count ?? 0);
		if (num == 0)
		{
			return Array.Empty<OpenTypeTag>();
		}
		List<OpenTypeTag> list = new List<OpenTypeTag>(num);
		if (featureListTable != null)
		{
			foreach (OpenTypeTag feature in featureListTable.Features)
			{
				if (!list.Contains(feature))
				{
					list.Add(feature);
				}
			}
		}
		if (featureListTable2 != null)
		{
			foreach (OpenTypeTag feature2 in featureListTable2.Features)
			{
				if (!list.Contains(feature2))
				{
					list.Add(feature2);
				}
			}
		}
		return list;
	}

	private static FontStyle GetFontStyle(OS2Table? oS2Table, HeadTable? headTable, PostTable postTable)
	{
		bool flag = false;
		bool flag2 = false;
		if (oS2Table.HasValue)
		{
			flag = (oS2Table.Value.Selection & OS2Table.FontSelectionFlags.ITALIC) != 0;
			flag2 = (oS2Table.Value.Selection & OS2Table.FontSelectionFlags.OBLIQUE) != 0;
		}
		if (!flag && headTable != null)
		{
			flag = headTable.MacStyle.HasFlag(MacStyleFlags.Italic);
		}
		float italicAngle = postTable.ItalicAngle;
		if (flag2)
		{
			return FontStyle.Oblique;
		}
		if (Math.Abs(italicAngle) > 0.01f && !flag)
		{
			return FontStyle.Oblique;
		}
		if (flag)
		{
			return FontStyle.Italic;
		}
		return FontStyle.Normal;
	}

	private static FontWeight GetFontWeight(OS2Table? os2Table, HeadTable? headTable)
	{
		if (os2Table.HasValue && os2Table.Value.WeightClass >= 1 && os2Table.Value.WeightClass <= 1000)
		{
			return (FontWeight)os2Table.Value.WeightClass;
		}
		if (headTable != null && headTable.MacStyle.HasFlag(MacStyleFlags.Bold))
		{
			return FontWeight.Bold;
		}
		if (os2Table.HasValue && os2Table.Value.Panose.FamilyKind == PanoseFamilyKind.LatinText)
		{
			return os2Table.Value.Panose.Weight switch
			{
				PanoseWeight.VeryLight => FontWeight.Thin, 
				PanoseWeight.Light => FontWeight.Light, 
				PanoseWeight.Thin => FontWeight.ExtraLight, 
				PanoseWeight.Book => FontWeight.Normal, 
				PanoseWeight.Medium => FontWeight.Medium, 
				PanoseWeight.Demi => FontWeight.DemiBold, 
				PanoseWeight.Bold => FontWeight.Bold, 
				PanoseWeight.Heavy => FontWeight.ExtraBold, 
				PanoseWeight.Black => FontWeight.Black, 
				PanoseWeight.ExtraBlack => FontWeight.ExtraBlack, 
				_ => FontWeight.Normal, 
			};
		}
		return FontWeight.Normal;
	}

	private static FontStretch GetFontStretch(OS2Table? os2Table)
	{
		if (os2Table.HasValue && os2Table.Value.WidthClass >= 1 && os2Table.Value.WidthClass <= 9)
		{
			return (FontStretch)os2Table.Value.WidthClass;
		}
		if (os2Table.HasValue && os2Table.Value.Panose.FamilyKind == PanoseFamilyKind.LatinText)
		{
			switch (os2Table.Value.Panose.Proportion)
			{
			case PanoseProportion.VeryCondensed:
				return FontStretch.UltraCondensed;
			case PanoseProportion.Condensed:
				return FontStretch.Condensed;
			case PanoseProportion.OldStyle:
			case PanoseProportion.Modern:
			case PanoseProportion.EvenWidth:
				return FontStretch.Normal;
			case PanoseProportion.Extended:
				return FontStretch.Expanded;
			case PanoseProportion.VeryExtended:
				return FontStretch.UltraExpanded;
			case PanoseProportion.Monospaced:
				return FontStretch.Normal;
			default:
				return FontStretch.Normal;
			}
		}
		return FontStretch.Normal;
	}

	private void Dispose(bool disposing)
	{
		if (!_isDisposed)
		{
			_isDisposed = true;
			if (disposing)
			{
				PlatformTypeface.Dispose();
			}
		}
	}
}
