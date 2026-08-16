using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Logging;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
///     The font manager is used to query the system's installed fonts and is responsible for caching loaded fonts.
///     It is also responsible for the font fallback.
/// </summary>
public sealed class FontManager : IDisposable
{
	internal static Uri SystemFontsKey = new Uri("fonts:SystemFonts", UriKind.Absolute);

	public const string FontCollectionScheme = "fonts";

	public const string SystemFontScheme = "systemfont";

	public const string CompositeFontScheme = "compositefont";

	private readonly ConcurrentDictionary<Uri, IFontCollection> _fontCollections = new ConcurrentDictionary<Uri, IFontCollection>();

	private readonly IReadOnlyList<FontFallback>? _fontFallbacks;

	private readonly IReadOnlyDictionary<string, FontFamily>? _fontFamilyMappings;

	/// <summary>
	/// Get the current font manager instance.
	/// </summary>
	public static FontManager Current
	{
		get
		{
			FontManager service = AvaloniaLocator.Current.GetService<FontManager>();
			if (service != null)
			{
				return service;
			}
			service = new FontManager(AvaloniaLocator.Current.GetRequiredService<IFontManagerImpl>());
			AvaloniaLocator.CurrentMutable.Bind<FontManager>().ToConstant(service);
			return service;
		}
	}

	/// <summary>
	///     Gets the system's default font family.
	/// </summary>
	public FontFamily DefaultFontFamily { get; }

	/// <summary>
	///     Get all system fonts.
	/// </summary>
	public IFontCollection SystemFonts
	{
		get
		{
			if (TryGetFontCollection(SystemFontsKey, out IFontCollection fontCollection))
			{
				return fontCollection;
			}
			return new EmptySystemFontCollection();
		}
	}

	internal IFontManagerImpl PlatformImpl { get; }

	public FontManager(IFontManagerImpl platformImpl)
	{
		PlatformImpl = platformImpl;
		FontManagerOptions service = AvaloniaLocator.Current.GetService<FontManagerOptions>();
		_fontFallbacks = service?.FontFallbacks;
		_fontFamilyMappings = service?.FontFamilyMappings;
		string defaultFontFamilyName = GetDefaultFontFamilyName(service);
		DefaultFontFamily = new FontFamily(defaultFontFamilyName);
	}

	/// <summary>
	///     Tries to get a glyph typeface for specified typeface.
	/// </summary>
	/// <param name="typeface">The typeface.</param>
	/// <param name="glyphTypeface">The created glyphTypeface</param>
	/// <returns>
	///     <c>True</c>, if the <see cref="T:Avalonia.Media.FontManager" /> could create the glyph typeface, <c>False</c> otherwise.
	/// </returns>
	public bool TryGetGlyphTypeface(Typeface typeface, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		glyphTypeface = null;
		FontFamily fontFamily = GetMappedFontFamily(typeface.FontFamily);
		if (typeface.FontFamily.Name == "$Default")
		{
			return TryGetGlyphTypeface(new Typeface(DefaultFontFamily, typeface.Style, typeface.Weight, typeface.Stretch), out glyphTypeface);
		}
		if (fontFamily.Key != null)
		{
			if (!(fontFamily.Key is CompositeFontFamilyKey compositeFontFamilyKey))
			{
				string primaryFamilyName = fontFamily.FamilyNames.PrimaryFamilyName;
				if (TryGetGlyphTypefaceByKeyAndName(typeface, fontFamily.Key, primaryFamilyName, out glyphTypeface))
				{
					return true;
				}
				return false;
			}
			for (int i = 0; i < compositeFontFamilyKey.Keys.Count; i++)
			{
				FontFamilyKey key = compositeFontFamilyKey.Keys[i];
				string text = fontFamily.FamilyNames[i];
				if (_fontFamilyMappings != null && _fontFamilyMappings.TryGetValue(text, out FontFamily value))
				{
					key = ((!(value.Key != null)) ? new FontFamilyKey(SystemFontsKey) : value.Key);
					text = value.FamilyNames.PrimaryFamilyName;
				}
				if (text == "$Default")
				{
					return TryGetGlyphTypeface(new Typeface(DefaultFontFamily, typeface.Style, typeface.Weight, typeface.Stretch), out glyphTypeface);
				}
				if (TryGetGlyphTypefaceByKeyAndName(typeface, key, text, out glyphTypeface) && glyphTypeface.FamilyName.Contains(text))
				{
					return true;
				}
			}
		}
		else
		{
			string primaryFamilyName2 = fontFamily.FamilyNames.PrimaryFamilyName;
			if (SystemFonts.TryGetGlyphTypeface(primaryFamilyName2, typeface.Style, typeface.Weight, typeface.Stretch, out glyphTypeface))
			{
				return true;
			}
		}
		if (typeface.FontFamily == DefaultFontFamily)
		{
			return false;
		}
		return TryGetGlyphTypeface(new Typeface(DefaultFontFamily, typeface.Style, typeface.Weight, typeface.Stretch), out glyphTypeface);
		FontFamily GetMappedFontFamily(FontFamily fontFamily2)
		{
			if (_fontFamilyMappings == null || !_fontFamilyMappings.TryGetValue(fontFamily2.FamilyNames.PrimaryFamilyName, out FontFamily value2))
			{
				return fontFamily2;
			}
			return value2;
		}
	}

