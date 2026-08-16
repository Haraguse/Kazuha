using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Media.Immutable;

/// <summary>
/// Represents the sequence of dashes and gaps that will be applied by an
/// <see cref="T:Avalonia.Media.Immutable.ImmutablePen" />.
/// </summary>
public class ImmutableDashStyle : IDashStyle, IEquatable<IDashStyle>
{
	private readonly double[] _dashes;

	/// <inheritdoc />
	public IReadOnlyList<double> Dashes => _dashes;

	/// <inheritdoc />
	public double Offset { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Immutable.ImmutableDashStyle" /> class.
	/// </summary>
	/// <param name="dashes">The dashes collection.</param>
	/// <param name="offset">The dash sequence offset.</param>
	public ImmutableDashStyle(IEnumerable<double>? dashes, double offset)
	{
		_dashes = dashes?.ToArray() ?? Array.Empty<double>();
		Offset = offset;
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		return Equals(obj as IDashStyle);
	}

	/// <inheritdoc />
	public bool Equals(IDashStyle? other)
	{
		if (this == other)
		{
			return true;
		}
		if (other != null && Offset == other.Offset)
		{
			return SequenceEqual(_dashes, other.Dashes);
		}
		return false;
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		int num = 717868523;
		num = num * -1521134295 + Offset.GetHashCode();
		double[] dashes = _dashes;
		foreach (double num2 in dashes)
		{
			num = num * -1521134295 + num2.GetHashCode();
		}
		return num;
	}

	private static bool SequenceEqual(double[] left, IReadOnlyList<double>? right)
	{
		if (left == right)
		{
			return true;
		}
		if (right == null || left.Length != right.Count)
		{
			return false;
		}
		for (int i = 0; i < left.Length; i++)
		{
			if (left[i] != right[i])
			{
				return false;
			}
		}
		return true;
	}
}
