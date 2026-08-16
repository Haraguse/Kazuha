using System;
using System.Threading;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// A text run that holds shaped characters.
/// </summary>
/// <remarks>
/// Glyph data in the underlying <see cref="P:Avalonia.Media.TextFormatting.ShapedTextRun.ShapedBuffer" /> is immutable after shaping:
/// LTR buffers are in ascending-cluster (logical) order, RTL buffers are in
/// descending-cluster (visual) order. <see cref="T:Avalonia.Media.TextFormatting.BidiReorderer" /> only reorders runs,
/// it never mutates glyph order.
///
/// Ref-counted: the initial constructor call establishes one reference. Call
/// <see cref="M:Avalonia.Media.TextFormatting.ShapedTextRun.AddRef" /> when taking an additional reference (e.g., when a
/// <see cref="T:Avalonia.Media.TextFormatting.TextRunCache" /> stores a shaped run a formatter is also about to use),
/// and <see cref="M:Avalonia.Media.TextFormatting.ShapedTextRun.Dispose" /> to release. The underlying shaped buffer is disposed only
/// when the last reference is released.
/// </remarks>
public sealed class ShapedTextRun : DrawableTextRun, IDisposable
{
	private GlyphRun? _glyphRun;

	private int _refCount = 1;

	public sbyte BidiLevel => ShapedBuffer.BidiLevel;

	public ShapedBuffer ShapedBuffer { get; }

	/// <inheritdoc />
	public override ReadOnlyMemory<char> Text => ShapedBuffer.Text;

	/// <inheritdoc />
	public override TextRunProperties Properties { get; }

	/// <inheritdoc />
	public override int Length => ShapedBuffer.Text.Length;

	public TextMetrics TextMetrics { get; }

	public override double Baseline => 0.0 - TextMetrics.Ascent + TextMetrics.LineGap * 0.5;

	public override Size Size => GlyphRun.Bounds.Size;

	public GlyphRun GlyphRun => _glyphRun ?? (_glyphRun = CreateGlyphRun());

	public ShapedTextRun(ShapedBuffer shapedBuffer, TextRunProperties properties)
	{
		ShapedBuffer = shapedBuffer;
		Properties = properties;
		TextMetrics = new TextMetrics(properties.CachedGlyphTypeface, properties.FontRenderingEmSize);
	}

	/// <summary>
	/// Takes an additional reference to this run. Must be paired with <see cref="M:Avalonia.Media.TextFormatting.ShapedTextRun.Dispose" />.
	/// </summary>
	internal ShapedTextRun AddRef()
	{
		Interlocked.Increment(ref _refCount);
		return this;
	}

	/// <inheritdoc />
	public override void Draw(DrawingContext drawingContext, Point origin)
	{
		using (drawingContext.PushTransform(Matrix.CreateTranslation(origin)))
		{
			if (GlyphRun.GlyphInfos.Count == 0 || Properties.Typeface == default(Typeface) || Properties.ForegroundBrush == null)
			{
				return;
			}
			if (Properties.BackgroundBrush != null)
			{
				drawingContext.DrawRectangle(Properties.BackgroundBrush, null, GlyphRun.Bounds);
			}
			drawingContext.DrawGlyphRun(Properties.ForegroundBrush, GlyphRun);
			if (Properties.TextDecorations == null)
			{
				return;
			}
			foreach (TextDecoration textDecoration in Properties.TextDecorations)
			{
				textDecoration.Draw(drawingContext, GlyphRun, TextMetrics, Properties.ForegroundBrush);
			}
		}
	}

	/// <summary>
	/// Returns the largest count of <b>logical leading</b> characters of this
	/// run that fit within <paramref name="availableWidth" />. Cluster-atomic
	/// and direction-agnostic — for RTL runs the result is the count of chars
	/// from the logical start (not the visually-leftmost chars, which would
	/// be the logical tail).
	/// </summary>
	/// <param name="availableWidth">The available width.</param>
	/// <param name="length">The count of fitting characters.</param>
	/// <returns>
	/// <c>true</c> if at least one character fits within
	/// <paramref name="availableWidth" />; otherwise <c>false</c>.
	/// </returns>
	public bool TryMeasureCharacters(double availableWidth, out int length)
	{
		length = ShapedBuffer.FindLeadingCharCountWithinWidth(availableWidth);
		return length > 0;
	}

	/// <summary>
	/// Returns the largest count of <b>logical trailing</b> characters of
	/// this run that fit within <paramref name="availableWidth" />, along
	/// with the cumulative advance they consume. Cluster-atomic and
	/// direction-agnostic.
	/// </summary>
	internal bool TryMeasureCharactersBackwards(double availableWidth, out int length, out double width)
	{
		length = ShapedBuffer.FindTrailingCharCountWithinWidth(availableWidth, out width);
		return length > 0;
	}

	internal SplitResult<ShapedTextRun> Split(int length)
	{
		if (length == 0)
		{
			throw new ArgumentOutOfRangeException("length", "length must be greater than zero.");
		}
		SplitResult<ShapedBuffer> splitResult = ShapedBuffer.Split(length);
		ShapedTextRun shapedTextRun = new ShapedTextRun(splitResult.First, Properties);
		if (shapedTextRun.Length < length)
		{
			throw new InvalidOperationException("Split length too small.");
		}
		if (splitResult.Second == null)
		{
			return new SplitResult<ShapedTextRun>(shapedTextRun, null);
		}
		ShapedTextRun second = new ShapedTextRun(splitResult.Second, Properties);
		return new SplitResult<ShapedTextRun>(shapedTextRun, second);
	}

	internal GlyphRun CreateGlyphRun()
	{
		GlyphTypeface glyphTypeface = ShapedBuffer.GlyphTypeface;
		double fontRenderingEmSize = ShapedBuffer.FontRenderingEmSize;
		ReadOnlyMemory<char> text = Text;
		ShapedBuffer shapedBuffer = ShapedBuffer;
		int bidiLevel = BidiLevel;
		return new GlyphRun(glyphTypeface, fontRenderingEmSize, text, shapedBuffer, null, bidiLevel);
	}

	public void Dispose()
	{
		if (Interlocked.Decrement(ref _refCount) == 0)
		{
			_glyphRun?.Dispose();
			ShapedBuffer.Dispose();
		}
	}
}
