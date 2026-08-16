using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts;

public abstract class FontCollectionBase : IFontCollection, IReadOnlyList<FontFamily>, IEnumerable<FontFamily>, IEnumerable, IReadOnlyCollection<FontFamily>, IDisposable
{
	private readonly record struct ScriptFallbackKey(Script Script, string? CultureName);

	private static readonly Comparer<FontFamily> FontFamilyNameComparer = Comparer<FontFamily>.Create((FontFamily a, FontFamily b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

	internal readonly ConcurrentDictionary<string, ConcurrentDictionary<FontCollectionKey, GlyphTypeface?>> _glyphTypefaceCache = new ConcurrentDictionary<string, ConcurrentDictionary<FontCollectionKey, GlyphTypeface>>();

	private readonly ConcurrentDictionary<ScriptFallbackKey, string?> _scriptFallbackCache = new ConcurrentDictionary<ScriptFallbackKey, string>();

	private readonly object _fontFamiliesLock = new object();

	private volatile FontFamily[] _fontFamilies = Array.Empty<FontFamily>();

	private readonly IFontManagerImpl _fontManagerImpl;

	private readonly IAssetLoader _assetLoader;

	public abstract Uri Key { get; }

	public int Count => _fontFamilies.Length;

	public FontFamily this[int index] => _fontFamilies[index];

	protected FontCollectionBase()
	{
		_fontManagerImpl = AvaloniaLocator.Current.GetRequiredService<IFontManagerImpl>();
		_assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
	}

	public virtual bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch, string? familyName, CultureInfo? culture, out Typeface match)
	{
		return TryMatchCharacter(codepoint, style, weight, stretch, familyName, culture, Script.Unknown, out match);
	}

	/// <summary>
	/// Character-to-typeface match with an optional shaping-capability constraint. When
	/// <paramref name="shapingScript" /> is a complex script, only candidates that can shape it
	/// (<see cref="M:Avalonia.Media.GlyphTypeface.CanShapeScript(Avalonia.Media.TextFormatting.Unicode.Script)" />) are considered; <see cref="F:Avalonia.Media.TextFormatting.Unicode.Script.Unknown" />
	/// imposes no constraint and is identical to the public overload.
	/// </summary>
	internal bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch, string? familyName, CultureInfo? culture, Script shapingScript, out Typeface match)
	{
		match = default(Typeface);
		FontCollectionKey fontCollectionKey = new FontCollectionKey
		{
			Style = style,
			Weight = weight,
			Stretch = stretch
		};
		Codepoint codepoint2 = new Codepoint((uint)codepoint);
		Script script = codepoint2.Script;
		Script script2 = FontFallbackScriptHints.RefineWithCulture(codepoint2, culture);
		ScriptFallbackKey key = new ScriptFallbackKey(script2, culture?.Name);
		if (familyName != null && _glyphTypefaceCache.TryGetValue(familyName, out ConcurrentDictionary<FontCollectionKey, GlyphTypeface> value) && TryGetCoveringMatchForFamily(value, fontCollectionKey, codepoint, culture, shapingScript, out GlyphTypeface glyphTypeface) && IsCultureCompatible(glyphTypeface, culture, script))
		{
			match = BuildTypefaceWithSynthesis(glyphTypeface, fontCollectionKey);
			return true;
		}
		if ((FontFallbackScriptHints.IsLocaleSensitive(script2) || culture != null) && _scriptFallbackCache.TryGetValue(key, out string value2) && value2 != null && !string.Equals(value2, familyName, StringComparison.OrdinalIgnoreCase) && _glyphTypefaceCache.TryGetValue(value2, out ConcurrentDictionary<FontCollectionKey, GlyphTypeface> value3) && TryGetCoveringMatchForFamily(value3, fontCollectionKey, codepoint, culture, shapingScript, out GlyphTypeface glyphTypeface2))
		{
			match = BuildTypefaceWithSynthesis(glyphTypeface2, fontCollectionKey);
			return true;
		}
		if (TryMatchInCache(codepoint, fontCollectionKey, familyName, culture, script, script2, shapingScript, isLastResort: false, out GlyphTypeface bestGlyphTypeface, out string bestFamilyName))
		{
			bestGlyphTypeface = PreferExactKey(bestGlyphTypeface, fontCollectionKey, codepoint, culture, shapingScript);
			if (FontFallbackScriptHints.IsLocaleSensitive(script2) || culture != null)
			{
				_scriptFallbackCache.TryAdd(key, bestFamilyName);
			}
			match = BuildTypefaceWithSynthesis(bestGlyphTypeface, fontCollectionKey);
			return true;
		}
		if (shapingScript == Script.Unknown)
		{
			bool flag = _scriptFallbackCache.TryGetValue(key, out string value4);
			if ((!flag || value4 != null) && TryMatchCharacterFromPlatform(codepoint, fontCollectionKey, familyName, culture, out GlyphTypeface glyphTypeface3))
			{
				_scriptFallbackCache.TryAdd(key, glyphTypeface3.FamilyName);
				match = BuildTypefaceWithSynthesis(glyphTypeface3, fontCollectionKey);
				return true;
			}
			if (!flag)
			{
				_scriptFallbackCache.TryAdd(key, null);
			}
		}
		if (TryMatchInCache(codepoint, fontCollectionKey, familyName, culture, script, script2, shapingScript, isLastResort: true, out GlyphTypeface bestGlyphTypeface2, out string _))
		{
			match = BuildTypefaceWithSynthesis(bestGlyphTypeface2, fontCollectionKey);
			return true;
		}
		return false;
	}

