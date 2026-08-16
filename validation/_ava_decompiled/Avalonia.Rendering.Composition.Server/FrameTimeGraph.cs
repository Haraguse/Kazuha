using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Represents a simple time graph for diagnostics purpose, used to show layout and render times.
/// </summary>
internal sealed class FrameTimeGraph
{
	private const double HeaderPadding = 2.0;

	private readonly IPlatformRenderInterface _renderInterface;

	private readonly ImmutableSolidColorBrush _borderBrush;

	private readonly ImmutablePen _graphPen;

	private readonly double[] _frameValues;

	private readonly Size _size;

	private readonly Size _headerSize;

	private readonly Size _graphSize;

	private readonly double _defaultMaxY;

	private readonly string _title;

	private readonly DiagnosticTextRenderer _textRenderer;

	private int _startFrameIndex;

	private int _frameCount;

	public Size Size => _size;

	public FrameTimeGraph(int maxFrames, Size size, double defaultMaxY, string title, DiagnosticTextRenderer textRenderer)
	{
		_renderInterface = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
		_borderBrush = new ImmutableSolidColorBrush(2155905152u);
		_graphPen = new ImmutablePen(Brushes.Blue);
		_frameValues = new double[maxFrames];
		_size = size;
		_headerSize = new Size(size.Width, textRenderer.GetMaxHeight() + 4.0);
		_graphSize = new Size(size.Width, size.Height - _headerSize.Height);
		_defaultMaxY = defaultMaxY;
		_title = title;
		_textRenderer = textRenderer;
	}

	public void AddFrameValue(double value)
	{
		if (_frameCount < _frameValues.Length)
		{
			_frameValues[_startFrameIndex + _frameCount] = value;
			_frameCount++;
			return;
		}
		_frameValues[_startFrameIndex] = value;
		if (++_startFrameIndex == _frameValues.Length)
		{
			_startFrameIndex = 0;
		}
	}

	public void Reset()
	{
		_startFrameIndex = 0;
		_frameCount = 0;
	}

	public void Render(ImmediateDrawingContext context)
	{
		using (context.PushClip(new Rect(_size)))
		{
			context.DrawRectangle(_borderBrush, null, new Rect(_size));
			context.DrawRectangle(_borderBrush, null, new Rect(_headerSize));
			using (context.PushPreTransform(Matrix.CreateTranslation(2.0, 2.0)))
			{
				_textRenderer.DrawAsciiText(context, _title.AsSpan(), Brushes.Black);
				if (_frameCount > 0)
				{
					var (value, value2, num) = GetYValues();
					DrawLabelledValue(context, "Min", value, _headerSize.Width * 0.19);
					DrawLabelledValue(context, "Avg", value2, _headerSize.Width * 0.46);
					DrawLabelledValue(context, "Max", num, _headerSize.Width * 0.73);
					using (context.PushPreTransform(Matrix.CreateTranslation(0.0, _headerSize.Height)))
					{
						context.PlatformImpl.DrawGeometry(null, _graphPen, BuildGraphGeometry(Math.Max(num, _defaultMaxY)));
						return;
					}
				}
			}
		}
	}

	private void DrawLabelledValue(ImmediateDrawingContext context, string label, double value, double left)
	{
		using (context.PushPreTransform(Matrix.CreateTranslation(left + 2.0, 2.0)))
		{
			IImmutableSolidColorBrush foreground = ((value <= _defaultMaxY) ? Brushes.Black : Brushes.Red);
			Span<char> span = stackalloc char[24];
			Span<char> destination = span;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			MemoryExtensions.TryWriteInterpolatedStringHandler handler = new MemoryExtensions.TryWriteInterpolatedStringHandler(4, 2, destination, invariantCulture, out var shouldAppend);
			if (shouldAppend && handler.AppendFormatted(label) && handler.AppendLiteral(": ") && handler.AppendFormatted(value, 5, "F2"))
			{
				handler.AppendLiteral("ms");
			}
			else
				_ = 0;
			destination.TryWrite(invariantCulture, ref handler, out var charsWritten);
			_textRenderer.DrawAsciiText(context, span.Slice(0, charsWritten), foreground);
		}
	}

	private IStreamGeometryImpl BuildGraphGeometry(double maxY)
	{
		IStreamGeometryImpl streamGeometryImpl = _renderInterface.CreateStreamGeometry();
		using IStreamGeometryContextImpl streamGeometryContextImpl = streamGeometryImpl.Open();
		double num = _graphSize.Width / (double)_frameValues.Length;
		double num2 = _graphSize.Height / maxY;
		streamGeometryContextImpl.BeginFigure(new Point(0.0, _graphSize.Height - GetFrameValue(0) * num2), isFilled: false);
		for (int i = 1; i < _frameCount; i++)
		{
			double x = Math.Round((double)i * num);
			double y = _graphSize.Height - GetFrameValue(i) * num2;
			streamGeometryContextImpl.LineTo(new Point(x, y));
		}
		streamGeometryContextImpl.EndFigure(isClosed: false);
		return streamGeometryImpl;
	}

	private (double Min, double Average, double Max) GetYValues()
	{
		double num = double.MaxValue;
		double num2 = double.MinValue;
		double num3 = 0.0;
		for (int i = 0; i < _frameCount; i++)
		{
			double frameValue = GetFrameValue(i);
			num3 += frameValue;
			if (frameValue < num)
			{
				num = frameValue;
			}
			if (frameValue > num2)
			{
				num2 = frameValue;
			}
		}
		return (Min: num, Average: num3 / (double)_frameCount, Max: num2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private double GetFrameValue(int frameOffset)
	{
		return _frameValues[(_startFrameIndex + frameOffset) % _frameValues.Length];
	}
}