	private bool TryGetGlyphTypefaceByKeyAndName(Typeface typeface, FontFamilyKey key, string familyName, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		Uri source = key.Source.EnsureAbsolute(key.BaseUri);
		if (TryGetFontCollection(source, out IFontCollection fontCollection))
		{
			if (fontCollection.TryGetGlyphTypeface(familyName, typeface.Style, typeface.Weight, typeface.Stretch, out glyphTypeface))
			{
				return true;
			}
			Logger.TryGet(LogEventLevel.Debug, "Fonts")?.Log(this, $"Font family '{familyName}' could not be found. Present font families: [{string.Join(",", fontCollection)}]");
			return false;
		}
		glyphTypeface = null;
		return false;
	}

	/// <summary>
	/// Add a font collection to the manager.
	/// </summary>
	/// <param name="fontCollection">The font collection.</param>
	/// <exception cref="T:System.ArgumentException"></exception>
	/// <remarks>If a font collection's key is already present the collection is replaced.</remarks>
	public void AddFontCollection(IFontCollection fontCollection)
	{
		Uri key = fontCollection.Key;
		if (!fontCollection.Key.IsFontCollection())
		{
			throw new ArgumentException("Font collection Key should follow the fonts: scheme.", "fontCollection");
		}
		_fontCollections.AddOrUpdate(key, fontCollection, delegate(Uri _, IFontCollection oldCollection)
		{
			oldCollection.Dispose();
			return fontCollection;
		});
	}

	/// <summary>
	/// Removes the font collection that corresponds to specified key.
	/// </summary>
	/// <param name="key">The font collection's key.</param>
	public void RemoveFontCollection(Uri key)
	{
		if (_fontCollections.TryRemove(key, out IFontCollection value))
		{
			value.Dispose();
		}
	}

