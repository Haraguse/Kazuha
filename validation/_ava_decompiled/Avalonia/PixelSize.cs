using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia;

/// <summary>
/// Represents a size in device pixels.
/// </summary>
public readonly struct PixelSize : IEquatable<PixelSize>
{
	/// <summary>
	/// A size representing zero
	/// </summary>
	public static readonly PixelSize Empty = new PixelSize(0, 0);

	private const double FromSizeCeilingEpsilon = 1E-06;

	/// <summary>
	/// Gets the aspect ratio of the size.
	/// </summary>
	public double AspectRatio => (double)Width / (double)Height;

	/// <summary>
	/// Gets the width.
	/// </summary>
	public int Width { get; }

	/// <summary>
	/// Gets the height.
	/// </summary>
	public int Height { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.PixelSize" /> structure.
	/// </summary>
	/// <param name="width">The width.</param>
	/// <param name="height">The height.</param>
	public PixelSize(int width, int height)
	{
		Width = width;
		Height = height;
	}

	/// <summary>
	/// Checks for equality between two <see cref="T:Avalonia.PixelSize" />s.
	/// </summary>
	/// <param name="left">The first size.</param>
	/// <param name="right">The second size.</param>
	/// <returns>True if the sizes are equal; otherwise false.</returns>
	public static bool operator ==(PixelSize left, PixelSize right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Checks for inequality between two <see cref="T:Avalonia.Size" />s.
	/// </summary>
	/// <param name="left">The first size.</param>
	/// <param name="right">The second size.</param>
	/// <returns>True if the sizes are unequal; otherwise false.</returns>
	public static bool operator !=(PixelSize left, PixelSize right)
	{
		return !(left == right);
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.PixelSize" /> string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>The <see cref="T:Avalonia.PixelSize" />.</returns>
	/// <exception cref="T:System.FormatException" />
	public static PixelSize Parse(string s)
	{
		if (TryParse(s, out var result))
		{
			return result;
		}
		throw new FormatException("Invalid PixelSize.");
	}

	/// <summary>
	/// Try parsing <paramref name="source" /> as <see cref="T:Avalonia.PixelSize" />.
	/// </summary>
	/// <param name="source">The <see cref="T:System.String" /> to parse.</param>
	/// <param name="result">The result of parsing. if <paramref name="source" /> is not valid <paramref name="result" /> is <see cref="F:Avalonia.PixelSize.Empty" /> </param>
	/// <returns><c>true</c> if <paramref name="source" /> is valid <see cref="T:Avalonia.PixelSize" />, otherwise <c>false</c>.</returns>
	public static bool TryParse([NotNullWhen(true)] string? source, out PixelSize result)
	{
		result = Empty;
		if (string.IsNullOrEmpty(source))
		{
			return false;
		}
		using SpanStringTokenizer spanStringTokenizer = new SpanStringTokenizer(source, ',', "Invalid PixelSize.");
		if (spanStringTokenizer.TryReadInt32(out var result2) && spanStringTokenizer.TryReadInt32(out var result3))
		{
			result = new PixelSize(result2, result3);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Returns a boolean indicating whether the size is equal to the other given size.
	/// </summary>
	/// <param name="other">The other size to test equality against.</param>
	/// <returns>True if this size is equal to other; False otherwise.</returns>
	public bool Equals(PixelSize other)
	{
		if (Width == other.Width)
		{
			return Height == other.Height;
		}
		return false;
	}

	/// <summary>
	/// Checks for equality between a size and an object.
	/// </summary>
	/// <param name="obj">The object.</param>
	/// <returns>
	/// True if <paramref name="obj" /> is a size that equals the current size.
	/// </returns>
	public override bool Equals(object? obj)
	{
		if (obj is PixelSize other)
		{
			return Equals(other);
		}
		return false;
	}

	/// <summary>
	/// Returns a hash code for a <see cref="T:Avalonia.PixelSize" />.
	/// </summary>
	/// <returns>The hash code.</returns>
	public override int GetHashCode()
	{
		return (17 * 23 + Width.GetHashCode()) * 23 + Height.GetHashCode();
	}

	/// <summary>
	/// Returns a new <see cref="T:Avalonia.PixelSize" /> with the same height and the specified width.
	/// </summary>
	/// <param name="width">The width.</param>
	/// <returns>The new <see cref="T:Avalonia.PixelSize" />.</returns>
	public PixelSize WithWidth(int width)
	{
		return new PixelSize(width, Height);
	}

	/// <summary>
	/// Returns a new <see cref="T:Avalonia.PixelSize" /> with the same width and the specified height.
	/// </summary>
	/// <param name="height">The height.</param>
	/// <returns>The new <see cref="T:Avalonia.PixelSize" />.</returns>
	public PixelSize WithHeight(int height)
	{
		return new PixelSize(Width, height);
	}

	/// <summary>
	/// Converts the <see cref="T:Avalonia.PixelSize" /> to a device-independent <see cref="T:Avalonia.Size" /> using the
	/// specified scaling factor.
	/// </summary>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The device-independent size.</returns>
	public Size ToSize(double scale)
	{
		return new Size((double)Width / scale, (double)Height / scale);
	}

	/// <summary>
	/// Converts the <see cref="T:Avalonia.PixelSize" /> to a device-independent <see cref="T:Avalonia.Size" /> using the
	/// specified scaling factor.
	/// </summary>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The device-independent size.</returns>
	public Size ToSize(Vector scale)
	{
		return new Size((double)Width / scale.X, (double)Height / scale.Y);
	}

	/// <summary>
	/// Converts the <see cref="T:Avalonia.PixelSize" /> to a device-independent <see cref="T:Avalonia.Size" /> using the
	/// specified dots per inch (DPI).
	/// </summary>
	/// <param name="dpi">The dots per inch.</param>
	/// <returns>The device-independent size.</returns>
	public Size ToSizeWithDpi(double dpi)
	{
		return ToSize(dpi / 96.0);
	}

	/// <summary>
	/// Converts the <see cref="T:Avalonia.PixelSize" /> to a device-independent <see cref="T:Avalonia.Size" /> using the
	/// specified dots per inch (DPI).
	/// </summary>
	/// <param name="dpi">The dots per inch.</param>
	/// <returns>The device-independent size.</returns>
	public Size ToSizeWithDpi(Vector dpi)
	{
		return ToSize(new Vector(dpi.X / 96.0, dpi.Y / 96.0));
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.Size" /> to device pixels using the specified scaling factor.
	/// </summary>
	/// <param name="size">The size.</param>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The device-independent size.</returns>
	public static PixelSize FromSize(Size size, double scale)
	{
		return new PixelSize((int)Math.Ceiling(size.Width * scale), (int)Math.Ceiling(size.Height * scale));
	}

	/// <summary>
	/// A reversible variant of <see cref="M:Avalonia.PixelSize.FromSize(Avalonia.Size,System.Double)" /> that uses Round instead of Ceiling to make it reversible from ToSize
	/// </summary>
	/// <param name="size">The size.</param>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The device-independent size.</returns>
	internal static PixelSize FromSizeRounded(Size size, double scale)
	{
		return new PixelSize((int)Math.Round(size.Width * scale), (int)Math.Round(size.Height * scale));
	}

	/// <summary>
	/// Converts logical size back to PixelSize and rounds it up with a small epsilon to avoid having
	/// extra pixels when doing platform pixel size -&gt; logical size -&gt; pixel size conversions
	/// </summary>
	/// <param name="size">The logical size.</param>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The pixel size that contains the logical size at the given scale.</returns>
	internal static PixelSize FromSizeCeiling(Size size, double scale)
	{
		return new PixelSize(CeilWithEpsilon(size.Width * scale), CeilWithEpsilon(size.Height * scale));
	}

	private static int CeilWithEpsilon(double value)
	{
		double num = Math.Round(value);
		if (Math.Abs(value - num) < 1E-06)
		{
			return (int)num;
		}
		return (int)Math.Ceiling(value);
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.Size" /> to device pixels using the specified scaling factor.
	/// </summary>
	/// <param name="size">The size.</param>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The device-independent size.</returns>
	public static PixelSize FromSize(Size size, Vector scale)
	{
		return new PixelSize((int)Math.Ceiling(size.Width * scale.X), (int)Math.Ceiling(size.Height * scale.Y));
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.Size" /> to device pixels using the specified dots per inch (DPI).
	/// </summary>
	/// <param name="size">The size.</param>
	/// <param name="dpi">The dots per inch.</param>
	/// <returns>The device-independent size.</returns>
	public static PixelSize FromSizeWithDpi(Size size, double dpi)
	{
		return FromSize(size, dpi / 96.0);
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.Size" /> to device pixels using the specified dots per inch (DPI).
	/// </summary>
	/// <param name="size">The size.</param>
	/// <param name="dpi">The dots per inch.</param>
	/// <returns>The device-independent size.</returns>
	public static PixelSize FromSizeWithDpi(Size size, Vector dpi)
	{
		return FromSize(size, new Vector(dpi.X / 96.0, dpi.Y / 96.0));
	}

	/// <summary>
	/// Returns the string representation of the size.
	/// </summary>
	/// <returns>The string representation of the size.</returns>
	public override string ToString()
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", Width, Height);
	}
}
