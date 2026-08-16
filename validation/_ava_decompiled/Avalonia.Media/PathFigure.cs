using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Avalonia.Media;

public sealed class PathFigure : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.PathFigure.IsClosed" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsClosedProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.PathFigure.IsFilled" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsFilledProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.PathFigure.Segments" /> property.
	/// </summary>
	public static readonly DirectProperty<PathFigure, PathSegments?> SegmentsProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.PathFigure.StartPoint" /> property.
	/// </summary>
	public static readonly StyledProperty<Point> StartPointProperty;

	private PathSegments? _segments;

	private IDisposable? _segmentsDisposable;

	private IDisposable? _segmentsPropertiesDisposable;

	/// <summary>
	/// Gets or sets a value indicating whether this instance is closed.
	/// </summary>
	/// <value>
	///   <c>true</c> if this instance is closed; otherwise, <c>false</c>.
	/// </value>
	public bool IsClosed
	{
		get
		{
			return GetValue(IsClosedProperty);
		}
		set
		{
			SetValue(IsClosedProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether this instance is filled.
	/// </summary>
	/// <value>
	///   <c>true</c> if this instance is filled; otherwise, <c>false</c>.
	/// </value>
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
	/// Gets or sets the segments.
	/// </summary>
	/// <value>
	/// The segments.
	/// </value>
	[Content]
	public PathSegments? Segments
	{
		get
		{
			return _segments;
		}
		set
		{
			SetAndRaise(SegmentsProperty, ref _segments, value);
		}
	}

	/// <summary>
	/// Gets or sets the start point.
	/// </summary>
	/// <value>
	/// The start point.
	/// </value>
	public Point StartPoint
	{
		get
		{
			return GetValue(StartPointProperty);
		}
		set
		{
			SetValue(StartPointProperty, value);
		}
	}

	internal event EventHandler? SegmentsInvalidated;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.PathFigure" /> class.
	/// </summary>
	public PathFigure()
	{
		Segments = new PathSegments();
	}

	static PathFigure()
	{
		IsClosedProperty = AvaloniaProperty.Register<PathFigure, bool>("IsClosed", defaultValue: true);
		IsFilledProperty = AvaloniaProperty.Register<PathFigure, bool>("IsFilled", defaultValue: true);
		SegmentsProperty = AvaloniaProperty.RegisterDirect("Segments", (PathFigure f) => f.Segments, delegate(PathFigure f, PathSegments? s)
		{
			f.Segments = s;
		});
		StartPointProperty = AvaloniaProperty.Register<PathFigure, Point>("StartPoint");
		SegmentsProperty.Changed.AddClassHandler(delegate(PathFigure s, AvaloniaPropertyChangedEventArgs e)
		{
			s.OnSegmentsChanged();
		});
	}

	private void OnSegmentsChanged()
	{
		_segmentsDisposable?.Dispose();
		_segmentsPropertiesDisposable?.Dispose();
		_segmentsDisposable = _segments?.ForEachItem((Action<PathSegment>)delegate
		{
			InvalidateSegments();
		}, (Action<PathSegment>)delegate
		{
			InvalidateSegments();
		}, (Action)InvalidateSegments, false);
		_segmentsPropertiesDisposable = _segments?.TrackItemPropertyChanged(delegate
		{
			InvalidateSegments();
		});
	}

	private void InvalidateSegments()
	{
		SegmentsInvalidated?.Invoke(this, EventArgs.Empty);
	}

	public override string ToString()
	{
		object[] obj = new object[3] { StartPoint, null, null };
		IEnumerable<PathSegment> segments = _segments;
		obj[1] = string.Join(" ", segments ?? Enumerable.Empty<PathSegment>());
		obj[2] = (IsClosed ? "Z" : "");
		return FormattableString.Invariant(FormattableStringFactory.Create("M {0} {1}{2}", obj));
	}

	internal void ApplyTo(StreamGeometryContext ctx)
	{
		ctx.BeginFigure(StartPoint, IsFilled);
		if (Segments != null)
		{
			foreach (PathSegment segment in Segments)
			{
				segment.ApplyTo(ctx);
			}
		}
		ctx.EndFigure(IsClosed);
	}
}
