using System;
using System.Globalization;

namespace Avalonia;

/// <summary>
/// Defines a scalar value that may be defined relative to a containing element.
/// </summary>
public struct RelativeScalar : IEquatable<RelativeScalar>
{
	private readonly double _scalar;

	private readonly RelativeUnit _unit;

	/// <summary>
	/// Gets the scalar.
	/// </summary>
	public double Scalar => _scalar;

	/// <summary>
	/// Gets the unit.
	/// </summary>
	public RelativeUnit Unit => _unit;

	/// <summary>
	/// The value at the beginning of the range
	/// </summary>
	public static RelativeScalar Beginning { get; } = new RelativeScalar(0.0, RelativeUnit.Relative);

	/// <summary>
	/// The value at the middle of the range
	/// </summary>
	public static RelativeScalar Middle { get; } = new RelativeScalar(0.5, RelativeUnit.Relative);

	/// <summary>
	/// The value at the end of the range
	/// </summary>
	public static RelativeScalar End { get; } = new RelativeScalar(1.0, RelativeUnit.Relative);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.RelativeScalar" /> struct.
	/// </summary>
	/// <param name="scalar">The scalar value.</param>
	/// <param name="unit">The unit.</param>
	public RelativeScalar(double scalar, RelativeUnit unit)
	{
		_scalar = scalar;
		_unit = unit;
	}

	public bool Equals(RelativeScalar other)
	{
		if (_scalar.Equals(other._scalar))
		{
			return _unit == other._unit;
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj is RelativeScalar other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _scalar.GetHashCode() ^ (int)_unit;
	}

	public static bool operator ==(RelativeScalar left, RelativeScalar right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(RelativeScalar left, RelativeScalar right)
	{
		return !left.Equals(right);
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.RelativeScalar" /> into a final value.
	/// </summary>
	/// <returns>The origin point in pixels.</returns>
	public double ToValue(double size)
	{
		if (_unit != RelativeUnit.Absolute)
		{
			return size * _scalar;
		}
		return _scalar;
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.RelativeScalar" /> string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>The parsed <see cref="T:Avalonia.RelativeScalar" />.</returns>
	public static RelativeScalar Parse(string s)
	{
		string text = s.Trim();
		if (text.EndsWith("%"))
		{
			return new RelativeScalar(double.Parse(text.TrimEnd('%'), CultureInfo.InvariantCulture) * 0.01, RelativeUnit.Relative);
		}
		return new RelativeScalar(double.Parse(text, CultureInfo.InvariantCulture), RelativeUnit.Absolute);
	}

	/// <summary>
	/// Returns a String representing this RelativeScalar instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override string ToString()
	{
		if (_unit != RelativeUnit.Absolute)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}%", _scalar * 100.0);
		}
		return _scalar.ToString(CultureInfo.InvariantCulture);
	}
}
