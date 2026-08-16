using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Visuals.Platform;

public class PathGeometryContext : IGeometryContext, IDisposable
{
	private PathFigure? _currentFigure;

	private PathGeometry? _pathGeometry;

	public PathGeometryContext(PathGeometry pathGeometry)
	{
		_pathGeometry = pathGeometry ?? throw new ArgumentNullException("pathGeometry");
	}

	public void Dispose()
	{
		_pathGeometry = null;
	}

	/// <inheritdoc />
	public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true)
	{
		ArcSegment item = new ArcSegment
		{
			Size = size,
			RotationAngle = rotationAngle,
			IsLargeArc = isLargeArc,
			SweepDirection = sweepDirection,
			Point = point,
			IsStroked = isStroked
		};
		CurrentFigureSegments().Add(item);
	}

	/// <inheritdoc />
	public void BeginFigure(Point startPoint, bool isFilled)
	{
		ThrowIfDisposed();
		_currentFigure = new PathFigure
		{
			StartPoint = startPoint,
			IsClosed = false,
			IsFilled = isFilled
		};
		PathGeometry pathGeometry = _pathGeometry;
		if (pathGeometry.Figures == null)
		{
			PathFigures pathFigures = (pathGeometry.Figures = new PathFigures());
		}
		_pathGeometry.Figures.Add(_currentFigure);
	}

	/// <inheritdoc />
	public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
	{
		BezierSegment item = new BezierSegment
		{
			Point1 = controlPoint1,
			Point2 = controlPoint2,
			Point3 = endPoint,
			IsStroked = isStroked
		};
		CurrentFigureSegments().Add(item);
	}

	/// <inheritdoc />
	public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
	{
		QuadraticBezierSegment item = new QuadraticBezierSegment
		{
			Point1 = controlPoint,
			Point2 = endPoint,
			IsStroked = isStroked
		};
		CurrentFigureSegments().Add(item);
	}

	/// <inheritdoc />
	public void LineTo(Point point, bool isStroked = true)
	{
		LineSegment item = new LineSegment
		{
			Point = point,
			IsStroked = isStroked
		};
		CurrentFigureSegments().Add(item);
	}

	/// <inheritdoc />
	public void EndFigure(bool isClosed)
	{
		if (_currentFigure != null)
		{
			_currentFigure.IsClosed = isClosed;
		}
		_currentFigure = null;
	}

	/// <inheritdoc />
	public void SetFillRule(FillRule fillRule)
	{
		ThrowIfDisposed();
		_pathGeometry.FillRule = fillRule;
	}

	[MemberNotNull("_pathGeometry")]
	private void ThrowIfDisposed()
	{
		if (_pathGeometry == null)
		{
			throw new ObjectDisposedException("PathGeometryContext");
		}
	}

	private PathSegments CurrentFigureSegments()
	{
		ThrowIfDisposed();
		if (_currentFigure == null)
		{
			throw new InvalidOperationException("No figure in progress.");
		}
		if (_currentFigure.Segments == null)
		{
			throw new InvalidOperationException("Current figure's segments cannot be null.");
		}
		return _currentFigure.Segments;
	}
}
