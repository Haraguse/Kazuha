using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// An FPS counter helper that can draw itself on the render thread
/// </summary>
internal class FpsCounter
{
	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private readonly DiagnosticTextRenderer _textRenderer;

	private int _framesThisSecond;

	private int _totalFrames;

	private int _fps;

	private TimeSpan _lastFpsUpdate;

	public FpsCounter(DiagnosticTextRenderer textRenderer)
	{
		_textRenderer = textRenderer;
	}

	public void FpsTick()
	{
		_framesThisSecond++;
	}

	public Rect? RenderFps(ImmediateDrawingContext context, string aux, bool hasLayer, Rect? oldRect)
	{
		TimeSpan elapsed = _stopwatch.Elapsed;
		TimeSpan timeSpan = elapsed - _lastFpsUpdate;
		_framesThisSecond++;
		_totalFrames++;
		if (timeSpan.TotalSeconds > 1.0)
		{
			_fps = (int)((double)_framesThisSecond / timeSpan.TotalSeconds);
			_framesThisSecond = 0;
			_lastFpsUpdate = elapsed;
		}
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(14, 3, invariantCulture);
		handler.AppendLiteral("Frame #");
		handler.AppendFormatted(_totalFrames, "00000000");
		handler.AppendLiteral(" FPS: ");
		handler.AppendFormatted(_fps, "000");
		handler.AppendLiteral(" ");
		handler.AppendFormatted(aux);
		string text = string.Create(invariantCulture, ref handler);
		Size size = _textRenderer.MeasureAsciiText(text.AsSpan());
		Rect rect = new Rect(0.0, 0.0, size.Width + 3.0, size.Height + 3.0);
		if (hasLayer && oldRect.HasValue)
		{
			rect = rect.Union(oldRect.Value);
		}
		ImmutableSolidColorBrush immutableSolidColorBrush = new ImmutableSolidColorBrush(Colors.Black, 0.5);
		IImmutableSolidColorBrush brush;
		if (!hasLayer)
		{
			brush = Brushes.Black;
		}
		else
		{
			IImmutableSolidColorBrush immutableSolidColorBrush2 = immutableSolidColorBrush;
			brush = immutableSolidColorBrush2;
		}
		context.DrawRectangle(brush, null, rect);
		_textRenderer.DrawAsciiText(context, text.AsSpan(), Brushes.White);
		return rect;
	}

	public void Reset()
	{
		_framesThisSecond = 0;
		_totalFrames = 0;
		_fps = 0;
	}
}
