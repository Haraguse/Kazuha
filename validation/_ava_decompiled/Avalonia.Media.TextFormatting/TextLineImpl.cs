using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting;

internal class TextLineImpl : TextLine
{
	internal IReadOnlyList<IndexedTextRun>? _indexedTextRuns;

	private readonly TextRun[] _textRuns;

	private readonly double _paragraphWidth;

	private readonly TextParagraphProperties _paragraphProperties;

	private TextLineMetrics _textLineMetrics;

	private TextLineBreak? _textLineBreak;

	private readonly FlowDirection _resolvedFlowDirection;

	private Rect _inkBounds;

	private Rect _bounds;

	internal static Comparer<TextBounds> TextBoundsComparer { get; } = Comparer<TextBounds>.Create((TextBounds x, TextBounds y) => x.Rectangle.Left.CompareTo(y.Rectangle.Left));

	/// <inheritdoc />
	public override IReadOnlyList<TextRun> TextRuns => _textRuns;

	/// <inheritdoc />
	public override int FirstTextSourceIndex { get; }

	/// <inheritdoc />
	public override int Length { get; }

	/// <inheritdoc />
	public override TextLineBreak? TextLineBreak => _textLineBreak;

	/// <inheritdoc />
	public override bool HasCollapsed { get; }

	/// <inheritdoc />
	public override bool HasOverflowed => _textLineMetrics.HasOverflowed;

	/// <inheritdoc />
	public override double Baseline => _textLineMetrics.TextBaseline;

	/// <inheritdoc />
	public override double Extent => _textLineMetrics.Extent;

	/// <inheritdoc />
	public override double Height => _textLineMetrics.Height;

	/// <inheritdoc />
	public override int NewLineLength => _textLineMetrics.NewlineLength;

	/// <inheritdoc />
	public override double OverhangAfter => _textLineMetrics.OverhangAfter;

	/// <inheritdoc />
	public override double OverhangLeading => _textLineMetrics.OverhangLeading;

	/// <inheritdoc />
	public override double OverhangTrailing => _textLineMetrics.OverhangTrailing;

	/// <inheritdoc />
	public override int TrailingWhitespaceLength => _textLineMetrics.TrailingWhitespaceLength;

	/// <inheritdoc />
	public override double Start => _textLineMetrics.Start;

	/// <inheritdoc />
	public override double Width => _textLineMetrics.Width;

	/// <inheritdoc />
	public override double WidthIncludingTrailingWhitespace => _textLineMetrics.WidthIncludingTrailingWhitespace;

	/// <summary>
	/// Get the logical text bounds.
	/// </summary>
	internal Rect Bounds => _bounds;

	/// <summary>
	/// Get the bounding box that is covered with black pixels.
	/// </summary>
	internal Rect InkBounds => _inkBounds;

	public TextLineImpl(TextRun[] textRuns, int firstTextSourceIndex, int length, double paragraphWidth, TextParagraphProperties paragraphProperties, FlowDirection resolvedFlowDirection = FlowDirection.LeftToRight, TextLineBreak? lineBreak = null, bool hasCollapsed = false)
	{
		FirstTextSourceIndex = firstTextSourceIndex;
		Length = length;
		_textLineBreak = lineBreak;
		HasCollapsed = hasCollapsed;
		_textRuns = textRuns;
		_paragraphWidth = paragraphWidth;
		_paragraphProperties = paragraphProperties;
		_resolvedFlowDirection = resolvedFlowDirection;
	}

	/// <inheritdoc />
	public override void Draw(DrawingContext drawingContext, Point lineOrigin)
	{
		var (num3, num4) = lineOrigin + new Point(Start, 0.0);
		TextRun[] textRuns = _textRuns;
		for (int i = 0; i < textRuns.Length; i++)
		{
			if (textRuns[i] is DrawableTextRun drawableTextRun)
			{
				double baselineOffset = GetBaselineOffset(this, drawableTextRun);
				drawableTextRun.Draw(drawingContext, new Point(num3, num4 + baselineOffset));
				num3 += drawableTextRun.Size.Width;
			}
		}
	}

	public static double GetBaselineOffset(TextLine textLine, DrawableTextRun textRun)
	{
		double baseline = textRun.Baseline;
		BaselineAlignment? baselineAlignment = textRun.Properties?.BaselineAlignment;
		double num = 0.0 - baseline;
		switch (baselineAlignment)
		{
		case BaselineAlignment.Baseline:
			return num + textLine.Baseline;
		case BaselineAlignment.Top:
		case BaselineAlignment.TextTop:
			return num + (textLine.Height - textLine.Extent + textRun.Size.Height / 2.0);
		case BaselineAlignment.Center:
			return num + (textLine.Height / 2.0 + baseline - textRun.Size.Height / 2.0);
		case BaselineAlignment.Bottom:
		case BaselineAlignment.TextBottom:
		case BaselineAlignment.Subscript:
			return num + (textLine.Height - textRun.Size.Height + baseline);
		case BaselineAlignment.Superscript:
			return num + baseline;
		default:
			throw new ArgumentOutOfRangeException("baselineAlignment", baselineAlignment, null);
		}
	}