	private bool TryMatchInCache(int codepoint, FontCollectionKey key, string? skipFamilyName, CultureInfo? culture, Script script, Script refinedScript, Script shapingScript, bool isLastResort, [NotNullWhen(true)] out GlyphTypeface? bestGlyphTypeface, [NotNullWhen(true)] out string? bestFamilyName)
	{
		bestGlyphTypeface = null;
		bestFamilyName = null;
		FontFamily[] fontFamilies = _fontFamilies;
		int num = int.MinValue;
		for (int i = 0; i < fontFamilies.Length; i++)
		{
			string name = fontFamilies[i].Name;
			if ((skipFamilyName == null || !string.Equals(name, skipFamilyName, StringComparison.OrdinalIgnoreCase)) && _glyphTypefaceCache.TryGetValue(name, out ConcurrentDictionary<FontCollectionKey, GlyphTypeface> value) && TryGetCoveringMatch(value, key, codepoint, isLastResort, shapingScript, out GlyphTypeface glyphTypeface))
			{
				int num2 = ScoreCandidate(glyphTypeface, key, culture, script, refinedScript);
				if (num2 > num)
				{
					num = num2;
					bestGlyphTypeface = glyphTypeface;
					bestFamilyName = name;
				}
			}
		}
		return bestGlyphTypeface != null;
	}

	private static int ScoreCandidate(GlyphTypeface candidate, FontCollectionKey requestedKey, CultureInfo? culture, Script script, Script refinedScript)
	{
		int num = 0;
		if (culture != null && candidate.FamilyNames.ContainsKey(culture))
		{
			num += 8;
		}
		else if (culture != null)
		{
			CultureInfo parent = culture.Parent;
			if (parent != null && parent != CultureInfo.InvariantCulture && candidate.FamilyNames.ContainsKey(parent))
			{
				num += 4;
			}
		}
		if (culture != null && FontFallbackScriptHints.IsFontCompatibleWithCulture(candidate, culture))
		{
			num += 4;
		}
		if (FontFallbackScriptHints.IsLocaleSensitive(refinedScript))
		{
			if (candidate.SupportsScript(refinedScript))
			{
				num += 2;
			}
			else if (refinedScript != script && candidate.SupportsScript(script))
			{
				num++;
			}
		}
		if (candidate.ToFontCollectionKey() == requestedKey)
		{
			num++;
		}
		return num;
	}

	private static bool IsCultureCompatible(GlyphTypeface candidate, CultureInfo? culture, Script script)
	{
		if (culture == null || !FontFallbackScriptHints.IsLocaleSensitive(script))
		{
			return true;
		}
		if (FontFallbackScriptHints.IsFontCompatibleWithCulture(candidate, culture))
		{
			return true;
		}
		if (candidate.FamilyNames.Count == 0)
		{
			return true;
		}
		if (candidate.FamilyNames.ContainsKey(culture))
		{
			return true;
		}
		CultureInfo parent = culture.Parent;
		if (parent != null && parent != CultureInfo.InvariantCulture && candidate.FamilyNames.ContainsKey(parent))
		{
			return true;
		}
		return false;
	}

	private Typeface BuildTypefaceWithSynthesis(GlyphTypeface glyphTypeface, FontCollectionKey requestedKey)
	{
		if (glyphTypeface.ToFontCollectionKey() != requestedKey)
		{
			TryAddGlyphTypeface(glyphTypeface.FamilyName, requestedKey, glyphTypeface);
		}
		return new Typeface(new FontFamily(null, Key.AbsoluteUri + "#" + glyphTypeface.FamilyName), requestedKey.Style, requestedKey.Weight, requestedKey.Stretch);
	}

	/// <summary>
	/// Hook for platform-backed collections (e.g. <see cref="T:Avalonia.Media.Fonts.SystemFontCollection" />) to consult
	/// the underlying font manager for a fallback typeface. Invoked at most once per
	/// (script-bucket, culture) pair from <see cref="M:Avalonia.Media.Fonts.FontCollectionBase.TryMatchCharacter(System.Int32,Avalonia.Media.FontStyle,Avalonia.Media.FontWeight,Avalonia.Media.FontStretch,System.String,System.Globalization.CultureInfo,Avalonia.Media.Typeface@)" />.
	/// </summary>
	protected virtual bool TryMatchCharacterFromPlatform(int codepoint, FontCollectionKey key, string? familyName, CultureInfo? culture, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		glyphTypeface = null;
		return false;
	}

