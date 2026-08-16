namespace Avalonia.Media;

public sealed class GlyphRunDrawing : Drawing
{
	public static readonly StyledProperty<IBrush?> ForegroundProperty = AvaloniaProperty.Register<GlyphRunDrawing, IBrush>("Foreground");

	public static readonly StyledProperty<GlyphRun?> GlyphRunProperty = AvaloniaProperty.Register<GlyphRunDrawing, GlyphRun>("GlyphRun");

	public IBrush? Foreground
	{
		get
		{
			return GetValue(ForegroundProperty);
		}
		set
		{
			SetValue(ForegroundProperty, value);
		}
	}

	public GlyphRun? GlyphRun
	{
		get
		{
			return GetValue(GlyphRunProperty);
		}
		set
		{
			SetValue(GlyphRunProperty, value);
		}
	}

	internal override void DrawCore(DrawingContext context)
	{
		if (GlyphRun != null)
		{
			context.DrawGlyphRun(Foreground, GlyphRun);
		}
	}

	public override Rect GetBounds()
	{
		if (GlyphRun == null)
		{
			return default(Rect);
		}
		return GlyphRun.Bounds;
	}
}
