using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;

namespace Avalonia;

/// <summary>
/// Defines a rectangle.
/// </summary>
public readonly struct Rect : IEquatable<Rect>
{
	/// <summary>
	/// The X position.
	/// </summary>
	private readonly double _x;

	/// <summary>
	/// The Y position.
	/// </summary>
	private readonly double _y;

	/// <summary>
	/// The width.
	/// </summary>
	private readonly double _width;

	/// <summary>
	/// The height.
	/// </summary>
	private readonly double _height;

	/// <summary>
	/// Gets the X position.
	/// </summary>
	public double X => _x;

	/// <summary>
	/// Gets the Y position.
	/// </summary>
	public double Y => _y;

	/// <summary>
	/// Gets the width.
	/// </summary>
	public double Width => _width;

	/// <summary>
	/// Gets the height.
	/// </summary>
	public double Height => _height;

	/// <summary>
	/// Gets the position of the rectangle.
	/// </summary>
	public Point Position => new Point(_x, _y);

	/// <summary>
	/// Gets the size of the rectangle.
	/// </summary>
	public Size Size => new Size(_width, _height);

	/// <summary>
	/// Gets the right position of the rectangle.
	/// </summary>
	public double Right => _x + _width;

	/// <summary>
	/// Gets the bottom position of the rectangle.
	/// </summary>
	public double Bottom => _y + _height;

	/// <summary>
	/// Gets the left position.
	/// </summary>
	public double Left => _x;

	/// <summary>
	/// Gets the top position.
	/// </summary>
	public double Top => _y;

	/// <summary>
	/// Gets the top left point of the rectangle.
	/// </summary>
	public Point TopLeft => new Point(_x, _y);

	/// <summary>
	/// Gets the top right point of the rectangle.
	/// </summary>
	public Point TopRight => new Point(Right, _y);

	/// <summary>
	/// Gets the bottom left point of the rectangle.
	/// </summary>
	public Point BottomLeft => new Point(_x, Bottom);

	/// <summary>
	/// Gets the bottom right point of the rectangle.
	/// </summary>
	public Point BottomRight => new Point(Right, Bottom);

	/// <summary>
	/// Gets the center point of the rectangle.
	/// </summary>
	public Point Center => new Point(_x + _width / 2.0, _y + _height / 2.0);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Rect" /> structure.
	/// </summary>
	/// <param name="x">The X position.</param>
	/// <param name="y">The Y position.</param>
	/// <param name="width">The width.</param>
	/// <param name="height">The height.</param>
	public Rect(double x, double y, double width, double height)
	{
		_x = x;
		_y = y;
		_width = width;
		_height = height;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Rect" /> structure.
	/// </summary>
	/// <param name="size">The size of the rectangle.</param>
	public Rect(Size size)
	{
		_x = 0.0;
		_y = 0.0;
		_width = size.Width;
		_height = size.Height;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Rect" /> structure.
	/// </summary>
	/// <param name="position">The position of the rectangle.</param>
	/// <param name="size">The size of the rectangle.</param>
	public Rect(Point position, Size size)
	{
		_x = position.X;
		_y = position.Y;
		_width = size.Width;
		_height = size.Height;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Rect" /> structure.
	/// </summary>
	/// <param name="topLeft">The top left position of the rectangle.</param>
	/// <param name="bottomRight">The bottom right position of the rectangle.</param>
	public Rect(Point topLeft, Point bottomRight)
	{
		_x = topLeft.X;
		_y = topLeft.Y;
		_width = bottomRight.X - topLeft.X;
		_height = bottomRight.Y - topLeft.Y;
	}

	/// <summary>
	/// Checks for equality between two <see cref="T:Avalonia.Rect" />s.
	/// </summary>
	/// <param name="left">The first rect.</param>
	/// <param name="right">The second rect.</param>
	/// <returns>True if the rects are equal; otherwise false.</returns>
	public static bool operator ==(Rect left, Rect right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Checks for inequality between two <see cref="T:Avalonia.Rect" />s.
	/// </summary>
	/// <param name="left">The first rect.</param>
	/// <param name="right">The second rect.</param>
	/// <returns>True if the rects are unequal; otherwise false.</returns>
	public static bool operator !=(Rect left, Rect right)
	{
		return !(left == right);
	}

	/// <summary>
	/// Multiplies a rectangle by a scaling vector.
	/// </summary>
	/// <param name="rect">The rectangle.</param>
	/// <param name="scale">The vector scale.</param>
	/// <returns>The scaled rectangle.</returns>
	public static Rect operator *(Rect rect, Vector scale)
	{
		return new Rect(rect.X * scale.X, rect.Y * scale.Y, rect.Width * scale.X, rect.Height * scale.Y);
	}

	/// <summary>
	/// Multiplies a rectangle by a scale.
	/// </summary>
	/// <param name="rect">The rectangle.</param>
	/// <param name="scale">The scale.</param>
	/// <returns>The scaled rectangle.</returns>
	public static Rect operator *(Rect rect, double scale)
	{
		return new Rect(rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
	}

	/// <summary>
	/// Divides a rectangle by a vector.
	/// </summary>
	/// <param name="rect">The rectangle.</param>
	/// <param name="scale">The vector scale.</param>
	/// <returns>The scaled rectangle.</returns>
	public static Rect operator /(Rect rect, Vector scale)
	{
		return new Rect(rect.X / scale.X, rect.Y / scale.Y, rect.Width / scale.X, rect.Height / scale.Y);
	}

	/// <summary>
	/// Determines whether a point is in the bounds of the rectangle.
	/// </summary>
	/// <param name="p">The point.</param>
	/// <returns>true if the point is in the bounds of the rectangle; otherwise false.</returns>
	public bool Contains(Point p)
	{
		if (p.X >= _x && p.X <= _x + _width && p.Y >= _y)
		{
			return p.Y <= _y + _height;
		}
		return false;
	}

	/// <summary>
	/// Determines whether a point is in the bounds of the rectangle, exclusive of the
	/// rectangle's bottom/right edge.
	/// </summary>
	/// <param name="p">The point.</param>
	/// <returns>true if the point is in the bounds of the rectangle; otherwise false.</returns>    
	public bool ContainsExclusive(Point p)
	{
		if (p.X >= _x && p.X < _x + _width && p.Y >= _y)
		{
			return p.Y < _y + _height;
		}
		return false;
	}

	/// <summary>
	/// Determines whether the rectangle fully contains another rectangle.
	/// </summary>
	/// <param name="r">The rectangle.</param>
	/// <returns>true if the rectangle is fully contained; otherwise false.</returns>
	public bool Contains(Rect r)
	{
		if (Contains(r.TopLeft))
		{
			return Contains(r.BottomRight);
		}
		return false;
	}

	/// <summary>
	/// Centers another rectangle in this rectangle.
	/// </summary>
	/// <param name="rect">The rectangle to center.</param>
	/// <returns>The centered rectangle.</returns>
	public Rect CenterRect(Rect rect)
	{
		return new Rect(_x + (_width - rect._width) / 2.0, _y + (_height - rect._height) / 2.0, rect._width, rect._height);
	}

	/// <summary>
	/// Inflates the rectangle.
	/// </summary>
	/// <param name="thickness">The thickness to be subtracted for each side of the rectangle.</param>
	/// <returns>The inflated rectangle.</returns>
	public Rect Inflate(double thickness)
	{
		return Inflate(new Thickness(thickness));
	}

	/// <summary>
	/// Inflates the rectangle.
	/// </summary>
	/// <param name="thickness">The thickness to be subtracted for each side of the rectangle.</param>
	/// <returns>The inflated rectangle.</returns>
	public Rect Inflate(Thickness thickness)
	{
		return new Rect(new Point(_x - thickness.Left, _y - thickness.Top), Size.Inflate(thickness));
	}

	/// <summary>
	/// Deflates the rectangle.
	/// </summary>
	/// <param name="thickness">The thickness to be subtracted for each side of the rectangle.</param>
	/// <returns>The deflated rectangle.</returns>
	public Rect Deflate(double thickness)
	{
		return Deflate(new Thickness(thickness));
	}

	/// <summary>
	/// Deflates the rectangle by a <see cref="T:Avalonia.Thickness" />.
	/// </summary>
	/// <param name="thickness">The thickness to be subtracted for each side of the rectangle.</param>
	/// <returns>The deflated rectangle.</returns>
	public Rect Deflate(Thickness thickness)
	{
		return new Rect(new Point(_x + thickness.Left, _y + thickness.Top), Size.Deflate(thickness));
	}

	/// <summary>
	/// Returns a boolean indicating whether the rect is equal to the other given rect.
	/// </summary>
	/// <param name="other">The other rect to test equality against.</param>
	/// <returns>True if this rect is equal to other; False otherwise.</returns>
	public bool Equals(Rect other)
	{
		if (_x == other._x && _y == other._y && _width == other._width)
		{
			return _height == other._height;
		}
		return false;
	}

	/// <summary>
	/// Returns a boolean indicating whether the given object is equal to this rectangle.
	/// </summary>
	/// <param name="obj">The object to compare against.</param>
	/// <returns>True if the object is equal to this rectangle; false otherwise.</returns>
	public override bool Equals(object? obj)
	{
		if (obj is Rect other)
		{
			return Equals(other);
		}
		return false;
	}

	/// <summary>
	/// Returns the hash code for this instance.
	/// </summary>
	/// <returns>The hash code.</returns>
	public override int GetHashCode()
	{
		return (((17 * 23 + X.GetHashCode()) * 23 + Y.GetHashCode()) * 23 + Width.GetHashCode()) * 23 + Height.GetHashCode();
	}

	/// <summary>
	/// Gets the intersection of two rectangles.
	/// </summary>
	/// <param name="rect">The other rectangle.</param>
	/// <returns>The intersection.</returns>
	public Rect Intersect(Rect rect)
	{
		double num = ((rect.X > X) ? rect.X : X);
		double num2 = ((rect.Y > Y) ? rect.Y : Y);
		double num3 = ((rect.Right < Right) ? rect.Right : Right);
		double num4 = ((rect.Bottom < Bottom) ? rect.Bottom : Bottom);
		if (num3 > num && num4 > num2)
		{
			return new Rect(num, num2, num3 - num, num4 - num2);
		}
		return default(Rect);
	}

	/// <summary>
	/// Determines whether a rectangle intersects with this rectangle.
	/// </summary>
	/// <param name="rect">The other rectangle.</param>
	/// <returns>
	/// True if the specified rectangle intersects with this one; otherwise false.
	/// </returns>
	public bool Intersects(Rect rect)
	{
		if (rect.X < Right && X < rect.Right && rect.Y < Bottom)
		{
			return Y < rect.Bottom;
		}
		return false;
	}

	/// <summary>
	/// Returns the axis-aligned bounding box of a transformed rectangle.
	/// </summary>
	/// <param name="matrix">The transform.</param>
	/// <returns>The bounding box</returns>
	public Rect TransformToAABB(Matrix matrix)
	{
		ReadOnlySpan<Point> readOnlySpan = stackalloc Point[4]
		{
			TopLeft.Transform(matrix),
			TopRight.Transform(matrix),
			BottomRight.Transform(matrix),
			BottomLeft.Transform(matrix)
		};
		double num = double.MaxValue;
		double num2 = double.MinValue;
		double num3 = double.MaxValue;
		double num4 = double.MinValue;
		ReadOnlySpan<Point> readOnlySpan2 = readOnlySpan;
		for (int i = 0; i < readOnlySpan2.Length; i++)
		{
			Point point = readOnlySpan2[i];
			if (point.X < num)
			{
				num = point.X;
			}
			if (point.X > num2)
			{
				num2 = point.X;
			}
			if (point.Y < num3)
			{
				num3 = point.Y;
			}
			if (point.Y > num4)
			{
				num4 = point.Y;
			}
		}
		return new Rect(new Point(num, num3), new Point(num2, num4));
	}

	internal Rect TransformToAABB(Matrix4x4 matrix)
	{
		ReadOnlySpan<Point> readOnlySpan = stackalloc Point[4]
		{
			TopLeft.Transform(matrix),
			TopRight.Transform(matrix),
			BottomRight.Transform(matrix),
			BottomLeft.Transform(matrix)
		};
		double num = double.MaxValue;
		double num2 = double.MinValue;
		double num3 = double.MaxValue;
		double num4 = double.MinValue;
		ReadOnlySpan<Point> readOnlySpan2 = readOnlySpan;
		for (int i = 0; i < readOnlySpan2.Length; i++)
		{
			Point point = readOnlySpan2[i];
			if (point.X < num)
			{
				num = point.X;
			}
			if (point.X > num2)
			{
				num2 = point.X;
			}
			if (point.Y < num3)
			{
				num3 = point.Y;
			}
			if (point.Y > num4)
			{
				num4 = point.Y;
			}
		}
		return new Rect(new Point(num, num3), new Point(num2, num4));
	}

	/// <summary>
	/// Translates the rectangle by an offset.
	/// </summary>
	/// <param name="offset">The offset.</param>
	/// <returns>The translated rectangle.</returns>
	public Rect Translate(Vector offset)
	{
		return new Rect(Position + offset, Size);
	}

	/// <summary>
	/// Normalizes the rectangle so both the <see cref="P:Avalonia.Rect.Width" /> and <see cref="P:Avalonia.Rect.Height" /> are positive, without changing the location of the rectangle
	/// </summary>
	/// <returns>Normalized Rect</returns>
	/// <remarks>
	/// Empty rect will be return when Rect contains invalid values. Like NaN.
	/// </remarks>
	public Rect Normalize()
	{
		Rect result = this;
		if (double.IsNaN(result.Right) || double.IsNaN(result.Bottom) || double.IsNaN(result.X) || double.IsNaN(result.Y) || double.IsNaN(Height) || double.IsNaN(Width))
		{
			return default(Rect);
		}
		if (result.Width < 0.0)
		{
			double num = X + Width;
			double width = X - num;
			result = result.WithX(num).WithWidth(width);
		}
		if (result.Height < 0.0)
		{
			double num2 = Y + Height;
			double height = Y - num2;
			result = result.WithY(num2).WithHeight(height);
		}
		return result;
	}

	/// <summary>
	/// Gets the union of two rectangles.
	/// </summary>
	/// <param name="rect">The other rectangle.</param>
	/// <returns>The union.</returns>
	public Rect Union(Rect rect)
	{
		if (Width == 0.0 && Height == 0.0)
		{
			return rect;
		}
		if (rect.Width == 0.0 && rect.Height == 0.0)
		{
			return this;
		}
		double x = Math.Min(X, rect.X);
		double x2 = Math.Max(Right, rect.Right);
		double y = Math.Min(Y, rect.Y);
		return new Rect(bottomRight: new Point(x2, Math.Max(Bottom, rect.Bottom)), topLeft: new Point(x, y));
	}

	internal static Rect? Union(Rect? left, Rect? right)
	{
		if (!left.HasValue)
		{
			return right;
		}
		if (!right.HasValue)
		{
			return left;
		}
		return left.Value.Union(right.Value);
	}

	/// <summary>
	/// Returns a new <see cref="T:Avalonia.Rect" /> with the specified X position.
	/// </summary>
	/// <param name="x">The x position.</param>
	/// <returns>The new <see cref="T:Avalonia.Rect" />.</returns>
	public Rect WithX(double x)
	{
		return new Rect(x, _y, _width, _height);
	}

	/// <summary>
	/// Returns a new <see cref="T:Avalonia.Rect" /> with the specified Y position.
	/// </summary>
	/// <param name="y">The y position.</param>
	/// <returns>The new <see cref="T:Avalonia.Rect" />.</returns>
	public Rect WithY(double y)
	{
		return new Rect(_x, y, _width, _height);
	}

	/// <summary>
	/// Returns a new <see cref="T:Avalonia.Rect" /> with the specified width.
	/// </summary>
	/// <param name="width">The width.</param>
	/// <returns>The new <see cref="T:Avalonia.Rect" />.</returns>
	public Rect WithWidth(double width)
	{
		return new Rect(_x, _y, width, _height);
	}

	/// <summary>
	/// Returns a new <see cref="T:Avalonia.Rect" /> with the specified height.
	/// </summary>
	/// <param name="height">The height.</param>
	/// <returns>The new <see cref="T:Avalonia.Rect" />.</returns>
	public Rect WithHeight(double height)
	{
		return new Rect(_x, _y, _width, height);
	}

	/// <summary>
	/// Returns the string representation of the rectangle.
	/// </summary>
	/// <returns>The string representation of the rectangle.</returns>
	public override string ToString()
	{
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		InlineArray4<object> buffer = default(InlineArray4<object>);
		buffer[0] = _x;
		buffer[1] = _y;
		buffer[2] = _width;
		buffer[3] = _height;
		return string.Format((IFormatProvider?)invariantCulture, "{0}, {1}, {2}, {3}", (ReadOnlySpan<object?>)buffer);
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.Rect" /> string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>The parsed <see cref="T:Avalonia.Rect" />.</returns>
	public static Rect Parse(string s)
	{
		using SpanStringTokenizer spanStringTokenizer = new SpanStringTokenizer(s, CultureInfo.InvariantCulture, "Invalid Rect.");
		return new Rect(spanStringTokenizer.ReadDouble(), spanStringTokenizer.ReadDouble(), spanStringTokenizer.ReadDouble(), spanStringTokenizer.ReadDouble());
	}

	/// <summary>
	/// This method should be used internally to check for the rect emptiness
	/// Once we add support for WPF-like empty rects, there will be an actual implementation
	/// For now it's internal to keep some loud community members happy about the API being pretty 
	/// </summary>
	internal bool IsEmpty()
	{
		return this == default(Rect);
	}
}