	/// <inheritdoc />
	public override TextLine Collapse(params TextCollapsingProperties?[] collapsingPropertiesList)
	{
		if (collapsingPropertiesList.Length == 0)
		{
			return this;
		}
		TextCollapsingProperties textCollapsingProperties = collapsingPropertiesList[0];
		if (textCollapsingProperties == null)
		{
			return this;
		}
		TextRun[] array = textCollapsingProperties.Collapse(this);
		if (array == null)
		{
			return this;
		}
		TextLineImpl textLineImpl = new TextLineImpl(array, FirstTextSourceIndex, Length, _paragraphWidth, _paragraphProperties, _resolvedFlowDirection, TextLineBreak, hasCollapsed: true);
		if (array.Length != 0)
		{
			textLineImpl.FinalizeLine();
		}
		return textLineImpl;
	}

	/// <inheritdoc />
	public override void Justify(JustificationProperties justificationProperties)
	{
		justificationProperties.Justify(this);
		_textLineMetrics = CreateLineMetrics();
	}

	/// <summary>
	/// Replaces the run at <paramref name="index" /> in <see cref="F:Avalonia.Media.TextFormatting.TextLineImpl._textRuns" /> and re-points the
	/// matching bidi-reordered <see cref="T:Avalonia.Media.TextFormatting.IndexedTextRun" /> at the new run, so draw and hit-test
	/// paths that resolve runs through <see cref="F:Avalonia.Media.TextFormatting.TextLineImpl._indexedTextRuns" /> observe the replacement.
	/// Used by justification to swap in a copy-on-write run without mutating shared glyph storage.
	/// </summary>
	internal void ReplaceTextRun(int index, TextRun textRun)
	{
		TextRun textRun2 = _textRuns[index];
		if (textRun2 == textRun)
		{
			return;
		}
		_textRuns[index] = textRun;
		if (_indexedTextRuns == null)
		{
			return;
		}
		for (int i = 0; i < _indexedTextRuns.Count; i++)
		{
			if (_indexedTextRuns[i].TextRun == textRun2)
			{
				_indexedTextRuns[i].TextRun = textRun;
				break;
			}
		}
	}

	/// <inheritdoc />
	public override CharacterHit GetCharacterHitFromDistance(double distance)
	{
		if (_textRuns.Length == 0)
		{
			return new CharacterHit(FirstTextSourceIndex);
		}
		distance -= Start;
		int num = _textRuns.Length - 1;
		if (_textRuns[num] is TextEndOfLine)
		{
			num--;
		}
		if (num < 0)
		{
			return new CharacterHit(FirstTextSourceIndex);
		}
		if (distance <= 0.0)
		{
			return GetRunCharacterHit(_textRuns[0], GetRunTextSourcePosition(0), 0.0);
		}
		if (distance >= WidthIncludingTrailingWhitespace)
		{
			return GetRunCharacterHit(_textRuns[num], GetRunTextSourcePosition(num), distance);
		}
		CharacterHit result = default(CharacterHit);
		double num2 = 0.0;
		int num3 = 0;
		for (int i = 0; i <= num; i++)
		{
			TextRun textRun = _textRuns[i];
			num3 = i;
			if (textRun is ShapedTextRun shapedTextRun && !shapedTextRun.ShapedBuffer.IsLeftToRight)
			{
				int j;
				for (j = i; j + 1 <= _textRuns.Length - 1; j++)
				{
					if (!(_textRuns[++j] is ShapedTextRun shapedTextRun2))
					{
						break;
					}
					if (shapedTextRun2.ShapedBuffer.IsLeftToRight)
					{
						break;
					}
				}
				int num4 = i;
				while (i <= j && num4 <= _textRuns.Length - 1)
				{
					textRun = _textRuns[num4];
					num3 = num4;
					if (textRun is ShapedTextRun)
					{
						ShapedTextRun shapedTextRun3 = (ShapedTextRun)textRun;
						if (!(num2 + shapedTextRun3.Size.Width <= distance))
						{
							return GetRunCharacterHit(textRun, GetRunTextSourcePosition(num4), distance - num2);
						}
						num2 += shapedTextRun3.Size.Width;
					}
					num4++;
				}
			}
			result = GetRunCharacterHit(textRun, GetRunTextSourcePosition(num3), distance - num2);
			if (textRun is DrawableTextRun drawableTextRun)
			{
				if (i >= _textRuns.Length - 1 || !(num2 + drawableTextRun.Size.Width < distance))
				{
					break;
				}
				num2 += drawableTextRun.Size.Width;
			}
		}
		return result;
	}

	private static CharacterHit GetRunCharacterHit(TextRun run, int currentPosition, double distance)
	{
		CharacterHit result;
		if (!(run is ShapedTextRun shapedTextRun))
		{
			result = ((!(run is DrawableTextRun drawableTextRun)) ? new CharacterHit(currentPosition, run.Length) : ((!(distance < drawableTextRun.Size.Width / 2.0)) ? new CharacterHit(currentPosition, run.Length) : new CharacterHit(currentPosition)));
		}
		else
		{
			result = shapedTextRun.GlyphRun.GetCharacterHitFromDistance(distance, out var _);
			int num = Math.Max(0, currentPosition - shapedTextRun.GlyphRun.Metrics.FirstCluster);
			result = new CharacterHit(num + result.FirstCharacterIndex, result.TrailingLength);
		}
		return result;
	}

