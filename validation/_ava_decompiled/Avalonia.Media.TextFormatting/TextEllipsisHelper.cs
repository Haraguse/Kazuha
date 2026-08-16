using System;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting;

internal static class TextEllipsisHelper
{
	public static TextRun[]? Collapse(TextLine textLine, TextCollapsingProperties properties, bool isWordEllipsis)
	{
		LogicalTextRunEnumerator logicalTextRunEnumerator = new LogicalTextRunEnumerator(textLine);
		if (logicalTextRunEnumerator.Count == 0)
		{
			return null;
		}
		ShapedTextRun shapedTextRun = TextFormatter.CreateSymbol(properties.Symbol, properties.FlowDirection);
		if (properties.Width < shapedTextRun.GlyphRun.Bounds.Width)
		{
			return Array.Empty<TextRun>();
		}
		double num = properties.Width - shapedTextRun.Size.Width;
		int num2 = 0;
		TextRun run;
		while (logicalTextRunEnumerator.MoveNext(out run))
		{
			if (!(run is ShapedTextRun { Size: { Width: var width } } shapedTextRun2))
			{
				if (run is DrawableTextRun { Size: var size2 } drawableTextRun)
				{
					if (size2.Width > num)
					{
						return TextCollapsingProperties.CreateCollapsedRuns(textLine, num2, shapedTextRun);
					}
					num -= drawableTextRun.Size.Width;
				}
			}
			else
			{
				if (width > num)
				{
					if (shapedTextRun2.TryMeasureCharacters(num, out var length) && isWordEllipsis && length < textLine.Length)
					{
						int num3 = 0;
						ReadOnlySpan<char> span = run.Text.Span;
						WordBreakEnumerator wordBreakEnumerator = new WordBreakEnumerator(span);
						WordSegment segment;
						while (num3 < length && wordBreakEnumerator.MoveNext(out segment))
						{
							int num4 = segment.Offset + segment.Length;
							if (num4 == 0 || num4 >= length)
							{
								break;
							}
							num3 = ((Codepoint.ReadAt(span, segment.Offset, out var _).WordBreakClass != WordBreakClass.WSegSpace) ? num4 : segment.Offset);
						}
						length = num3;
					}
					num2 += length;
					return TextCollapsingProperties.CreateCollapsedRuns(textLine, num2, shapedTextRun);
				}
				num -= width;
			}
			num2 += run.Length;
		}
		return null;
	}
}
