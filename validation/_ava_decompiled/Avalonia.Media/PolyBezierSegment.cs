using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Logging;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// PolyBezierSegment
/// </summary>
public sealed class PolyBezierSegment : PathSegment
{
	/// <summary>
	/// Points DirectProperty definition
	/// </summary>
	public static readonly DirectProperty<PolyBezierSegment, Points?> PointsProperty = AvaloniaProperty.RegisterDirect("Points", (PolyBezierSegment o) => o.Points, delegate(PolyBezierSegment o, Points? v)
	{
		o.Points = v;
	});

	private Points? _points = new Points();

	/// <summary>
	/// Gets or sets the Point collection that defines this <see cref="T:Avalonia.Media.PolyBezierSegment" /> object.
	/// </summary>
	/// <value>
	/// The points.
	/// </value>
	[Content]
	public Points? Points
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

	public PolyBezierSegment()
	{
	}

	public PolyBezierSegment(IEnumerable<Point> points, bool isStroked)
	{
		if (points == null)
		{
			throw new ArgumentNullException("points");
		}
		Points = new Points(points);
		base.IsStroked = isStroked;
	}

	internal override void ApplyTo(StreamGeometryContext ctx)
	{
		bool isStroked = base.IsStroked;
		Points points = _points;
		if (points != null && points.Count > 0)
		{
			int i;
			for (i = 0; i < points.Count; i += 3)
			{
				ctx.CubicBezierTo(points[i], points[i + 1], points[i + 2], isStroked);
			}
			int num = i - points.Count;
			if (num != 0)
			{
				Logger.TryGet(LogEventLevel.Warning, "Visual")?.Log("PolyBezierSegment", $"{"PolyBezierSegment"} has ivalid number of points. Last {Math.Abs(num)} points will be ignored.");
			}
		}
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = StringBuilderCache.Acquire();
		Points points = _points;
		if (points != null && points.Count > 0)
		{
			stringBuilder.Append('C').Append(' ');
			foreach (Point point in _points)
			{
				stringBuilder.Append(FormattableString.Invariant($"{point}"));
				stringBuilder.Append(' ');
			}
			stringBuilder.Length--;
		}
		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}
}
