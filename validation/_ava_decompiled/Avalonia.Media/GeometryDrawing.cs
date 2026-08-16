using Avalonia.Media.Immutable;
using Avalonia.Metadata;

namespace Avalonia.Media;

/// <summary>
/// Represents a drawing operation that combines 
/// a geometry with and brush and/or pen to produce rendered content.
/// </summary>
public sealed class GeometryDrawing : Drawing
{
	private static readonly IPen s_boundsPen = new ImmutablePen(Colors.Black.ToUInt32(), 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.GeometryDrawing.Geometry" /> property.
	/// </summary>
	public static readonly StyledProperty<Geometry?> GeometryProperty = AvaloniaProperty.Register<GeometryDrawing, Geometry>("Geometry");

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.GeometryDrawing.Brush" /> property.
	/// </summary>
	public static readonly StyledProperty<IBrush?> BrushProperty = AvaloniaProperty.Register<GeometryDrawing, IBrush>("Brush", Brushes.Transparent);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.GeometryDrawing.Pen" /> property.
	/// </summary>
	public static readonly StyledProperty<IPen?> PenProperty = AvaloniaProperty.Register<GeometryDrawing, IPen>("Pen");

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Media.Geometry" /> that describes the shape of this <see cref="T:Avalonia.Media.GeometryDrawing" />.
	/// </summary>
	[Content]
	public Geometry? Geometry
	{
		get
		{
			return GetValue(GeometryProperty);
		}
		set
		{
			SetValue(GeometryProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Media.IBrush" /> used to fill the interior of the shape described by this <see cref="T:Avalonia.Media.GeometryDrawing" />.
	/// </summary>
	public IBrush? Brush
	{
		get
		{
			return GetValue(BrushProperty);
		}
		set
		{
			SetValue(BrushProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Media.IPen" /> used to stroke this <see cref="T:Avalonia.Media.GeometryDrawing" />.
	/// </summary>
	public IPen? Pen
	{
		get
		{
			return GetValue(PenProperty);
		}
		set
		{
			SetValue(PenProperty, value);
		}
	}

	internal override void DrawCore(DrawingContext context)
	{
		if (Geometry != null)
		{
			context.DrawGeometry(Brush, Pen, Geometry);
		}
	}

	public override Rect GetBounds()
	{
		IPen pen = Pen ?? s_boundsPen;
		return Geometry?.GetRenderBounds(pen) ?? default(Rect);
	}
}