	/// <inheritdoc />
	public override double GetDistanceFromCharacterHit(CharacterHit characterHit)
	{
		if (_indexedTextRuns == null || _indexedTextRuns.Count == 0)
		{
			return Start;
		}
		int firstTextSourceIndex = Math.Min(characterHit.FirstCharacterIndex + characterHit.TrailingLength, FirstTextSourceIndex + Length);
		int currentPosition = FirstTextSourceIndex;
		TextRun textRun = null;
		IndexedTextRun indexedTextRun = FindIndexedRun(out var index);
		while (currentPosition < FirstTextSourceIndex + Length)
		{
			textRun = indexedTextRun.TextRun;
			if (textRun == null || indexedTextRun.TextSourceCharacterIndex + textRun.Length > characterHit.FirstCharacterIndex || currentPosition + textRun.Length >= FirstTextSourceIndex + Length)
			{
				break;
			}
			currentPosition += textRun.Length;
			indexedTextRun = FindIndexedRun(out index);
		}
		if (textRun == null)
		{
			return Start;
		}
		double directionalWidth = 0.0;
		int runIndex = indexedTextRun.RunIndex;
		FlowDirection flowDirection = GetDirection(textRun, _resolvedFlowDirection);
		double num = Start + GetPreceedingDistance(indexedTextRun.RunIndex);
		if (textRun is DrawableTextRun { Size: var size })
		{
			directionalWidth = size.Width;
		}
		int lastDirectionalRunIndex = GetLastDirectionalRunIndex(index, flowDirection, ref directionalWidth);
		int newPosition;
		int coveredLength;
		if (flowDirection == FlowDirection.RightToLeft)
		{
			return GetTextRunBoundsRightToLeft(runIndex, lastDirectionalRunIndex, num + directionalWidth, firstTextSourceIndex, currentPosition, 1, out newPosition, out coveredLength).Rectangle.Right;
		}
		return GetTextBoundsLeftToRight(runIndex, lastDirectionalRunIndex, num, firstTextSourceIndex, currentPosition, 1, out coveredLength, out newPosition).Rectangle.Left;
		IndexedTextRun FindIndexedRun(out int reference)
		{
			reference = 0;
			IndexedTextRun indexedTextRun2 = _indexedTextRuns[reference];
			while (indexedTextRun2.TextSourceCharacterIndex != currentPosition && reference + 1 != _indexedTextRuns.Count)
			{
				reference++;
				indexedTextRun2 = _indexedTextRuns[reference];
			}
			return indexedTextRun2;
		}
		static FlowDirection GetDirection(TextRun textRun2, FlowDirection currentDirection)
		{
			if (textRun2 is ShapedTextRun shapedTextRun)
			{
				if (!shapedTextRun.ShapedBuffer.IsLeftToRight)
				{
					return FlowDirection.RightToLeft;
				}
				return FlowDirection.LeftToRight;
			}
			return currentDirection;
		}
		double GetPreceedingDistance(int firstIndex)
		{
			double num2 = 0.0;
			for (int i = 0; i < firstIndex; i++)
			{
				if (_textRuns[i] is DrawableTextRun drawableTextRun2)
				{
					num2 += drawableTextRun2.Size.Width;
				}
			}
			return num2;
		}
	}

	/// <inheritdoc />
	public override CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
	{
		if (_textRuns.Length == 0 || _indexedTextRuns == null)
		{
			return default(CharacterHit);
		}
		CharacterHit characterHit2 = characterHit;
		int num = characterHit.FirstCharacterIndex + characterHit.TrailingLength;
		TextRun runAtCharacterIndex = GetRunAtCharacterIndex(num, LogicalDirection.Forward, out var textPosition);
		CharacterHit result = characterHit;
		if (!(runAtCharacterIndex is ShapedTextRun shapedTextRun))
		{
			if (runAtCharacterIndex != null)
			{
				result = new CharacterHit(textPosition + runAtCharacterIndex.Length);
			}
		}
		else
		{
			int num2 = Math.Max(0, textPosition - shapedTextRun.GlyphRun.Metrics.FirstCluster);
			if (characterHit.FirstCharacterIndex < textPosition && num2 > 0)
			{
				CharacterHit characterHit3 = shapedTextRun.GlyphRun.FindNearestCharacterHit(shapedTextRun.GlyphRun.Metrics.FirstCluster, out var _);
				result = new CharacterHit(characterHit3.FirstCharacterIndex + num2, characterHit3.TrailingLength);
			}
			else
			{
				if (num2 > 0)
				{
					characterHit2 = new CharacterHit(Math.Max(0, characterHit.FirstCharacterIndex - num2), characterHit.TrailingLength);
				}
				result = shapedTextRun.GlyphRun.GetNextCaretCharacterHit(characterHit2);
				if (num2 > 0)
				{
					result = new CharacterHit(result.FirstCharacterIndex + num2, result.TrailingLength);
				}
			}
		}
		if (num == result.FirstCharacterIndex + result.TrailingLength)
		{
			return characterHit;
		}
		return result;
	}

	/// <inheritdoc />
	public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
	{
		return GetPreviousCharacterHit(characterHit, isBackspaceDelete: false);
	}

	/// <inheritdoc />
	public override CharacterHit GetBackspaceCaretCharacterHit(CharacterHit characterHit)
	{
		return GetPreviousCharacterHit(characterHit, isBackspaceDelete: true);
	}

