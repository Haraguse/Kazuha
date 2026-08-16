using System.Collections.Generic;

namespace Avalonia.Media;

/// <summary>
/// Represents a set of line segments defined by a points collection with each Point specifying the end point of a line segment.
/// </summary>
public sealed class PolyLineSegment : PathSegment
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.PolyLineSegment.Points" /> property.
	/// </summary>
	public static readonly StyledProperty<IList<Point>> PointsProperty = AvaloniaProperty.Register<PolyLineSegment, IList<Point>>("Points");

	/// <summary>
	/// Gets or sets the points.
	/// </summary>
	/// <value>
	/// The points.
	/// </value>
	public IList<Point> Points
	{
		get
		{
			return GetValue(PointsProperty);
		}
		set
		{
			SetValue(PointsProperty, value);
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.PolyLineSegment" /> class.
	/// </summary>
	public PolyLineSegment()
	{
		Points = new Points();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.PolyLineSegment" /> class.
	/// </summary>
	/// <param name="points">The points.</param>
	public PolyLineSegment(IEnumerable<Point> points)
	{
		Points = new Points(points);
	}

	internal override void ApplyTo(StreamGeometryContext ctx)
	{
		IList<Point> points = Points;
		if (points.Count > 0)
		{
			for (int i = 0; i < points.Count; i++)
			{
				ctx.LineTo(points[i], base.IsStroked);
			}
		}
	}

	public override string ToString()
	{
		if (Points.Count < 1)
		{
			return "";
		}
		return "L " + string.Join(" ", Points);
	}
}
