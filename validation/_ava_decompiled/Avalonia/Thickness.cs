using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia;

/// <summary>
/// Describes the thickness of a frame around a rectangle.
/// </summary>
public readonly struct Thickness : IEquatable<Thickness>
{
	/// <summary>
	/// The thickness on the left.
	/// </summary>
	private readonly double _left;

	/// <summary>
	/// The thickness on the top.
	/// </summary>
	private readonly double _top;

	/// <summary>
	/// The thickness on the right.
	/// </summary>
	private readonly double _right;

	/// <summary>
	/// The thickness on the bottom.
	/// </summary>
	private readonly double _bottom;

	/// <summary>
	/// Gets the thickness on the left.
	/// </summary>
	public double Left => _left;

	/// <summary>
	/// Gets the thickness on the top.
	/// </summary>
	public double Top => _top;

	/// <summary>
	/// Gets the thickness on the right.
	/// </summary>
	public double Right => _right;

	/// <summary>
	/// Gets the thickness on the bottom.
	/// </summary>
	public double Bottom => _bottom;

	/// <summary>
	/// Gets a value indicating whether all sides are equal.
	/// </summary>
	public bool IsUniform
	{
		get
		{
			if (Left.Equals(Right) && Top.Equals(Bottom))
			{
				return Right.Equals(Bottom);
			}
			return false;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Thickness" /> structure.
	/// </summary>
	/// <param name="uniformLength">The length that should be applied to all sides.</param>
	public Thickness(double uniformLength)
	{
		_left = (_top = (_right = (_bottom = uniformLength)));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Thickness" /> structure.
	/// </summary>
	/// <param name="horizontal">The thickness on the left and right.</param>
	/// <param name="vertical">The thickness on the top and bottom.</param>
	public Thickness(double horizontal, double vertical)
	{
		_left = (_right = horizontal);
		_top = (_bottom = vertical);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Thickness" /> structure.
	/// </summary>
	/// <param name="left">The thickness on the left.</param>
	/// <param name="top">The thickness on the top.</param>
	/// <param name="right">The thickness on the right.</param>
	/// <param name="bottom">The thickness on the bottom.</param>
	public Thickness(double left, double top, double right, double bottom)
	{
		_left = left;
		_top = top;
		_right = right;
		_bottom = bottom;
	}

	/// <summary>
	/// Compares two Thicknesses.
	/// </summary>
	/// <param name="a">The first thickness.</param>
	/// <param name="b">The second thickness.</param>
	/// <returns>The equality.</returns>
	public static bool operator ==(Thickness a, Thickness b)
	{
		return a.Equals(b);
	}

	/// <summary>
	/// Compares two Thicknesses.
	/// </summary>
	/// <param name="a">The first thickness.</param>
	/// <param name="b">The second thickness.</param>
	/// <returns>The inequality.</returns>
	public static bool operator !=(Thickness a, Thickness b)
	{
		return !a.Equals(b);
	}

	/// <summary>
	/// Adds two Thicknesses.
	/// </summary>
	/// <param name="a">The first thickness.</param>
	/// <param name="b">The second thickness.</param>
	/// <returns>The equality.</returns>
	public static Thickness operator +(Thickness a, Thickness b)
	{
		return new Thickness(a.Left + b.Left, a.Top + b.Top, a.Right + b.Right, a.Bottom + b.Bottom);
	}

	/// <summary>
	/// Subtracts two Thicknesses.
	/// </summary>
	/// <param name="a">The first thickness.</param>
	/// <param name="b">The second thickness.</param>
	/// <returns>The equality.</returns>
	public static Thickness operator -(Thickness a, Thickness b)
	{
		return new Thickness(a.Left - b.Left, a.Top - b.Top, a.Right - b.Right, a.Bottom - b.Bottom);
	}

	/// <summary>
	/// Multiplies a Thickness to a scalar.
	/// </summary>
	/// <param name="a">The thickness.</param>
	/// <param name="b">The scalar.</param>
	/// <returns>The equality.</returns>
	public static Thickness operator *(Thickness a, double b)
	{
		return new Thickness(a.Left * b, a.Top * b, a.Right * b, a.Bottom * b);
	}

	/// <summary>
	/// Adds a Thickness to a Size.
	/// </summary>
	/// <param name="size">The size.</param>
	/// <param name="thickness">The thickness.</param>
	/// <returns>The equality.</returns>
	public static Size operator +(Size size, Thickness thickness)
	{
		return new Size(size.Width + thickness.Left + thickness.Right, size.Height + thickness.Top + thickness.Bottom);
	}

	/// <summary>
	/// Subtracts a Thickness from a Size.
	/// </summary>
	/// <param name="size">The size.</param>
	/// <param name="thickness">The thickness.</param>
	/// <returns>The equality.</returns>
	public static Size operator -(Size size, Thickness thickness)
	{
		return new Size(size.Width - (thickness.Left + thickness.Right), size.Height - (thickness.Top + thickness.Bottom));
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.Thickness" /> string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>The <see cref="T:Avalonia.Thickness" />.</returns>
	public static Thickness Parse(string s)
	{
		using SpanStringTokenizer spanStringTokenizer = new SpanStringTokenizer(s, CultureInfo.InvariantCulture, "Invalid Thickness.");
		if (spanStringTokenizer.TryReadDouble(out var result))
		{
			if (spanStringTokenizer.TryReadDouble(out var result2))
			{
				if (spanStringTokenizer.TryReadDouble(out var result3))
				{
					return new Thickness(result, result2, result3, spanStringTokenizer.ReadDouble());
				}
				return new Thickness(result, result2);
			}
			return new Thickness(result);
		}
		throw new FormatException("Invalid Thickness.");
	}

	/// <summary>
	/// Returns a boolean indicating whether the thickness is equal to the other given point.
	/// </summary>
	/// <param name="other">The other thickness to test equality against.</param>
	/// <returns>True if this thickness is equal to other; False otherwise.</returns>
	public bool Equals(Thickness other)
	{
		if (_left == other._left && _top == other._top && _right == other._right)
		{
			return _bottom == other._bottom;
		}
		return false;
	}

	/// <summary>
	/// Checks for equality between a thickness and an object.
	/// </summary>
	/// <param name="obj">The object.</param>
	/// <returns>
	/// True if <paramref name="obj" /> is a size that equals the current size.
	/// </returns>
	public override bool Equals(object? obj)
	{
		if (obj is Thickness other)
		{
			return Equals(other);
		}
		return false;
	}

	/// <summary>
	/// Returns a hash code for a <see cref="T:Avalonia.Thickness" />.
	/// </summary>
	/// <returns>The hash code.</returns>
	public override int GetHashCode()
	{
		return (((17 * 23 + Left.GetHashCode()) * 23 + Top.GetHashCode()) * 23 + Right.GetHashCode()) * 23 + Bottom.GetHashCode();
	}

	/// <summary>
	/// Returns the string representation of the thickness.
	/// </summary>
	/// <returns>The string representation of the thickness.</returns>
	public override string ToString()
	{
		return FormattableString.Invariant($"{_left},{_top},{_right},{_bottom}");
	}

	/// <summary>
	/// Deconstructor the thickness into its left, top, right and bottom thickness values.
	/// </summary>
	/// <param name="left">The thickness on the left.</param>
	/// <param name="top">The thickness on the top.</param>
	/// <param name="right">The thickness on the right.</param>
	/// <param name="bottom">The thickness on the bottom.</param>
	public void Deconstruct(out double left, out double top, out double right, out double bottom)
	{
		left = _left;
		top = _top;
		right = _right;
		bottom = _bottom;
	}
}
