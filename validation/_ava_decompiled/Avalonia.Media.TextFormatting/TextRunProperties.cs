using System;
using System.Globalization;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Provides a set of properties, such as typeface or foreground brush, that can be applied to a TextRun object. This is an abstract class.
/// </summary>
/// <remarks>
/// The text layout client provides a concrete implementation of this abstract class.
/// This enables the client to implement text run properties in a way that corresponds with the associated formatting store.
/// </remarks>
public abstract class TextRunProperties : IEquatable<TextRunProperties>
{
	private GlyphTypeface? _cachedGlyphTypeFace;

	/// <summary>
	/// Run typeface
	/// </summary>
	public abstract Typeface Typeface { get; }

	/// <summary>
	/// Em size of font used to format and display text
	/// </summary>
	public abstract double FontRenderingEmSize { get; }

	/// <summary>
	///  Run TextDecorations. 
	/// </summary>
	public abstract TextDecorationCollection? TextDecorations { get; }

	/// <summary>
	/// Brush used to fill text.
	/// </summary>
	public abstract IBrush? ForegroundBrush { get; }

	/// <summary>
	/// Brush used to paint background of run.
	/// </summary>
	public abstract IBrush? BackgroundBrush { get; }

	/// <summary>
	/// Run text culture.
	/// </summary>
	public abstract CultureInfo? CultureInfo { get; }

	/// <summary>
	/// Optional features of used font.
	/// </summary>
	public virtual FontFeatureCollection? FontFeatures => null;

	/// <summary>
	/// Run vertical box alignment
	/// </summary>
	public virtual BaselineAlignment BaselineAlignment => BaselineAlignment.Baseline;

	internal GlyphTypeface CachedGlyphTypeface => _cachedGlyphTypeFace ?? (_cachedGlyphTypeFace = Typeface.GlyphTypeface);

	public bool Equals(TextRunProperties? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this == other)
		{
			return true;
		}
		if (Typeface.Equals(other.Typeface) && FontRenderingEmSize.Equals(other.FontRenderingEmSize) && object.Equals(TextDecorations, other.TextDecorations) && object.Equals(ForegroundBrush, other.ForegroundBrush) && object.Equals(BackgroundBrush, other.BackgroundBrush) && object.Equals(CultureInfo, other.CultureInfo))
		{
			return object.Equals(FontFeatures, other.FontFeatures);
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (this != obj)
		{
			if (obj is TextRunProperties other)
			{
				return Equals(other);
			}
			return false;
		}
		return true;
	}

	public override int GetHashCode()
	{
		return (((((((((Typeface.GetHashCode() * 397) ^ FontRenderingEmSize.GetHashCode()) * 397) ^ ((TextDecorations != null) ? TextDecorations.GetHashCode() : 0)) * 397) ^ ((ForegroundBrush != null) ? ForegroundBrush.GetHashCode() : 0)) * 397) ^ ((BackgroundBrush != null) ? BackgroundBrush.GetHashCode() : 0)) * 397) ^ ((CultureInfo != null) ? CultureInfo.GetHashCode() : 0);
	}

	public static bool operator ==(TextRunProperties left, TextRunProperties right)
	{
		return object.Equals(left, right);
	}

	public static bool operator !=(TextRunProperties left, TextRunProperties right)
	{
		return !object.Equals(left, right);
	}

	internal TextRunProperties WithTypeface(Typeface typeface)
	{
		if (this is GenericTextRunProperties genericTextRunProperties && genericTextRunProperties.Typeface == typeface)
		{
			return this;
		}
		return new GenericTextRunProperties(typeface, FontRenderingEmSize, TextDecorations, ForegroundBrush, BackgroundBrush, BaselineAlignment, CultureInfo, FontFeatures);
	}
}