	private static FlowDirection GetRunDirection(TextRun? textRun, FlowDirection currentDirection)
	{
		if (textRun is ShapedTextRun shapedTextRun)
		{
			if (!shapedTextRun.ShapedBuffer.IsLeftToRight)
			{
				return FlowDirection.RightToLeft;
			}
			return FlowDirection.LeftToRight;
		}
		return currentDirection;
	}

	/// <summary>
	/// Gets the text source character index for the run at the given visual position in <see cref="F:Avalonia.Media.TextFormatting.TextLineImpl._textRuns" />.
	/// </summary>
	/// <param name="visualRunIndex">The index of the run in the visual-order <see cref="F:Avalonia.Media.TextFormatting.TextLineImpl._textRuns" /> array.</param>
	/// <returns>The text source character index where the run starts.</returns>
	private int GetRunTextSourcePosition(int visualRunIndex)
	{
		if (_indexedTextRuns != null)
		{
			for (int i = 0; i < _indexedTextRuns.Count; i++)
			{
				if (_indexedTextRuns[i].RunIndex == visualRunIndex)
				{
					return _indexedTextRuns[i].TextSourceCharacterIndex;
				}
			}
		}
		int num = FirstTextSourceIndex;
		for (int j = 0; j < visualRunIndex && j < _textRuns.Length; j++)
		{
			num += _textRuns[j].Length;
		}
		return num;
	}

	/// <summary>
	/// Get the last consecutive visual run index that shares the same direction as the current direction.
	/// </summary>
	/// <param name="indexedRunIndex">The current logical run's index.</param>
	/// <param name="flowDirection">The current flow direction.</param>
	/// <param name="directionalWidth">The current directional width.</param>
	/// <returns>
	/// The last consecutive visual run index that shares the same direction as the current direction.
	/// </returns>
	private int GetLastDirectionalRunIndex(int indexedRunIndex, FlowDirection flowDirection, ref double directionalWidth)
	{
		if (_indexedTextRuns == null)
		{
			return -1;
		}
		int runIndex = _indexedTextRuns[indexedRunIndex].RunIndex;
		while (indexedRunIndex + 1 < _indexedTextRuns.Count)
		{
			IndexedTextRun indexedTextRun = _indexedTextRuns[++indexedRunIndex];
			if (indexedTextRun.RunIndex != runIndex + 1)
			{
				break;
			}
			TextRun textRun = indexedTextRun.TextRun;
			if (textRun == null || GetRunDirection(textRun, flowDirection) != flowDirection)
			{
				break;
			}
			if (textRun is DrawableTextRun drawableTextRun)
			{
				directionalWidth += drawableTextRun.Size.Width;
			}
			runIndex = indexedTextRun.RunIndex;
		}
		return runIndex;
	}

