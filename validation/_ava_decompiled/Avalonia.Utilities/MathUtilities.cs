using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities;

/// <summary>
/// Provides math utilities not provided in System.Math.
/// </summary>
internal static class MathUtilities
{
	internal const double DoubleEpsilon = 2.220446049250313E-16;

	internal const float FloatEpsilon = 1.1920929E-07f;

	/// <summary>
	/// AreClose - Returns whether or not two doubles are "close".  That is, whether or 
	/// not they are within epsilon of each other.
	/// </summary> 
	/// <param name="value1"> The first double to compare. </param>
	/// <param name="value2"> The second double to compare. </param>
	public static bool AreClose(double value1, double value2)
	{
		if (value1 == value2)
		{
			return true;
		}
		double num = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * 2.220446049250313E-16;
		double num2 = value1 - value2;
		if (0.0 - num < num2)
		{
			return num > num2;
		}
		return false;
	}

	/// <summary>
	/// AreClose - Returns whether or not two doubles are "close".  That is, whether or
	/// not they are within epsilon of each other.
	/// </summary>
	/// <param name="value1"> The first double to compare. </param>
	/// <param name="value2"> The second double to compare. </param>
	/// <param name="eps"> The fixed epsilon value used to compare.</param>
	public static bool AreClose(double value1, double value2, double eps)
	{
		if (value1 == value2)
		{
			return true;
		}
		double num = value1 - value2;
		if (0.0 - eps < num)
		{
			return eps > num;
		}
		return false;
	}

	/// <summary>
	/// AreClose - Returns whether or not two floats are "close".  That is, whether or 
	/// not they are within epsilon of each other.
	/// </summary> 
	/// <param name="value1"> The first float to compare. </param>
	/// <param name="value2"> The second float to compare. </param>
	public static bool AreClose(float value1, float value2)
	{
		if (value1 == value2)
		{
			return true;
		}
		float num = (Math.Abs(value1) + Math.Abs(value2) + 10f) * 1.1920929E-07f;
		float num2 = value1 - value2;
		if (0f - num < num2)
		{
			return num > num2;
		}
		return false;
	}

	/// <summary>
	/// LessThan - Returns whether or not the first double is less than the second double.
	/// That is, whether or not the first is strictly less than *and* not within epsilon of
	/// the other number.
	/// </summary>
	/// <param name="value1"> The first double to compare. </param>
	/// <param name="value2"> The second double to compare. </param>
	public static bool LessThan(double value1, double value2)
	{
		if (value1 < value2)
		{
			return !AreClose(value1, value2);
		}
		return false;
	}

	/// <summary>
	/// LessThan - Returns whether or not the first float is less than the second float.
	/// That is, whether or not the first is strictly less than *and* not within epsilon of
	/// the other number.
	/// </summary>
	/// <param name="value1"> The first single float to compare. </param>
	/// <param name="value2"> The second single float to compare. </param>
	public static bool LessThan(float value1, float value2)
	{
		if (value1 < value2)
		{
			return !AreClose(value1, value2);
		}
		return false;
	}

	/// <summary>
	/// GreaterThan - Returns whether or not the first double is greater than the second double.
	/// That is, whether or not the first is strictly greater than *and* not within epsilon of
	/// the other number.
	/// </summary>
	/// <param name="value1"> The first double to compare. </param>
	/// <param name="value2"> The second double to compare. </param>
	public static bool GreaterThan(double value1, double value2)
	{
		if (value1 > value2)
		{
			return !AreClose(value1, value2);
		}
		return false;
	}

	/// <summary>
	/// GreaterThan - Returns whether or not the first float is greater than the second float.
	/// That is, whether or not the first is strictly greater than *and* not within epsilon of
	/// the other number.
	/// </summary>
	/// <param name="value1"> The first float to compare. </param>
	/// <param name="value2"> The second float to compare. </param>
	public static bool GreaterThan(float value1, float value2)
	{
		if (value1 > value2)
		{
			return !AreClose(value1, value2);
		}
		return false;
	}

	/// <summary>
	/// LessThanOrClose - Returns whether or not the first double is less than or close to
	/// the second double.  That is, whether or not the first is strictly less than or within
	/// epsilon of the other number.
	/// </summary>
	/// <param name="value1"> The first double to compare. </param>
	/// <param name="value2"> The second double to compare. </param>
	public static bool LessThanOrClose(double value1, double value2)
	{
		if (!(value1 < value2))
		{
			return AreClose(value1, value2);
		}
		return true;
	}

