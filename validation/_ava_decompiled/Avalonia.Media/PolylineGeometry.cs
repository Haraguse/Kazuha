using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Media;

/// <summary>
/// Represents the geometry of an polyline or polygon.
/// </summary>
public class PolylineGeometry : Geometry
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.PolylineGeometry.Points" /> property.
	/// </summary>
	public static readonly DirectProperty<PolylineGeometry, IList<Point>> PointsProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.PolylineGeometry.IsFilled" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsFilledProperty;

	private IList<Point> _points;

	private IDisposable? _pointsObserver;

	private readonly FillRule _fillRule;

	/// <summary>
	/// Gets or sets the figures.
	/// </summary>
	/// <value>
	/// The points.
	/// </value>
	[Content]
	public IList<Point> Points
	{
		get
		{
			return _points;
		}
		set
		{
			SetAndRaise(PointsProperty, ref _points, value);
		}
	}

	public bool IsFilled
	{
		get
		{
			return GetValue(IsFilledProperty);
		}
		set
		{
			SetValue(IsFilledProperty, value);
		}
	}

	/// <summary>
	/// Gets how the intersecting areas of the polyline are combined.
	/// </summary>
	public FillRule FillRule => _fillRule;

	static PolylineGeometry()
	{
		PointsProperty = AvaloniaProperty.RegisterDirect("Points", (PolylineGeometry g) => g.Points, delegate(PolylineGeometry g, IList<Point> f)
		{
			g.Points = f;
		});
		IsFilledProperty = AvaloniaProperty.Register<PolylineGeometry, bool>("IsFilled", defaultValue: false);
		Geometry.AffectsGeometry(IsFilledProperty);
		PointsProperty.Changed.AddClassHandler(delegate(PolylineGeometry s, AvaloniaPropertyChangedEventArgs e)
		{
			s.OnPointsChanged(e.NewValue as IList<Point>);
		});
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.PolylineGeometry" /> class.
	/// </summary>
	public PolylineGeometry()
	{
		_points = new Points();
		_fillRule = FillRule.EvenOdd;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.PolylineGeometry" /> class.
	/// </summary>
	public PolylineGeometry(IEnumerable<Point> points, bool isFilled)
	{
		_points = new Points(points);
		IsFilled = isFilled;
		_fillRule = FillRule.EvenOdd;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.PolylineGeometry" /> class.
	/// </summary>
	public PolylineGeometry(IEnumerable<Point> points, bool isFilled, FillRule fillRule)
	{
		_points = new Points(points);
		IsFilled = isFilled;
		_fillRule = fillRule;
	}

	/// <inheritdoc />
	public override Geometry Clone()
	{
		return new PolylineGeometry(Points, IsFilled, _fillRule)
		{
			Transform = base.Transform
		};
	}

	private protected sealed override IGeometryImpl? CreateDefiningGeometry()
	{
		IStreamGeometryImpl streamGeometryImpl = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>().CreateStreamGeometry();
		using (IStreamGeometryContextImpl streamGeometryContextImpl = streamGeometryImpl.Open())
		{
			streamGeometryContextImpl.SetFillRule(_fillRule);
			IList<Point> points = Points;
			bool isFilled = IsFilled;
			if (points.Count > 0)
			{
				streamGeometryContextImpl.BeginFigure(points[0], isFilled);
				for (int i = 1; i < points.Count; i++)
				{
					streamGeometryContextImpl.LineTo(points[i]);
				}
				streamGeometryContextImpl.EndFigure(isFilled);
			}
		}
		return streamGeometryImpl;
	}

	private void OnPointsChanged(IList<Point>? newValue)
	{
		_pointsObserver?.Dispose();
		_pointsObserver = (newValue as INotifyCollectionChanged)?.GetWeakCollectionChangedObservable().Subscribe(delegate
		{
			InvalidateGeometry();
		});
	}
}
