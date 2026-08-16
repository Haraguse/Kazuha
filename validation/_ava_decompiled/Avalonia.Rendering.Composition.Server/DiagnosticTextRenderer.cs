using System;
using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// A class used to render diagnostic strings (only!), with caching of ASCII glyph runs.
/// </summary>
internal sealed class DiagnosticTextRenderer
{
	private const char FirstChar = ' ';

	private const char LastChar = '~';

	private readonly GlyphRun[] _runs = new GlyphRun[95];

	public double GetMaxHeight()
	{
		double num = 0.0;
		for (char c = ' '; c <= '~'; c = (char)(c + 1))
		{
			double height = _runs[c - 32].Bounds.Height;
			if (height > num)
			{
				num = height;
			}
		}
		return num;
	}

	public DiagnosticTextRenderer(GlyphTypeface glyphTypeface, double fontRenderingEmSize)
	{
		char[] array = new char[95];
		for (char c = ' '; c <= '~'; c = (char)(c + 1))
		{
			int num = c - 32;
			array[num] = c;
			ushort num2 = glyphTypeface.CharacterToGlyphMap[c];
			_runs[num] = new GlyphRun(glyphTypeface, fontRenderingEmSize, array.AsMemory(num, 1), new ushort[1] { num2 });
		}
	}

	public Size MeasureAsciiText(ReadOnlySpan<char> text)
	{
		double num = 0.0;
		double num2 = 0.0;
		ReadOnlySpan<char> readOnlySpan = text;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			char c2 = ((c >= ' ' && c <= '~') ? c : ' ');
			GlyphRun glyphRun = _runs[c2 - 32];
			num += glyphRun.Bounds.Width;
			num2 = Math.Max(num2, glyphRun.Bounds.Height);
		}
		return new Size(num, num2);
	}

	public void DrawAsciiText(ImmediateDrawingContext context, ReadOnlySpan<char> text, IImmutableBrush foreground)
	{
		double num = 0.0;
		ReadOnlySpan<char> readOnlySpan = text;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			char c2 = ((c >= ' ' && c <= '~') ? c : ' ');
			GlyphRun glyphRun = _runs[c2 - 32];
			using (context.PushPreTransform(Matrix.CreateTranslation(num, 0.0)))
			{
				context.PlatformImpl.DrawGlyphRun(foreground, glyphRun.PlatformImpl.Item);
			}
			num += glyphRun.Bounds.Width;
		}
	}
}
