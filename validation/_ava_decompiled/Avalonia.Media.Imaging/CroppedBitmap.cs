using System;

namespace Avalonia.Media.Imaging;

/// <summary>
/// Crops a Bitmap.
/// </summary>
public class CroppedBitmap : AvaloniaObject, IImage, IAffectsRender, IDisposable
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Imaging.CroppedBitmap.Source" /> property.
	/// </summary>
	public static readonly StyledProperty<IImage?> SourceProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Imaging.CroppedBitmap.SourceRect" /> property.
	/// </summary>
	public static readonly StyledProperty<PixelRect> SourceRectProperty;

	/// <summary>
	/// Gets or sets the source for the bitmap.
	/// </summary>
	public IImage? Source
	{
		get
		{
			return GetValue(SourceProperty);
		}
		set
		{
			SetValue(SourceProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the rectangular area that the bitmap is cropped to.
	/// </summary>
	public PixelRect SourceRect
	{
		get
		{
			return GetValue(SourceRectProperty);
		}
		set
		{
			SetValue(SourceRectProperty, value);
		}
	}

	public Size Size
	{
		get
		{
			if (!(Source is IBitmap bitmap))
			{
				return default(Size);
			}
			if (SourceRect.Width == 0 && SourceRect.Height == 0)
			{
				return Source.Size;
			}
			return SourceRect.Size.ToSizeWithDpi(bitmap.Dpi);
		}
	}

	public event EventHandler? Invalidated;

	static CroppedBitmap()
	{
		SourceProperty = AvaloniaProperty.Register<CroppedBitmap, IImage>("Source");
		SourceRectProperty = AvaloniaProperty.Register<CroppedBitmap, PixelRect>("SourceRect");
		SourceRectProperty.Changed.AddClassHandler(delegate(CroppedBitmap x, AvaloniaPropertyChangedEventArgs e)
		{
			x.SourceRectChanged(e);
		});
		SourceProperty.Changed.AddClassHandler(delegate(CroppedBitmap x, AvaloniaPropertyChangedEventArgs e)
		{
			x.SourceChanged(e);
		});
	}

	public CroppedBitmap()
	{
	}

	public CroppedBitmap(IImage source, PixelRect sourceRect)
	{
		Source = source;
		SourceRect = sourceRect;
	}

	private void SourceChanged(AvaloniaPropertyChangedEventArgs e)
	{
		if (e.NewValue != null)
		{
			if (!(e.NewValue is IBitmap))
			{
				throw new ArgumentException("Only IBitmap supported as source");
			}
			Invalidated?.Invoke(this, e);
		}
	}

	private void SourceRectChanged(AvaloniaPropertyChangedEventArgs e)
	{
		Invalidated?.Invoke(this, e);
	}

	public virtual void Dispose()
	{
		(Source as IBitmap)?.Dispose();
	}

	public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
	{
		if (Source is IBitmap bitmap)
		{
			Point point = SourceRect.TopLeft.ToPointWithDpi(bitmap.Dpi);
			Source.Draw(context, sourceRect.Translate(new Vector(point.X, point.Y)), destRect);
		}
	}
}
