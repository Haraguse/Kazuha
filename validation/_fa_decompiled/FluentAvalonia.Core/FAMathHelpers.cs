using System;

namespace FluentAvalonia.Core;

/// <summary>
/// Maths Helpers
/// </summary>
public static class FAMathHelpers
{
	/// <summary>
	/// Returns <paramref name="value" /> clamped to the inclusive range of min and max.
	/// </summary>
	/// <param name="value">The value to be clamped.</param>
	/// <param name="min">The lower bound of the result.</param>
	/// <param name="max">The upper bound of the result.</param>
	/// <returns><see cref="T:System.Single" /></returns>
	/// <exception cref="T:System.ArgumentException">Thrown when <paramref name="min" /> is greatest of <paramref name="max" />.</exception>
	[Obsolete("Use float.Clamp methods instead")]
	public static float Clamp(float value, float min, float max)
	{
		return Math.Clamp(value, min, max);
	}

	/// <summary>
	/// Returns <paramref name="value" /> clamped to the inclusive range of min and max.
	/// </summary>
	/// <param name="value">The value to be clamped.</param>
	/// <param name="min">The lower bound of the result.</param>
	/// <param name="max">The upper bound of the result.</param>
	/// <returns><see cref="T:System.Double" /></returns>
	/// <exception cref="T:System.ArgumentException">Thrown when <paramref name="min" /> is greatest of <paramref name="max" />.</exception>
	[Obsolete("Use double clamp methods instead")]
	public static double Clamp(double value, double min, double max)
	{
		return Math.Clamp(value, min, max);
	}

	/// <summary>
	/// Returns <paramref name="value" /> clamped to the inclusive range of min and max.
	/// </summary>
	/// <param name="value">The value to be clamped.</param>
	/// <param name="min">The lower bound of the result.</param>
	/// <param name="max">The upper bound of the result.</param>
	/// <returns><see cref="T:System.Int32" /></returns>
	/// <exception cref="T:System.ArgumentException">Thrown when <paramref name="min" /> is greatest of <paramref name="max" />.</exception>
	[Obsolete("Use int.Clamp methods instead")]
	public static int Clamp(int value, int min, int max)
	{
		return Math.Clamp(value, min, max);
	}

	public static bool IsZero(double value, double eps = 1E-05)
	{
		return double.Abs(value) < eps;
	}

	public static bool IsClose(double value1, double value2, double eps = 1E-05)
	{
		return double.Abs(value1 - value2) < eps;
	}
}
