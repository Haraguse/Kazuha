namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases a <see cref="T:System.Double" /> value
/// using a user-defined cubic bezier curve.
/// Good for custom easing functions that doesn't quite
/// fit with the built-in ones. 
/// </summary>
public class SplineEasing : Easing
{
	private readonly KeySpline _internalKeySpline;

	/// <summary>
	/// X coordinate of the first control point
	/// </summary>
	public double X1
	{
		get
		{
			return _internalKeySpline.ControlPointX1;
		}
		set
		{
			_internalKeySpline.ControlPointX1 = value;
		}
	}

	/// <summary>
	/// Y coordinate of the first control point
	/// </summary>
	public double Y1
	{
		get
		{
			return _internalKeySpline.ControlPointY1;
		}
		set
		{
			_internalKeySpline.ControlPointY1 = value;
		}
	}

	/// <summary>
	/// X coordinate of the second control point
	/// </summary> 
	public double X2
	{
		get
		{
			return _internalKeySpline.ControlPointX2;
		}
		set
		{
			_internalKeySpline.ControlPointX2 = value;
		}
	}

	/// <summary>
	/// Y coordinate of the second control point
	/// </summary>
	public double Y2
	{
		get
		{
			return _internalKeySpline.ControlPointY2;
		}
		set
		{
			_internalKeySpline.ControlPointY2 = value;
		}
	}

	public SplineEasing(double x1 = 0.0, double y1 = 0.0, double x2 = 1.0, double y2 = 1.0)
	{
		_internalKeySpline = new KeySpline();
		X1 = x1;
		Y1 = y1;
		X2 = x2;
		Y1 = y2;
	}

	public SplineEasing(KeySpline keySpline)
	{
		_internalKeySpline = keySpline;
	}

	public SplineEasing()
	{
		_internalKeySpline = new KeySpline();
	}

	/// <inheritdoc />
	public override double Ease(double progress)
	{
		return _internalKeySpline.GetSplineProgress(progress);
	}
}
