using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia;

/// <summary>
/// Represents the radii of a rectangle's corners.
/// </summary>
public readonly struct CornerRadius : IEquatable<CornerRadius>
{
	/// <summary>
	/// Radius of the top left corner.
	/// </summary>
	public double TopLeft { get; }

	/// <summary>
	/// Radius of the top right corner.
	/// </summary>
	public double TopRight { get; }

	/// <summary>
	/// Radius of the bottom right corner.
	/// </summary>
	public double BottomRight { get; }

	/// <summary>
	/// Radius of the bottom left corner.
	/// </summary>
	public double BottomLeft { get; }

	/// <summary>
	/// Gets a value indicating whether all corner radii are equal.
	/// </summary>
	public bool IsUniform
	{
		get
		{
			if (TopLeft.Equals(TopRight) && BottomLeft.Equals(BottomRight))
			{
				return TopRight.Equals(BottomRight);
			}
			return false;
		}
	}

	public CornerRadius(double uniformRadius)
	{
		TopLeft = (TopRight = (BottomLeft = (BottomRight = uniformRadius)));
	}

	public CornerRadius(double top, double bottom)
	{
		TopLeft = (TopRight = top);
		BottomLeft = (BottomRight = bottom);
	}

	public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft)
	{
		TopLeft = topLeft;
		TopRight = topRight;
		BottomRight = bottomRight;
		BottomLeft = bottomLeft;
	}

	/// <summary>
	/// Returns a boolean indicating whether the corner radius is equal to the other given corner radius.
	/// </summary>
	/// <param name="other">The other corner radius to test equality against.</param>
	/// <returns>True if this corner radius is equal to other; False otherwise.</returns>
	public bool Equals(CornerRadius other)
	{
		if (TopLeft == other.TopLeft && TopRight == other.TopRight && BottomRight == other.BottomRight)
		{
			return BottomLeft == other.BottomLeft;
		}
		return false;
	}

	/// <summary>
	/// Returns a boolean indicating whether the given Object is equal to this corner radius instance.
	/// </summary>
	/// <param name="obj">The Object to compare against.</param>
	/// <returns>True if the Object is equal to this corner radius; False otherwise.</returns>
	public override bool Equals(object? obj)
	{
		if (obj is CornerRadius other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return TopLeft.GetHashCode() ^ TopRight.GetHashCode() ^ BottomLeft.GetHashCode() ^ BottomRight.GetHashCode();
	}

	public override string ToString()
	{
		return FormattableString.Invariant($"{TopLeft},{TopRight},{BottomRight},{BottomLeft}");
	}

	public static CornerRadius Parse(string s)
	{
		using SpanStringTokenizer spanStringTokenizer = new SpanStringTokenizer(s, CultureInfo.InvariantCulture, "Invalid CornerRadius.");
		if (spanStringTokenizer.TryReadDouble(out var result))
		{
			if (spanStringTokenizer.TryReadDouble(out var result2))
			{
				if (spanStringTokenizer.TryReadDouble(out var result3))
				{
					return new CornerRadius(result, result2, result3, spanStringTokenizer.ReadDouble());
				}
				return new CornerRadius(result, result2);
			}
			return new CornerRadius(result);
		}
		throw new FormatException("Invalid CornerRadius.");
	}

	public static bool operator ==(CornerRadius left, CornerRadius right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CornerRadius left, CornerRadius right)
	{
		return !(left == right);
	}
}
