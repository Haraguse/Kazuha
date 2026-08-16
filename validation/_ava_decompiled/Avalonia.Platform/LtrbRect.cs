using System;
using Avalonia.Metadata;

namespace Avalonia.Platform;

/// <summary>
/// This struct is essentially the same thing as MilRectD
/// Unlike our "normal" Rect which is more human-readable and human-usable
/// this struct is optimized for actual processing that doesn't really care
/// about Width and Height but pretty much always only cares about
/// Right and Bottom edge coordinates
///
/// Not having to constantly convert between Width/Height and Right/Bottom for no actual reason
/// saves us some perf
///
/// This structure is intended to be mostly internal, but it's exposed as a PrivateApi type so it can
/// be passed to the drawing backend when needed
/// </summary>
[PrivateApi]
public struct LtrbRect
{
	public double Left;

	public double Top;

	public double Right;

	public double Bottom;

	public double Width => Right - Left;

	public double Height => Bottom - Top;

	internal static LtrbRect Infinite { get; } = new LtrbRect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity);

	internal bool IsWellOrdered
	{
		get
		{
			if (Left <= Right)
			{
				return Top <= Bottom;
			}
			return false;
		}
	}

	internal bool IsZeroSize
	{
		get
		{
			if (Left != Right)
			{
				return Top == Bottom;
			}
			return true;
		}
	}

	internal bool IsEmpty => IsZeroSize;

	internal Point TopLeft => new Point(Left, Top);

	internal Point TopRight => new Point(Right, Top);

	internal Point BottomLeft => new Point(Left, Bottom);

	internal Point BottomRight => new Point(Right, Bottom);

	internal LtrbRect(double x, double y, double right, double bottom)
	{
		Left = x;
		Top = y;
		Right = right;
		Bottom = bottom;
	}

	internal LtrbRect(Rect rc)
	{
		rc = rc.Normalize();
		Left = rc.X;
		Top = rc.Y;
		Right = rc.Right;
		Bottom = rc.Bottom;
	}

	internal LtrbRect? NullIfZeroSize()
	{
		if (!IsZeroSize)
		{
			return this;
		}
		return null;
	}

	internal LtrbRect? IntersectOrNull(LtrbRect rect)
	{
		double num = ((rect.Left > Left) ? rect.Left : Left);
		double num2 = ((rect.Top > Top) ? rect.Top : Top);
		double num3 = ((rect.Right < Right) ? rect.Right : Right);
		double num4 = ((rect.Bottom < Bottom) ? rect.Bottom : Bottom);
		if (num3 > num && num4 > num2)
		{
			return new LtrbRect(num, num2, num3, num4);
		}
		return null;
	}

	internal LtrbRect IntersectOrEmpty(LtrbRect rect)
	{
		double num = ((rect.Left > Left) ? rect.Left : Left);
		double num2 = ((rect.Top > Top) ? rect.Top : Top);
		double num3 = ((rect.Right < Right) ? rect.Right : Right);
		double num4 = ((rect.Bottom < Bottom) ? rect.Bottom : Bottom);
		if (num3 > num && num4 > num2)
		{
			return new LtrbRect(num, num2, num3, num4);
		}
		return default(LtrbRect);
	}

	internal bool Intersects(LtrbRect rect)
	{
		if (rect.Left < Right && Left < rect.Right && rect.Top < Bottom)
		{
			return Top < rect.Bottom;
		}
		return false;
	}

	internal Rect ToRect()
	{
		return new Rect(Left, Top, Right - Left, Bottom - Top);
	}

	internal LtrbRect Inflate(Thickness thickness)
	{
		return new LtrbRect(Left - thickness.Left, Top - thickness.Top, Right + thickness.Right, Bottom + thickness.Bottom);
	}

	public static bool operator ==(LtrbRect left, LtrbRect right)
	{
		if (left.Left == right.Left && left.Top == right.Top && left.Right == right.Right)
		{
			return left.Bottom == right.Bottom;
		}
		return false;
	}

	public static bool operator !=(LtrbRect left, LtrbRect right)
	{
		if (left.Left == right.Left && left.Top == right.Top && left.Right == right.Right)
		{
			return left.Bottom != right.Bottom;
		}
		return true;
	}

	public bool Equals(LtrbRect other)
	{
		if (other.Left == Left && other.Top == Top && other.Right == Right)
		{
			return other.Bottom == Bottom;
		}
		return false;
	}

	public bool Equals(ref LtrbRect other)
	{
		if (other.Left == Left && other.Top == Top && other.Right == Right)
		{
			return other.Bottom == Bottom;
		}
		return false;
	}

	internal LtrbRect TransformToAABB(Matrix matrix)
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
		return new LtrbRect(num, num3, num2, num4);
	}

	/// <summary>
	/// Perform _WPF-like_ union operation
	/// </summary>
	public LtrbRect Union(LtrbRect rect)
	{
		double x = Math.Min(Left, rect.Left);
		double right = Math.Max(Right, rect.Right);
		double y = Math.Min(Top, rect.Top);
		double bottom = Math.Max(Bottom, rect.Bottom);
		return new LtrbRect(x, y, right, bottom);
	}

	internal static LtrbRect? FullUnion(LtrbRect? left, LtrbRect? right)
	{
		if (!left.HasValue)
		{
			return right;
		}
		if (!right.HasValue)
		{
			return left;
		}
		return right.Value.Union(left.Value);
	}

	internal static LtrbRect? FullUnion(LtrbRect? left, Rect? right)
	{
		if (!right.HasValue)
		{
			return left;
		}
		if (!left.HasValue)
		{
			return new LtrbRect(right.Value);
		}
		return left.Value.Union(new LtrbRect(right.Value));
	}

	public override bool Equals(object? obj)
	{
		if (obj is LtrbRect other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((17 * 23 + Left.GetHashCode()) * 23 + Top.GetHashCode()) * 23 + Right.GetHashCode()) * 23 + Bottom.GetHashCode();
	}

	public override string ToString()
	{
		return $"{Left}:{Top}-{Right}:{Bottom} ({Width}x{Height})";
	}

	public bool Contains(Point point)
	{
		if (point.X >= Left && point.X <= Right && point.Y >= Top)
		{
			return point.Y <= Bottom;
		}
		return false;
	}

	public bool Contains(LtrbRect rect)
	{
		if (rect.Left >= Left && rect.Right <= Right && rect.Top >= Top)
		{
			return rect.Bottom <= Bottom;
		}
		return false;
	}
}
