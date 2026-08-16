using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia.Animation;

/// <summary>
/// Determines how an animation is used based on a cubic bezier curve.
/// X1 and X2 must be between 0.0 and 1.0, inclusive.
/// See https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.animation.keyspline
/// </summary>
[TypeConverter(typeof(KeySplineTypeConverter))]
public sealed class KeySpline : AvaloniaObject
{
	private double _controlPointX1;

	private double _controlPointY1;

	private double _controlPointX2;

	private double _controlPointY2;

	private bool _isSpecified;

	private bool _isDirty;

	private double _parameter;

	private double _Bx;

	private double _Cx;

	private double _Cx_Bx;

	private double _three_Cx;

	private double _By;

	private double _Cy;

	private const double _accuracy = 0.001;

	private const double _fuzz = 1E-06;

	/// <summary>
	/// X coordinate of the first control point
	/// </summary>
	public double ControlPointX1
	{
		get
		{
			return _controlPointX1;
		}
		set
		{
			if (IsValidXValue(value))
			{
				_controlPointX1 = value;
				_isDirty = true;
				return;
			}
			throw new ArgumentException("Invalid KeySpline X1 value. Must be >= 0.0 and <= 1.0.");
		}
	}

	/// <summary>
	/// Y coordinate of the first control point
	/// </summary>
	public double ControlPointY1
	{
		get
		{
			return _controlPointY1;
		}
		set
		{
			_controlPointY1 = value;
			_isDirty = true;
		}
	}

	/// <summary>
	/// X coordinate of the second control point
	/// </summary>
	public double ControlPointX2
	{
		get
		{
			return _controlPointX2;
		}
		set
		{
			if (IsValidXValue(value))
			{
				_controlPointX2 = value;
				_isDirty = true;
				return;
			}
			throw new ArgumentException("Invalid KeySpline X2 value. Must be >= 0.0 and <= 1.0.");
		}
	}

	/// <summary>
	/// Y coordinate of the second control point
	/// </summary>
	public double ControlPointY2
	{
		get
		{
			return _controlPointY2;
		}
		set
		{
			_controlPointY2 = value;
			_isDirty = true;
		}
	}

	/// <summary>
	/// Create a <see cref="T:Avalonia.Animation.KeySpline" /> with X1 = Y1 = 0 and X2 = Y2 = 1.
	/// </summary>
	public KeySpline()
	{
		_controlPointX1 = 0.0;
		_controlPointY1 = 0.0;
		_controlPointX2 = 1.0;
		_controlPointY2 = 1.0;
		_isDirty = true;
	}

	/// <summary>
	/// Create a <see cref="T:Avalonia.Animation.KeySpline" /> with the given parameters
	/// </summary>
	/// <param name="x1">X coordinate for the first control point</param>
	/// <param name="y1">Y coordinate for the first control point</param>
	/// <param name="x2">X coordinate for the second control point</param>
	/// <param name="y2">Y coordinate for the second control point</param>
	public KeySpline(double x1, double y1, double x2, double y2)
	{
		_controlPointX1 = x1;
		_controlPointY1 = y1;
		_controlPointX2 = x2;
		_controlPointY2 = y2;
		_isDirty = true;
	}

	/// <summary>
	/// Parse a <see cref="T:Avalonia.Animation.KeySpline" /> from a string. The string
	/// needs to contain 4 values in it for the 2 control points.
	/// </summary>
	/// <param name="value">string with 4 values in it</param>
	/// <param name="culture">culture of the string</param>
	/// <exception cref="T:System.FormatException">Thrown if the string does not have 4 values</exception>
	/// <returns>A <see cref="T:Avalonia.Animation.KeySpline" /> with the appropriate values set</returns>
	public static KeySpline Parse(string value, CultureInfo? culture)
	{
		if (culture == null)
		{
			culture = CultureInfo.InvariantCulture;
		}
		using SpanStringTokenizer spanStringTokenizer = new SpanStringTokenizer(value, culture, "Invalid KeySpline string: \"" + value + "\".");
		return new KeySpline(spanStringTokenizer.ReadDouble(), spanStringTokenizer.ReadDouble(), spanStringTokenizer.ReadDouble(), spanStringTokenizer.ReadDouble());
	}

