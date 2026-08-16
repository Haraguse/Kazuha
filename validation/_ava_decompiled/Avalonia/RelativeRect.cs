using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia;

/// <summary>
/// Defines a rectangle that may be defined relative to a containing element.
/// </summary>
public readonly struct RelativeRect : IEquatable<RelativeRect>
{
	private static readonly char[] PercentChar = new char[1] { '%' };

	/// <summary>
	/// A rectangle that represents 100% of an area.
	/// </summary>
	public static readonly RelativeRect Fill = new RelativeRect(0.0, 0.0, 1.0, 1.0, RelativeUnit.Relative);

	/// <summary>
	/// Gets the unit of the rectangle.
	/// </summary>
	public RelativeUnit Unit { get; }

	/// <summary>
	/// Gets the rectangle.
	/// </summary>
	public Rect Rect { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.RelativeRect" /> structure.
	/// </summary>
	/// <param name="x">The X position.</param>
	/// <param name="y">The Y position.</param>
	/// <param name="width">The width.</param>
	/// <param name="height">The height.</param>
	/// <param name="unit">The unit of the rect.</param>
	public RelativeRect(double x, double y, double width, double height, RelativeUnit unit)
	{
		Rect = new Rect(x, y, width, height);
		Unit = unit;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.RelativeRect" /> structure.
	/// </summary>
	/// <param name="rect">The rectangle.</param>
	/// <param name="unit">The unit of the rect.</param>
	public RelativeRect(Rect rect, RelativeUnit unit)
	{
		Rect = rect;
		Unit = unit;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.RelativeRect" /> structure.
	/// </summary>
	/// <param name="size">The size of the rectangle.</param>
	/// <param name="unit">The unit of the rect.</param>
	public RelativeRect(Size size, RelativeUnit unit)
	{
		Rect = new Rect(size);
		Unit = unit;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.RelativeRect" /> structure.
	/// </summary>
	/// <param name="position">The position of the rectangle.</param>
	/// <param name="size">The size of the rectangle.</param>
	/// <param name="unit">The unit of the rect.</param>
	public RelativeRect(Point position, Size size, RelativeUnit unit)
	{
		Rect = new Rect(position, size);
		Unit = unit;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.RelativeRect" /> structure.
	/// </summary>
	/// <param name="topLeft">The top left position of the rectangle.</param>
	/// <param name="bottomRight">The bottom right position of the rectangle.</param>
	/// <param name="unit">The unit of the rect.</param>
	public RelativeRect(Point topLeft, Point bottomRight, RelativeUnit unit)
	{
		Rect = new Rect(topLeft, bottomRight);
		Unit = unit;
	}

	/// <summary>
	/// Checks for equality between two <see cref="T:Avalonia.RelativeRect" />s.
	/// </summary>
	/// <param name="left">The first rectangle.</param>
	/// <param name="right">The second rectangle.</param>
	/// <returns>True if the rectangles are equal; otherwise false.</returns>
	public static bool operator ==(RelativeRect left, RelativeRect right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Checks for inequality between two <see cref="T:Avalonia.RelativeRect" />s.
	/// </summary>
	/// <param name="left">The first rectangle.</param>
	/// <param name="right">The second rectangle.</param>
	/// <returns>True if the rectangles are unequal; otherwise false.</returns>
	public static bool operator !=(RelativeRect left, RelativeRect right)
	{
		return !left.Equals(right);
	}

	/// <summary>
	/// Checks if the <see cref="T:Avalonia.RelativeRect" /> equals another object.
	/// </summary>
	/// <param name="obj">The other object.</param>
	/// <returns>True if the objects are equal, otherwise false.</returns>
	public override bool Equals(object? obj)
	{
		if (obj is RelativeRect p)
		{
			return Equals(p);
		}
		return false;
	}

	/// <summary>
	/// Checks if the <see cref="T:Avalonia.RelativeRect" /> equals another rectangle.
	/// </summary>
	/// <param name="p">The other rectangle.</param>
	/// <returns>True if the objects are equal, otherwise false.</returns>
	public bool Equals(RelativeRect p)
	{
		if (Unit == p.Unit)
		{
			return Rect == p.Rect;
		}
		return false;
	}

	/// <summary>
	/// Gets a hashcode for a <see cref="T:Avalonia.RelativeRect" />.
	/// </summary>
	/// <returns>A hash code.</returns>
	public override int GetHashCode()
	{
		return ((int)Unit * 397) ^ Rect.GetHashCode();
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.RelativeRect" /> into pixels.
	/// </summary>
	/// <param name="size">The size of the visual.</param>
	/// <returns>The origin point in pixels.</returns>
	public Rect ToPixels(Size size)
	{
		if (Unit != RelativeUnit.Absolute)
		{
			return new Rect(Rect.X * size.Width, Rect.Y * size.Height, Rect.Width * size.Width, Rect.Height * size.Height);
		}
		return Rect;
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.RelativeRect" /> into pixels.
	/// </summary>
	/// <param name="boundingBox">The bounding box of the visual.</param>
	/// <returns>The origin point in pixels.</returns>
	public Rect ToPixels(Rect boundingBox)
	{
		if (Unit != RelativeUnit.Absolute)
		{
			return new Rect(boundingBox.X + Rect.X * boundingBox.Width, boundingBox.Y + Rect.Y * boundingBox.Height, Rect.Width * boundingBox.Width, Rect.Height * boundingBox.Height);
		}
		return Rect;
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.RelativeRect" /> string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>The parsed <see cref="T:Avalonia.RelativeRect" />.</returns>
	public static RelativeRect Parse(string s)
	{
		using SpanStringTokenizer spanStringTokenizer = new SpanStringTokenizer(s, ',', "Invalid RelativeRect.");
		ReadOnlySpan<char> span = spanStringTokenizer.ReadSpan();
		ReadOnlySpan<char> span2 = spanStringTokenizer.ReadSpan();
		ReadOnlySpan<char> span3 = spanStringTokenizer.ReadSpan();
		ReadOnlySpan<char> span4 = spanStringTokenizer.ReadSpan();
		RelativeUnit unit = RelativeUnit.Absolute;
		double num = 1.0;
		bool flag = span.EndsWith(PercentChar, StringComparison.Ordinal);
		bool flag2 = span2.EndsWith(PercentChar, StringComparison.Ordinal);
		bool flag3 = span3.EndsWith(PercentChar, StringComparison.Ordinal);
		bool flag4 = span4.EndsWith(PercentChar, StringComparison.Ordinal);
		if (flag & flag2 & flag3 & flag4)
		{
			span = span.TrimEnd(PercentChar);
			span2 = span2.TrimEnd(PercentChar);
			span3 = span3.TrimEnd(PercentChar);
			span4 = span4.TrimEnd(PercentChar);
			unit = RelativeUnit.Relative;
			num = 0.01;
		}
		else if (flag | flag2 | flag3 | flag4)
		{
			throw new FormatException("If one coordinate is relative, all must be.");
		}
		return new RelativeRect(span.ParseDouble(CultureInfo.InvariantCulture) * num, span2.ParseDouble(CultureInfo.InvariantCulture) * num, span3.ParseDouble(CultureInfo.InvariantCulture) * num, span4.ParseDouble(CultureInfo.InvariantCulture) * num, unit);
	}
}
