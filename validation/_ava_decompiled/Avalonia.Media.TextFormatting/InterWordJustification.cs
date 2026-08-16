using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Distributes the remaining line width across the break opportunities reported by the line
/// breaker - inter-word gaps for space-delimited scripts, and inter-character/inter-syllable
/// gaps for CJK and Korean (which the line breaker treats as break opportunities).
/// </summary>
/// <remarks>
/// Known limitations:
/// <list type="bullet">
/// <item>Thai, Lao, Khmer and Myanmar produce no line-break opportunities without dictionary or
/// ML based word segmentation (character-class line breaking cannot find their word boundaries),
/// so such lines are left un-justified (start-aligned) rather than spaced incorrectly.</item>
/// <item>Arabic and Hebrew are justified with inter-word spacing rather than the idiomatic
/// kashida (tatweel) elongation, which would require shaper-level support.</item>
/// </list>
/// </remarks>
internal class InterWordJustification : JustificationProperties
{
	public override double Width { get; }

	public InterWordJustification(double width)
	{
		Width = width;
	}

	public override void Justify(TextLine textLine)
	{
		if (!(textLine is TextLineImpl textLineImpl))
		{
			return;
		}
		double width = Width;
		if (double.IsInfinity(width))
		{
			return;
		}
		Queue<int> queue = new Queue<int>();
		int num = textLine.FirstTextSourceIndex;
		for (int i = 0; i < textLineImpl.TextRuns.Count; i++)
		{
			TextRun textRun = textLineImpl.TextRuns[i];
			ReadOnlyMemory<char> text = textRun.Text;
			if (text.IsEmpty)
			{
				continue;
			}
			LineBreakEnumerator lineBreakEnumerator = new LineBreakEnumerator(text.Span);
			LineBreak lineBreak;
			while (lineBreakEnumerator.MoveNext(out lineBreak))
			{
				if (!lineBreak.Required && lineBreak.PositionWrap != textRun.Length)
				{
					int num2 = num + lineBreak.PositionMeasure;
					if (lineBreak.PositionMeasure == lineBreak.PositionWrap)
					{
						num2--;
					}
					queue.Enqueue(num2);
				}
			}
			num += textRun.Length;
		}
		if (queue.Count == 0)
		{
			return;
		}
		double num3 = Math.Max(0.0, width - textLineImpl.Width) / (double)queue.Count;
		num = textLine.FirstTextSourceIndex;
		for (int j = 0; j < textLineImpl.TextRuns.Count; j++)
		{
			TextRun textRun2 = textLineImpl.TextRuns[j];
			int length = textRun2.Length;
			int num4 = num + length;
			ShapedTextRun shapedTextRun = (textRun2.Text.IsEmpty ? null : (textRun2 as ShapedTextRun));
			GlyphRun glyphRun = shapedTextRun?.GlyphRun;
			ShapedBuffer shapedBuffer = null;
			while (queue.Count > 0)
			{
				int num5 = queue.Peek();
				if (num5 >= num4)
				{
					break;
				}
				queue.Dequeue();
				if (num5 >= num && shapedTextRun != null)
				{
					if (shapedBuffer == null)
					{
						shapedBuffer = shapedTextRun.ShapedBuffer.CloneWritable();
					}
					int num6 = Math.Max(0, num - glyphRun.Metrics.FirstCluster);
					int index = glyphRun.FindGlyphIndex(num5 - num6);
					GlyphInfo glyphInfo = shapedBuffer[index];
					shapedBuffer[index] = new GlyphInfo(glyphInfo.GlyphIndex, glyphInfo.GlyphCluster, glyphInfo.GlyphAdvance + num3);
				}
			}
			if (shapedBuffer != null)
			{
				ShapedTextRun textRun3 = new ShapedTextRun(shapedBuffer, shapedTextRun.Properties);
				textLineImpl.ReplaceTextRun(j, textRun3);
				shapedTextRun.Dispose();
			}
			num += length;
		}
	}
}