	/// <summary>
	/// Calculates spline progress from a linear progress.
	/// </summary>
	/// <param name="linearProgress">the linear progress</param>
	/// <returns>the spline progress</returns>
	public double GetSplineProgress(double linearProgress)
	{
		if (_isDirty)
		{
			Build();
		}
		if (!_isSpecified)
		{
			return linearProgress;
		}
		SetParameterFromX(linearProgress);
		return GetBezierValue(_By, _Cy, _parameter);
	}

	/// <summary>
	/// Check to see whether the <see cref="T:Avalonia.Animation.KeySpline" /> is valid by looking
	/// at its X values.
	/// </summary>
	/// <returns>true if the X values for this <see cref="T:Avalonia.Animation.KeySpline" /> fall in 
	/// acceptable range; false otherwise.</returns>
	public bool IsValid()
	{
		if (IsValidXValue(_controlPointX1))
		{
			return IsValidXValue(_controlPointX2);
		}
		return false;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	private static bool IsValidXValue(double value)
	{
		if (value >= 0.0)
		{
			return value <= 1.0;
		}
		return false;
	}

	/// <summary>
	/// Compute cached coefficients.
	/// </summary>
	private void Build()
	{
		if (_controlPointX1 == 0.0 && _controlPointY1 == 0.0 && _controlPointX2 == 1.0 && _controlPointY2 == 1.0)
		{
			_isSpecified = false;
		}
		else
		{
			_isSpecified = true;
			_parameter = 0.0;
			_Bx = 3.0 * _controlPointX1;
			_Cx = 3.0 * _controlPointX2;
			_Cx_Bx = 2.0 * (_Cx - _Bx);
			_three_Cx = 3.0 - _Cx;
			_By = 3.0 * _controlPointY1;
			_Cy = 3.0 * _controlPointY2;
		}
		_isDirty = false;
	}

	/// <summary>
	/// Get an X or Y value with the Bezier formula.
	/// </summary>
	/// <param name="b">the second Bezier coefficient</param>
	/// <param name="c">the third Bezier coefficient</param>
	/// <param name="t">the parameter value to evaluate at</param>
	/// <returns>the value of the Bezier function at the given parameter</returns>
	private static double GetBezierValue(double b, double c, double t)
	{
		double num = 1.0 - t;
		double num2 = t * t;
		return b * t * num * num + c * num2 * num + num2 * t;
	}

	/// <summary>
	/// Get X and dX/dt at a given parameter
	/// </summary>
	/// <param name="t">the parameter value to evaluate at</param>
	/// <param name="x">the value of x there</param>
	/// <param name="dx">the value of dx/dt there</param>
	private void GetXAndDx(double t, out double x, out double dx)
	{
		double num = 1.0 - t;
		double num2 = t * t;
		double num3 = num * num;
		x = _Bx * t * num3 + _Cx * num2 * num + num2 * t;
		dx = _Bx * num3 + _Cx_Bx * num * t + _three_Cx * num2;
	}

	/// <summary>
	/// Compute the parameter value that corresponds to a given X value, using a modified
	/// clamped Newton-Raphson algorithm to solve the equation X(t) - time = 0. We make 
	/// use of some known properties of this particular function:
	/// * We are only interested in solutions in the interval [0,1]
	/// * X(t) is increasing, so we can assume that if X(t) &gt; time t &gt; solution.  We use
	///   that to clamp down the search interval with every probe.
	/// * The derivative of X and Y are between 0 and 3.
	/// </summary>
	/// <param name="time">the time, scaled to fit in [0,1]</param>
	private void SetParameterFromX(double time)
	{
		double num = 0.0;
		double num2 = 1.0;
		if (time == 0.0)
		{
			_parameter = 0.0;
			return;
		}
		if (time == 1.0)
		{
			_parameter = 1.0;
			return;
		}
		while (num2 - num > 1E-06)
		{
			GetXAndDx(_parameter, out var x, out var dx);
			double num3 = Math.Abs(dx);
			if (x > time)
			{
				num2 = _parameter;
			}
			else
			{
				num = _parameter;
			}
			if (Math.Abs(x - time) < 0.001 * num3)
			{
				break;
			}
			if (num3 > 1E-06)
			{
				double num4 = _parameter - (x - time) / dx;
				if (num4 >= num2)
				{
					_parameter = (_parameter + num2) / 2.0;
				}
				else if (num4 <= num)
				{
					_parameter = (_parameter + num) / 2.0;
				}
				else
				{
					_parameter = num4;
				}
			}
			else
			{
				_parameter = (num + num2) / 2.0;
			}
		}
	}
}
