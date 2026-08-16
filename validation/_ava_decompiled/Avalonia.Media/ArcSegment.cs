using System;

namespace Avalonia.Media;

public sealed class ArcSegment : PathSegment
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.ArcSegment.IsLargeArc" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsLargeArcProperty = AvaloniaProperty.Register<ArcSegment, bool>("IsLargeArc", defaultValue: false);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.ArcSegment.Point" /> property.
	/// </summary>
	public static readonly StyledProperty<Point> PointProperty = AvaloniaProperty.Register<ArcSegment, Point>("Point");

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.ArcSegment.RotationAngle" /> property.
	/// </summary>
	public static readonly StyledProperty<double> RotationAngleProperty = AvaloniaProperty.Register<ArcSegment, double>("RotationAngle", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.ArcSegment.Size" /> property.
	/// </summary>
	public static readonly StyledProperty<Size> SizeProperty = AvaloniaProperty.Register<ArcSegment, Size>("Size");

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.ArcSegment.SweepDirection" /> property.
	/// </summary>
	public static readonly StyledProperty<SweepDirection> SweepDirectionProperty = AvaloniaProperty.Register<ArcSegment, SweepDirection>("SweepDirection", SweepDirection.Clockwise);

	/// <summary>
	/// Gets or sets a value indicating whether this instance is large arc.
	/// </summary>
	/// <value>
	/// <c>true</c> if this instance is large arc; otherwise, <c>false</c>.
	/// </value>
	public bool IsLargeArc
	{
		get
		{
			return GetValue(IsLargeArcProperty);
		}
		set
		{
			SetValue(IsLargeArcProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the point.
	/// </summary>
	/// <value>
	/// The point.
	/// </value>
	public Point Point
	{
		get
		{
			return GetValue(PointProperty);
		}
		set
		{
			SetValue(PointProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the rotation angle.
	/// </summary>
	/// <value>
	/// The rotation angle.
	/// </value>
	public double RotationAngle
	{
		get
		{
			return GetValue(RotationAngleProperty);
		}
		set
		{
			SetValue(RotationAngleProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the size.
	/// </summary>
	/// <value>
	/// The size.
	/// </value>
	public Size Size
	{
		get
		{
			return GetValue(SizeProperty);
		}
		set
		{
			SetValue(SizeProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the sweep direction.
	/// </summary>
	/// <value>
	/// The sweep direction.
	/// </value>
	public SweepDirection SweepDirection
	{
		get
		{
			return GetValue(SweepDirectionProperty);
		}
		set
		{
			SetValue(SweepDirectionProperty, value);
		}
	}

	internal override void ApplyTo(StreamGeometryContext ctx)
	{
		ctx.ArcTo(Point, Size, RotationAngle, IsLargeArc, SweepDirection, base.IsStroked);
	}

	public override string ToString()
	{
		return FormattableString.Invariant($"A {Size} {RotationAngle} {(IsLargeArc ? 1 : 0)} {(int)SweepDirection} {Point}");
	}
}