	public override IReadOnlyList<TextBounds> GetTextBounds(int firstTextSourceIndex, int textLength)
	{
		if (textLength == 0)
		{
			throw new ArgumentOutOfRangeException("textLength", textLength, "textLength ('0') must be a non-zero value. ");
		}
		if (_indexedTextRuns == null || _indexedTextRuns.Count == 0)
		{
			return Array.Empty<TextBounds>();
		}
		int currentPosition = FirstTextSourceIndex;
		int num = textLength;
		if (firstTextSourceIndex + textLength < FirstTextSourceIndex)
		{
			FlowDirection runDirection = GetRunDirection(_indexedTextRuns[0].TextRun, _resolvedFlowDirection);
			return new _003C_003Ez__ReadOnlySingleElementList<TextBounds>(new TextBounds(new Rect(0.0, 0.0, 0.0, Height), runDirection, new List<TextRunBounds>()));
		}
		if (firstTextSourceIndex >= FirstTextSourceIndex + Length)
		{
			FlowDirection runDirection2 = GetRunDirection(_indexedTextRuns[_indexedTextRuns.Count - 1].TextRun, _resolvedFlowDirection);
			return new _003C_003Ez__ReadOnlySingleElementList<TextBounds>(new TextBounds(new Rect(WidthIncludingTrailingWhitespace, 0.0, 0.0, Height), runDirection2, new List<TextRunBounds>()));
		}
		List<TextBounds> list = new List<TextBounds>();
		TextBounds textBounds = null;
		while (num > 0 && currentPosition < FirstTextSourceIndex + Length)
		{
			IndexedTextRun indexedTextRun = FindIndexedRun(out var index);
			if (indexedTextRun == null)
			{
				break;
			}
			TextRun textRun = indexedTextRun.TextRun;
			if (textRun == null)
			{
				break;
			}
			FlowDirection runDirection3 = GetRunDirection(textRun, _resolvedFlowDirection);
			if (indexedTextRun.TextSourceCharacterIndex + textRun.Length <= firstTextSourceIndex)
			{
				currentPosition += textRun.Length;
				continue;
			}
			double num2 = Start + GetPreceedingDistance(indexedTextRun.RunIndex);
			double directionalWidth = 0.0;
			if (textRun is DrawableTextRun { Size: var size })
			{
				directionalWidth = size.Width;
			}
			int runIndex = indexedTextRun.RunIndex;
			int lastDirectionalRunIndex = GetLastDirectionalRunIndex(index, runDirection3, ref directionalWidth);
			TextBounds textBounds2 = ((runDirection3 != FlowDirection.RightToLeft) ? GetTextBoundsLeftToRight(runIndex, lastDirectionalRunIndex, num2, firstTextSourceIndex, currentPosition, num, out var coveredLength, out currentPosition) : GetTextRunBoundsRightToLeft(runIndex, lastDirectionalRunIndex, num2 + directionalWidth, firstTextSourceIndex, currentPosition, num, out coveredLength, out currentPosition));
			if (textBounds != null && TryMergeWithLastBounds(textBounds2, textBounds))
			{
				textBounds2 = textBounds;
				list[list.Count - 1] = textBounds2;
			}
			else
			{
				list.Add(textBounds2);
			}
			textBounds = textBounds2;
			if (coveredLength <= 0)
			{
				throw new InvalidOperationException("Covered length must be greater than zero.");
			}
			num -= coveredLength;
		}
		list.Sort(TextBoundsComparer);
		return list;
		IndexedTextRun FindIndexedRun(out int reference)
		{
			reference = 0;
			IndexedTextRun indexedTextRun2 = _indexedTextRuns[reference];
			while (indexedTextRun2.TextSourceCharacterIndex != currentPosition && reference + 1 != _indexedTextRuns.Count)
			{
				reference++;
				indexedTextRun2 = _indexedTextRuns[reference];
			}
			return indexedTextRun2;
		}
		double GetPreceedingDistance(int firstIndex)
		{
			double num3 = 0.0;
			for (int i = 0; i < firstIndex; i++)
			{
				if (_textRuns[i] is DrawableTextRun drawableTextRun2)
				{
					num3 += drawableTextRun2.Size.Width;
				}
			}
			return num3;
		}
		static bool TryMergeWithLastBounds(TextBounds currentBounds, TextBounds lastBounds)
		{
			if (currentBounds.FlowDirection != lastBounds.FlowDirection)
			{
				return false;
			}
			if (currentBounds.Rectangle.Left == lastBounds.Rectangle.Right)
			{
				foreach (TextRunBounds textRunBound in currentBounds.TextRunBounds)
				{
					lastBounds.TextRunBounds.Add(textRunBound);
				}
				lastBounds.Rectangle = lastBounds.Rectangle.Union(currentBounds.Rectangle);
				return true;
			}
			if (currentBounds.Rectangle.Right == lastBounds.Rectangle.Left)
			{
				for (int i = 0; i < currentBounds.TextRunBounds.Count; i++)
				{
					lastBounds.TextRunBounds.Insert(i, currentBounds.TextRunBounds[i]);
				}
				lastBounds.Rectangle = lastBounds.Rectangle.Union(currentBounds.Rectangle);
				return true;
			}
			return false;
		}
	}

	private CharacterHit GetPreviousCharacterHit(CharacterHit characterHit, bool isBackspaceDelete)
	{
		if (_textRuns.Length == 0 || _indexedTextRuns == null)
		{
			return default(CharacterHit);
		}
		if (characterHit.TrailingLength > 0 && characterHit.FirstCharacterIndex <= FirstTextSourceIndex)
		{
			return new CharacterHit(FirstTextSourceIndex);
		}
		int num = characterHit.FirstCharacterIndex + characterHit.TrailingLength;
		if (num <= FirstTextSourceIndex)
		{
			return new CharacterHit(FirstTextSourceIndex);
		}
		TextRun runAtCharacterIndex = GetRunAtCharacterIndex(num, LogicalDirection.Backward, out var textPosition);
		CharacterHit result = characterHit;
		if (!(runAtCharacterIndex is ShapedTextRun shapedTextRun))
		{
			if (runAtCharacterIndex != null)
			{
				result = new CharacterHit(textPosition);
			}
		}
		else
		{
			int num2 = Math.Max(0, num - textPosition);
			int firstCluster = shapedTextRun.GlyphRun.Metrics.FirstCluster;
			int num3 = textPosition - firstCluster;
			if (isBackspaceDelete)
			{
				int num4 = 0;
				while (true)
				{
					Codepoint codepoint = Codepoint.ReadAt(shapedTextRun.GlyphRun.Characters.Span, num4, out var count);
					if (!(codepoint != Codepoint.ReplacementCodepoint))
					{
						break;
					}
					if (codepoint.Value == 13 && Codepoint.ReadAt(shapedTextRun.GlyphRun.Characters.Span, num4 + count, out var count2).Value == 10)
					{
						count += count2;
					}
					if (num4 + count >= num2)
					{
						break;
					}
					num4 += count;
				}
				result = new CharacterHit(num - num2 + num4);
			}
			else
			{
				result = shapedTextRun.GlyphRun.GetPreviousCaretCharacterHit(new CharacterHit(firstCluster + num2));
				if (num3 > 0)
				{
					result = new CharacterHit(num3 + result.FirstCharacterIndex, result.TrailingLength);
				}
			}
		}
		if (num == result.FirstCharacterIndex + result.TrailingLength)
		{
			return characterHit;
		}
		return result;
	}

