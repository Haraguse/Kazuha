using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;

namespace Avalonia;

/// <summary>
/// Represents a rectangle in device pixels.
/// </summary>
public readonly struct PixelRect : IEquatable<PixelRect>
{
	/// <summary>
	/// Gets the X position.
	/// </summary>
	public int X { get; }

	/// <summary>
	/// Gets the Y position.
	/// </summary>
	public int Y { get; }

	/// <summary>
	/// Gets the width.
	/// </summary>
	public int Width { get; }

	/// <summary>
	/// Gets the height.
	/// </summary>
	public int Height { get; }

	/// <summary>
	/// Gets the position of the rectangle.
	/// </summary>
	public PixelPoint Position => new PixelPoint(X, Y);

	/// <summary>
	/// Gets the size of the rectangle.
	/// </summary>
	public PixelSize Size => new PixelSize(Width, Height);

	/// <summary>
	/// Gets the right position of the rectangle.
	/// </summary>
	public int Right => X + Width;

	/// <summary>
	/// Gets the bottom position of the rectangle.
	/// </summary>
	public int Bottom => Y + Height;

	/// <summary>
	/// Gets the top left point of the rectangle.
	/// </summary>
	public PixelPoint TopLeft => new PixelPoint(X, Y);

	/// <summary>
	/// Gets the top right point of the rectangle.
	/// </summary>
	public PixelPoint TopRight => new PixelPoint(Right, Y);

	/// <summary>
	/// Gets the bottom left point of the rectangle.
	/// </summary>
	public PixelPoint BottomLeft => new PixelPoint(X, Bottom);

	/// <summary>
	/// Gets the bottom right point of the rectangle.
	/// </summary>
	public PixelPoint BottomRight => new PixelPoint(Right, Bottom);

	/// <summary>
	/// Gets the center point of the rectangle.
	/// </summary>
	public PixelPoint Center => new PixelPoint(X + Width / 2, Y + Height / 2);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.PixelRect" /> structure.
	/// </summary>
	/// <param name="x">The X position.</param>
	/// <param name="y">The Y position.</param>
	/// <param name="width">The width.</param>
	/// <param name="height">The height.</param>
	public PixelRect(int x, int y, int width, int height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.PixelRect" /> structure.
	/// </summary>
	/// <param name="size">The size of the rectangle.</param>
	public PixelRect(PixelSize size)
	{
		X = 0;
		Y = 0;
		Width = size.Width;
		Height = size.Height;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.PixelRect" /> structure.
	/// </summary>
	/// <param name="position">The position of the rectangle.</param>
	/// <param name="size">The size of the rectangle.</param>
	public PixelRect(PixelPoint position, PixelSize size)
	{
		X = position.X;
		Y = position.Y;
		Width = size.Width;
		Height = size.Height;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.PixelRect" /> structure.
	/// </summary>
	/// <param name="topLeft">The top left position of the rectangle.</param>
	/// <param name="bottomRight">The bottom right position of the rectangle.</param>
	public PixelRect(PixelPoint topLeft, PixelPoint bottomRight)
	{
		X = topLeft.X;
		Y = topLeft.Y;
		Width = bottomRight.X - topLeft.X;
		Height = bottomRight.Y - topLeft.Y;
	}

	/// <summary>
	/// Checks for equality between two <see cref="T:Avalonia.PixelRect" />s.
	/// </summary>
	/// <param name="left">The first rect.</param>
	/// <param name="right">The second rect.</param>
	/// <returns>True if the rects are equal; otherwise false.</returns>
	public static bool operator ==(PixelRect left, PixelRect right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Checks for inequality between two <see cref="T:Avalonia.PixelRect" />s.
	/// </summary>
	/// <param name="left">The first rect.</param>
	/// <param name="right">The second rect.</param>
	/// <returns>True if the rects are unequal; otherwise false.</returns>
	public static bool operator !=(PixelRect left, PixelRect right)
	{
		return !(left == right);
	}

	/// <summary>
	/// Determines whether a point in the bounds of the rectangle.
	/// </summary>
	/// <param name="p">The point.</param>
	/// <returns>true if the point is in the bounds of the rectangle; otherwise false.</returns>
	public bool Contains(PixelPoint p)
	{
		if (p.X >= X && p.X <= Right && p.Y >= Y)
		{
			return p.Y <= Bottom;
		}
		return false;
	}

	/// <summary>
	/// Determines whether a point is in the bounds of the rectangle, exclusive of the
	/// rectangle's bottom/right edge.
	/// </summary>
	/// <param name="p">The point.</param>
	/// <returns>true if the point is in the bounds of the rectangle; otherwise false.</returns>    
	public bool ContainsExclusive(PixelPoint p)
	{
		if (p.X >= X && p.X < X + Width && p.Y >= Y)
		{
			return p.Y < Y + Height;
		}
		return false;
	}

	/// <summary>
	/// Determines whether the rectangle fully contains another rectangle.
	/// </summary>
	/// <param name="r">The rectangle.</param>
	/// <returns>true if the rectangle is fully contained; otherwise false.</returns>
	public bool Contains(PixelRect r)
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
	public PixelRect CenterRect(PixelRect rect)
	{
		return new PixelRect(X + (Width - rect.Width) / 2, Y + (Height - rect.Height) / 2, rect.Width, rect.Height);
	}

	/// <summary>
	/// Returns a boolean indicating whether the rect is equal to the other given rect.
	/// </summary>
	/// <param name="other">The other rect to test equality against.</param>
	/// <returns>True if this rect is equal to other; False otherwise.</returns>
	public bool Equals(PixelRect other)
	{
		if (Position == other.Position)
		{
			return Size == other.Size;
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
		if (obj is PixelRect other)
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
	public PixelRect Intersect(PixelRect rect)
	{
		int num = ((rect.X > X) ? rect.X : X);
		int num2 = ((rect.Y > Y) ? rect.Y : Y);
		int num3 = ((rect.Right < Right) ? rect.Right : Right);
		int num4 = ((rect.Bottom < Bottom) ? rect.Bottom : Bottom);
		if (num3 > num && num4 > num2)
		{
			return new PixelRect(num, num2, num3 - num, num4 - num2);
		}
		return default(PixelRect);
	}

	/// <summary>
	/// Determines whether a rectangle intersects with this rectangle.
	/// </summary>
	/// <param name="rect">The other rectangle.</param>
	/// <returns>
	/// True if the specified rectangle intersects with this one; otherwise false.
	/// </returns>
	public bool Intersects(PixelRect rect)
	{
		if (rect.X < Right && X < rect.Right && rect.Y < Bottom)
		{
			return Y < rect.Bottom;
		}
		return false;
	}

	/// <summary>
	/// Translates the rectangle by an offset.
	/// </summary>
	/// <param name="offset">The offset.</param>
	/// <returns>The translated rectangle.</returns>
	public PixelRect Translate(PixelVector offset)
	{
		return new PixelRect(Position + offset, Size);
	}

	/// <summary>
	/// Gets the union of two rectangles.
	/// </summary>
	/// <param name="rect">The other rectangle.</param>
	/// <returns>The union.</returns>
	public PixelRect Union(PixelRect rect)
	{
		if (Width == 0 && Height == 0)
		{
			return rect;
		}
		if (rect.Width == 0 && rect.Height == 0)
		{
			return this;
		}
		int x = Math.Min(X, rect.X);
		int x2 = Math.Max(Right, rect.Right);
		int y = Math.Min(Y, rect.Y);
		return new PixelRect(bottomRight: new PixelPoint(x2, Math.Max(Bottom, rect.Bottom)), topLeft: new PixelPoint(x, y));
	}

	/// <summary>
	/// Returns a new <see cref="T:Avalonia.PixelRect" /> with the specified X position.
	/// </summary>
	/// <param name="x">The x position.</param>
	/// <returns>The new <see cref="T:Avalonia.PixelRect" />.</returns>
	public PixelRect WithX(int x)
	{
		return new PixelRect(x, Y, Width, Height);
	}

	/// <summary>
	/// Returns a new <see cref="T:Avalonia.PixelRect" /> with the specified Y position.
	/// </summary>
	/// <param name="y">The y position.</param>
	/// <returns>The new <see cref="T:Avalonia.PixelRect" />.</returns>
	public PixelRect WithY(int y)
	{
		return new PixelRect(X, y, Width, Height);
	}

	/// <summary>
	/// Returns a new <see cref="T:Avalonia.PixelRect" /> with the specified width.
	/// </summary>
	/// <param name="width">The width.</param>
	/// <returns>The new <see cref="T:Avalonia.PixelRect" />.</returns>
	public PixelRect WithWidth(int width)
	{
		return new PixelRect(X, Y, width, Height);
	}

	/// <summary>
	/// Returns a new <see cref="T:Avalonia.PixelRect" /> with the specified height.
	/// </summary>
	/// <param name="height">The height.</param>
	/// <returns>The new <see cref="T:Avalonia.PixelRect" />.</returns>
	public PixelRect WithHeight(int height)
	{
		return new PixelRect(X, Y, Width, height);
	}

	/// <summary>
	/// Converts the <see cref="T:Avalonia.PixelRect" /> to a device-independent <see cref="T:Avalonia.Rect" /> using the
	/// specified scaling factor.
	/// </summary>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The device-independent rect.</returns>
	public Rect ToRect(double scale)
	{
		return new Rect(Position.ToPoint(scale), Size.ToSize(scale));
	}

	/// <summary>
	/// Converts the <see cref="T:Avalonia.PixelRect" /> to a device-independent <see cref="T:Avalonia.Rect" /> using the
	/// specified scaling factor.
	/// </summary>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The device-independent rect.</returns>
	public Rect ToRect(Vector scale)
	{
		return new Rect(Position.ToPoint(scale), Size.ToSize(scale));
	}

	/// <summary>
	/// Converts the <see cref="T:Avalonia.PixelRect" /> to a device-independent <see cref="T:Avalonia.Rect" /> using the
	/// specified dots per inch (DPI).
	/// </summary>
	/// <param name="dpi">The dots per inch of the device.</param>
	/// <returns>The device-independent rect.</returns>
	public Rect ToRectWithDpi(double dpi)
	{
		return new Rect(Position.ToPointWithDpi(dpi), Size.ToSizeWithDpi(dpi));
	}

	/// <summary>
	/// Converts the <see cref="T:Avalonia.PixelRect" /> to a device-independent <see cref="T:Avalonia.Rect" /> using the
	/// specified dots per inch (DPI).
	/// </summary>
	/// <param name="dpi">The dots per inch of the device.</param>
	/// <returns>The device-independent rect.</returns>
	public Rect ToRectWithDpi(Vector dpi)
	{
		return new Rect(Position.ToPointWithDpi(dpi), Size.ToSizeWithDpi(dpi));
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.Rect" /> to device pixels using the specified scaling factor.
	/// </summary>
	/// <param name="rect">The rect.</param>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The device-independent rect.</returns>
	public static PixelRect FromRect(Rect rect, double scale)
	{
		return new PixelRect(PixelPoint.FromPoint(rect.Position, scale), FromPointCeiling(rect.BottomRight, new Vector(scale, scale)));
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.Rect" /> to device pixels using the specified scaling factor.
	/// </summary>
	/// <param name="rect">The rect.</param>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>The device-independent point.</returns>
	public static PixelRect FromRect(Rect rect, Vector scale)
	{
		return new PixelRect(PixelPoint.FromPoint(rect.Position, scale), FromPointCeiling(rect.BottomRight, scale));
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.Rect" /> to device pixels using the specified dots per inch (DPI).
	/// </summary>
	/// <param name="rect">The rect.</param>
	/// <param name="dpi">The dots per inch of the device.</param>
	/// <returns>The device-independent point.</returns>
	public static PixelRect FromRectWithDpi(Rect rect, double dpi)
	{
		return new PixelRect(PixelPoint.FromPointWithDpi(rect.Position, dpi), FromPointCeiling(rect.BottomRight, new Vector(dpi / 96.0, dpi / 96.0)));
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.Rect" /> to device pixels using the specified dots per inch (DPI).
	/// </summary>
	/// <param name="rect">The rect.</param>
	/// <param name="dpi">The dots per inch of the device.</param>
	/// <returns>The device-independent point.</returns>
	public static PixelRect FromRectWithDpi(Rect rect, Vector dpi)
	{
		return new PixelRect(PixelPoint.FromPointWithDpi(rect.Position, dpi), FromPointCeiling(rect.BottomRight, dpi / 96.0));
	}

	/// <summary>
	/// Returns the string representation of the rectangle.
	/// </summary>
	/// <returns>The string representation of the rectangle.</returns>
	public override string ToString()
	{
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		InlineArray4<object> buffer = default(InlineArray4<object>);
		buffer[0] = X;
		buffer[1] = Y;
		buffer[2] = Width;
		buffer[3] = Height;
		return string.Format((IFormatProvider?)invariantCulture, "{0}, {1}, {2}, {3}", (ReadOnlySpan<object?>)buffer);
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.PixelRect" /> string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>The parsed <see cref="T:Avalonia.PixelRect" />.</returns>
	public static PixelRect Parse(string s)
	{
		using SpanStringTokenizer spanStringTokenizer = new SpanStringTokenizer(s, CultureInfo.InvariantCulture, "Invalid PixelRect.");
		return new PixelRect(spanStringTokenizer.ReadInt32(), spanStringTokenizer.ReadInt32(), spanStringTokenizer.ReadInt32(), spanStringTokenizer.ReadInt32());
	}

	private static PixelPoint FromPointCeiling(Point point, Vector scale)
	{
		return new PixelPoint((int)Math.Ceiling(point.X * scale.X), (int)Math.Ceiling(point.Y * scale.Y));
	}
}
