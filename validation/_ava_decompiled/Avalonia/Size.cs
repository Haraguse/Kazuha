using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;

namespace Avalonia;

/// <summary>
/// Defines a size.
/// </summary>
public readonly struct Size : IEquatable<Size>
{
	/// <summary>
	/// A size representing infinity.
	/// </summary>
	public static readonly Size Infinity = new Size(double.PositiveInfinity, double.PositiveInfinity);

	/// <summary>
	/// The width.
	/// </summary>
	private readonly double _width;

	/// <summary>
	/// The height.
	/// </summary>
	private readonly double _height;

	/// <summary>
	/// Gets the aspect ratio of the size.
	/// </summary>
	public double AspectRatio => _width / _height;

	/// <summary>
	/// Gets the width.
	/// </summary>
	public double Width => _width;

	/// <summary>
	/// Gets the height.
	/// </summary>
	public double Height => _height;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Size" /> structure.
	/// </summary>
	/// <param name="width">The width.</param>
	/// <param name="height">The height.</param>
	public Size(double width, double height)
	{
		_width = width;
		_height = height;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Size" /> structure.
	/// </summary>
	/// <param name="vector2">The vector to take values from.</param>
	public Size(Vector2 vector2)
		: this(vector2.X, vector2.Y)
	{
	}

	/// <summary>
	/// Checks for equality between two <see cref="T:Avalonia.Size" />s.
	/// </summary>
	/// <param name="left">The first size.</param>
	/// <param name="right">The second size.</param>
	/// <returns>True if the sizes are equal; otherwise false.</returns>
	public static bool operator ==(Size left, Size right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Checks for inequality between two <see cref="T:Avalonia.Size" />s.
	/// </summary>
	/// <param name="left">The first size.</param>
	/// <param name="right">The second size.</param>
	/// <returns>True if the sizes are unequal; otherwise false.</returns>
	public static bool operator !=(Size left, Size right)
	{
		return !(left == right);
	}

	/// <summary>
	/// Scales a size.
	/// </summary>
	/// <param name="size">The size</param>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The scaled size.</returns>
	public static Size operator *(Size size, Vector scale)
	{
		return new Size(size._width * scale.X, size._height * scale.Y);
	}

	/// <summary>
	/// Scales a size.
	/// </summary>
	/// <param name="size">The size</param>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The scaled size.</returns>
	public static Size operator /(Size size, Vector scale)
	{
		return new Size(size._width / scale.X, size._height / scale.Y);
	}

	/// <summary>
	/// Divides a size by another size to produce a scaling factor.
	/// </summary>
	/// <param name="left">The first size</param>
	/// <param name="right">The second size.</param>
	/// <returns>The scaled size.</returns>
	public static Vector operator /(Size left, Size right)
	{
		return new Vector(left._width / right._width, left._height / right._height);
	}

	/// <summary>
	/// Scales a size.
	/// </summary>
	/// <param name="size">The size</param>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The scaled size.</returns>
	public static Size operator *(Size size, double scale)
	{
		return new Size(size._width * scale, size._height * scale);
	}

	/// <summary>
	/// Scales a size.
	/// </summary>
	/// <param name="size">The size</param>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The scaled size.</returns>
	public static Size operator /(Size size, double scale)
	{
		return new Size(size._width / scale, size._height / scale);
	}

	public static Size operator +(Size size, Size toAdd)
	{
		return new Size(size._width + toAdd._width, size._height + toAdd._height);
	}

	public static Size operator -(Size size, Size toSubtract)
	{
		return new Size(size._width - toSubtract._width, size._height - toSubtract._height);
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.Size" /> string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>The <see cref="T:Avalonia.Size" />.</returns>
	public static Size Parse(string s)
	{
		using SpanStringTokenizer spanStringTokenizer = new SpanStringTokenizer(s, CultureInfo.InvariantCulture, "Invalid Size.");
		return new Size(spanStringTokenizer.ReadDouble(), spanStringTokenizer.ReadDouble());
	}

	/// <summary>
	/// Constrains the size.
	/// </summary>
	/// <param name="constraint">The size to constrain to.</param>
	/// <returns>The constrained size.</returns>
	public Size Constrain(Size constraint)
	{
		return new Size(Math.Min(_width, constraint._width), Math.Min(_height, constraint._height));
	}

	/// <summary>
	/// Deflates the size by a <see cref="T:Avalonia.Thickness" />.
	/// </summary>
	/// <param name="thickness">The thickness.</param>
	/// <returns>The deflated size.</returns>
	/// <remarks>The deflated size cannot be less than 0.</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Size Deflate(Thickness thickness)
	{
		double num = _width - thickness.Left - thickness.Right;
		if (num < 0.0)
		{
			num = 0.0;
		}
		double num2 = _height - thickness.Top - thickness.Bottom;
		if (num2 < 0.0)
		{
			num2 = 0.0;
		}
		return new Size(num, num2);
	}

	/// <summary>
	/// Returns a boolean indicating whether the size is equal to the other given size (bitwise).
	/// </summary>
	/// <param name="other">The other size to test equality against.</param>
	/// <returns>True if this size is equal to other; False otherwise.</returns>
	public bool Equals(Size other)
	{
		if (_width == other._width)
		{
			return _height == other._height;
		}
		return false;
	}

	/// <summary>
	/// Returns a boolean indicating whether the size is equal to the other given size (numerically).
	/// </summary>
	/// <param name="other">The other size to test equality against.</param>
	/// <returns>True if this size is equal to other; False otherwise.</returns>
	public bool NearlyEquals(Size other)
	{
		if (MathUtilities.AreClose(_width, other._width))
		{
			return MathUtilities.AreClose(_height, other._height);
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
		if (obj is Size other)
		{
			return Equals(other);
		}
		return false;
	}

	/// <summary>
	/// Returns a hash code for a <see cref="T:Avalonia.Size" />.
	/// </summary>
	/// <returns>The hash code.</returns>
	public override int GetHashCode()
	{
		return (17 * 23 + Width.GetHashCode()) * 23 + Height.GetHashCode();
	}

	/// <summary>
	/// Inflates the size by a <see cref="T:Avalonia.Thickness" />.
	/// </summary>
	/// <param name="thickness">The thickness.</param>
	/// <returns>The inflated size.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Size Inflate(Thickness thickness)
	{
		return new Size(_width + thickness.Left + thickness.Right, _height + thickness.Top + thickness.Bottom);
	}

	/// <summary>
	/// Returns a new <see cref="T:Avalonia.Size" /> with the same height and the specified width.
	/// </summary>
	/// <param name="width">The width.</param>
	/// <returns>The new <see cref="T:Avalonia.Size" />.</returns>
	public Size WithWidth(double width)
	{
		return new Size(width, _height);
	}

	/// <summary>
	/// Returns a new <see cref="T:Avalonia.Size" /> with the same width and the specified height.
	/// </summary>
	/// <param name="height">The height.</param>
	/// <returns>The new <see cref="T:Avalonia.Size" />.</returns>
	public Size WithHeight(double height)
	{
		return new Size(_width, height);
	}

	/// <summary>
	/// Returns the string representation of the size.
	/// </summary>
	/// <returns>The string representation of the size.</returns>
	public override string ToString()
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", _width, _height);
	}

	/// <summary>
	/// Deconstructs the size into its Width and Height values.
	/// </summary>
	/// <param name="width">The width.</param>
	/// <param name="height">The height.</param>
	public void Deconstruct(out double width, out double height)
	{
		width = _width;
		height = _height;
	}
}
