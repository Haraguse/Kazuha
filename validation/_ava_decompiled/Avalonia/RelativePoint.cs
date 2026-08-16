using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia;

/// <summary>
/// Defines a point that may be defined relative to a containing element.
/// </summary>
public readonly struct RelativePoint : IEquatable<RelativePoint>
{
	/// <summary>
	/// A point at the top left of the containing element.
	/// </summary>
	public static readonly RelativePoint TopLeft = new RelativePoint(0.0, 0.0, RelativeUnit.Relative);

	/// <summary>
	/// A point at the center of the containing element.
	/// </summary>
	public static readonly RelativePoint Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

	/// <summary>
	/// A point at the bottom right of the containing element.
	/// </summary>
	public static readonly RelativePoint BottomRight = new RelativePoint(1.0, 1.0, RelativeUnit.Relative);

	private readonly Point _point;

	private readonly RelativeUnit _unit;

	/// <summary>
	/// Gets the point.
	/// </summary>
	public Point Point => _point;

	/// <summary>
	/// Gets the unit.
	/// </summary>
	public RelativeUnit Unit => _unit;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.RelativePoint" /> struct.
	/// </summary>
	/// <param name="x">The X point.</param>
	/// <param name="y">The Y point</param>
	/// <param name="unit">The unit.</param>
	public RelativePoint(double x, double y, RelativeUnit unit)
		: this(new Point(x, y), unit)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.RelativePoint" /> struct.
	/// </summary>
	/// <param name="point">The point.</param>
	/// <param name="unit">The unit.</param>
	public RelativePoint(Point point, RelativeUnit unit)
	{
		_point = point;
		_unit = unit;
	}

	/// <summary>
	/// Checks for equality between two <see cref="T:Avalonia.RelativePoint" />s.
	/// </summary>
	/// <param name="left">The first point.</param>
	/// <param name="right">The second point.</param>
	/// <returns>True if the points are equal; otherwise false.</returns>
	public static bool operator ==(RelativePoint left, RelativePoint right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Checks for inequality between two <see cref="T:Avalonia.RelativePoint" />s.
	/// </summary>
	/// <param name="left">The first point.</param>
	/// <param name="right">The second point.</param>
	/// <returns>True if the points are unequal; otherwise false.</returns>
	public static bool operator !=(RelativePoint left, RelativePoint right)
	{
		return !left.Equals(right);
	}

	/// <summary>
	/// Checks if the <see cref="T:Avalonia.RelativePoint" /> equals another object.
	/// </summary>
	/// <param name="obj">The other object.</param>
	/// <returns>True if the objects are equal, otherwise false.</returns>
	public override bool Equals(object? obj)
	{
		if (obj is RelativePoint p)
		{
			return Equals(p);
		}
		return false;
	}

	/// <summary>
	/// Checks if the <see cref="T:Avalonia.RelativePoint" /> equals another point.
	/// </summary>
	/// <param name="p">The other point.</param>
	/// <returns>True if the objects are equal, otherwise false.</returns>
	public bool Equals(RelativePoint p)
	{
		if (Unit == p.Unit)
		{
			return Point == p.Point;
		}
		return false;
	}

	/// <summary>
	/// Gets a hashcode for a <see cref="T:Avalonia.RelativePoint" />.
	/// </summary>
	/// <returns>A hash code.</returns>
	public override int GetHashCode()
	{
		return (_point.GetHashCode() * 397) ^ (int)_unit;
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.RelativePoint" /> into pixels.
	/// </summary>
	/// <param name="size">The size of the visual.</param>
	/// <returns>The origin point in pixels.</returns>
	public Point ToPixels(Size size)
	{
		if (_unit != RelativeUnit.Absolute)
		{
			return new Point(_point.X * size.Width, _point.Y * size.Height);
		}
		return _point;
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.RelativePoint" /> into pixels.
	/// </summary>
	/// <param name="rect">The bounding box of the rendering primitive.</param>
	/// <returns>The origin point in pixels.</returns>
	public Point ToPixels(Rect rect)
	{
		if (_unit != RelativeUnit.Absolute)
		{
			return new Point(rect.X + _point.X * rect.Width, rect.Y + _point.Y * rect.Height);
		}
		return _point;
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.RelativePoint" /> string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>The parsed <see cref="T:Avalonia.RelativePoint" />.</returns>
	public static RelativePoint Parse(string s)
	{
		using SpanStringTokenizer spanStringTokenizer = new SpanStringTokenizer(s, CultureInfo.InvariantCulture, "Invalid RelativePoint.");
		string text = spanStringTokenizer.ReadString();
		string text2 = spanStringTokenizer.ReadString();
		RelativeUnit unit = RelativeUnit.Absolute;
		double num = 1.0;
		if (text.EndsWith('%'))
		{
			if (!text2.EndsWith('%'))
			{
				throw new FormatException("If one coordinate is relative, both must be.");
			}
			text = text.TrimEnd('%');
			text2 = text2.TrimEnd('%');
			unit = RelativeUnit.Relative;
			num = 0.01;
		}
		return new RelativePoint(double.Parse(text, CultureInfo.InvariantCulture) * num, double.Parse(text2, CultureInfo.InvariantCulture) * num, unit);
	}

	/// <summary>
	/// Returns a String representing this RelativePoint instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override string ToString()
	{
		if (_unit != RelativeUnit.Absolute)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}%, {1}%", _point.X * 100.0, _point.Y * 100.0);
		}
		return _point.ToString();
	}
}
