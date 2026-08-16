using System;
using System.Collections.Generic;
using Avalonia.Media.Fonts;
using Avalonia.Utilities;

namespace Avalonia.Media;

public sealed class FontFamily
{
	public const string DefaultFontFamilyName = "$Default";

	/// <summary>
	/// Represents the default font family
	/// </summary>
	public static FontFamily Default { get; }

	/// <summary>
	/// Gets the primary family name of the font family.
	/// </summary>
	/// <value>
	/// The primary name of the font family.
	/// </value>
	public string Name => FamilyNames.PrimaryFamilyName;

	/// <summary>
	/// Gets the family names.
	/// </summary>
	/// <value>
	/// The family familyNames.
	/// </value>
	public FamilyNameCollection FamilyNames { get; }

	/// <summary>
	/// Gets the key for associated assets.
	/// </summary>
	/// <value>
	/// The family key.
	/// </value>
	/// <remarks>Key is only used for custom fonts.</remarks>
	public FontFamilyKey? Key { get; }

	/// <summary>
	/// Gets the typefaces for this font family.
	/// </summary>
	public IReadOnlyList<Typeface> FamilyTypefaces => FontManager.Current.GetFamilyTypefaces(this);

	static FontFamily()
	{
		Default = new FontFamily("$Default");
	}

	/// <inheritdoc />
	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
	/// </summary>
	/// <param name="name">The name of the <see cref="T:Avalonia.Media.FontFamily" />.</param>
	public FontFamily(string name)
		: this(null, name)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
	/// </summary>
	/// <param name="baseUri">Specifies the base uri that is used to resolve font family assets.</param>
	/// <param name="name">The name of the <see cref="T:Avalonia.Media.FontFamily" />.</param>
	/// <exception cref="T:System.ArgumentException">Base uri must be an absolute uri.</exception>
	public FontFamily(Uri? baseUri, string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		if (baseUri != null && !baseUri.IsAbsoluteUri)
		{
			throw new ArgumentException("Base uri must be an absolute uri.", "baseUri");
		}
		FrugalStructList<FontSourceIdentifier> fontSourceIdentifier = GetFontSourceIdentifier(name);
		FamilyNames = new FamilyNameCollection(fontSourceIdentifier);
		if (fontSourceIdentifier.Count == 1)
		{
			Uri source = fontSourceIdentifier[0].Source;
			if ((object)source != null)
			{
				if (source.IsAbsoluteUri)
				{
					Key = new FontFamilyKey(source);
				}
				else
				{
					Key = new FontFamilyKey(source, baseUri);
				}
			}
			return;
		}
		FontFamilyKey[] array = new FontFamilyKey[fontSourceIdentifier.Count];
		for (int i = 0; i < fontSourceIdentifier.Count; i++)
		{
			FontSourceIdentifier fontSourceIdentifier2 = fontSourceIdentifier[i];
			if ((object)fontSourceIdentifier2.Source != null)
			{
				array[i] = new FontFamilyKey(fontSourceIdentifier2.Source, baseUri);
			}
			else
			{
				array[i] = new FontFamilyKey(new Uri("systemfont:" + fontSourceIdentifier2.Name, UriKind.Absolute));
			}
		}
		Key = new CompositeFontFamilyKey(new Uri("compositefont:" + name, UriKind.Absolute), array);
	}

	/// <summary>
	/// Implicit conversion of string to FontFamily
	/// </summary>
	/// <param name="s"></param>
	public static implicit operator FontFamily(string s)
	{
		return new FontFamily(s);
	}

