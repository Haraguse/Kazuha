using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts;

internal class SystemFontCollection : FontCollectionBase
{
	private readonly IFontManagerImpl _platformImpl;

	public override Uri Key => FontManager.SystemFontsKey;

	public SystemFontCollection(IFontManagerImpl platformImpl)
	{
		_platformImpl = platformImpl ?? throw new ArgumentNullException("platformImpl");
		foreach (string item in from x in _platformImpl.GetInstalledFontFamilyNames()
			where !string.IsNullOrEmpty(x)
			select x)
		{
			AddFontFamily(item);
		}
	}

	public override bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		FontCollectionKey fontCollectionKey = new Typeface(familyName, style, weight, stretch).Normalize(out familyName).ToFontCollectionKey();
		if (TryGetGlyphTypeface(familyName, fontCollectionKey, allowNearestMatch: false, out glyphTypeface))
		{
			return true;
		}
		if (_glyphTypefaceCache.TryGetValue(familyName, out ConcurrentDictionary<FontCollectionKey, GlyphTypeface> value) && value.TryGetValue(fontCollectionKey, out glyphTypeface))
		{
			return glyphTypeface != null;
		}
		if (!_platformImpl.TryCreateGlyphTypeface(familyName, style, weight, stretch, out IPlatformTypeface platformTypeface))
		{
			TryAddGlyphTypeface(familyName, fontCollectionKey, null);
			return false;
		}
		if (fontCollectionKey != platformTypeface.ToFontCollectionKey() && TryGetGlyphTypeface(familyName, fontCollectionKey, allowNearestMatch: true, out glyphTypeface))
		{
			return true;
		}
		glyphTypeface = GlyphTypeface.TryCreate(platformTypeface);
		if (glyphTypeface == null)
		{
			return false;
		}
		TryAddGlyphTypeface(platformTypeface.FamilyName, fontCollectionKey, glyphTypeface);
		if (familyName != platformTypeface.FamilyName)
		{
			TryAddGlyphTypeface(familyName, fontCollectionKey, glyphTypeface);
		}
		if (!TryAddGlyphTypeface(glyphTypeface))
		{
			if (_glyphTypefaceCache.TryGetValue(familyName, out ConcurrentDictionary<FontCollectionKey, GlyphTypeface> value2) && value2.TryGetValue(fontCollectionKey, out var value3) && value3 != null)
			{
				glyphTypeface = value3;
				return true;
			}
			return false;
		}
		return TryGetGlyphTypeface(familyName, fontCollectionKey, allowNearestMatch: false, out glyphTypeface);
	}

	public override bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
	{
		return _platformImpl.TryGetFamilyTypefaces(familyName, out familyTypefaces);
	}

	public override bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch, string? familyName, CultureInfo? culture, out Typeface match)
	{
		return base.TryMatchCharacter(codepoint, style, weight, stretch, familyName, culture, out match);
	}

	protected override bool TryMatchCharacterFromPlatform(int codepoint, FontCollectionKey key, string? familyName, CultureInfo? culture, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
	{
		glyphTypeface = null;
		if (!_platformImpl.TryMatchCharacter(codepoint, key.Style, key.Weight, key.Stretch, familyName, culture, out IPlatformTypeface platformTypeface))
		{
			return false;
		}
		FontCollectionKey key2 = new FontCollectionKey(platformTypeface.Style, platformTypeface.Weight, platformTypeface.Stretch);
		if (_glyphTypefaceCache.TryGetValue(platformTypeface.FamilyName, out ConcurrentDictionary<FontCollectionKey, GlyphTypeface> value) && value.TryGetValue(key2, out var value2) && value2 != null)
		{
			glyphTypeface = value2;
			return true;
		}
		glyphTypeface = GlyphTypeface.TryCreate(platformTypeface);
		if (glyphTypeface == null)
		{
			return false;
		}
		TryAddGlyphTypeface(platformTypeface.FamilyName, key2, glyphTypeface);
		TryAddGlyphTypeface(glyphTypeface, key2);
		return true;
	}
}
