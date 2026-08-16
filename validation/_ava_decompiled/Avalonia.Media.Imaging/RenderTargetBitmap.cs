using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Utilities;

namespace Avalonia.Media.Imaging;

/// <summary>
/// A bitmap that holds the rendering of a <see cref="T:Avalonia.Visual" />.
/// </summary>
public class RenderTargetBitmap : Bitmap
{
	/// <summary>
	/// Gets the platform-specific bitmap implementation.
	/// </summary>
	internal new IRef<IRenderTargetBitmapImpl> PlatformImpl { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Imaging.RenderTargetBitmap" /> class.
	/// </summary>
	/// <param name="pixelSize">The size of the bitmap.</param>
	public RenderTargetBitmap(PixelSize pixelSize)
		: this(pixelSize, new Vector(96.0, 96.0))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Imaging.RenderTargetBitmap" /> class.
	/// </summary>
	/// <param name="pixelSize">The size of the bitmap in device pixels.</param>
	/// <param name="dpi">The DPI of the bitmap.</param>
	public RenderTargetBitmap(PixelSize pixelSize, Vector dpi)
		: this(RefCountable.Create(CreateImpl(pixelSize, dpi)))
	{
	}

	private RenderTargetBitmap(IRef<IRenderTargetBitmapImpl> impl)
		: base(impl)
	{
		PlatformImpl = impl;
	}

	/// <summary>
	/// Renders a visual to the <see cref="T:Avalonia.Media.Imaging.RenderTargetBitmap" />.
	/// </summary>
	/// <param name="visual">The visual to render.</param>
	public void Render(Visual visual)
	{
		using DrawingContext context = CreateDrawingContext();
		ImmediateRenderer.Render(context, visual);
	}

	/// <summary>
	/// Creates a platform-specific implementation for a <see cref="T:Avalonia.Media.Imaging.RenderTargetBitmap" />.
	/// </summary>
	/// <param name="size">The size of the bitmap in device pixels.</param>
	/// <param name="dpi">The DPI of the bitmap.</param>
	/// <returns>The platform-specific implementation.</returns>
	private static IRenderTargetBitmapImpl CreateImpl(PixelSize size, Vector dpi)
	{
		return AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>().CreateRenderTargetBitmap(size, dpi);
	}

	/// <summary>
	/// Creates a <see cref="T:Avalonia.Media.DrawingContext" /> for drawing to the <see cref="T:Avalonia.Media.Imaging.RenderTargetBitmap" />. 
	/// Clears the current image data to transparent.
	/// </summary>
	/// <returns>The drawing context.</returns>
	public DrawingContext CreateDrawingContext()
	{
		return CreateDrawingContext(clear: true);
	}

	/// <summary>
	/// Creates a <see cref="T:Avalonia.Media.DrawingContext" /> for drawing to the <see cref="T:Avalonia.Media.Imaging.RenderTargetBitmap" />.
	/// </summary>
	/// <param name="clear">If true, clears the current image data to transparent, if false, leaves the image data unchanged.</param>
	/// <returns>The drawing context.</returns>
	public DrawingContext CreateDrawingContext(bool clear)
	{
		IDrawingContextImpl drawingContextImpl = PlatformImpl.Item.CreateDrawingContext();
		if (clear)
		{
			drawingContextImpl.Clear(Colors.Transparent);
		}
		return new PlatformDrawingContext(drawingContextImpl);
	}

	public override void Dispose()
	{
		PlatformImpl.Dispose();
		base.Dispose();
	}
}
