using System;
using Avalonia.Metadata;

namespace Avalonia.Media;

/// <summary>
/// An <see cref="T:Avalonia.Media.IImage" /> that uses a <see cref="P:Avalonia.Media.DrawingImage.Drawing" /> for content.
/// </summary>
public class DrawingImage : AvaloniaObject, IImage, IAffectsRender
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.DrawingImage.Drawing" /> property.
	/// </summary>
	public static readonly StyledProperty<Drawing?> DrawingProperty = AvaloniaProperty.Register<DrawingImage, Drawing>("Drawing");

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.DrawingImage.Viewbox" /> property.
	/// </summary>
	public static readonly StyledProperty<Rect?> ViewboxProperty = AvaloniaProperty.Register<DrawingImage, Rect?>("Viewbox");

	/// <summary>
	/// Gets or sets the drawing content.
	/// </summary>
	[Content]
	public Drawing? Drawing
	{
		get
		{
			return GetValue(DrawingProperty);
		}
		set
		{
			SetValue(DrawingProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets a rectangular region of <see cref="P:Avalonia.Media.DrawingImage.Drawing" />, in device independent pixels, to display 
	/// when rendering this image.
	/// </summary>
	/// <remarks>
	/// This value can be used to display only part of <see cref="P:Avalonia.Media.DrawingImage.Drawing" />, or to surround it with empty 
	/// space. If null, <see cref="P:Avalonia.Media.DrawingImage.Drawing" /> will provide its own viewbox.
	/// </remarks>
	/// <seealso cref="M:Avalonia.Media.Drawing.GetBounds" />
	public Rect? Viewbox
	{
		get
		{
			return GetValue(ViewboxProperty);
		}
		set
		{
			SetValue(ViewboxProperty, value);
		}
	}

	/// <inheritdoc />
	public Size Size => GetBounds().Size;

	/// <inheritdoc />
	public event EventHandler? Invalidated;

	public DrawingImage()
	{
	}

	public DrawingImage(Drawing drawing)
	{
		Drawing = drawing;
	}

	private Rect GetBounds()
	{
		return Viewbox ?? Drawing?.GetBounds() ?? default(Rect);
	}

	/// <inheritdoc />
	void IImage.Draw(DrawingContext context, Rect sourceRect, Rect destRect)
	{
		Drawing drawing = Drawing;
		if (drawing == null || sourceRect.Size == default(Size) || destRect.Size == default(Size))
		{
			return;
		}
		Rect bounds = GetBounds();
		if (bounds.Size == default(Size))
		{
			return;
		}
		Matrix matrix = Matrix.CreateScale(destRect.Width / sourceRect.Width, destRect.Height / sourceRect.Height);
		Matrix matrix2 = Matrix.CreateTranslation(0.0 - sourceRect.X + destRect.X - bounds.X, 0.0 - sourceRect.Y + destRect.Y - bounds.Y);
		using (context.PushClip(destRect))
		{
			using (context.PushTransform(matrix2 * matrix))
			{
				drawing.Draw(context);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == DrawingProperty)
		{
			var (drawing, drawing2) = change.GetOldAndNewValue<Drawing>();
			if (drawing != null)
			{
				drawing.Invalidated -= DrawingInvalidated;
			}
			if (drawing2 != null)
			{
				drawing2.Invalidated += DrawingInvalidated;
			}
			RaiseInvalidated(EventArgs.Empty);
		}
		else if (change.Property == ViewboxProperty)
		{
			RaiseInvalidated(EventArgs.Empty);
		}
		void DrawingInvalidated(object? sender, EventArgs e)
		{
			RaiseInvalidated(EventArgs.Empty);
		}
	}

	/// <summary>
	/// Raises the <see cref="E:Avalonia.Media.DrawingImage.Invalidated" /> event.
	/// </summary>
	/// <param name="e">The event args.</param>
	protected void RaiseInvalidated(EventArgs e)
	{
		Invalidated?.Invoke(this, e);
	}
}
