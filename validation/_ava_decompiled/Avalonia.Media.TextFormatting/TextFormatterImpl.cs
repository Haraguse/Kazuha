using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting;

internal sealed class TextFormatterImpl : TextFormatter
{
	private struct TextRunEnumerator(ITextSource textSource, int firstTextSourceIndex)
	{
		private readonly ITextSource _textSource = textSource;

		private int _pos = firstTextSourceIndex;

		public TextRun? Current { get; private set; } = null;

		public bool MoveNext()
		{
			Current = _textSource.GetTextRun(_pos);
			if (Current == null)
			{
				return false;
			}
			if (Current.Length == 0)
			{
				return false;
			}
			_pos += Current.Length;
			return true;
		}
	}

	private static readonly char[] s_empty = new char[1] { ' ' };

	private static readonly string s_defaultText = new string('a', 1);

	[ThreadStatic]
	private static BidiData? t_bidiData;

	[ThreadStatic]
	private static BidiAlgorithm? t_bidiAlgorithm;

	/// <inheritdoc />
	public override TextLine? FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth, TextParagraphProperties paragraphProperties, TextLineBreak? previousLineBreak = null)
	{
		return FormatLine(textSource, firstTextSourceIndex, paragraphWidth, paragraphProperties, previousLineBreak, null);
	}

	/// <inheritdoc />
	public override TextLine? FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth, TextParagraphProperties paragraphProperties, TextLineBreak? previousLineBreak, TextRunCache? textRunCache)
	{
		TextLineBreak textLineBreak = null;
		FormattingObjectPool instance = FormattingObjectPool.Instance;
		FontManager current = FontManager.Current;
		if (previousLineBreak is WrappingTextLineBreak wrappingTextLineBreak)
		{
			List<TextRun> list = wrappingTextLineBreak.AcquireRemainingRuns();
			if (list != null && paragraphProperties.TextWrapping != TextWrapping.NoWrap)
			{
				return PerformTextWrapping(list, canReuseTextRunList: true, firstTextSourceIndex, paragraphWidth, paragraphProperties, previousLineBreak.FlowDirection, previousLineBreak, instance);
			}
		}
		if (textRunCache != null && textRunCache.TryGetShapedRuns(firstTextSourceIndex, out var result))
		{
			return FormatLineFromCache(result, firstTextSourceIndex, paragraphWidth, paragraphProperties, instance);
		}
		FormattingObjectPool.RentedList<TextRun> rentedList = null;
		FormattingObjectPool.RentedList<TextRun> rentedList2 = null;
		try
		{
			rentedList = FetchTextRuns(textSource, firstTextSourceIndex, instance, out TextEndOfLine endOfLine, out int textSourceLength);
			if (rentedList.Count == 0)
			{
				return null;
			}
			rentedList2 = ShapeTextRuns(rentedList, paragraphProperties, instance, current, out var resolvedFlowDirection);
			if (textLineBreak == null && endOfLine != null)
			{
				textLineBreak = new TextLineBreak(endOfLine, resolvedFlowDirection);
			}
			textRunCache?.Add(firstTextSourceIndex, new CachedShapingResult(rentedList2.ToArray(), resolvedFlowDirection, endOfLine, textSourceLength));
			switch (paragraphProperties.TextWrapping)
			{
			case TextWrapping.NoWrap:
			{
				TextLineImpl textLineImpl = new TextLineImpl(rentedList2.ToArray(), firstTextSourceIndex, textSourceLength, paragraphWidth, paragraphProperties, resolvedFlowDirection, textLineBreak);
				textLineImpl.FinalizeLine();
				return textLineImpl;
			}
			case TextWrapping.Wrap:
			case TextWrapping.WrapWithOverflow:
				return PerformTextWrapping(rentedList2, canReuseTextRunList: false, firstTextSourceIndex, paragraphWidth, paragraphProperties, resolvedFlowDirection, textLineBreak, instance);
			default:
				throw new ArgumentOutOfRangeException("TextWrapping");
			}
		}
		finally
		{
			instance.TextRunLists.Return(ref rentedList2);
			instance.TextRunLists.Return(ref rentedList);
		}
	}

	/// <summary>
	/// Formats a line from cached shaped runs, skipping shaping and bidi processing.
	/// </summary>
	private static TextLine FormatLineFromCache(CachedShapingResult cached, int firstTextSourceIndex, double paragraphWidth, TextParagraphProperties paragraphProperties, FormattingObjectPool objectPool)
	{
		FlowDirection resolvedFlowDirection = cached.ResolvedFlowDirection;
		TextLineBreak textLineBreak = null;
		if (cached.TextEndOfLine != null)
		{
			textLineBreak = new TextLineBreak(cached.TextEndOfLine, resolvedFlowDirection);
		}
		switch (paragraphProperties.TextWrapping)
		{
		case TextWrapping.NoWrap:
		{
			TextLineImpl textLineImpl = new TextLineImpl(AddRefShapedRuns(cached.ShapedRuns), firstTextSourceIndex, cached.TextSourceLength, paragraphWidth, paragraphProperties, resolvedFlowDirection, textLineBreak);
			textLineImpl.FinalizeLine();
			return textLineImpl;
		}
		case TextWrapping.Wrap:
		case TextWrapping.WrapWithOverflow:
		{
			List<TextRun> list = new List<TextRun>(cached.ShapedRuns.Length);
			for (int i = 0; i < cached.ShapedRuns.Length; i++)
			{
				list.Add((cached.ShapedRuns[i] is ShapedTextRun shapedTextRun) ? shapedTextRun.AddRef() : cached.ShapedRuns[i]);
			}
			return PerformTextWrapping(list, canReuseTextRunList: false, firstTextSourceIndex, paragraphWidth, paragraphProperties, resolvedFlowDirection, textLineBreak, objectPool);
		}
		default:
			throw new ArgumentOutOfRangeException("TextWrapping");
		}
	}

	/// <summary>
	/// Produces an array of text runs for a line, adding an extra reference to each
	/// <see cref="T:Avalonia.Media.TextFormatting.ShapedTextRun" /> so that the caller owns a disposable reference.
	/// </summary>
	private static TextRun[] AddRefShapedRuns(IReadOnlyList<TextRun> runs)
	{
		TextRun[] array = new TextRun[runs.Count];
		for (int i = 0; i < runs.Count; i++)
		{
			array[i] = ((runs[i] is ShapedTextRun shapedTextRun) ? shapedTextRun.AddRef() : runs[i]);
		}
		return array;
	}

	/// <summary>
	/// Split a sequence of runs into two segments at specified length.
	/// </summary>
	/// <param name="textRuns">The text run's.</param>
	/// <param name="length">The length to split at.</param>
	/// <param name="objectPool">A pool used to get reusable formatting objects.</param>
	/// <returns>The split text runs.</returns>
	internal static SplitResult<FormattingObjectPool.RentedList<TextRun>> SplitTextRuns(IReadOnlyList<TextRun> textRuns, int length, FormattingObjectPool objectPool)
	{
		int firstLength;
		return SplitTextRuns(textRuns, length, objectPool, out firstLength);
	}

	/// <summary>
	/// Split a sequence of runs into two segments at specified length. The actual
	/// length of the first segment (which may differ from <paramref name="length" />
	/// when the split lands on a cluster boundary) is returned via
	/// <paramref name="firstLength" />. This lets the wrap caller avoid a separate
	/// second pass to sum run lengths.
	/// </summary>
	internal static SplitResult<FormattingObjectPool.RentedList<TextRun>> SplitTextRuns(IReadOnlyList<TextRun> textRuns, int length, FormattingObjectPool objectPool, out int firstLength)
	{
		if (length == 0)
		{
			FormattingObjectPool.RentedList<TextRun> rentedList = objectPool.TextRunLists.Rent();
			for (int i = 0; i < textRuns.Count; i++)
			{
				rentedList.Add(textRuns[i]);
			}
			firstLength = 0;
			return new SplitResult<FormattingObjectPool.RentedList<TextRun>>(null, rentedList);
		}
		FormattingObjectPool.RentedList<TextRun> rentedList2 = objectPool.TextRunLists.Rent();
		int num = 0;
		for (int j = 0; j < textRuns.Count; j++)
		{
			TextRun textRun = textRuns[j];
			int length2 = textRun.Length;
			if (num + length2 < length)
			{
				num += length2;
				continue;
			}
			int num2 = ((length2 >= 1) ? (j + 1) : j);
			if (num2 > 1)
			{
				for (int k = 0; k < j; k++)
				{
					rentedList2.Add(textRuns[k]);
				}
			}
			int num3 = textRuns.Count - num2;
			if (num + length2 == length)
			{
				FormattingObjectPool.RentedList<TextRun> rentedList3 = ((num3 > 0) ? objectPool.TextRunLists.Rent() : null);
				if (rentedList3 != null)
				{
					int num4 = ((length2 >= 1) ? 1 : 0);
					for (int l = 0; l < num3; l++)
					{
						rentedList3.Add(textRuns[j + l + num4]);
					}
				}
				rentedList2.Add(textRun);
				firstLength = num + length2;
				return new SplitResult<FormattingObjectPool.RentedList<TextRun>>(rentedList2, rentedList3);
			}
			num3++;
			FormattingObjectPool.RentedList<TextRun> rentedList4 = objectPool.TextRunLists.Rent();
			int num5 = 0;
			int num6;
			if (textRun is ShapedTextRun shapedTextRun)
			{
				SplitResult<ShapedTextRun> splitResult = shapedTextRun.Split(length - num);
				if (splitResult.First != null)
				{
					rentedList2.Add(splitResult.First);
					num5 = splitResult.First.Length;
				}
				if (splitResult.Second != null)
				{
					rentedList4.Add(splitResult.Second);
				}
				shapedTextRun.Dispose();
				num6 = 1;
			}
			else if (num == 0)
			{
				rentedList2.Add(textRun);
				num5 = length2;
				num6 = 1;
			}
			else
			{
				num6 = 0;
			}
			for (int m = num6; m < num3; m++)
			{
				rentedList4.Add(textRuns[j + m]);
			}
			firstLength = num + num5;
			return new SplitResult<FormattingObjectPool.RentedList<TextRun>>(rentedList2, rentedList4);
		}
		for (int n = 0; n < textRuns.Count; n++)
		{
			rentedList2.Add(textRuns[n]);
		}
		firstLength = num;
		return new SplitResult<FormattingObjectPool.RentedList<TextRun>>(rentedList2, null);
	}

	/// <summary>
	/// Shape specified text runs with specified paragraph embedding.
	/// </summary>
	/// <param name="textRuns">The text runs to shape.</param>
	/// <param name="paragraphProperties">The default paragraph properties.</param>
	/// <param name="resolvedFlowDirection">The resolved flow direction.</param>
	/// <param name="objectPool">A pool used to get reusable formatting objects.</param>
	/// <param name="fontManager">The font manager to use.</param>
	/// <returns>
	/// A list of shaped text characters.
	/// </returns>
	private static FormattingObjectPool.RentedList<TextRun> ShapeTextRuns(IReadOnlyList<TextRun> textRuns, TextParagraphProperties paragraphProperties, FormattingObjectPool objectPool, FontManager fontManager, out FlowDirection resolvedFlowDirection)
	{
		FlowDirection flowDirection = paragraphProperties.FlowDirection;
		FormattingObjectPool.RentedList<TextRun> rentedList = objectPool.TextRunLists.Rent();
		if (textRuns.Count == 0)
		{
			resolvedFlowDirection = flowDirection;
			return rentedList;
		}
		BidiData bidiData = t_bidiData ?? (t_bidiData = new BidiData());
		bidiData.Reset();
		bidiData.ParagraphEmbeddingLevel = (sbyte)flowDirection;
		for (int i = 0; i < textRuns.Count; i++)
		{
			TextRun textRun = textRuns[i];
			ReadOnlyMemory<char> text = textRun.Text;
			ReadOnlySpan<char> text2;
			if (!text.IsEmpty)
			{
				text = textRun.Text;
				text2 = text.Span;
			}
			else
			{
				text2 = ((textRun.Length != 1) ? new string('a', textRun.Length).AsSpan() : s_defaultText.AsSpan());
			}
			bidiData.Append(text2);
		}
		BidiAlgorithm bidiAlgorithm = t_bidiAlgorithm ?? (t_bidiAlgorithm = new BidiAlgorithm());
		bidiAlgorithm.Process(bidiData);
		sbyte b = bidiAlgorithm.ResolveEmbeddingLevel(bidiData.Classes);
		resolvedFlowDirection = (((b & 1) != 0) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
		FormattingObjectPool.RentedList<TextRun> rentedList2 = objectPool.TextRunLists.Rent();
		FormattingObjectPool.RentedList<UnshapedTextRun> rentedList3 = objectPool.UnshapedTextRunLists.Rent();
		try
		{
			CoalesceLevels(textRuns, bidiAlgorithm.ResolvedLevels.Span, fontManager, rentedList2);
			bidiData.Reset();
			bidiAlgorithm.Reset();
			TextShaper current = TextShaper.Current;
			for (int j = 0; j < rentedList2.Count; j++)
			{
				TextRun textRun2 = rentedList2[j];
				UnshapedTextRun unshapedTextRun = textRun2 as UnshapedTextRun;
				if (unshapedTextRun != null)
				{
					rentedList3.Clear();
					rentedList3.Add(unshapedTextRun);
					ReadOnlyMemory<char> readOnlyMemory = unshapedTextRun.Text;
					TextRunProperties properties = unshapedTextRun.Properties;
					ReadOnlyMemory<char> joinedMemory;
					while (j + 1 < rentedList2.Count && rentedList2[j + 1] is UnshapedTextRun unshapedTextRun2 && unshapedTextRun.BidiLevel == unshapedTextRun2.BidiLevel && TryJoinContiguousMemories(readOnlyMemory, unshapedTextRun2.Text, out joinedMemory) && CanShapeTogether(properties, unshapedTextRun2.Properties))
					{
						rentedList3.Add(unshapedTextRun2);
						j++;
						unshapedTextRun = unshapedTextRun2;
						readOnlyMemory = joinedMemory;
					}
					ShapeTogether(options: new TextShaperOptions(properties.CachedGlyphTypeface, properties.FontRenderingEmSize, unshapedTextRun.BidiLevel, properties.CultureInfo, paragraphProperties.DefaultIncrementalTab, paragraphProperties.LetterSpacing, properties.FontFeatures), textRuns: rentedList3, text: readOnlyMemory, textShaper: current, results: rentedList);
				}
				else
				{
					rentedList.Add(textRun2);
				}
			}
			return rentedList;
		}
		finally
		{
			objectPool.TextRunLists.Return(ref rentedList2);
			objectPool.UnshapedTextRunLists.Return(ref rentedList3);
		}
	}

	/// <summary>
	/// Tries to join two potnetially contiguous memory regions.
	/// </summary>
	/// <param name="x">The first memory region.</param>
	/// <param name="y">The second memory region.</param>
	/// <param name="joinedMemory">On success, a memory region representing the union of the two regions.</param>
	/// <returns>true if the two regions were contigous; false otherwise.</returns>
	private static bool TryJoinContiguousMemories(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y, out ReadOnlyMemory<char> joinedMemory)
	{
		ArraySegment<char> segment;
		MemoryManager<char> manager;
		MemoryManager<char> manager2;
		int start3;
		int length3;
		int joinedStart3;
		if (MemoryMarshal.TryGetString(x, out string text, out int start, out int length))
		{
			if (MemoryMarshal.TryGetString(y, out string text2, out int start2, out int length2) && (object)text == text2 && TryGetContiguousStart(start, length, start2, length2, out var joinedStart))
			{
				joinedMemory = text.AsMemory(joinedStart, length + length2);
				return true;
			}
		}
		else if (MemoryMarshal.TryGetArray(x, out segment))
		{
			if (MemoryMarshal.TryGetArray(y, out var segment2) && segment.Array == segment2.Array && TryGetContiguousStart(segment.Offset, segment.Count, segment2.Offset, segment2.Count, out var joinedStart2))
			{
				joinedMemory = segment.Array.AsMemory(joinedStart2, segment.Count + segment2.Count);
				return true;
			}
		}
		else if (MemoryMarshal.TryGetMemoryManager<char, MemoryManager<char>>(x, out manager, out start, out length) && MemoryMarshal.TryGetMemoryManager<char, MemoryManager<char>>(y, out manager2, out start3, out length3) && manager == manager2 && TryGetContiguousStart(start, length, start3, length3, out joinedStart3))
		{
			joinedMemory = manager.Memory.Slice(joinedStart3, length + length3);
			return true;
		}
		joinedMemory = default(ReadOnlyMemory<char>);
		return false;
		static bool TryGetContiguousStart(int xStart, int xLength, int yStart, int yLength, out int reference)
		{
			(int, int) tuple = (xStart, xLength);
			(int, int) tuple2 = (yStart, yLength);
			(int, int) tuple4;
			(int, int) tuple5;
			if (xStart > yStart)
			{
				(int, int) tuple3 = tuple;
				tuple4 = tuple3;
				tuple5 = tuple2;
			}
			else
			{
				(int, int) tuple3 = tuple2;
				tuple4 = tuple3;
				tuple5 = tuple;
			}
			if (tuple5.Item1 + tuple5.Item2 == tuple4.Item1)
			{
				(reference, _) = tuple5;
				return true;
			}
			reference = 0;
			return false;
		}
	}

	private static bool CanShapeTogether(TextRunProperties x, TextRunProperties y)
	{
		if (MathUtilities.AreClose(x.FontRenderingEmSize, y.FontRenderingEmSize) && x.Typeface == y.Typeface)
		{
			return x.BaselineAlignment == y.BaselineAlignment;
		}
		return false;
	}

	private static void ShapeTogether(IReadOnlyList<UnshapedTextRun> textRuns, ReadOnlyMemory<char> text, TextShaperOptions options, TextShaper textShaper, FormattingObjectPool.RentedList<TextRun> results)
	{
		ShapedBuffer shapedBuffer = textShaper.ShapeText(text, options);
		int num = 0;
		for (int i = 0; i < textRuns.Count; i++)
		{
			UnshapedTextRun unshapedTextRun = textRuns[i];
			SplitResult<ShapedBuffer> splitResult = shapedBuffer.Split(num + unshapedTextRun.Length);
			if (splitResult.First == null || splitResult.First.Length == 0)
			{
				num += unshapedTextRun.Length;
			}
			else
			{
				num = 0;
				results.Add(new ShapedTextRun(splitResult.First, unshapedTextRun.Properties));
			}
			if (splitResult.Second == null)
			{
				break;
			}
			shapedBuffer = splitResult.Second;
		}
	}

	/// <summary>
	/// Coalesces ranges of the same bidi level to form <see cref="T:Avalonia.Media.TextFormatting.UnshapedTextRun" />
	/// </summary>
	/// <param name="textCharacters">The text characters to form <see cref="T:Avalonia.Media.TextFormatting.UnshapedTextRun" /> from.</param>
	/// <param name="levels">The bidi levels.</param>
	/// <param name="fontManager">The font manager to use.</param>
	/// <param name="processedRuns">A list that will be filled with the processed runs.</param>
	/// <returns></returns>
	private static void CoalesceLevels(IReadOnlyList<TextRun> textCharacters, ReadOnlySpan<sbyte> levels, FontManager fontManager, FormattingObjectPool.RentedList<TextRun> processedRuns)
	{
		if (levels.Length == 0)
		{
			return;
		}
		int num = 0;
		sbyte b = levels[0];
		TextRunProperties previousProperties = null;
		TextCharacters textCharacters2 = null;
		ReadOnlyMemory<char> text = default(ReadOnlyMemory<char>);
		for (int i = 0; i < textCharacters.Count; i++)
		{
			int num2 = 0;
			textCharacters2 = textCharacters[i] as TextCharacters;
			if (textCharacters2 == null)
			{
				TextRun textRun = textCharacters[i];
				processedRuns.Add(textRun);
				num += textRun.Length;
				continue;
			}
			text = textCharacters2.Text;
			ReadOnlySpan<char> span = text.Span;
			while (num2 < span.Length)
			{
				Codepoint.ReadAt(span, num2, out var count);
				if (num + 1 == levels.Length)
				{
					break;
				}
				num++;
				num2 += count;
				if (num2 == span.Length)
				{
					textCharacters2.GetShapeableCharacters(text.Slice(0, num2), b, fontManager, ref previousProperties, processedRuns);
					b = levels[num];
				}
				else if (levels[num] != b)
				{
					textCharacters2.GetShapeableCharacters(text.Slice(0, num2), b, fontManager, ref previousProperties, processedRuns);
					text = text.Slice(num2);
					span = text.Span;
					num2 = 0;
					b = levels[num];
				}
			}
		}
		if (textCharacters2 != null && !text.IsEmpty)
		{
			textCharacters2.GetShapeableCharacters(text, b, fontManager, ref previousProperties, processedRuns);
		}
	}

	/// <summary>
	/// Fetches text runs.
	/// </summary>
	/// <param name="textSource">The text source.</param>
	/// <param name="firstTextSourceIndex">The first text source index.</param>
	/// <param name="objectPool">A pool used to get reusable formatting objects.</param>
	/// <param name="endOfLine">On return, the end of line, if any.</param>
	/// <param name="textSourceLength">On return, the processed text source length.</param>
	/// <returns>
	/// The formatted text runs.
	/// </returns>
	private static FormattingObjectPool.RentedList<TextRun> FetchTextRuns(ITextSource textSource, int firstTextSourceIndex, FormattingObjectPool objectPool, out TextEndOfLine? endOfLine, out int textSourceLength)
	{
		textSourceLength = 0;
		endOfLine = null;
		FormattingObjectPool.RentedList<TextRun> rentedList = objectPool.TextRunLists.Rent();
		TextRunEnumerator textRunEnumerator = new TextRunEnumerator(textSource, firstTextSourceIndex);
		while (textRunEnumerator.MoveNext())
		{
			TextRun current = textRunEnumerator.Current;
			if (current is TextEndOfLine textEndOfLine)
			{
				endOfLine = textEndOfLine;
				textSourceLength += textEndOfLine.Length;
				rentedList.Add(current);
				break;
			}
			if (current is TextCharacters textCharacters)
			{
				if (TryGetLineBreak(textCharacters, out var lineBreak))
				{
					TextCharacters item = new TextCharacters(textCharacters.Text.Slice(0, lineBreak.PositionWrap), textCharacters.Properties);
					rentedList.Add(item);
					textSourceLength += lineBreak.PositionWrap;
					return rentedList;
				}
				rentedList.Add(textCharacters);
			}
			else
			{
				rentedList.Add(current);
			}
			textSourceLength += current.Length;
		}
		return rentedList;
	}

	private static bool TryGetLineBreak(TextRun textRun, out LineBreak lineBreak)
	{
		lineBreak = default(LineBreak);
		ReadOnlyMemory<char> text = textRun.Text;
		if (text.IsEmpty)
		{
			return false;
		}
		LineBreakEnumerator lineBreakEnumerator = new LineBreakEnumerator(text.Span);
		while (lineBreakEnumerator.MoveNext(out lineBreak))
		{
			if (lineBreak.Required)
			{
				if (lineBreak.PositionWrap < textRun.Length)
				{
					return true;
				}
				return true;
			}
		}
		return false;
	}

	private static int MeasureLength(IReadOnlyList<TextRun> textRuns, double paragraphWidth)
	{
		int num = 0;
		double num2 = 0.0;
		for (int i = 0; i < textRuns.Count; i++)
		{
			TextRun textRun = textRuns[i];
			if (!(textRun is ShapedTextRun shapedTextRun))
			{
				if (textRun is DrawableTextRun drawableTextRun)
				{
					if (MathUtilities.GreaterThan(num2 + drawableTextRun.Size.Width, paragraphWidth))
					{
						return num;
					}
					num += textRun.Length;
					num2 += drawableTextRun.Size.Width;
				}
				else
				{
					num += textRun.Length;
				}
				continue;
			}
			ShapedBuffer shapedBuffer = shapedTextRun.ShapedBuffer;
			if (shapedBuffer.Length == 0)
			{
				continue;
			}
			double num3 = paragraphWidth - num2;
			double totalGlyphAdvance = shapedBuffer.TotalGlyphAdvance;
			if (!MathUtilities.GreaterThan(totalGlyphAdvance, num3))
			{
				num2 += totalGlyphAdvance;
				num += textRun.Length;
				continue;
			}
			int num4 = shapedBuffer.FindLeadingCharCountWithinWidth(num3);
			if (num4 == 0 && num == 0)
			{
				num4 = shapedBuffer.FirstClusterCharLength;
			}
			num += num4;
			if (i < textRuns.Count - 1 && num4 == textRun.Length && textRuns[i + 1] is TextEndOfLine textEndOfLine)
			{
				num += textEndOfLine.Length;
			}
			return num;
		}
		return num;
	}

	/// <summary>
	/// Creates an empty text line.
	/// </summary>
	/// <returns>The empty text line.</returns>
	public static TextLineImpl CreateEmptyTextLine(int firstTextSourceIndex, double paragraphWidth, TextParagraphProperties paragraphProperties)
	{
		FlowDirection flowDirection = paragraphProperties.FlowDirection;
		TextRunProperties defaultTextRunProperties = paragraphProperties.DefaultTextRunProperties;
		GlyphTypeface cachedGlyphTypeface = defaultTextRunProperties.CachedGlyphTypeface;
		ushort glyphIndex = cachedGlyphTypeface.CharacterToGlyphMap[s_empty[0]];
		GlyphInfo[] array = new GlyphInfo[1]
		{
			new GlyphInfo(glyphIndex, firstTextSourceIndex, 0.0)
		};
		ShapedBuffer shapedBuffer = new ShapedBuffer(s_empty.AsMemory(), array, cachedGlyphTypeface, defaultTextRunProperties.FontRenderingEmSize, (sbyte)flowDirection);
		TextLineImpl textLineImpl = new TextLineImpl(new TextRun[1]
		{
			new ShapedTextRun(shapedBuffer, defaultTextRunProperties)
		}, firstTextSourceIndex, 0, paragraphWidth, paragraphProperties, flowDirection);
		textLineImpl.FinalizeLine();
		return textLineImpl;
	}

	/// <summary>
	/// Performs text wrapping returns a list of text lines.
	/// </summary>
	/// <param name="textRuns"></param>
	/// <param name="canReuseTextRunList">Whether <see cref="T:Avalonia.Media.TextFormatting.TextRun" /> can be reused to store the split runs.</param>
	/// <param name="firstTextSourceIndex">The first text source index.</param>
	/// <param name="paragraphWidth">The paragraph width.</param>
	/// <param name="paragraphProperties">The text paragraph properties.</param>
	/// <param name="resolvedFlowDirection"></param>
	/// <param name="currentLineBreak">The current line break if the line was explicitly broken.</param>
	/// <param name="objectPool">A pool used to get reusable formatting objects.</param>
	/// <returns>The wrapped text line.</returns>
	private static TextLineImpl PerformTextWrapping(List<TextRun> textRuns, bool canReuseTextRunList, int firstTextSourceIndex, double paragraphWidth, TextParagraphProperties paragraphProperties, FlowDirection resolvedFlowDirection, TextLineBreak? currentLineBreak, FormattingObjectPool objectPool)
	{
		if (textRuns.Count == 0)
		{
			return CreateEmptyTextLine(firstTextSourceIndex, paragraphWidth, paragraphProperties);
		}
		int num = MeasureLength(textRuns, paragraphWidth);
		ReadOnlyMemory<char> text;
		if (num == 0)
		{
			if (paragraphProperties.TextWrapping == TextWrapping.NoWrap)
			{
				for (int i = 0; i < textRuns.Count; i++)
				{
					num += textRuns[i].Length;
				}
			}
			else
			{
				TextRun textRun = textRuns[0];
				if (textRun is ShapedTextRun)
				{
					text = textRun.Text;
					GraphemeEnumerator graphemeEnumerator = new GraphemeEnumerator(text.Span);
					num = ((!graphemeEnumerator.MoveNext(out var grapheme)) ? 1 : grapheme.Length);
				}
				else
				{
					num = textRun.Length;
				}
			}
		}
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		TextWrapping textWrapping = paragraphProperties.TextWrapping;
		int count = textRuns.Count;
		for (int j = 0; j < count; j++)
		{
			bool flag = false;
			TextRun textRun2 = textRuns[j];
			int length = textRun2.Length;
			if (textRun2 is ShapedTextRun)
			{
				text = textRun2.Text;
				LineBreakEnumerator lineBreakEnumerator = new LineBreakEnumerator(text.Span);
				LineBreak lineBreak;
				while (lineBreakEnumerator.MoveNext(out lineBreak))
				{
					if (lineBreak.Required && num2 + lineBreak.PositionMeasure <= num)
					{
						flag = true;
						num4 = num2 + lineBreak.PositionWrap;
						break;
					}
					if (num2 + lineBreak.PositionMeasure > num)
					{
						if (textWrapping == TextWrapping.WrapWithOverflow)
						{
							if (num3 > 0)
							{
								num4 = num3;
								flag = true;
								break;
							}
							if (j < count - 1)
							{
								if (lineBreak.PositionWrap != length)
								{
									flag = true;
									num4 = num2 + lineBreak.PositionWrap;
									break;
								}
								while (lineBreakEnumerator.MoveNext(out lineBreak))
								{
									num4 += lineBreak.PositionWrap;
									if (lineBreak.PositionWrap != length)
									{
										break;
									}
									j++;
									if (j >= count)
									{
										break;
									}
									textRun2 = textRuns[j];
									length = textRun2.Length;
									text = textRun2.Text;
									lineBreakEnumerator = new LineBreakEnumerator(text.Span);
								}
							}
							else
							{
								num4 = num2 + lineBreak.PositionWrap;
							}
							if (num4 == 0 && num > 0)
							{
								num4 = num;
							}
							flag = true;
						}
						else
						{
							num4 = ((num3 == 0) ? num : num3);
							flag = true;
						}
						break;
					}
					if (lineBreak.PositionMeasure != lineBreak.PositionWrap || lineBreak.PositionWrap != length)
					{
						num3 = num2 + lineBreak.PositionWrap;
					}
				}
			}
			if (!flag)
			{
				num2 += length;
				continue;
			}
			num = num4;
			break;
		}
		var (rentedList3, rentedList4) = SplitTextRuns(textRuns, num, objectPool, out var firstLength);
		try
		{
			TextLineBreak lineBreak2;
			if (rentedList4 != null && rentedList4.Count > 0)
			{
				int count2 = rentedList4.Count;
				List<TextRun> list;
				if (canReuseTextRunList)
				{
					list = textRuns;
					list.Clear();
					if (list.Capacity < count2)
					{
						list.Capacity = count2;
					}
				}
				else
				{
					list = new List<TextRun>(count2);
				}
				for (int k = 0; k < count2; k++)
				{
					list.Add(rentedList4[k]);
				}
				lineBreak2 = new WrappingTextLineBreak(null, resolvedFlowDirection, list);
			}
			else
			{
				TextEndOfLine textEndOfLine = currentLineBreak?.TextEndOfLine;
				lineBreak2 = ((textEndOfLine == null) ? null : new TextLineBreak(textEndOfLine, resolvedFlowDirection));
			}
			if (rentedList3 == null)
			{
				return CreateEmptyTextLine(firstTextSourceIndex, paragraphWidth, paragraphProperties);
			}
			if (rentedList4 != null && rentedList4.Count > 0)
			{
				ResetTrailingWhitespaceBidiLevels(rentedList3, paragraphProperties.FlowDirection, objectPool);
			}
			int count3 = rentedList3.Count;
			TextRun[] array = new TextRun[count3];
			for (int l = 0; l < count3; l++)
			{
				array[l] = rentedList3[l];
			}
			TextLineImpl textLineImpl = new TextLineImpl(array, firstTextSourceIndex, firstLength, paragraphWidth, paragraphProperties, resolvedFlowDirection, lineBreak2);
			textLineImpl.FinalizeLine();
			return textLineImpl;
		}
		finally
		{
			objectPool.TextRunLists.Return(ref rentedList3);
			objectPool.TextRunLists.Return(ref rentedList4);
		}
	}

	private static void ResetTrailingWhitespaceBidiLevels(FormattingObjectPool.RentedList<TextRun> lineTextRuns, FlowDirection paragraphFlowDirection, FormattingObjectPool objectPool)
	{
		if (lineTextRuns.Count == 0)
		{
			return;
		}
		int index = lineTextRuns.Count - 1;
		if (!(lineTextRuns[index] is ShapedTextRun shapedTextRun))
		{
			return;
		}
		sbyte b = (sbyte)paragraphFlowDirection;
		if (shapedTextRun.BidiLevel == b)
		{
			return;
		}
		int trailingWhitespaceLength = shapedTextRun.GlyphRun.Metrics.TrailingWhitespaceLength;
		if (trailingWhitespaceLength == 0)
		{
			return;
		}
		int length = shapedTextRun.Length - trailingWhitespaceLength;
		var (rentedList3, rentedList4) = SplitTextRuns(new _003C_003Ez__ReadOnlySingleElementList<TextRun>(shapedTextRun), length, objectPool);
		try
		{
			if (rentedList4 == null)
			{
				return;
			}
			for (int i = 0; i < rentedList4.Count; i++)
			{
				if (rentedList4[i] is ShapedTextRun shapedTextRun2)
				{
					ShapedBuffer shapedBuffer = shapedTextRun2.ShapedBuffer.WithBidiLevel(b);
					if (shapedBuffer != shapedTextRun2.ShapedBuffer)
					{
						rentedList4[i] = new ShapedTextRun(shapedBuffer, shapedTextRun2.Properties);
						shapedTextRun2.Dispose();
					}
				}
			}
			lineTextRuns.RemoveAt(index);
			if (rentedList3 != null)
			{
				lineTextRuns.AddRange(rentedList3);
			}
			lineTextRuns.AddRange(rentedList4);
		}
		finally
		{
			objectPool.TextRunLists.Return(ref rentedList3);
			objectPool.TextRunLists.Return(ref rentedList4);
		}
	}
}
