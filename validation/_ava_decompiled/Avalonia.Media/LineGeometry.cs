using Avalonia.Platform;

namespace Avalonia.Media;

/// <summary>
/// Represents the geometry of a line.
/// </summary>
public class LineGeometry : Geometry
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.LineGeometry.StartPoint" /> property.
	/// </summary>
	public static readonly StyledProperty<Point> StartPointProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.LineGeometry.EndPoint" /> property.
	/// </summary>
	public static readonly StyledProperty<Point> EndPointProperty;

	/// <summary>
	/// Gets or sets the start point of the line.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the end point of the line.
	/// </summary>
	public Point EndPoint
	{
		get
		{
			return GetValue(EndPointProperty);
		}
		set
		{
			SetValue(EndPointProperty, value);
		}
	}

	static LineGeometry()
	{
		StartPointProperty = AvaloniaProperty.Register<LineGeometry, Point>("StartPoint");
		EndPointProperty = AvaloniaProperty.Register<LineGeometry, Point>("EndPoint");
		Geometry.AffectsGeometry(StartPointProperty, EndPointProperty);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.LineGeometry" /> class.
	/// </summary>
	public LineGeometry()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.LineGeometry" /> class.
	/// </summary>
	/// <param name="startPoint">The start point.</param>
	/// <param name="endPoint">The end point.</param>
	public LineGeometry(Point startPoint, Point endPoint)
		: this()
	{
		StartPoint = startPoint;
		EndPoint = endPoint;
	}

	/// <inheritdoc />
	public override Geometry Clone()
	{
		return new LineGeometry(StartPoint, EndPoint);
	}

	/// <inheritdoc />
	private protected sealed override IGeometryImpl? CreateDefiningGeometry()
	{
		return AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>().CreateLineGeometry(StartPoint, EndPoint);
	}
}