	/// <summary>
	/// Resolves a covering face for a single family at the requested key. Takes the cheap cached
	/// covering match first (an exact-key hit needs nothing more) and only escalates to the exact
	/// key when that match differs in any axis, so a Bold (or Italic, or Condensed) face cached for
	/// one run is not reused for a differently-keyed request of the same family.
	/// </summary>
	private bool TryGetCoveringMatchForFamily(ConcurrentDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces, FontCollectionKey key, int codepoint, CultureInfo? culture, Script shapingScript, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		if (!TryGetCoveringMatch(glyphTypefaces, key, codepoint, isLastResort: false, shapingScript, out glyphTypeface))
		{
			return false;
		}
		glyphTypeface = PreferExactKey(glyphTypeface, key, codepoint, culture, shapingScript);
		return true;
	}

	/// <summary>
	/// When <paramref name="glyphTypeface" /> differs from the requested <paramref name="key" /> in
	/// any axis (style, weight or stretch), asks the platform for the exact-key face of the same
	/// family - the only source of a key the cache lacks - via <see cref="M:Avalonia.Media.Fonts.FontCollectionBase.TryMatchCharacterFromPlatform(System.Int32,Avalonia.Media.Fonts.FontCollectionKey,System.String,System.Globalization.CultureInfo,Avalonia.Media.GlyphTypeface@)" />,
	/// and returns it when it is an exact, shapeable match; otherwise returns the input unchanged.
	/// The platform is consulted only on a mismatch, and a collection without one keeps the
	/// neighbouring match. This guards every key axis, not just weight.
	/// </summary>
	private GlyphTypeface PreferExactKey(GlyphTypeface glyphTypeface, FontCollectionKey key, int codepoint, CultureInfo? culture, Script shapingScript)
	{
		if (glyphTypeface.ToFontCollectionKey() != key && TryMatchCharacterFromPlatform(codepoint, key, glyphTypeface.FamilyName, culture, out GlyphTypeface glyphTypeface2) && glyphTypeface2.ToFontCollectionKey() == key && CanShape(glyphTypeface2, shapingScript))
		{
			return glyphTypeface2;
		}
		return glyphTypeface;
	}

	/// <summary>
	/// Picks a variant of the family that both is close to the requested key and actually maps
	/// the requested codepoint. Falls back through the existing weight/stretch search but
	/// filters every candidate through the font's character-to-glyph map.
	/// </summary>
	private static bool CanShape(GlyphTypeface glyphTypeface, Script shapingScript)
	{
		if (shapingScript != Script.Unknown)
		{
			return glyphTypeface.CanShapeScript(shapingScript);
		}
		return true;
	}

