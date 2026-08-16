using Avalonia.Platform;

namespace Avalonia.Media;

/// <summary>
/// Represents the geometry of a rectangle.
/// </summary>
public class RectangleGeometry : Geometry
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.RectangleGeometry.RadiusX" /> property.
	/// </summary>
	public static readonly StyledProperty<double> RadiusXProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.RectangleGeometry.RadiusY" /> property.
	/// </summary>
	public static readonly StyledProperty<double> RadiusYProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.RectangleGeometry.Rect" /> property.
	/// </summary>
	public static readonly StyledProperty<Rect> RectProperty;

	/// <summary>
	/// Gets or sets the radius on the X-axis used to round the corners of the rectangle.
	/// Corner radii are represented by an ellipse so this is the X-axis width of the ellipse.
	/// </summary>
	/// <remarks>
	/// In order for this property to be used, <see cref="P:Avalonia.Media.RectangleGeometry.Rect" /> must not be set
	/// (equal to the default <see cref="T:Avalonia.Rect" /> value).
	/// </remarks>
	public double RadiusX
	{
		get
		{
			return GetValue(RadiusXProperty);
		}
		set
		{
			SetValue(RadiusXProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the radius on the Y-axis used to round the corners of the rectangle.
	/// Corner radii are represented by an ellipse so this is the Y-axis height of the ellipse.
	/// </summary>
	/// <remarks>
	/// In order for this property to be used, <see cref="P:Avalonia.Media.RectangleGeometry.Rect" /> must not be set
	/// (equal to the default <see cref="T:Avalonia.Rect" /> value).
	/// </remarks>
	public double RadiusY
	{
		get
		{
			return GetValue(RadiusYProperty);
		}
		set
		{
			SetValue(RadiusYProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the bounds of the rectangle.
	/// </summary>
	public Rect Rect
	{
		get
		{
			return GetValue(RectProperty);
		}
		set
		{
			SetValue(RectProperty, value);
		}
	}

	static RectangleGeometry()
	{
		RadiusXProperty = AvaloniaProperty.Register<RectangleGeometry, double>("RadiusX", 0.0);
		RadiusYProperty = AvaloniaProperty.Register<RectangleGeometry, double>("RadiusY", 0.0);
		RectProperty = AvaloniaProperty.Register<RectangleGeometry, Rect>("Rect");
		Geometry.AffectsGeometry(RadiusXProperty, RadiusYProperty, RectProperty);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.RectangleGeometry" /> class.
	/// </summary>
	public RectangleGeometry()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.RectangleGeometry" /> class.
	/// </summary>
	/// <param name="rect">The rectangle bounds.</param>
	public RectangleGeometry(Rect rect)
	{
		Rect = rect;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.RectangleGeometry" /> class.
	/// </summary>
	/// <param name="rect">The rectangle bounds.</param>
	/// <param name="radiusX">The radius on the X-axis used to round the corners of the rectangle.</param>
	/// <param name="radiusY">The radius on the Y-axis used to round the corners of the rectangle.</param>
	public RectangleGeometry(Rect rect, double radiusX, double radiusY)
	{
		Rect = rect;
		RadiusX = radiusX;
		RadiusY = radiusY;
	}

	/// <inheritdoc />
	public override Geometry Clone()
	{
		return new RectangleGeometry(Rect, RadiusX, RadiusY);
	}

	private protected sealed override IGeometryImpl? CreateDefiningGeometry()
	{
		double radiusX = RadiusX;
		double radiusY = RadiusY;
		IPlatformRenderInterface requiredService = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
		if (radiusX == 0.0 && radiusY == 0.0)
		{
			return requiredService.CreateRectangleGeometry(Rect);
		}
		IStreamGeometryImpl streamGeometryImpl = requiredService.CreateStreamGeometry();
		using StreamGeometryContext context = new StreamGeometryContext(streamGeometryImpl.Open());
		GeometryBuilder.DrawRoundedCornersRectangle(context, Rect, radiusX, radiusY);
		return streamGeometryImpl;
	}
}