	private TextBounds GetTextRunBoundsRightToLeft(int firstRunIndex, int lastRunIndex, double endX, int firstTextSourceIndex, int currentPosition, int remainingLength, out int coveredLength, out int newPosition)
	{
		coveredLength = 0;
		List<TextRunBounds> list = new List<TextRunBounds>();
		double num = endX;
		for (int num2 = lastRunIndex; num2 >= firstRunIndex; num2--)
		{
			TextRun textRun = _textRuns[num2];
			if (textRun is ShapedTextRun currentRun)
			{
				TextRunBounds runBounds = GetRunBounds(currentRun, num, firstTextSourceIndex, remainingLength, currentPosition);
				if (runBounds.TextSourceCharacterIndex < FirstTextSourceIndex + Length)
				{
					list.Insert(0, runBounds);
				}
				if (num2 == lastRunIndex)
				{
					endX = runBounds.Rectangle.Right;
					num = endX;
				}
				num -= runBounds.Rectangle.Width;
				currentPosition = runBounds.TextSourceCharacterIndex + runBounds.Length;
				coveredLength += runBounds.Length;
				remainingLength -= runBounds.Length;
			}
			else
			{
				if (currentPosition < FirstTextSourceIndex + Length)
				{
					if (textRun is DrawableTextRun drawableTextRun)
					{
						num -= drawableTextRun.Size.Width;
						TextRunBounds item = new TextRunBounds(new Rect(num, 0.0, drawableTextRun.Size.Width, Height), currentPosition, textRun.Length, textRun);
						list.Insert(0, item);
					}
					else
					{
						TextRunBounds item2 = new TextRunBounds(new Rect(endX, 0.0, 0.0, Height), currentPosition, textRun.Length, textRun);
						list.Add(item2);
					}
				}
				currentPosition += textRun.Length;
				coveredLength += textRun.Length;
				remainingLength -= textRun.Length;
			}
			if (remainingLength <= 0)
			{
				break;
			}
		}
		newPosition = currentPosition;
		double width = endX - num;
		return new TextBounds(new Rect(num, 0.0, width, Height), FlowDirection.RightToLeft, list);
	}

	private TextBounds GetTextBoundsLeftToRight(int firstRunIndex, int lastRunIndex, double startX, int firstTextSourceIndex, int currentPosition, int remainingLength, out int coveredLength, out int newPosition)
	{
		coveredLength = 0;
		List<TextRunBounds> list = new List<TextRunBounds>(1);
		double num = startX;
		for (int i = firstRunIndex; i <= lastRunIndex; i++)
		{
			TextRun textRun = _textRuns[i];
			if (textRun is ShapedTextRun currentRun)
			{
				TextRunBounds runBounds = GetRunBounds(currentRun, num, firstTextSourceIndex, remainingLength, currentPosition);
				if (runBounds.TextSourceCharacterIndex < FirstTextSourceIndex + Length)
				{
					list.Add(runBounds);
				}
				currentPosition = runBounds.TextSourceCharacterIndex + runBounds.Length;
				if (i == firstRunIndex)
				{
					startX = runBounds.Rectangle.Left;
				}
				num = runBounds.Rectangle.Right;
				coveredLength += runBounds.Length;
				remainingLength -= runBounds.Length;
			}
			else
			{
				if (currentPosition < FirstTextSourceIndex + Length)
				{
					if (textRun is DrawableTextRun drawableTextRun)
					{
						TextRunBounds item = new TextRunBounds(new Rect(num, 0.0, drawableTextRun.Size.Width, Height), currentPosition, textRun.Length, textRun);
						list.Add(item);
						num += drawableTextRun.Size.Width;
					}
					else
					{
						TextRunBounds item2 = new TextRunBounds(new Rect(num, 0.0, 0.0, Height), currentPosition, textRun.Length, textRun);
						list.Add(item2);
					}
				}
				currentPosition += textRun.Length;
				coveredLength += textRun.Length;
				remainingLength -= textRun.Length;
			}
			if (remainingLength <= 0)
			{
				break;
			}
		}
		newPosition = currentPosition;
		double width = num - startX;
		return new TextBounds(new Rect(startX, 0.0, width, Height), FlowDirection.LeftToRight, list);
	}