	/// <summary>
	/// LessThanOrClose - Returns whether or not the first float is less than or close to
	/// the second float.  That is, whether or not the first is strictly less than or within
	/// epsilon of the other number.
	/// </summary>
	/// <param name="value1"> The first float to compare. </param>
	/// <param name="value2"> The second float to compare. </param>
	public static bool LessThanOrClose(float value1, float value2)
	{
		if (!(value1 < value2))
		{
			return AreClose(value1, value2);
		}
		return true;
	}

	/// <summary>
	/// GreaterThanOrClose - Returns whether or not the first double is greater than or close to
	/// the second double.  That is, whether or not the first is strictly greater than or within
	/// epsilon of the other number.
	/// </summary>
	/// <param name="value1"> The first double to compare. </param>
	/// <param name="value2"> The second double to compare. </param>
	public static bool GreaterThanOrClose(double value1, double value2)
	{
		if (!(value1 > value2))
		{
			return AreClose(value1, value2);
		}
		return true;
	}

	/// <summary>
	/// GreaterThanOrClose - Returns whether or not the first float is greater than or close to
	/// the second float.  That is, whether or not the first is strictly greater than or within
	/// epsilon of the other number.
	/// </summary>
	/// <param name="value1"> The first float to compare. </param>
	/// <param name="value2"> The second float to compare. </param>
	public static bool GreaterThanOrClose(float value1, float value2)
	{
		if (!(value1 > value2))
		{
			return AreClose(value1, value2);
		}
		return true;
	}

	/// <summary>
	/// IsOne - Returns whether or not the double is "close" to 1.  Same as AreClose(double, 1),
	/// but this is faster.
	/// </summary>
	/// <param name="value"> The double to compare to 1. </param>
	public static bool IsOne(double value)
	{
		return Math.Abs(value - 1.0) < 2.220446049250313E-15;
	}

	/// <summary>
	/// IsOne - Returns whether or not the float is "close" to 1.  Same as AreClose(float, 1),
	/// but this is faster.
	/// </summary>
	/// <param name="value"> The float to compare to 1. </param>
	public static bool IsOne(float value)
	{
		return Math.Abs(value - 1f) < 1.1920929E-06f;
	}

	/// <summary>
	/// IsZero - Returns whether or not the double is "close" to 0.  Same as AreClose(double, 0),
	/// but this is faster.
	/// </summary>
	/// <param name="value"> The double to compare to 0. </param>
	public static bool IsZero(double value)
	{
		return Math.Abs(value) < 2.220446049250313E-15;
	}

	/// <summary>
	/// IsZero - Returns whether or not the float is "close" to 0.  Same as AreClose(float, 0),
	/// but this is faster.
	/// </summary>
	/// <param name="value"> The float to compare to 0. </param>
	public static bool IsZero(float value)
	{
		return Math.Abs(value) < 1.1920929E-06f;
	}

	/// <summary>
	/// Clamps a value between a minimum and maximum value.
	/// </summary>
	/// <param name="val">The value.</param>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>The clamped value.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Clamp(double val, double min, double max)
	{
		if (min > max)
		{
			ThrowCannotBeGreaterThanException(min, max);
		}
		if (val < min)
		{
			return min;
		}
		if (val > max)
		{
			return max;
		}
		return val;
	}

	/// <summary>
	/// Clamps a value between a minimum and maximum value.
	/// </summary>
	/// <param name="val">The value.</param>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>The clamped value.</returns>
	public static decimal Clamp(decimal val, decimal min, decimal max)
	{
		if (min > max)
		{
			ThrowCannotBeGreaterThanException(min, max);
		}
		if (val < min)
		{
			return min;
		}
		if (val > max)
		{
			return max;
		}
		return val;
	}

	/// <summary>
	/// Clamps a value between a minimum and maximum value.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>The clamped value.</returns>
	public static float Clamp(float value, float min, float max)
	{
		float val = Math.Max(min, max);
		float val2 = Math.Min(min, max);
		return Math.Min(Math.Max(value, val2), val);
	}

	/// <summary>
	/// Clamps a value between a minimum and maximum value.
	/// </summary>
	/// <param name="val">The value.</param>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>The clamped value.</returns>
	public static int Clamp(int val, int min, int max)
	{
		if (min > max)
		{
			ThrowCannotBeGreaterThanException(min, max);
		}
		if (val < min)
		{
			return min;
		}
		if (val > max)
		{
			return max;
		}
		return val;
	}

	/// <summary>
	/// Converts an angle in degrees to radians.
	/// </summary>
	/// <param name="angle">The angle in degrees.</param>
	/// <returns>The angle in radians.</returns>
	public static double Deg2Rad(double angle)
	{
		return angle * (Math.PI / 180.0);
	}

	/// <summary>
	/// Converts an angle in gradians to radians.
	/// </summary>
	/// <param name="angle">The angle in gradians.</param>
	/// <returns>The angle in radians.</returns>
	public static double Grad2Rad(double angle)
	{
		return angle * (Math.PI / 200.0);
	}

