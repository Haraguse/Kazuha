using System;

namespace Avalonia.Media.Fonts;

/// <summary>
/// Represents an identifier for a <see cref="T:Avalonia.Media.FontFamily" />
/// </summary>
public class FontFamilyKey
{
	/// <summary>
	/// Source of stored font asset that belongs to a <see cref="T:Avalonia.Media.FontFamily" />
	/// </summary>
	public Uri Source { get; }

	/// <summary>
	/// A base URI to use if <see cref="P:Avalonia.Media.Fonts.FontFamilyKey.Source" /> is relative
	/// </summary>
	public Uri? BaseUri { get; }

	/// <summary>
	/// Creates a new instance of <see cref="T:Avalonia.Media.Fonts.FontFamilyKey" />
	/// </summary>
	/// <param name="source"></param>
	/// <param name="baseUri"></param>
	public FontFamilyKey(Uri source, Uri? baseUri = null)
	{
		Source = source ?? throw new ArgumentNullException("source");
		BaseUri = baseUri;
	}

	/// <summary>
	/// Returns a hash code for this instance.
	/// </summary>
	/// <returns>
	/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
	/// </returns>
	public override int GetHashCode()
	{
		int num = -2128831035;
		num = (num * 16777619) ^ Source.GetHashCode();
		if (BaseUri != null)
		{
			num = (num * 16777619) ^ BaseUri.GetHashCode();
		}
		return num;
	}

	public static bool operator !=(FontFamilyKey? a, FontFamilyKey? b)
	{
		return !(a == b);
	}

	public static bool operator ==(FontFamilyKey? a, FontFamilyKey? b)
	{
		if ((object)a == b)
		{
			return true;
		}
		return a?.Equals(b) ?? false;
	}

	/// <summary>
	/// Determines whether the specified <see cref="T:System.Object" />, is equal to this instance.
	/// </summary>
	/// <param name="obj">The <see cref="T:System.Object" /> to compare with this instance.</param>
	/// <returns>
	///   <c>true</c> if the specified <see cref="T:System.Object" /> is equal to this instance; otherwise, <c>false</c>.
	/// </returns>
	public override bool Equals(object? obj)
	{
		if (!(obj is FontFamilyKey fontFamilyKey))
		{
			return false;
		}
		if (Source != fontFamilyKey.Source)
		{
			return false;
		}
		if (BaseUri != fontFamilyKey.BaseUri)
		{
			return false;
		}
		return true;
	}

	/// <summary>
	/// Returns a <see cref="T:System.String" /> that represents this instance.
	/// </summary>
	/// <returns>
	/// A <see cref="T:System.String" /> that represents this instance.
	/// </returns>
	public override string ToString()
	{
		if (!Source.IsAbsoluteUri && BaseUri != null)
		{
			return BaseUri.AbsoluteUri + Source.OriginalString;
		}
		return Source.ToString();
	}
}
