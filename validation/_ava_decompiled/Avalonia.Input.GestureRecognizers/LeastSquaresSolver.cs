using System;

namespace Avalonia.Input.GestureRecognizers;

internal sealed class LeastSquaresSolver
{
	private readonly ref struct _Matrix
	{
		private readonly int _columns;

		private readonly Span<double> _elements;

		public double this[int row, int col]
		{
			get
			{
				return _elements[row * _columns + col];
			}
			set
			{
				_elements[row * _columns + col] = value;
			}
		}

		internal _Matrix(int cols, Span<double> elements)
		{
			_columns = cols;
			_elements = elements;
		}

		public Span<double> GetRow(int row)
		{
			return _elements.Slice(row * _columns, _columns);
		}
	}

	private const double PrecisionErrorTolerance = 1E-10;

	/// <summary>
	/// Fits a polynomial of the given degree to the data points.
	/// When there is not enough data to fit a curve null is returned.
	/// </summary>
	public static PolynomialFit? Solve(int degree, ReadOnlySpan<double> x, ReadOnlySpan<double> y, ReadOnlySpan<double> w)
	{
		if (degree > x.Length)
		{
			return null;
		}
		PolynomialFit polynomialFit = new PolynomialFit(degree);
		int length = x.Length;
		int num = degree + 1;
		int cols = length;
		Span<double> elements = stackalloc double[num * length];
		_Matrix matrix = new _Matrix(cols, elements);
		for (int i = 0; i < length; i++)
		{
			matrix[0, i] = w[i];
			for (int j = 1; j < num; j++)
			{
				matrix[j, i] = matrix[j - 1, i] * x[i];
			}
		}
		int cols2 = length;
		elements = stackalloc double[num * length];
		_Matrix matrix2 = new _Matrix(cols2, elements);
		cols = num;
		elements = stackalloc double[num * num];
		_Matrix matrix3 = new _Matrix(cols, elements);
		for (int k = 0; k < num; k++)
		{
			for (int l = 0; l < length; l++)
			{
				matrix2[k, l] = matrix[k, l];
			}
			for (int m = 0; m < k; m++)
			{
				double num2 = Multiply(matrix2.GetRow(k), matrix2.GetRow(m));
				for (int n = 0; n < length; n++)
				{
					matrix2[k, n] -= num2 * matrix2[m, n];
				}
			}
			double num3 = Norm(matrix2.GetRow(k));
			if (num3 < 1E-10)
			{
				return null;
			}
			double num4 = 1.0 / num3;
			for (int num5 = 0; num5 < length; num5++)
			{
				matrix2[k, num5] *= num4;
			}
			for (int num6 = 0; num6 < num; num6++)
			{
				matrix3[k, num6] = ((num6 < k) ? 0.0 : Multiply(matrix2.GetRow(k), matrix.GetRow(num6)));
			}
		}
		Span<double> v = stackalloc double[length];
		for (int num7 = 0; num7 < length; num7++)
		{
			v[num7] = y[num7] * w[num7];
		}
		for (int num8 = num - 1; num8 >= 0; num8--)
		{
			polynomialFit.Coefficients[num8] = Multiply(matrix2.GetRow(num8), v);
			for (int num9 = num - 1; num9 > num8; num9--)
			{
				polynomialFit.Coefficients[num8] -= matrix3[num8, num9] * polynomialFit.Coefficients[num9];
			}
			polynomialFit.Coefficients[num8] /= matrix3[num8, num8];
		}
		double num10 = 0.0;
		for (int num11 = 0; num11 < length; num11++)
		{
			num10 += y[num11];
		}
		num10 /= (double)length;
		double num12 = 0.0;
		double num13 = 0.0;
		for (int num14 = 0; num14 < length; num14++)
		{
			double num15 = 1.0;
			double num16 = y[num14] - polynomialFit.Coefficients[0];
			for (int num17 = 1; num17 < num; num17++)
			{
				num15 *= x[num14];
				num16 -= num15 * polynomialFit.Coefficients[num17];
			}
			num12 += w[num14] * w[num14] * num16 * num16;
			double num18 = y[num14] - num10;
			num13 += w[num14] * w[num14] * num18 * num18;
		}
		polynomialFit.Confidence = ((num13 <= 1E-10) ? 1.0 : (1.0 - num12 / num13));
		return polynomialFit;
	}

	private static double Multiply(Span<double> v1, Span<double> v2)
	{
		double num = 0.0;
		for (int i = 0; i < v1.Length; i++)
		{
			num += v1[i] * v2[i];
		}
		return num;
	}

	private static double Norm(Span<double> v)
	{
		return Math.Sqrt(Multiply(v, v));
	}
}