	/// <summary>
	/// Converts an angle in turns to radians.
	/// </summary>
	/// <param name="angle">The angle in turns.</param>
	/// <returns>The angle in radians.</returns>
	public static double Turn2Rad(double angle)
	{
		return angle * 2.0 * Math.PI;
	}

	/// <summary>
	/// Calculates the point of an angle on an ellipse.
	/// </summary>
	/// <param name="centre">The centre point of the ellipse.</param>
	/// <param name="radiusX">The x radius of the ellipse.</param>
	/// <param name="radiusY">The y radius of the ellipse.</param>
	/// <param name="angle">The angle in radians.</param>
	/// <returns>A point on the ellipse.</returns>
	public static Point GetEllipsePoint(Point centre, double radiusX, double radiusY, double angle)
	{
		return new Point(radiusX * Math.Cos(angle) + centre.X, radiusY * Math.Sin(angle) + centre.Y);
	}

	/// <summary>
	/// Gets the minimum and maximum from the specified numbers.
	/// </summary>
	/// <param name="a">The first number.</param>
	/// <param name="b">The second number.</param>
	/// <returns>A tuple containing the minimum and maximum of the two specified numbers.</returns>
	public static (double min, double max) GetMinMax(double a, double b)
	{
		if (!(a < b))
		{
			return (min: b, max: a);
		}
		return (min: a, max: b);
	}

	/// <summary>
	/// Gets the minimum and maximum from the specified number and the difference with that number.
	/// </summary>
	/// <param name="initialValue">The initial value to use.</param>
	/// <param name="delta">The difference for <paramref name="initialValue" />.</param>
	/// <returns>A tuple containing the minimum and maximum of the specified number and the difference with that number.</returns>
	public static (double min, double max) GetMinMaxFromDelta(double initialValue, double delta)
	{
		return GetMinMax(initialValue, initialValue + delta);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsNegativeOrNonFinite(double d)
	{
		return BitConverter.DoubleToUInt64Bits(d) >= 9218868437227405312L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsFinite(double d)
	{
		return double.IsFinite(d);
	}

	internal static int WhichPolygonSideIntersects(uint cPoly, ReadOnlySpan<Vector> pPtPoly, Vector ptCurrent, Vector vecEdge)
	{
		uint num = 0u;
		uint num2 = 0u;
		uint num3 = 0u;
		Point point = new Point(0.0 - vecEdge.Y, vecEdge.X);
		for (int i = 0; i < cPoly; i++)
		{
			double num4 = Vector.Dot(ptCurrent - pPtPoly[i], point);
			if (num4 > 0.0)
			{
				num++;
			}
			else if (num4 < 0.0)
			{
				num2++;
			}
			else
			{
				num3++;
			}
			if ((num != 0 && num2 != 0) || num3 != 0)
			{
				return 0;
			}
		}
		if (num == 0)
		{
			return -1;
		}
		return 1;
	}

	internal static bool DoPolygonsIntersect(uint cPolyA, ReadOnlySpan<Vector> pPtPolyA, uint cPolyB, ReadOnlySpan<Vector> pPtPolyB)
	{
		for (int i = 0; i < cPolyA; i++)
		{
			Vector vecEdge = pPtPolyA[(int)((i + 1) % cPolyA)] - pPtPolyA[i];
			if (WhichPolygonSideIntersects(cPolyB, pPtPolyB, pPtPolyA[i], vecEdge) < 0)
			{
				return false;
			}
		}
		for (int j = 0; j < cPolyB; j++)
		{
			Vector vecEdge2 = pPtPolyB[(int)((j + 1) % cPolyB)] - pPtPolyB[j];
			if (WhichPolygonSideIntersects(cPolyA, pPtPolyA, pPtPolyB[j], vecEdge2) < 0)
			{
				return false;
			}
		}
		return true;
	}

	internal static bool IsEntirelyContained(uint cPolyA, ReadOnlySpan<Vector> pPtPolyA, uint cPolyB, ReadOnlySpan<Vector> pPtPolyB)
	{
		for (int i = 0; i < cPolyB; i++)
		{
			Vector vecEdge = pPtPolyB[(i + 1) % (int)cPolyB] - pPtPolyB[i];
			if (WhichPolygonSideIntersects(cPolyA, pPtPolyA, pPtPolyB[i], vecEdge) <= 0)
			{
				return false;
			}
		}
		return true;
	}

	private static void ThrowCannotBeGreaterThanException<T>(T min, T max)
	{
		throw new ArgumentException($"{min} cannot be greater than {max}.");
	}
}
