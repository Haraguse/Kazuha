using System;

namespace Avalonia.VisualTree;

/// <summary>
/// Holds information about the bounds of a control, together with a transform and a clip.
/// </summary>
public readonly struct TransformedBounds : IEquatable<TransformedBounds>
{
	/// <summary>
	/// Gets the control's bounds in its local coordinate space.
	/// </summary>
	public Rect Bounds { get; }

	/// <summary>
	/// Gets the control's clip rectangle in global coordinate space.
	/// </summary>
	public Rect Clip { get; }

	/// <summary>
	/// Gets the transform from local to global coordinate space.
	/// </summary>
	public Matrix Transform { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.VisualTree.TransformedBounds" /> struct.
	/// </summary>
	/// <param name="bounds">The control's bounds.</param>
	/// <param name="clip">The control's clip rectangle.</param>
	/// <param name="transform">The control's transform.</param>
	public TransformedBounds(Rect bounds, Rect clip, Matrix transform)
	{
		Bounds = bounds;
		Clip = clip;
		Transform = transform;
	}

	public bool Contains(Point point)
	{
		if (Transform.HasInverse)
		{
			Point p = point * Transform.Invert();
			return Bounds.Contains(p);
		}
		return Bounds.Contains(point);
	}

	public bool Equals(TransformedBounds other)
	{
		if (Bounds == other.Bounds && Clip == other.Clip)
		{
			return Transform == other.Transform;
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj is TransformedBounds other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((Bounds.GetHashCode() * 397) ^ Clip.GetHashCode()) * 397) ^ Transform.GetHashCode();
	}

	public static bool operator ==(TransformedBounds left, TransformedBounds right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(TransformedBounds left, TransformedBounds right)
	{
		return !left.Equals(right);
	}

	public override string ToString()
	{
		return FormattableString.Invariant($"Bounds: {Bounds} Clip: {Clip} Transform {Transform}");
	}
}