	private static FrugalStructList<FontSourceIdentifier> GetFontSourceIdentifier(string name)
	{
		FrugalStructList<FontSourceIdentifier> result = new FrugalStructList<FontSourceIdentifier>(1);
		int num = -1;
		do
		{
			int num2 = num + 1;
			num = name.IndexOf(',', num2);
			int num3 = ((num == -1) ? name.Length : num);
			ReadOnlySpan<char> span = name.AsSpan(num2..num3).Trim();
			FontSourceIdentifier? fontSourceIdentifier = null;
			int num4 = span.IndexOf('#');
			if (num4 != -1 && span.Slice(num4 + 1).IndexOf('#') == -1)
			{
				ReadOnlySpan<char> span2 = span.Slice(0, num4).Trim();
				ReadOnlySpan<char> readOnlySpan = span.Slice(num4 + 1).Trim();
				if (span2.IsEmpty)
				{
					fontSourceIdentifier = new FontSourceIdentifier(readOnlySpan.ToString(), null);
				}
				else
				{
					string uriString = span2.ToString();
					if (span2.Contains('/') && Uri.TryCreate(uriString, UriKind.Relative, out Uri result2))
					{
						fontSourceIdentifier = new FontSourceIdentifier(readOnlySpan.ToString(), result2);
					}
					else if (Uri.TryCreate(uriString, UriKind.Absolute, out result2))
					{
						fontSourceIdentifier = new FontSourceIdentifier(readOnlySpan.ToString(), result2);
					}
				}
			}
			FontSourceIdentifier value = fontSourceIdentifier.GetValueOrDefault();
			if (!fontSourceIdentifier.HasValue)
			{
				value = new FontSourceIdentifier((span.Length == name.Length) ? name : span.ToString(), null);
				fontSourceIdentifier = value;
			}
			result.Add(fontSourceIdentifier.Value);
		}
		while (num != -1);
		return result;
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.Media.FontFamily" /> string.
	/// </summary>
	/// <param name="s">The <see cref="T:Avalonia.Media.FontFamily" /> string.</param>
	/// <returns></returns>
	/// <exception cref="T:System.ArgumentException">
	/// Specified family is not supported.
	/// </exception>
	public static FontFamily Parse(string s)
	{
		return Parse(s, null);
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.Media.FontFamily" /> string.
	/// </summary>
	/// <param name="s">The <see cref="T:Avalonia.Media.FontFamily" /> string.</param>
	/// <param name="baseUri">Specifies the base uri that is used to resolve font family assets.</param>
	/// <returns></returns>
	/// <exception cref="T:System.ArgumentException">
	/// Specified family is not supported.
	/// </exception>
	public static FontFamily Parse(string s, Uri? baseUri)
	{
		if (string.IsNullOrEmpty(s))
		{
			throw new ArgumentException("Specified family is not supported.", "s");
		}
		return new FontFamily(baseUri, s);
	}

	/// <summary>
	/// Returns a <see cref="T:System.String" /> that represents this instance.
	/// </summary>
	/// <returns>
	/// A <see cref="T:System.String" /> that represents this instance.
	/// </returns>
	public override string ToString()
	{
		if (Key != null)
		{
			return Key?.ToString() + "#" + FamilyNames;
		}
		return FamilyNames.ToString();
	}

	/// <summary>
	/// Returns a hash code for this instance.
	/// </summary>
	/// <returns>
	/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
	/// </returns>
	public override int GetHashCode()
	{
		return (FamilyNames.GetHashCode() * 397) ^ (((object)Key != null) ? Key.GetHashCode() : 0);
	}

	public static bool operator !=(FontFamily? a, FontFamily? b)
	{
		return !(a == b);
	}

	public static bool operator ==(FontFamily? a, FontFamily? b)
	{
		if ((object)a == b)
		{
			return true;
		}
		return a?.Equals(b) ?? false;
	}

	public override bool Equals(object? obj)
	{
		if (this == obj)
		{
			return true;
		}
		if (!(obj is FontFamily fontFamily))
		{
			return false;
		}
		if (!object.Equals(Key, fontFamily.Key))
		{
			return false;
		}
		return fontFamily.FamilyNames.Equals(FamilyNames);
	}
}