	private static bool TryGetCoveringMatch(ConcurrentDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces, FontCollectionKey key, int codepoint, bool isLastResort, Script shapingScript, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		if (glyphTypefaces.TryGetValue(key, out glyphTypeface) && glyphTypeface != null && glyphTypeface.IsLastResort == isLastResort && glyphTypeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out var glyphId) && CanShape(glyphTypeface, shapingScript))
		{
			return true;
		}
		GlyphTypeface glyphTypeface2 = null;
		int num = int.MaxValue;
		FontCollectionKey[] array = glyphTypefaces.Keys.ToArray();
		Array.Sort(array);
		FontCollectionKey[] array2 = array;
		foreach (FontCollectionKey fontCollectionKey in array2)
		{
			if (glyphTypefaces.TryGetValue(fontCollectionKey, out GlyphTypeface value) && value != null && value.IsLastResort == isLastResort && value.CharacterToGlyphMap.TryGetGlyph(codepoint, out glyphId) && CanShape(value, shapingScript))
			{
				int num2 = KeyDistance(fontCollectionKey, key);
				if (num2 < num)
				{
					num = num2;
					glyphTypeface2 = value;
				}
			}
		}
		glyphTypeface = glyphTypeface2;
		return glyphTypeface != null;
	}

	private static int KeyDistance(FontCollectionKey a, FontCollectionKey b)
	{
		int num = a.Weight - b.Weight;
		if (num < 0)
		{
			num = -num;
		}
		int num2 = a.Stretch - b.Stretch;
		if (num2 < 0)
		{
			num2 = -num2;
		}
		int num3 = ((a.Style != b.Style) ? 1 : 0);
		return num + num2 * 100 + num3 * 10000;
	}

	public virtual bool TryCreateSyntheticGlyphTypeface(GlyphTypeface glyphTypeface, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface? syntheticGlyphTypeface)
	{
		syntheticGlyphTypeface = null;
		if (!_glyphTypefaceCache.TryGetValue(glyphTypeface.FamilyName, out ConcurrentDictionary<FontCollectionKey, GlyphTypeface> _))
		{
			return false;
		}
		FontCollectionKey fontCollectionKey = new FontCollectionKey(style, weight, stretch);
		if (glyphTypeface.ToFontCollectionKey() == fontCollectionKey)
		{
			return false;
		}
		FontSimulations fontSimulations = FontSimulations.None;
		if (style != FontStyle.Normal && glyphTypeface.Style != style)
		{
			fontSimulations |= FontSimulations.Oblique;
		}
		if (weight >= FontWeight.DemiBold && glyphTypeface.Weight < weight)
		{
			fontSimulations |= FontSimulations.Bold;
		}
		if (fontSimulations != FontSimulations.None && glyphTypeface.PlatformTypeface.TryGetStream(out Stream stream))
		{
			using (stream)
			{
				if (_fontManagerImpl.TryCreateGlyphTypeface(stream, fontSimulations, out IPlatformTypeface platformTypeface))
				{
					syntheticGlyphTypeface = GlyphTypeface.TryCreate(platformTypeface, fontSimulations);
					if (syntheticGlyphTypeface == null)
					{
						return false;
					}
					if (!string.IsNullOrEmpty(glyphTypeface.TypographicFamilyName))
					{
						TryAddGlyphTypeface(glyphTypeface.TypographicFamilyName, fontCollectionKey, syntheticGlyphTypeface);
					}
					foreach (KeyValuePair<CultureInfo, string> familyName in glyphTypeface.FamilyNames)
					{
						TryAddGlyphTypeface(familyName.Value, fontCollectionKey, syntheticGlyphTypeface);
					}
					return true;
				}
				return false;
			}
		}
		return false;
	}

	public IEnumerator<FontFamily> GetEnumerator()
	{
		return ((IEnumerable<FontFamily>)_fontFamilies).GetEnumerator();
	}

	public virtual bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		FontCollectionKey key = new Typeface(familyName, style, weight, stretch).Normalize(out familyName).ToFontCollectionKey();
		return TryGetGlyphTypeface(familyName, key, allowNearestMatch: true, out glyphTypeface);
	}

	public virtual bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
	{
		familyTypefaces = null;
		if (_glyphTypefaceCache.TryGetValue(familyName, out ConcurrentDictionary<FontCollectionKey, GlyphTypeface> value))
		{
			KeyValuePair<FontCollectionKey, GlyphTypeface>[] array = value.ToArray();
			Typeface[] array2 = new Typeface[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				FontCollectionKey key = array[i].Key;
				array2[i] = new Typeface(new FontFamily(Key?.ToString() + "#" + familyName), key.Style, key.Weight, key.Stretch);
			}
			familyTypefaces = array2;
			return true;
		}
		return false;
	}

	public bool TryGetNearestMatch(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		if (!_glyphTypefaceCache.TryGetValue(familyName, out ConcurrentDictionary<FontCollectionKey, GlyphTypeface> value))
		{
			glyphTypeface = null;
			return false;
		}
		FontCollectionKey key = new FontCollectionKey
		{
			Style = style,
			Weight = weight,
			Stretch = stretch
		};
		return TryGetNearestMatch(value, key, out glyphTypeface);
	}

	/// <summary>
	/// Attempts to add the specified <see cref="T:Avalonia.Media.GlyphTypeface" /> to the font collection.
	/// </summary>
	/// <remarks>This method checks the <see cref="P:Avalonia.Media.GlyphTypeface.FamilyName" /> and, if applicable,
	/// the typographic family name and other family names provided by the <see cref="T:Avalonia.Media.GlyphTypeface" /> interface.
	/// If any of these names can be associated with the glyph typeface, the typeface is added to the collection.
	/// The method ensures that duplicate entries are not added.</remarks>
	/// <param name="glyphTypeface">The glyph typeface to add. Must not be <see langword="null" /> and must have a non-empty <see cref="P:Avalonia.Media.GlyphTypeface.FamilyName" />.</param>
	/// <returns><see langword="true" /> if the glyph typeface was successfully added to the collection; otherwise, <see langword="false" />.</returns>
	public bool TryAddGlyphTypeface(GlyphTypeface glyphTypeface)
	{
		FontCollectionKey key = glyphTypeface.ToFontCollectionKey();
		return TryAddGlyphTypeface(glyphTypeface, key);
	}

	/// <summary>
	/// Attempts to add the specified glyph typeface to the collection using the provided key.
	/// </summary>
	/// <remarks>The method adds the glyph typeface using both its typographic family name and all
	/// available family names. If the glyph typeface or its family name is invalid, the method returns false and
	/// does not add the typeface.</remarks>
	/// <param name="glyphTypeface">The glyph typeface to add. Cannot be null, and its FamilyName property must not be null or empty.</param>
	/// <param name="key">The key that identifies the font collection to which the glyph typeface will be added.</param>
	/// <returns>true if the glyph typeface was successfully added to the collection; otherwise, false.</returns>
	public bool TryAddGlyphTypeface(GlyphTypeface glyphTypeface, FontCollectionKey key)
	{
		if (glyphTypeface == null || string.IsNullOrEmpty(glyphTypeface.FamilyName))
		{
			return false;
		}
		bool result = false;
		if (!string.IsNullOrEmpty(glyphTypeface.TypographicFamilyName) && TryAddGlyphTypeface(glyphTypeface.TypographicFamilyName, key, glyphTypeface))
		{
			result = true;
		}
		foreach (KeyValuePair<CultureInfo, string> familyName in glyphTypeface.FamilyNames)
		{
			if (TryAddGlyphTypeface(familyName.Value, key, glyphTypeface))
			{
				result = true;
			}
		}
		return result;
	}

	/// <summary>
	/// Attempts to add a glyph typeface from the specified font stream.
	/// </summary>
	/// <remarks>The method first attempts to create a glyph typeface from the provided font stream.
	/// If successful, it adds the created glyph typeface to the collection.</remarks>
	/// <param name="stream">The font stream containing the font data. The stream must be readable and positioned at the beginning of the
	/// font data.</param>
	/// <param name="glyphTypeface">When this method returns, contains the created <see cref="T:Avalonia.Media.GlyphTypeface" /> instance if the operation
	/// succeeds; otherwise, <see langword="null" />.</param>
	/// <returns><see langword="true" /> if the glyph typeface was successfully created and added; otherwise, <see langword="false" />.</returns>
	public bool TryAddGlyphTypeface(Stream stream, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		if (!_fontManagerImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out IPlatformTypeface platformTypeface))
		{
			glyphTypeface = null;
			return false;
		}
		glyphTypeface = GlyphTypeface.TryCreate(platformTypeface);
		if (glyphTypeface != null)
		{
			return TryAddGlyphTypeface(glyphTypeface);
		}
		return false;
	}

	/// <summary>
	/// Attempts to add a font source to the font collection.
	/// </summary>
	/// <remarks>This method processes the specified font source and attempts to load all available
	/// fonts from it.  Fonts are added to the collection based on their family name and typographic family name (if
	/// available). If the <paramref name="source" /> is <see langword="null" />, the method returns <see langword="false" />.</remarks>
	/// <param name="source">The URI of the font source to add. This can be a file path, a resource URI, or another valid font source
	/// URI.</param>
	/// <returns><see langword="true" /> if at least one font from the specified source was successfully added to the font
	/// collection;  otherwise, <see langword="false" />.</returns>
	public bool TryAddFontSource(Uri source)
	{
		if ((object)source == null)
		{
			return false;
		}
		bool result = false;
		switch (source.Scheme)
		{
		case "avares":
		case "resm":
			foreach (Uri item in FontFamilyLoader.LoadFontAssets(source))
			{
				Stream stream3 = _assetLoader.Open(item);
				if (!_fontManagerImpl.TryCreateGlyphTypeface(stream3, FontSimulations.None, out IPlatformTypeface platformTypeface3))
				{
					continue;
				}
				GlyphTypeface glyphTypeface3 = GlyphTypeface.TryCreate(platformTypeface3);
				if (glyphTypeface3 != null)
				{
					FontCollectionKey key = glyphTypeface3.ToFontCollectionKey();
					if (!string.IsNullOrEmpty(glyphTypeface3.TypographicFamilyName) && TryAddGlyphTypeface(glyphTypeface3.TypographicFamilyName, key, glyphTypeface3))
					{
						result = true;
					}
					if (TryAddGlyphTypeface(glyphTypeface3.FamilyName, key, glyphTypeface3))
					{
						result = true;
					}
				}
			}
			break;
		case "file":
			if (FontFamilyLoader.IsFontSource(source))
			{
				if (!File.Exists(source.LocalPath))
				{
					return false;
				}
				using (FileStream stream = File.OpenRead(source.LocalPath))
				{
					if (_fontManagerImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out IPlatformTypeface platformTypeface))
					{
						GlyphTypeface glyphTypeface = GlyphTypeface.TryCreate(platformTypeface);
						if (glyphTypeface != null && TryAddGlyphTypeface(glyphTypeface))
						{
							result = true;
						}
					}
				}
				break;
			}
			if (!Directory.Exists(source.LocalPath))
			{
				return false;
			}
			foreach (string item2 in Directory.EnumerateFiles(source.LocalPath))
			{
				if (!FontFamilyLoader.IsFontFile(item2))
				{
					continue;
				}
				using FileStream stream2 = File.OpenRead(item2);
				if (_fontManagerImpl.TryCreateGlyphTypeface(stream2, FontSimulations.None, out IPlatformTypeface platformTypeface2))
				{
					GlyphTypeface glyphTypeface2 = GlyphTypeface.TryCreate(platformTypeface2);
					if (glyphTypeface2 != null && TryAddGlyphTypeface(glyphTypeface2))
					{
						result = true;
					}
				}
			}
			break;
		default:
			return false;
		}
		return result;
	}

	/// <summary>
	/// Inserts the specified font family into the internal collection, maintaining the collection in sorted order
	/// by font family name.
	/// </summary>
	/// <remarks>If a font family with the same name already exists in the collection, the new
	/// instance will be inserted alongside it. The collection remains sorted after insertion.</remarks>
	/// <param name="fontFamily">The font family to add to the collection. Cannot be null.</param>
	protected void AddFontFamily(FontFamily fontFamily)
	{
		if (fontFamily == null)
		{
			throw new ArgumentNullException("fontFamily");
		}
		lock (_fontFamiliesLock)
		{
			FontFamily[] fontFamilies = _fontFamilies;
			int num = Array.BinarySearch(fontFamilies, fontFamily, FontFamilyNameComparer);
			if (num < 0)
			{
				num = ~num;
				FontFamily[] array = new FontFamily[fontFamilies.Length + 1];
				if (num > 0)
				{
					Array.Copy(fontFamilies, 0, array, 0, num);
				}
				array[num] = fontFamily;
				if (num < fontFamilies.Length)
				{
					Array.Copy(fontFamilies, num, array, num + 1, fontFamilies.Length - num);
				}
				_fontFamilies = array;
			}
		}
	}

	/// <summary>
	/// Attempts to retrieve a glyph typeface that matches the specified font family name and font collection key.
	/// </summary>
	/// <remarks>This method performs a binary search to locate font families with names that match
	/// the specified <paramref name="familyName" />. If multiple matches are found, the method iterates over them to
	/// find the best match based on the provided <paramref name="key" />.</remarks>
	/// <param name="familyName">The name of the font family to search for. This parameter is case-insensitive.</param>
	/// <param name="key">The key representing the desired font collection attributes.</param>
	/// <param name="allowNearestMatch">Whether to allow a nearest match (as opposed to only an exact match).</param>
	/// <param name="glyphTypeface">When this method returns, contains the matching <see cref="T:Avalonia.Media.GlyphTypeface" /> if a match is found; otherwise,
	/// <see langword="null" />.</param>
	/// <returns><see langword="true" /> if a matching glyph typeface is found; otherwise, <see langword="false" />.</returns>
	protected bool TryGetGlyphTypeface(string familyName, FontCollectionKey key, bool allowNearestMatch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		glyphTypeface = null;
		if (_glyphTypefaceCache.TryGetValue(familyName, out ConcurrentDictionary<FontCollectionKey, GlyphTypeface> value) && TryGetMatch(value, key, allowNearestMatch, out glyphTypeface, out var isNearestMatch))
		{
			FontCollectionKey fontCollectionKey = glyphTypeface.ToFontCollectionKey();
			if (isNearestMatch && fontCollectionKey != key)
			{
				if (TryCreateSyntheticGlyphTypeface(glyphTypeface, key.Style, key.Weight, key.Stretch, out GlyphTypeface syntheticGlyphTypeface))
				{
					glyphTypeface = syntheticGlyphTypeface;
				}
				else
				{
					TryAddGlyphTypeface(familyName, key, glyphTypeface);
				}
			}
			return true;
		}
		FontFamily[] fontFamilies = _fontFamilies;
		int num = 0;
		int num2 = fontFamilies.Length - 1;
		int num3 = -1;
		bool isNearestMatch2;
		while (num <= num2)
		{
			int num4 = (num + num2) / 2;
			int num5 = string.Compare(fontFamilies[num4].Name, familyName, StringComparison.OrdinalIgnoreCase);
			if (num5 < 0)
			{
				num = num4 + 1;
				continue;
			}
			if (num5 == 0)
			{
				if (_glyphTypefaceCache.TryGetValue(fontFamilies[num4].Name, out ConcurrentDictionary<FontCollectionKey, GlyphTypeface> value2) && TryGetMatch(value2, key, allowNearestMatch, out glyphTypeface, out isNearestMatch2))
				{
					return true;
				}
				return false;
			}
			if (fontFamilies[num4].Name.StartsWith(familyName, StringComparison.OrdinalIgnoreCase))
			{
				num3 = num4;
				num2 = num4 - 1;
			}
			else
			{
				num2 = num4 - 1;
			}
		}
		if (num3 != -1)
		{
			for (int i = num3; i < fontFamilies.Length; i++)
			{
				FontFamily fontFamily = fontFamilies[i];
				if (!fontFamily.Name.StartsWith(familyName, StringComparison.OrdinalIgnoreCase))
				{
					break;
				}
				if (_glyphTypefaceCache.TryGetValue(fontFamily.Name, out value) && TryGetMatch(value, key, allowNearestMatch, out glyphTypeface, out isNearestMatch2))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool TryGetMatch(IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces, FontCollectionKey key, bool allowNearestMatch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface, out bool isNearestMatch)
	{
		if (glyphTypefaces.TryGetValue(key, out glyphTypeface) && glyphTypeface != null)
		{
			isNearestMatch = false;
			return true;
		}
		if (allowNearestMatch && TryGetNearestMatch(glyphTypefaces, key, out glyphTypeface))
		{
			isNearestMatch = true;
			return true;
		}
		isNearestMatch = false;
		return false;
	}

	/// <summary>
	/// Attempts to retrieve the nearest matching <see cref="T:Avalonia.Media.GlyphTypeface" /> for the specified font key from the
	/// provided collection of glyph typefaces.
	/// </summary>
	/// <remarks>This method attempts to find the best match for the specified font key by considering
	/// various fallback strategies, such as normalizing the font style, stretch, and weight.
	/// If no suitable match is found, the method will return the first available non-null <see cref="T:Avalonia.Media.GlyphTypeface" /> from the
	/// collection, if any.</remarks>
	/// <param name="glyphTypefaces">A collection of glyph typefaces, indexed by <see cref="T:Avalonia.Media.Fonts.FontCollectionKey" />.</param>
	/// <param name="key">The <see cref="T:Avalonia.Media.Fonts.FontCollectionKey" /> representing the desired font attributes.</param>
	/// <param name="glyphTypeface">When this method returns, contains the <see cref="T:Avalonia.Media.GlyphTypeface" /> that most closely matches the specified
	/// key, if a match is found; otherwise, <see langword="null" />.</param>
	/// <returns><see langword="true" /> if a matching <see cref="T:Avalonia.Media.GlyphTypeface" /> is found; otherwise, <see langword="false" />.</returns>
	protected bool TryGetNearestMatch(IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces, FontCollectionKey key, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		if (!TryGetNearestMatchCore(glyphTypefaces, key, isLastResort: false, out glyphTypeface))
		{
			return TryGetNearestMatchCore(glyphTypefaces, key, isLastResort: true, out glyphTypeface);
		}
		return true;
	}

	private static bool TryGetNearestMatchCore(IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces, FontCollectionKey key, bool isLastResort, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		if (glyphTypefaces.TryGetValue(key, out glyphTypeface) && glyphTypeface != null && glyphTypeface.IsLastResort == isLastResort)
		{
			return true;
		}
		if (key.Style != FontStyle.Normal)
		{
			key = key with
			{
				Style = FontStyle.Normal
			};
		}
		if (key.Stretch != FontStretch.Normal)
		{
			if (TryFindStretchFallback(glyphTypefaces, key, isLastResort, out glyphTypeface))
			{
				return true;
			}
			if (key.Weight != FontWeight.Normal && TryFindStretchFallback(glyphTypefaces, key with
			{
				Weight = FontWeight.Normal
			}, isLastResort, out glyphTypeface))
			{
				return true;
			}
			key = key with
			{
				Stretch = FontStretch.Normal
			};
		}
		if (TryFindWeightFallback(glyphTypefaces, key, isLastResort, out glyphTypeface))
		{
			return true;
		}
		if (TryFindStretchFallback(glyphTypefaces, key, isLastResort, out glyphTypeface))
		{
			return true;
		}
		foreach (GlyphTypeface value in glyphTypefaces.Values)
		{
			if (value != null && isLastResort == value.IsLastResort)
			{
				glyphTypeface = value;
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Attempts to add a glyph typeface to the cache for the specified font family and key.
	/// </summary>
	/// <remarks>If the specified font family does not exist in the cache, it is added along with the
	/// glyph typeface. The method ensures that the font family is inserted in a sorted order within the internal
	/// collection.</remarks>
	/// <param name="familyName">The name of the font family to which the glyph typeface belongs. Cannot be null or empty.</param>
	/// <param name="key">The key associated with the glyph typeface in the cache.</param>
	/// <param name="glyphTypeface">The glyph typeface to add to the cache. Can be null.</param>
	/// <returns><see langword="true" /> if the glyph typeface was successfully added to the cache; otherwise, <see langword="false" />.</returns>
	protected bool TryAddGlyphTypeface(string familyName, FontCollectionKey key, GlyphTypeface? glyphTypeface)
	{
		if (string.IsNullOrEmpty(familyName))
		{
			return false;
		}
		if (_glyphTypefaceCache.TryGetValue(familyName, out ConcurrentDictionary<FontCollectionKey, GlyphTypeface> value))
		{
			if (value.TryGetValue(key, out var value2))
			{
				if (value2 == glyphTypeface || (value2 == null && glyphTypeface == null))
				{
					return true;
				}
				return false;
			}
			return value.TryAdd(key, glyphTypeface);
		}
		ConcurrentDictionary<FontCollectionKey, GlyphTypeface> concurrentDictionary = new ConcurrentDictionary<FontCollectionKey, GlyphTypeface>();
		ConcurrentDictionary<FontCollectionKey, GlyphTypeface> orAdd = _glyphTypefaceCache.GetOrAdd(familyName, concurrentDictionary);
		if (orAdd == concurrentDictionary)
		{
			FontFamily fontFamily = new FontFamily(Key?.ToString() + "#" + familyName);
			AddFontFamily(fontFamily);
		}
		if (orAdd.TryGetValue(key, out var value3))
		{
			if (value3 == glyphTypeface || (value3 == null && glyphTypeface == null))
			{
				return true;
			}
			return false;
		}
		return orAdd.TryAdd(key, glyphTypeface);
	}

	/// <summary>
	/// Attempts to locate a fallback glyph typeface with a similar font stretch to the specified key within the
	/// provided collection.
	/// </summary>
	/// <remarks>The search prioritizes font stretches closest to the requested value, expanding
	/// outward until a match is found or all options are exhausted.</remarks>
	/// <param name="glyphTypefaces">A dictionary mapping font collection keys to their corresponding glyph typefaces. Used as the source for
	/// searching fallback typefaces.</param>
	/// <param name="key">The font collection key specifying the desired font stretch and other font attributes to match.</param>
	/// <param name="isLastResort">Whether to match last resort fonts.</param>
	/// <param name="glyphTypeface">When this method returns, contains the found glyph typeface with a similar stretch if one exists; otherwise,
	/// null.</param>
	/// <returns>true if a suitable fallback glyph typeface is found; otherwise, false.</returns>
	private static bool TryFindStretchFallback(IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces, FontCollectionKey key, bool isLastResort, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		glyphTypeface = null;
		int stretch = (int)key.Stretch;
		if (stretch < 5)
		{
			for (int i = 0; stretch + i < 9; i++)
			{
				if (TryGetWithStretch(stretch + i, out glyphTypeface))
				{
					return true;
				}
			}
		}
		else
		{
			for (int j = 0; stretch - j > 1; j++)
			{
				if (TryGetWithStretch(stretch - j, out glyphTypeface))
				{
					return true;
				}
			}
		}
		return false;
		bool TryGetWithStretch(int effectiveStretch, [NotNullWhen(true)] out GlyphTypeface? reference)
		{
			if (glyphTypefaces.TryGetValue(key with
			{
				Stretch = (FontStretch)effectiveStretch
			}, out reference) && reference != null)
			{
				return reference.IsLastResort == isLastResort;
			}
			return false;
		}
	}

	/// <summary>
	/// Attempts to locate a fallback glyph typeface in the specified collection that closely matches the weight of
	/// the provided key.
	/// </summary>
	/// <remarks>The method searches for the closest available weight to the requested value,
	/// considering both lighter and heavier alternatives within the collection. If no exact match is found, it
	/// progressively searches for the nearest available weight in both directions.</remarks>
	/// <param name="glyphTypefaces">A dictionary mapping font collection keys to glyph typeface instances. The method searches this collection
	/// for a suitable fallback.</param>
	/// <param name="key">The font collection key specifying the desired font attributes, including weight, for which a fallback glyph
	/// typeface is sought.</param>
	/// <param name="isLastResort">Whether to match last resort fonts.</param>
	/// <param name="glyphTypeface">When this method returns, contains the matching glyph typeface if a suitable fallback is found; otherwise,
	/// null.</param>
	/// <returns>true if a fallback glyph typeface matching the requested weight is found; otherwise, false.</returns>
	private static bool TryFindWeightFallback(IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces, FontCollectionKey key, bool isLastResort, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		glyphTypeface = null;
		int weight = (int)key.Weight;
		if (weight >= 400 && weight <= 500)
		{
			for (int i = 0; weight + i <= 500; i += 50)
			{
				if (TryGetWithWeight(weight + i, out glyphTypeface))
				{
					return true;
				}
			}
			for (int j = 0; weight - j >= 100; j += 50)
			{
				if (TryGetWithWeight(weight - j, out glyphTypeface))
				{
					return true;
				}
			}
			for (int k = 0; weight + k <= 900; k += 50)
			{
				if (TryGetWithWeight(weight + k, out glyphTypeface))
				{
					return true;
				}
			}
		}
		if (weight < 400)
		{
			for (int l = 0; weight - l >= 100; l += 50)
			{
				if (TryGetWithWeight(weight - l, out glyphTypeface))
				{
					return true;
				}
			}
			for (int m = 0; weight + m <= 900; m += 50)
			{
				if (TryGetWithWeight(weight + m, out glyphTypeface))
				{
					return true;
				}
			}
		}
		if (weight > 500)
		{
			for (int n = 0; weight + n <= 900; n += 50)
			{
				if (TryGetWithWeight(weight + n, out glyphTypeface))
				{
					return true;
				}
			}
			for (int num = 0; weight - num >= 100; num += 50)
			{
				if (TryGetWithWeight(weight - num, out glyphTypeface))
				{
					return true;
				}
			}
		}
		return false;
		bool TryGetWithWeight(int effectiveWeight, [NotNullWhen(true)] out GlyphTypeface? reference)
		{
			if (glyphTypefaces.TryGetValue(key with
			{
				Weight = (FontWeight)effectiveWeight
			}, out reference) && reference != null)
			{
				return reference.IsLastResort == isLastResort;
			}
			return false;
		}
	}

	void IDisposable.Dispose()
	{
		foreach (ConcurrentDictionary<FontCollectionKey, GlyphTypeface> value in _glyphTypefaceCache.Values)
		{
			foreach (KeyValuePair<FontCollectionKey, GlyphTypeface> item in value)
			{
				item.Value?.Dispose();
			}
		}
		GC.SuppressFinalize(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
