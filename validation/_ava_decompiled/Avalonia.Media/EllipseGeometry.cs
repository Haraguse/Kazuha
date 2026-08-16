using Avalonia.Platform;

namespace Avalonia.Media;

/// <summary>
/// Represents the geometry of an ellipse or circle.
/// </summary>
public class EllipseGeometry : Geometry
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.EllipseGeometry.Rect" /> property.
	/// </summary>
	public static readonly StyledProperty<Rect> RectProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.EllipseGeometry.RadiusX" /> property.
	/// </summary>
	public static readonly StyledProperty<double> RadiusXProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.EllipseGeometry.RadiusY" /> property.
	/// </summary>
	public static readonly StyledProperty<double> RadiusYProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.EllipseGeometry.Center" /> property.
	/// </summary>
	public static readonly StyledProperty<Point> CenterProperty;

	/// <summary>
	/// Gets or sets a rect that defines the bounds of the ellipse.
	/// </summary>
	/// <remarks>
	/// When set, this takes priority over the other properties that define an
	/// ellipse using a center point and X/Y-axis radii.
	/// </remarks>
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

	/// <summary>
	/// Gets or sets a double that defines the radius in the X-axis of the ellipse.
	/// </summary>
	/// <remarks>
	/// In order for this property to be used, <see cref="P:Avalonia.Media.EllipseGeometry.Rect" /> must not be set
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
	/// Gets or sets a double that defines the radius in the Y-axis of the ellipse.
	/// </summary>
	/// <remarks>
	/// In order for this property to be used, <see cref="P:Avalonia.Media.EllipseGeometry.Rect" /> must not be set
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
	/// Gets or sets a point that defines the center of the ellipse.
	/// </summary>
	/// <remarks>
	/// In order for this property to be used, <see cref="P:Avalonia.Media.EllipseGeometry.Rect" /> must not be set
	/// (equal to the default <see cref="T:Avalonia.Rect" /> value).
	/// </remarks>
	public Point Center
	{
		get
		{
			return GetValue(CenterProperty);
		}
		set
		{
			SetValue(CenterProperty, value);
		}
	}

	static EllipseGeometry()
	{
		RectProperty = AvaloniaProperty.Register<EllipseGeometry, Rect>("Rect");
		RadiusXProperty = AvaloniaProperty.Register<EllipseGeometry, double>("RadiusX", 0.0);
		RadiusYProperty = AvaloniaProperty.Register<EllipseGeometry, double>("RadiusY", 0.0);
		CenterProperty = AvaloniaProperty.Register<EllipseGeometry, Point>("Center");
		Geometry.AffectsGeometry(RectProperty, RadiusXProperty, RadiusYProperty, CenterProperty);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.EllipseGeometry" /> class.
	/// </summary>
	public EllipseGeometry()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.EllipseGeometry" /> class.
	/// </summary>
	/// <param name="rect">The rectangle that the ellipse should fill.</param>
	public EllipseGeometry(Rect rect)
		: this()
	{
		Rect = rect;
	}

	/// <inheritdoc />
	public override Geometry Clone()
	{
		return new EllipseGeometry
		{
			Rect = Rect,
			RadiusX = RadiusX,
			RadiusY = RadiusY,
			Center = Center
		};
	}

	/// <inheritdoc />
	private protected sealed override IGeometryImpl? CreateDefiningGeometry()
	{
		IPlatformRenderInterface requiredService = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
		if (Rect != default(Rect))
		{
			return requiredService.CreateEllipseGeometry(Rect);
		}
		double x = Center.X - RadiusX;
		double y = Center.Y - RadiusY;
		double width = RadiusX * 2.0;
		double height = RadiusY * 2.0;
		return requiredService.CreateEllipseGeometry(new Rect(x, y, width, height));
	}
}