	/// <summary>
	///     Tries to match a specified character to a <see cref="T:Avalonia.Media.Typeface" /> that supports specified font properties.
	/// </summary>
	/// <param name="codepoint">The codepoint to match against.</param>
	/// <param name="fontStyle">The font style.</param>
	/// <param name="fontWeight">The font weight.</param>
	/// <param name="fontStretch">The font stretch.</param>
	/// <param name="fontFamily">The font family. This is optional and used for fallback lookup.</param>
	/// <param name="culture">The culture.</param>
	/// <param name="typeface">The matching <see cref="T:Avalonia.Media.Typeface" />.</param>
	/// <returns>
	///     <c>True</c>, if the <see cref="T:Avalonia.Media.FontManager" /> could match the character to specified parameters, <c>False</c> otherwise.
	/// </returns>
	public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, FontFamily? fontFamily, CultureInfo? culture, out Typeface typeface)
	{
		return TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, fontFamily, culture, Script.Unknown, out typeface);
	}

	/// <summary>
	/// Character-to-typeface match with an optional shaping-capability constraint: when
	/// <paramref name="shapingScript" /> is a complex script, only fonts that can shape it are
	/// considered. <see cref="F:Avalonia.Media.TextFormatting.Unicode.Script.Unknown" /> imposes no constraint and is identical to the
	/// public overload.
	/// </summary>
	internal bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, FontFamily? fontFamily, CultureInfo? culture, Script shapingScript, out Typeface typeface)
	{
		if (_fontFallbacks != null)
		{
			foreach (FontFallback fontFallback in _fontFallbacks)
			{
				if (fontFallback.UnicodeRange.IsInRange(codepoint))
				{
					typeface = new Typeface(fontFallback.FontFamily, fontStyle, fontWeight, fontStretch);
					if (TryGetGlyphTypeface(typeface, out GlyphTypeface glyphTypeface) && glyphTypeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out var _) && (shapingScript == Script.Unknown || glyphTypeface.CanShapeScript(shapingScript)))
					{
						return true;
					}
				}
			}
		}
		if (fontFamily?.Key != null)
		{
			if (fontFamily.Key is CompositeFontFamilyKey compositeFontFamilyKey)
			{
				for (int i = 0; i < compositeFontFamilyKey.Keys.Count; i++)
				{
					FontFamilyKey fontFamilyKey = compositeFontFamilyKey.Keys[i];
					string text = fontFamily.FamilyNames[i];
					Uri source = fontFamilyKey.Source.EnsureAbsolute(fontFamilyKey.BaseUri);
					if (text == "$Default")
					{
						text = DefaultFontFamily.Name;
					}
					if (TryGetFontCollection(source, out IFontCollection fontCollection) && fontCollection.TryGetGlyphTypeface(text, fontStyle, fontWeight, fontStretch, out GlyphTypeface _) && TryMatchCharacterInCollection(fontCollection, codepoint, fontStyle, fontWeight, fontStretch, text, culture, shapingScript, out typeface) && (!(typeface.FontFamily.Name == DefaultFontFamily.Name) || i + 1 >= compositeFontFamilyKey.Keys.Count))
					{
						return true;
					}
				}
			}
			Uri uri = fontFamily.Key.Source.EnsureAbsolute(fontFamily.Key.BaseUri);
			if (uri.IsFontCollection() && TryGetFontCollection(uri, out IFontCollection fontCollection2) && TryMatchCharacterInCollection(fontCollection2, codepoint, fontStyle, fontWeight, fontStretch, fontFamily.Name, culture, shapingScript, out typeface))
			{
				return true;
			}
		}
		return TryMatchCharacterInCollection(SystemFonts, codepoint, fontStyle, fontWeight, fontStretch, fontFamily?.Name, culture, shapingScript, out typeface);
	}

	private static bool TryMatchCharacterInCollection(IFontCollection fontCollection, int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, string? familyName, CultureInfo? culture, Script shapingScript, out Typeface typeface)
	{
		if (fontCollection is FontCollectionBase fontCollectionBase)
		{
			return fontCollectionBase.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName, culture, shapingScript, out typeface);
		}
		return fontCollection.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName, culture, out typeface);
	}

	internal IReadOnlyList<Typeface> GetFamilyTypefaces(FontFamily fontFamily)
	{
		FontFamilyKey key = fontFamily.Key;
		if (key == null)
		{
			if (SystemFonts.TryGetFamilyTypefaces(fontFamily.Name, out IReadOnlyList<Typeface> familyTypefaces))
			{
				return familyTypefaces;
			}
		}
		else
		{
			Uri source = key.Source.EnsureAbsolute(key.BaseUri);
			if (TryGetFontCollection(source, out IFontCollection fontCollection) && fontCollection.TryGetFamilyTypefaces(fontFamily.Name, out IReadOnlyList<Typeface> familyTypefaces2))
			{
				return familyTypefaces2;
			}
		}
		return Array.Empty<Typeface>();
	}

	internal bool TryGetFontCollection(Uri source, [NotNullWhen(true)] out IFontCollection? fontCollection)
	{
		if (source.Scheme == "systemfont" || source == SystemFontsKey)
		{
			fontCollection = GetOrCreateFontCollection(SystemFontsKey, PlatformImpl, (Uri _, IFontManagerImpl impl) => new SystemFontCollection(impl));
			return true;
		}
		if (source.IsFontCollection())
		{
			return _fontCollections.TryGetValue(source, out fontCollection);
		}
		if (source.IsAbsoluteResm() || source.IsAvares())
		{
			fontCollection = GetOrCreateFontCollection(source, 0, (Uri key, int _) => new EmbeddedFontCollection(key, key));
			return true;
		}
		fontCollection = null;
		return false;
	}

	/// <summary>
	/// Thread-safe get-or-create that disposes any candidate that loses the insertion race,
	/// preventing resource leaks that <see cref="M:System.Collections.Concurrent.ConcurrentDictionary`2.GetOrAdd(`0,System.Func{`0,`1})" />
	/// can cause when the factory is invoked concurrently by multiple threads.
	/// </summary>
	private IFontCollection GetOrCreateFontCollection<TState>(Uri key, TState state, Func<Uri, TState, IFontCollection> factory)
	{
		if (_fontCollections.TryGetValue(key, out IFontCollection value))
		{
			return value;
		}
		IFontCollection fontCollection = factory(key, state);
		IFontCollection orAdd = _fontCollections.GetOrAdd(key, fontCollection);
		if (orAdd != fontCollection)
		{
			fontCollection.Dispose();
		}
		return orAdd;
	}

	private string GetDefaultFontFamilyName(FontManagerOptions? options)
	{
		string text = options?.DefaultFamilyName ?? PlatformImpl.GetDefaultFontFamilyName();
		if (string.IsNullOrEmpty(text) && SystemFonts.Count > 0)
		{
			text = SystemFonts[0].Name;
		}
		if (string.IsNullOrEmpty(text))
		{
			throw new InvalidOperationException("Default font family name can't be null or empty.");
		}
		if (text == "$Default")
		{
			throw new InvalidOperationException("'$Default' is a placeholder and cannot be used as the default font family name. Provide a concrete font family name via FontManagerOptions or the platform implementation.");
		}
		return text;
	}

	void IDisposable.Dispose()
	{
		foreach (KeyValuePair<Uri, IFontCollection> fontCollection in _fontCollections)
		{
			fontCollection.Value.Dispose();
		}
		_fontCollections.Clear();
		(PlatformImpl as IDisposable)?.Dispose();
	}
}
