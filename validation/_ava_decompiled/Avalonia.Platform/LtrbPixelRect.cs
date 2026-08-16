using System;
using Avalonia.Metadata;

namespace Avalonia.Platform;

/// <summary>
/// This struct is essentially the same thing as RECT from win32 API
/// Unlike our "normal" PixelRect which is more human-readable and human-usable
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
public struct LtrbPixelRect
{
	public int Left;

	public int Top;

	public int Right;

	public int Bottom;

	internal bool IsEmpty
	{
		get
		{
			if (Left == Right)
			{
				return Top == Bottom;
			}
			return false;
		}
	}

	internal LtrbPixelRect(int x, int y, int right, int bottom)
	{
		Left = x;
		Top = y;
		Right = right;
		Bottom = bottom;
	}

	internal LtrbPixelRect(PixelSize size)
	{
		Left = 0;
		Top = 0;
		Right = size.Width;
		Bottom = size.Height;
	}

	internal PixelRect ToPixelRect()
	{
		return new PixelRect(Left, Top, Right - Left, Bottom - Top);
	}

	internal LtrbPixelRect Union(LtrbPixelRect rect)
	{
		if (IsEmpty)
		{
			return rect;
		}
		if (rect.IsEmpty)
		{
			return this;
		}
		int x = Math.Min(Left, rect.Left);
		int right = Math.Max(Right, rect.Right);
		int y = Math.Min(Top, rect.Top);
		int bottom = Math.Max(Bottom, rect.Bottom);
		return new LtrbPixelRect(x, y, right, bottom);
	}

	internal bool Contains(int x, int y)
	{
		if (x >= Left && x <= Right && y >= Top)
		{
			return y <= Bottom;
		}
		return false;
	}

	public static bool operator ==(LtrbPixelRect left, LtrbPixelRect right)
	{
		if (left.Left == right.Left && left.Top == right.Top && left.Right == right.Right)
		{
			return left.Bottom == right.Bottom;
		}
		return false;
	}

	public static bool operator !=(LtrbPixelRect left, LtrbPixelRect right)
	{
		if (left.Left == right.Left && left.Top == right.Top && left.Right == right.Right)
		{
			return left.Bottom != right.Bottom;
		}
		return true;
	}

	public bool Equals(LtrbPixelRect other)
	{
		if (other.Left == Left && other.Top == Top && other.Right == Right)
		{
			return other.Bottom == Bottom;
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj is LtrbPixelRect other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((17 * 23 + Left.GetHashCode()) * 23 + Top.GetHashCode()) * 23 + Right.GetHashCode()) * 23 + Bottom.GetHashCode();
	}

	internal Rect ToRectUnscaled()
	{
		return new Rect(Left, Top, Right - Left, Bottom - Top);
	}

	internal LtrbRect ToLtrbRectUnscaled()
	{
		return new LtrbRect(Left, Top, Right, Bottom);
	}

	internal static LtrbPixelRect FromRectUnscaled(LtrbRect rect)
	{
		return new LtrbPixelRect((int)rect.Left, (int)rect.Top, (int)Math.Ceiling(rect.Right), (int)Math.Ceiling(rect.Bottom));
	}
}
