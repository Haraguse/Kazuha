using System;
using System.Diagnostics;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// Represents a typeface.
/// </summary>
[DebuggerDisplay("Name = {FontFamily.Name}, Weight = {Weight}, Style = {Style}")]
public readonly struct Typeface : IEquatable<Typeface>
{
	public static Typeface Default { get; } = new Typeface(Avalonia.Media.FontFamily.Default);

	/// <summary>
	/// Gets the font family.
	/// </summary>
	public FontFamily FontFamily { get; }

	/// <summary>
	/// Gets the font style.
	/// </summary>
	public FontStyle Style { get; }

	/// <summary>
	/// Gets the font weight.
	/// </summary>
	public FontWeight Weight { get; }

	/// <summary>
	/// Gets the font stretch.
	/// </summary>
	public FontStretch Stretch { get; }

	/// <summary>
	/// Gets the glyph typeface.
	/// </summary>
	/// <value>
	/// The glyph typeface.
	/// </value>
	public GlyphTypeface GlyphTypeface
	{
		get
		{
			if (FontManager.Current.TryGetGlyphTypeface(this, out GlyphTypeface glyphTypeface))
			{
				return glyphTypeface;
			}
			throw new InvalidOperationException($"Could not create glyphTypeface. Font family: {FontFamily?.Name} (key: {FontFamily?.Key}). Style: {Style}. Weight: {Weight}. Stretch: {Stretch}");
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Typeface" /> class.
	/// </summary>
	/// <param name="fontFamily">The font family.</param>
	/// <param name="style">The font style.</param>
	/// <param name="weight">The font weight.</param>
	/// <param name="stretch">The font stretch.</param>
	public Typeface(FontFamily fontFamily, FontStyle style = FontStyle.Normal, FontWeight weight = FontWeight.Normal, FontStretch stretch = FontStretch.Normal)
	{
		if (weight <= (FontWeight)0)
		{
			throw new ArgumentException("Font weight must be > 0.");
		}
		if (stretch < FontStretch.UltraCondensed)
		{
			throw new ArgumentException("Font stretch must be > 1.");
		}
		FontFamily = fontFamily ?? Avalonia.Media.FontFamily.Default;
		Style = style;
		Weight = weight;
		Stretch = stretch;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Typeface" /> class.
	/// </summary>
	/// <param name="fontFamilyName">The name of the font family.</param>
	/// <param name="style">The font style.</param>
	/// <param name="weight">The font weight.</param>
	/// <param name="stretch">The font stretch.</param>
	public Typeface(string fontFamilyName, FontStyle style = FontStyle.Normal, FontWeight weight = FontWeight.Normal, FontStretch stretch = FontStretch.Normal)
		: this(string.IsNullOrEmpty(fontFamilyName) ? Avalonia.Media.FontFamily.Default : new FontFamily(fontFamilyName), style, weight, stretch)
	{
	}

	public static bool operator !=(Typeface a, Typeface b)
	{
		return !(a == b);
	}

	public static bool operator ==(Typeface a, Typeface b)
	{
		return a.Equals(b);
	}

	public override bool Equals(object? obj)
	{
		if (obj is Typeface other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(Typeface other)
	{
		if (FontFamily == other.FontFamily && Style == other.Style && Weight == other.Weight)
		{
			return Stretch == other.Stretch;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)(((((uint)(((FontFamily != null) ? FontFamily.GetHashCode() : 0) * 397) ^ (uint)Style) * 397) ^ (uint)Weight) * 397) ^ (int)Stretch;
	}

	/// <summary>
	/// Normalizes the typeface by extracting and removing style, weight, and stretch information from the font
	/// family name, and returns a new <see cref="T:Avalonia.Media.Typeface" /> instance with the updated properties.
	/// </summary>
	/// <remarks>This method analyzes the font family name to identify and extract any style, weight,
	/// or stretch information embedded within it. If such information is found, it is removed from the family name,
	/// and the corresponding properties of the returned <see cref="T:Avalonia.Media.Typeface" /> are updated accordingly. If no such
	/// information is found, the method returns the current instance without modification.</remarks>
	/// <param name="normalizedFamilyName">When this method returns, contains the normalized font family name with style, weight, and stretch
	/// information removed. This parameter is passed uninitialized.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.Typeface" /> instance with the updated <see cref="T:Avalonia.Media.FontStyle" />, <see cref="T:Avalonia.Media.FontWeight" />,
	/// and <see cref="T:Avalonia.Media.FontStretch" /> properties, or the current instance if no normalization was performed.</returns>
	public Typeface Normalize(out string normalizedFamilyName)
	{
		normalizedFamilyName = FontFamily.FamilyNames.PrimaryFamilyName;
		if (!normalizedFamilyName.Contains(' '))
		{
			return this;
		}
		FontStyle style = Style;
		FontWeight weight = Weight;
		FontStretch stretch = Stretch;
		StringBuilder stringBuilder = null;
		int num = 0;
		SpanStringTokenizer spanStringTokenizer = new SpanStringTokenizer(normalizedFamilyName, ' ');
		spanStringTokenizer.ReadSpan();
		ReadOnlySpan<char> result;
		while (spanStringTokenizer.TryReadSpan(out result))
		{
			if (new SpanStringTokenizer(result).TryReadInt32(out var _))
			{
				continue;
			}
			bool flag = false;
			FontWeight result4;
			FontStretch result5;
			if (Enum.TryParse<FontStyle>(result, ignoreCase: true, out var result3))
			{
				style = result3;
				flag = true;
			}
			else if (Enum.TryParse<FontWeight>(result, ignoreCase: true, out result4))
			{
				weight = result4;
				flag = true;
			}
			else if (Enum.TryParse<FontStretch>(result, ignoreCase: true, out result5))
			{
				stretch = result5;
				flag = true;
			}
			if (flag)
			{
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(normalizedFamilyName);
				}
				stringBuilder.Remove(spanStringTokenizer.CurrentTokenIndex - num, result.Length);
				num += result.Length;
			}
		}
		normalizedFamilyName = (stringBuilder?.ToString() ?? normalizedFamilyName).TrimEnd();
		return new Typeface(FontFamily, style, weight, stretch);
	}
}