	private TextRunBounds GetRunBounds(ShapedTextRun currentRun, double currentX, int firstTextSourceIndex, int remainingLength, int currentPosition)
	{
		bool num = currentRun.BidiLevel % 2 == 0;
		double num2 = currentX;
		double num3 = currentX;
		int num4 = Math.Max(0, firstTextSourceIndex - currentPosition);
		int firstCluster = currentRun.GlyphRun.Metrics.FirstCluster;
		int num5 = firstCluster + num4;
		int num6 = num5 + remainingLength;
		int num7 = currentPosition - firstCluster;
		int num8 = 0;
		double width;
		if (num4 > 0)
		{
			CharacterHit characterHit = currentRun.GlyphRun.FindNearestCharacterHit(num5, out width);
			int firstCharacterIndex = characterHit.FirstCharacterIndex;
			int num9 = firstCharacterIndex + characterHit.TrailingLength;
			if (firstCharacterIndex < num5 && num9 > num5)
			{
				num8 = num5 - firstCharacterIndex;
				num5 -= num8;
			}
		}
		double distanceFromCharacterHit = currentRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(num5));
		double distanceFromCharacterHit2 = currentRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(num6));
		if (num)
		{
			num3 = num2 + distanceFromCharacterHit2;
			num2 += distanceFromCharacterHit;
		}
		else
		{
			num2 -= currentRun.Size.Width - distanceFromCharacterHit;
			num3 -= currentRun.Size.Width - distanceFromCharacterHit2;
		}
		CharacterHit characterHit2 = currentRun.GlyphRun.FindNearestCharacterHit(num5, out width);
		int num10 = characterHit2.FirstCharacterIndex;
		if (num10 < num5)
		{
			num10 += characterHit2.TrailingLength;
		}
		CharacterHit characterHit3 = currentRun.GlyphRun.FindNearestCharacterHit(num6, out width);
		int num11 = ((characterHit3.FirstCharacterIndex >= num6) ? characterHit3.FirstCharacterIndex : (characterHit3.FirstCharacterIndex + characterHit3.TrailingLength));
		int length = Math.Max(0, Math.Abs(num10 - num11) - num8);
		if (num3 < num2)
		{
			double num12 = num2;
			num2 = num3;
			num3 = num12;
		}
		double width2 = num3 - num2;
		int firstCharacterIndex2 = num7 + num10 + num8;
		return new TextRunBounds(new Rect(num2, 0.0, width2, Height), firstCharacterIndex2, length, currentRun);
	}

	public override void Dispose()
	{
		TextRun[] textRuns = _textRuns;
		for (int i = 0; i < textRuns.Length; i++)
		{
			if (textRuns[i] is ShapedTextRun shapedTextRun)
			{
				shapedTextRun.Dispose();
			}
		}
	}

	public void FinalizeLine()
	{
		_indexedTextRuns = BidiReorderer.Instance.BidiReorder(_textRuns, _paragraphProperties.FlowDirection, FirstTextSourceIndex);
		_textLineMetrics = CreateLineMetrics();
		if (_textLineBreak == null && _textRuns.Length > 1 && _textRuns[_textRuns.Length - 1] is TextEndOfLine textEndOfLine)
		{
			_textLineBreak = new TextLineBreak(textEndOfLine);
		}
	}

	/// <summary>
	/// Gets the run index of the specified codepoint index.
	/// </summary>
	/// <param name="codepointIndex">The codepoint index.</param>
	/// <param name="direction">The logical direction.</param>
	/// <param name="textPosition">The text position of the found run index.</param>
	/// <returns>The text run index.</returns>
	private TextRun? GetRunAtCharacterIndex(int codepointIndex, LogicalDirection direction, out int textPosition)
	{
		int i = 0;
		textPosition = FirstTextSourceIndex;
		if (_indexedTextRuns == null)
		{
			return null;
		}
		TextRun textRun = null;
		for (; i < _indexedTextRuns.Count; i++)
		{
			IndexedTextRun indexedTextRun = _indexedTextRuns[i];
			textRun = indexedTextRun.TextRun;
			if (!(textRun is ShapedTextRun shapedTextRun))
			{
				if (textRun == null)
				{
					continue;
				}
				if (direction == LogicalDirection.Forward)
				{
					if (textPosition == codepointIndex)
					{
						return textRun;
					}
				}
				else if (textPosition + textRun.Length == codepointIndex)
				{
					return textRun;
				}
				if (i + 1 >= _textRuns.Length)
				{
					return textRun;
				}
				textPosition += textRun.Length;
				continue;
			}
			int firstCluster = shapedTextRun.GlyphRun.Metrics.FirstCluster;
			firstCluster += Math.Max(0, indexedTextRun.TextSourceCharacterIndex - firstCluster);
			if (direction == LogicalDirection.Forward)
			{
				if (codepointIndex >= firstCluster && codepointIndex < firstCluster + textRun.Length)
				{
					return textRun;
				}
			}
			else if (codepointIndex > firstCluster && codepointIndex <= firstCluster + textRun.Length)
			{
				return textRun;
			}
			if (i + 1 >= _textRuns.Length)
			{
				return textRun;
			}
			textPosition += textRun.Length;
		}
		return textRun;
	}

	private TextLineMetrics CreateLineMetrics()
	{
		FontMetrics metrics = _paragraphProperties.DefaultTextRunProperties.CachedGlyphTypeface.Metrics;
		double num = _paragraphProperties.DefaultTextRunProperties.FontRenderingEmSize / (double)(int)metrics.DesignEmHeight;
		double num2 = 0.0;
		int num3 = 0;
		int num4 = 0;
		double num5 = (double)metrics.Ascent * num;
		double num6 = (double)metrics.Descent * num;
		double num7 = (double)metrics.LineGap * num;
		double lineHeight = _paragraphProperties.LineHeight;
		double lineSpacing = _paragraphProperties.LineSpacing;
		TextRun[] textRuns = _textRuns;
		foreach (TextRun textRun in textRuns)
		{
			if (!(textRun is ShapedTextRun { TextMetrics: var textMetrics }))
			{
				if (textRun is DrawableTextRun drawableTextRun)
				{
					if (drawableTextRun.Baseline > 0.0 - num5)
					{
						num5 = 0.0 - drawableTextRun.Baseline;
					}
					double num8 = drawableTextRun.Size.Height - drawableTextRun.Baseline;
					if (num8 > num6)
					{
						num6 = num8;
					}
				}
			}
			else
			{
				if (num5 > textMetrics.Ascent)
				{
					num5 = textMetrics.Ascent;
				}
				if (num6 < textMetrics.Descent)
				{
					num6 = textMetrics.Descent;
				}
				if (num7 < textMetrics.LineGap)
				{
					num7 = textMetrics.LineGap;
				}
			}
		}
		Rect rect = default(Rect);
		textRuns = _textRuns;
		foreach (TextRun textRun2 in textRuns)
		{
			if (!(textRun2 is ShapedTextRun shapedTextRun2))
			{
				if (textRun2 is DrawableTextRun drawableTextRun2)
				{
					double y = 0.0 - num5 - drawableTextRun2.Baseline;
					rect = rect.Union(new Rect(new Point(num2, y), drawableTextRun2.Size));
					num2 += drawableTextRun2.Size.Width;
				}
			}
			else
			{
				GlyphRun glyphRun = shapedTextRun2.GlyphRun;
				double y2 = 0.0 - num5 - shapedTextRun2.Baseline;
				Rect rect2 = glyphRun.InkBounds.Translate(new Vector(num2, y2));
				rect = rect.Union(rect2);
				num2 += shapedTextRun2.Size.Width;
			}
		}
		double num9 = num7 * 0.5;
		double num10 = num6 - num5 + num7;
		double textBaseline = 0.0 - num5 + num9;
		double num11 = num10;
		if (!double.IsNaN(lineHeight) && !MathUtilities.IsZero(lineHeight))
		{
			if (lineHeight <= num10)
			{
				num11 = lineHeight;
				textBaseline = 0.0 - num5;
			}
			else
			{
				num11 = lineHeight;
				double num12 = lineHeight - (num6 - num5);
				textBaseline = 0.0 - num5 + num12 / 2.0;
			}
		}
		num11 += lineSpacing;
		double num13 = num2;
		bool flag = _paragraphProperties.FlowDirection == FlowDirection.RightToLeft;
		for (int j = 0; j < _textRuns.Length; j++)
		{
			int num14 = (flag ? j : (_textRuns.Length - 1 - j));
			TextRun textRun3 = _textRuns[num14];
			if (textRun3 is ShapedTextRun shapedTextRun3)
			{
				GlyphRun glyphRun2 = shapedTextRun3.GlyphRun;
				GlyphRunMetrics metrics2 = glyphRun2.Metrics;
				num4 += metrics2.NewLineLength;
				if (metrics2.TrailingWhitespaceLength == 0)
				{
					break;
				}
				num3 += metrics2.TrailingWhitespaceLength;
				double num15 = glyphRun2.Bounds.Width - metrics2.Width;
				num13 -= num15;
				if (metrics2.TrailingWhitespaceLength != textRun3.Length)
				{
					break;
				}
			}
		}
		double height = rect.Height;
		double overhangAfter = rect.Bottom - num11 + num9;
		double left = rect.Left;
		double overhangTrailing = num2 - rect.Right;
		bool hasOverflowed = MathUtilities.GreaterThan(num13, _paragraphWidth);
		double paragraphOffsetX = GetParagraphOffsetX(num13, num2);
		_inkBounds = rect.Translate(new Vector(paragraphOffsetX, 0.0));
		_bounds = new Rect(paragraphOffsetX, 0.0, num2, num11);
		return new TextLineMetrics
		{
			HasOverflowed = hasOverflowed,
			Height = num11,
			Extent = height,
			NewlineLength = num4,
			Start = paragraphOffsetX,
			TextBaseline = textBaseline,
			TrailingWhitespaceLength = num3,
			Width = num13,
			WidthIncludingTrailingWhitespace = num2,
			OverhangLeading = left,
			OverhangTrailing = overhangTrailing,
			OverhangAfter = overhangAfter
		};
	}

	/// <summary>
	/// Gets the text line offset x.
	/// </summary>
	/// <param name="width">The line width.</param>
	/// <param name="widthIncludingTrailingWhitespace">The paragraph width including whitespace.</param>
	/// <returns>The paragraph offset.</returns>
	private double GetParagraphOffsetX(double width, double widthIncludingTrailingWhitespace)
	{
		if (double.IsPositiveInfinity(_paragraphWidth))
		{
			return 0.0;
		}
		TextAlignment textAlignment = _paragraphProperties.TextAlignment;
		FlowDirection flowDirection = _paragraphProperties.FlowDirection;
		if (textAlignment == TextAlignment.Justify)
		{
			textAlignment = TextAlignment.Start;
		}
		switch (textAlignment)
		{
		case TextAlignment.Start:
			textAlignment = ((flowDirection != FlowDirection.LeftToRight) ? TextAlignment.Right : TextAlignment.Left);
			break;
		case TextAlignment.End:
			textAlignment = ((flowDirection != FlowDirection.RightToLeft) ? TextAlignment.Right : TextAlignment.Left);
			break;
		case TextAlignment.DetectFromContent:
			textAlignment = ((_resolvedFlowDirection != FlowDirection.LeftToRight) ? TextAlignment.Right : TextAlignment.Left);
			break;
		}
		switch (textAlignment)
		{
		case TextAlignment.Center:
		{
			double num = (_paragraphWidth - width) / 2.0;
			if (flowDirection == FlowDirection.RightToLeft)
			{
				num -= widthIncludingTrailingWhitespace - width;
			}
			return Math.Max(0.0, num);
		}
		case TextAlignment.Right:
			return Math.Max(0.0, _paragraphWidth - widthIncludingTrailingWhitespace);
		default:
			return 0.0;
		}
	}
}
