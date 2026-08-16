using System;

namespace Avalonia.Media.Immutable;

/// <summary>
/// Fills an area with a solid color.
/// </summary>
public class ImmutableSolidColorBrush : IImmutableSolidColorBrush, ISolidColorBrush, IBrush, IImmutableBrush, IEquatable<ImmutableSolidColorBrush>
{
	/// <summary>
	/// Gets the color of the brush.
	/// </summary>
	public Color Color { get; }

	/// <summary>
	/// Gets the opacity of the brush.
	/// </summary>
	public double Opacity { get; }

	/// <summary>
	/// Gets the transform of the brush.
	/// </summary>
	public ITransform? Transform { get; }

	/// <summary>
	/// Gets the transform origin of the brush
	/// </summary>
	public RelativePoint TransformOrigin { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Immutable.ImmutableSolidColorBrush" /> class.
	/// </summary>
	/// <param name="color">The color to use.</param>
	/// <param name="opacity">The opacity of the brush.</param>
	/// <param name="transform">The transform of the brush.</param>
	public ImmutableSolidColorBrush(Color color, double opacity = 1.0, ImmutableTransform? transform = null)
	{
		Color = color;
		Opacity = opacity;
		Transform = transform;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Immutable.ImmutableSolidColorBrush" /> class.
	/// </summary>
	/// <param name="color">The color to use.</param>
	public ImmutableSolidColorBrush(uint color)
		: this(Color.FromUInt32(color))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Immutable.ImmutableSolidColorBrush" /> class.
	/// </summary>
	/// <param name="source">The brush from which this brush's properties should be copied.</param>
	public ImmutableSolidColorBrush(ISolidColorBrush source)
		: this(source.Color, source.Opacity, source.Transform?.ToImmutable())
	{
	}

	public bool Equals(ImmutableSolidColorBrush? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this == other)
		{
			return true;
		}
		if (Color.Equals(other.Color) && Opacity.Equals(other.Opacity))
		{
			if (Transform != null || other.Transform != null)
			{
				if (Transform != null)
				{
					return Transform.Equals(other.Transform);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj is ImmutableSolidColorBrush other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Color.GetHashCode() * 397) ^ Opacity.GetHashCode() ^ ((Transform != null) ? Transform.GetHashCode() : 0);
	}

	public static bool operator ==(ImmutableSolidColorBrush left, ImmutableSolidColorBrush right)
	{
		return object.Equals(left, right);
	}

	public static bool operator !=(ImmutableSolidColorBrush left, ImmutableSolidColorBrush right)
	{
		return !object.Equals(left, right);
	}

	/// <summary>
	/// Returns a string representation of the brush.
	/// </summary>
	/// <returns>A string representation of the brush.</returns>
	public override string ToString()
	{
		return Color.ToString();
	}
}
