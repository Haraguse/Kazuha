namespace Avalonia.Media;

/// <summary>
/// Draws an image within a region defined by a <see cref="P:Avalonia.Media.ImageDrawing.Rect" />.
/// </summary>
public sealed class ImageDrawing : Drawing
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.ImageDrawing.ImageSource" /> property.
	/// </summary>
	public static readonly StyledProperty<IImage?> ImageSourceProperty = AvaloniaProperty.Register<ImageDrawing, IImage>("ImageSource");

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.ImageDrawing.Rect" /> property.
	/// </summary>
	public static readonly StyledProperty<Rect> RectProperty = AvaloniaProperty.Register<ImageDrawing, Rect>("Rect");

	/// <summary>
	/// Gets or sets the source of the image.
	/// </summary>
	public IImage? ImageSource
	{
		get
		{
			return GetValue(ImageSourceProperty);
		}
		set
		{
			SetValue(ImageSourceProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets region in which the image is drawn.
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

	internal override void DrawCore(DrawingContext context)
	{
		IImage imageSource = ImageSource;
		Rect rect = Rect;
		if (imageSource != null && (rect.Width != 0.0 || rect.Height != 0.0))
		{
			context.DrawImage(imageSource, rect);
		}
	}

	public override Rect GetBounds()
	{
		return Rect;
	}
}
