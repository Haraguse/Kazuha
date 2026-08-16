using System;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Represents a multi line text layout.
/// </summary>
public class TextLayout : IDisposable
{
	private class CachedMetrics
	{
		public double Height;

		public double Baseline;

		public double Width;

		public double WidthIncludingTrailingWhitespace;

		public double Extent;

		public double OverhangAfter;

		public double OverhangLeading;

		public double OverhangTrailing;
	}

	private readonly ITextSource _textSource;

	private readonly TextParagraphProperties _paragraphProperties;

	private readonly TextTrimming _textTrimming;

	private readonly TextLine[] _textLines;

	private readonly CachedMetrics _metrics = new CachedMetrics();

	private readonly TextRunCache? _textRunCache;

	private int _textSourceLength;

	/// <summary>
	/// Gets or sets the height of each line of text.
	/// </summary>
	/// <remarks>
	/// A value of NaN (equivalent to an attribute value of "Auto") indicates that the line height
	/// is determined automatically from the current font characteristics. The default is NaN.
	/// </remarks>
	public double LineHeight => _paragraphProperties.LineHeight;

	/// <summary>
	/// Gets the maximum width.
	/// </summary>
	public double MaxWidth { get; }

	/// <summary>
	/// Gets the maximum height.
	/// </summary>
	public double MaxHeight { get; }

	/// <summary>
	/// Gets the maximum number of text lines.
	/// </summary>
	public int MaxLines { get; }

	/// <summary>
	/// Gets the text spacing.
	/// </summary>
	public double LetterSpacing => _paragraphProperties.LetterSpacing;

	/// <summary>
	/// Gets the text lines.
	/// </summary>
	/// <value>
	/// The text lines.
	/// </value>
	public IReadOnlyList<TextLine> TextLines => _textLines;

	/// <summary>
	/// The distance from the top of the first line to the bottom of the last line.
	/// </summary>
	public double Height => _metrics.Height;

	/// <summary>
	/// The distance from the topmost black pixel of the first line
	/// to the bottommost black pixel of the last line. 
	/// </summary>
	public double Extent => _metrics.Extent;

	/// <summary>
	/// The distance from the top of the first line to the baseline of the first line.
	/// </summary>
	public double Baseline => _metrics.Baseline;

	/// <summary>
	/// The distance from the bottom of the last line to the extent bottom.
	/// </summary>
	public double OverhangAfter => _metrics.OverhangAfter;

	/// <summary>
	/// The maximum distance from the leading black pixel to the leading alignment point of a line.
	/// </summary>
	public double OverhangLeading => _metrics.OverhangLeading;

	/// <summary>
	/// The maximum distance from the trailing black pixel to the trailing alignment point of a line.
	/// </summary>
	public double OverhangTrailing => _metrics.OverhangTrailing;

	/// <summary>
	/// The maximum advance width between the leading and trailing alignment points of a line,
	/// excluding the width of whitespace characters at the end of the line.
	/// </summary>
	public double Width => _metrics.Width;

	/// <summary>
	/// The maximum advance width between the leading and trailing alignment points of a line,
	/// including the width of whitespace characters at the end of the line.
	/// </summary>
	public double WidthIncludingTrailingWhitespace => _metrics.WidthIncludingTrailingWhitespace;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.TextFormatting.TextLayout" /> class.
	/// </summary>
	/// <param name="text">The text.</param>
	/// <param name="typeface">The typeface.</param>
	/// <param name="fontSize">Size of the font.</param>
	/// <param name="foreground">The foreground.</param>
	/// <param name="textAlignment">The text alignment.</param>
	/// <param name="textWrapping">The text wrapping.</param>
	/// <param name="textTrimming">The text trimming.</param>
	/// <param name="textDecorations">The text decorations.</param>
	/// <param name="flowDirection">The text flow direction.</param>
	/// <param name="maxWidth">The maximum width.</param>
	/// <param name="maxHeight">The maximum height.</param>
	/// <param name="lineHeight">The height of each line of text.</param>
	/// <param name="letterSpacing">The letter spacing that is applied to rendered glyphs.</param>
	/// <param name="maxLines">The maximum number of text lines.</param>
	/// <param name="fontFeatures">Optional list of turned on/off features.</param>
	/// <param name="textStyleOverrides">The text style overrides.</param>
	/// <param name="textRunCache">An optional cache for shaped text runs to avoid redundant shaping.</param>
	public TextLayout(string? text, Typeface typeface, double fontSize = 12.0, IBrush? foreground = null, TextAlignment textAlignment = TextAlignment.Left, TextWrapping textWrapping = TextWrapping.NoWrap, TextTrimming? textTrimming = null, TextDecorationCollection? textDecorations = null, FlowDirection flowDirection = FlowDirection.LeftToRight, double maxWidth = double.PositiveInfinity, double maxHeight = double.PositiveInfinity, double lineHeight = double.NaN, double letterSpacing = 0.0, int maxLines = 0, FontFeatureCollection? fontFeatures = null, IReadOnlyList<ValueSpan<TextRunProperties>>? textStyleOverrides = null, TextRunCache? textRunCache = null)
	{
		_paragraphProperties = CreateTextParagraphProperties(typeface, fontSize, foreground, textAlignment, textWrapping, textDecorations, flowDirection, lineHeight, letterSpacing, fontFeatures);
		_textSource = new FormattedTextSource(text ?? "", _paragraphProperties.DefaultTextRunProperties, textStyleOverrides);
		_textTrimming = textTrimming ?? TextTrimming.None;
		MaxWidth = maxWidth;
		MaxHeight = maxHeight;
		MaxLines = maxLines;
		_textRunCache = textRunCache;
		_textLines = CreateTextLines();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.TextFormatting.TextLayout" /> class.
	/// </summary>
	/// <remarks>
	/// This overload is provided for binary compatibility. New code should use the overload that accepts a <see cref="T:Avalonia.Media.TextFormatting.TextRunCache" />.
	/// </remarks>
	public TextLayout(string? text, Typeface typeface, double fontSize, IBrush? foreground, TextAlignment textAlignment, TextWrapping textWrapping, TextTrimming? textTrimming, TextDecorationCollection? textDecorations, FlowDirection flowDirection, double maxWidth, double maxHeight, double lineHeight, double letterSpacing, int maxLines, FontFeatureCollection? fontFeatures, IReadOnlyList<ValueSpan<TextRunProperties>>? textStyleOverrides)
		: this(text, typeface, fontSize, foreground, textAlignment, textWrapping, textTrimming, textDecorations, flowDirection, maxWidth, maxHeight, lineHeight, letterSpacing, maxLines, fontFeatures, textStyleOverrides, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.TextFormatting.TextLayout" /> class.
	/// </summary>
	/// <param name="textSource">The text source.</param>
	/// <param name="paragraphProperties">The default text paragraph properties.</param>
	/// <param name="textTrimming">The text trimming.</param>
	/// <param name="maxWidth">The maximum width.</param>
	/// <param name="maxHeight">The maximum height.</param>
	/// <param name="maxLines">The maximum number of text lines.</param>
	/// <param name="textRunCache">An optional cache for shaped text runs to avoid redundant shaping.</param>
	public TextLayout(ITextSource textSource, TextParagraphProperties paragraphProperties, TextTrimming? textTrimming = null, double maxWidth = double.PositiveInfinity, double maxHeight = double.PositiveInfinity, int maxLines = 0, TextRunCache? textRunCache = null)
	{
		_textSource = textSource;
		_paragraphProperties = paragraphProperties;
		_textTrimming = textTrimming ?? TextTrimming.None;
		MaxWidth = maxWidth;
		MaxHeight = maxHeight;
		MaxLines = maxLines;
		_textRunCache = textRunCache;
		_textLines = CreateTextLines();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.TextFormatting.TextLayout" /> class.
	/// </summary>
	/// <remarks>
	/// This overload is provided for binary compatibility. New code should use the overload that accepts a <see cref="T:Avalonia.Media.TextFormatting.TextRunCache" />.
	/// </remarks>
	public TextLayout(ITextSource textSource, TextParagraphProperties paragraphProperties, TextTrimming? textTrimming, double maxWidth, double maxHeight, int maxLines)
		: this(textSource, paragraphProperties, textTrimming, maxWidth, maxHeight, maxLines, null)
	{
	}

	/// <summary>
	/// Draws the text layout.
	/// </summary>
	/// <param name="context">The drawing context.</param>
	/// <param name="origin">The origin.</param>
	public void Draw(DrawingContext context, Point origin)
	{
		if (_textLines.Length != 0)
		{
			Point point = origin;
			var (x, num3) = point;
			TextLine[] textLines = _textLines;
			foreach (TextLine textLine in textLines)
			{
				textLine.Draw(context, new Point(x, num3));
				num3 += textLine.Height;
			}
		}
	}

	/// <summary>
	/// Get the pixel location relative to the top-left of the layout box given the text position.
	/// </summary>
	/// <param name="textPosition">The text position.</param>
	/// <returns></returns>
	public Rect HitTestTextPosition(int textPosition)
	{
		if (_textLines.Length == 0)
		{
			return default(Rect);
		}
		if (textPosition < 0)
		{
			textPosition = _textSourceLength;
		}
		double num = 0.0;
		for (int i = 0; i < _textLines.Length; i++)
		{
			TextLine textLine = _textLines[i];
			if (textLine.FirstTextSourceIndex + textLine.Length <= textPosition && i + 1 < _textLines.Length)
			{
				num += textLine.Height;
				continue;
			}
			CharacterHit characterHit = new CharacterHit(textPosition);
			double distanceFromCharacterHit = textLine.GetDistanceFromCharacterHit(characterHit);
			CharacterHit nextCaretCharacterHit = textLine.GetNextCaretCharacterHit(characterHit);
			double distanceFromCharacterHit2 = textLine.GetDistanceFromCharacterHit(nextCaretCharacterHit);
			return new Rect(distanceFromCharacterHit, num, distanceFromCharacterHit2 - distanceFromCharacterHit, textLine.Height);
		}
		return default(Rect);
	}

	public IEnumerable<Rect> HitTestTextRange(int start, int length)
	{
		if (start + length <= 0)
		{
			return Array.Empty<Rect>();
		}
		List<Rect> list = new List<Rect>(_textLines.Length);
		double num = 0.0;
		TextLine[] textLines = _textLines;
		foreach (TextLine textLine in textLines)
		{
			if (textLine.FirstTextSourceIndex + textLine.Length <= start)
			{
				num += textLine.Height;
				continue;
			}
			IReadOnlyList<TextBounds> textBounds = textLine.GetTextBounds(start, length);
			if (textBounds.Count > 0)
			{
				foreach (TextBounds item in textBounds)
				{
					Rect? rect = ((list.Count > 0) ? new Rect?(list[list.Count - 1]) : ((Rect?)null));
					if (rect.HasValue && MathUtilities.AreClose(rect.Value.Right, item.Rectangle.Left) && MathUtilities.AreClose(rect.Value.Top, num))
					{
						list[list.Count - 1] = rect.Value.WithWidth(rect.Value.Width + item.Rectangle.Width);
					}
					else
					{
						list.Add(item.Rectangle.WithY(num));
					}
					foreach (TextRunBounds textRunBound in item.TextRunBounds)
					{
						start += textRunBound.Length;
						length -= textRunBound.Length;
					}
				}
			}
			if (textLine.FirstTextSourceIndex + textLine.Length >= start + length)
			{
				break;
			}
			num += textLine.Height;
		}
		return list;
	}

	public TextHitTestResult HitTestPoint(in Point point)
	{
		double num = 0.0;
		TextLine textLine = null;
		CharacterHit characterHitFromDistance;
		for (int i = 0; i < _textLines.Length; i++)
		{
			textLine = _textLines[i];
			if (num + textLine.Height > point.Y)
			{
				characterHitFromDistance = textLine.GetCharacterHitFromDistance(point.X);
				return GetHitTestResult(textLine, characterHitFromDistance, point);
			}
			num += textLine.Height;
		}
		if (textLine == null)
		{
			return default(TextHitTestResult);
		}
		characterHitFromDistance = textLine.GetCharacterHitFromDistance(point.X);
		return GetHitTestResult(textLine, characterHitFromDistance, point);
	}

	public int GetLineIndexFromCharacterIndex(int charIndex, bool trailingEdge)
	{
		if (charIndex < 0)
		{
			return 0;
		}
		if (charIndex > _textSourceLength)
		{
			return _textLines.Length - 1;
		}
		for (int i = 0; i < _textLines.Length; i++)
		{
			TextLine textLine = _textLines[i];
			if (textLine.FirstTextSourceIndex + textLine.Length >= charIndex && charIndex >= textLine.FirstTextSourceIndex && charIndex <= textLine.FirstTextSourceIndex + textLine.Length - ((!trailingEdge) ? 1 : 0))
			{
				return i;
			}
		}
		return _textLines.Length - 1;
	}

	private TextHitTestResult GetHitTestResult(TextLine textLine, CharacterHit characterHit, Point point)
	{
		Point point2 = point;
		var (num3, num4) = point2;
		bool isInside = num3 >= 0.0 && num3 <= textLine.Width && num4 >= 0.0 && num4 <= textLine.Height;
		int num5 = 0;
		if (_paragraphProperties.FlowDirection == FlowDirection.LeftToRight)
		{
			num5 = textLine.FirstTextSourceIndex + textLine.Length;
			if (num3 >= textLine.Width && textLine.Length > 0 && textLine.NewLineLength > 0)
			{
				num5 -= textLine.NewLineLength;
			}
			TextEndOfLine textEndOfLine = textLine.TextLineBreak?.TextEndOfLine;
			if (textEndOfLine != null)
			{
				num5 -= textEndOfLine.Length;
			}
		}
		else
		{
			if (num3 <= textLine.WidthIncludingTrailingWhitespace - textLine.Width && textLine.Length > 0 && textLine.NewLineLength > 0)
			{
				num5 += textLine.NewLineLength;
			}
			TextEndOfLine textEndOfLine2 = textLine.TextLineBreak?.TextEndOfLine;
			if (textEndOfLine2 != null)
			{
				num5 += textEndOfLine2.Length;
			}
		}
		int num6 = characterHit.FirstCharacterIndex + characterHit.TrailingLength;
		bool isTrailing = (num5 == num6 && characterHit.TrailingLength > 0) || num4 > Height;
		if (num6 == textLine.FirstTextSourceIndex + textLine.Length)
		{
			num6 -= textLine.NewLineLength;
		}
		if (textLine.NewLineLength > 0 && num6 + textLine.NewLineLength == characterHit.FirstCharacterIndex + characterHit.TrailingLength)
		{
			characterHit = new CharacterHit(characterHit.FirstCharacterIndex);
		}
		return new TextHitTestResult(characterHit, num6, isInside, isTrailing);
	}

	/// <summary>
	/// Creates the default <see cref="T:Avalonia.Media.TextFormatting.TextParagraphProperties" /> that are used by the <see cref="T:Avalonia.Media.TextFormatting.TextFormatter" />.
	/// </summary>
	/// <param name="typeface">The typeface.</param>
	/// <param name="fontSize">The font size.</param>
	/// <param name="foreground">The foreground.</param>
	/// <param name="textAlignment">The text alignment.</param>
	/// <param name="textWrapping">The text wrapping.</param>
	/// <param name="textDecorations">The text decorations.</param>
	/// <param name="flowDirection">The text flow direction.</param>
	/// <param name="lineHeight">The height of each line of text.</param>
	/// <param name="letterSpacing">The letter spacing that is applied to rendered glyphs.</param>
	/// <param name="features">Optional list of turned on/off features.</param>
	/// <returns></returns>
	internal static TextParagraphProperties CreateTextParagraphProperties(Typeface typeface, double fontSize, IBrush? foreground, TextAlignment textAlignment, TextWrapping textWrapping, TextDecorationCollection? textDecorations, FlowDirection flowDirection, double lineHeight, double letterSpacing, FontFeatureCollection? features)
	{
		GenericTextRunProperties defaultTextRunProperties = new GenericTextRunProperties(typeface, fontSize, textDecorations, foreground, null, BaselineAlignment.Baseline, null, features);
		return new GenericTextParagraphProperties(flowDirection, textAlignment, firstLineInParagraph: true, alwaysCollapsible: false, defaultTextRunProperties, textWrapping, lineHeight, 0.0, letterSpacing);
	}

	private TextLine[] CreateTextLines()
	{
		FormattingObjectPool instance = FormattingObjectPool.Instance;
		bool first = true;
		if (MathUtilities.IsZero(MaxWidth) || MathUtilities.IsZero(MaxHeight))
		{
			TextLineImpl textLineImpl = TextFormatterImpl.CreateEmptyTextLine(0, double.PositiveInfinity, _paragraphProperties);
			UpdateMetrics(textLineImpl, ref first);
			return new TextLine[1] { textLineImpl };
		}
		FormattingObjectPool.RentedList<TextLine> rentedList = instance.TextLines.Rent();
		try
		{
			_textSourceLength = 0;
			TextLine textLine = null;
			TextFormatter current = TextFormatter.Current;
			TextLineImpl textLineImpl2;
			do
			{
				textLineImpl2 = current.FormatLine(_textSource, _textSourceLength, MaxWidth, _paragraphProperties, textLine?.TextLineBreak, _textRunCache) as TextLineImpl;
				if (textLineImpl2 == null)
				{
					if (textLine != null && textLine.NewLineLength > 0)
					{
						TextLineImpl textLineImpl3 = TextFormatterImpl.CreateEmptyTextLine(_textSourceLength, MaxWidth, _paragraphProperties);
						rentedList.Add(textLineImpl3);
						UpdateMetrics(textLineImpl3, ref first);
					}
					break;
				}
				_textSourceLength += textLineImpl2.Length;
				if (rentedList.Count > 0 && !double.IsPositiveInfinity(MaxHeight) && MathUtilities.GreaterThan(Height + textLineImpl2.Height, MaxHeight))
				{
					if (textLine?.TextLineBreak != null && _textTrimming != TextTrimming.None)
					{
						TextLine value = textLine.Collapse(GetCollapsingProperties(MaxWidth));
						rentedList[rentedList.Count - 1] = value;
					}
					break;
				}
				if (textLineImpl2.HasOverflowed && _textTrimming != TextTrimming.None)
				{
					textLineImpl2 = (TextLineImpl)textLineImpl2.Collapse(GetCollapsingProperties(MaxWidth));
				}
				rentedList.Add(textLineImpl2);
				UpdateMetrics(textLineImpl2, ref first);
				textLine = textLineImpl2;
				if (MaxLines > 0 && rentedList.Count >= MaxLines)
				{
					TextLineBreak textLineBreak = textLineImpl2.TextLineBreak;
					if (textLineBreak != null && textLineBreak.IsSplit)
					{
						rentedList[rentedList.Count - 1] = textLineImpl2.Collapse(GetCollapsingProperties(WidthIncludingTrailingWhitespace));
					}
					break;
				}
			}
			while (!(textLineImpl2.TextLineBreak?.TextEndOfLine is TextEndOfParagraph));
			if (rentedList.Count == 0)
			{
				TextLineImpl textLineImpl4 = TextFormatterImpl.CreateEmptyTextLine(0, MaxWidth, _paragraphProperties);
				rentedList.Add(textLineImpl4);
				UpdateMetrics(textLineImpl4, ref first);
			}
			if (_paragraphProperties.TextAlignment == TextAlignment.Justify)
			{
				double maxWidth = MaxWidth;
				if (!double.IsInfinity(maxWidth) && maxWidth > 0.0)
				{
					InterWordJustification justificationProperties = new InterWordJustification(maxWidth);
					for (int i = 0; i < rentedList.Count; i++)
					{
						TextLine textLine2 = rentedList[i];
						if (i != rentedList.Count - 1 && textLine2.NewLineLength <= 0 && textLine2.TextLineBreak?.TextEndOfLine == null)
						{
							textLine2.Justify(justificationProperties);
						}
					}
				}
			}
			return rentedList.ToArray();
		}
		finally
		{
			instance.TextLines.Return(ref rentedList);
		}
	}

	private void UpdateMetrics(TextLineImpl currentLine, ref bool first)
	{
		_metrics.Height += currentLine.Height;
		_metrics.Width = Math.Max(_metrics.Width, currentLine.Width);
		_metrics.Extent = Math.Max(_metrics.Extent, currentLine.Extent);
		double widthIncludingTrailingWhitespace = _metrics.WidthIncludingTrailingWhitespace;
		double widthIncludingTrailingWhitespace2 = currentLine.WidthIncludingTrailingWhitespace;
		if (widthIncludingTrailingWhitespace < widthIncludingTrailingWhitespace2)
		{
			_metrics.WidthIncludingTrailingWhitespace = currentLine.WidthIncludingTrailingWhitespace;
			_metrics.OverhangLeading = currentLine.OverhangLeading;
			_metrics.OverhangTrailing = currentLine.OverhangTrailing;
		}
		_metrics.OverhangAfter = currentLine.OverhangAfter;
		if (first)
		{
			_metrics.Baseline = currentLine.Baseline;
			first = false;
		}
	}

	/// <summary>
	/// Gets the <see cref="T:Avalonia.Media.TextFormatting.TextCollapsingProperties" /> for current text trimming mode.
	/// </summary>
	/// <param name="width">The collapsing width.</param>
	/// <returns>The <see cref="T:Avalonia.Media.TextFormatting.TextCollapsingProperties" />.</returns>
	private TextCollapsingProperties? GetCollapsingProperties(double width)
	{
		if (_textTrimming == TextTrimming.None)
		{
			return null;
		}
		return _textTrimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(width, _paragraphProperties.DefaultTextRunProperties, _paragraphProperties.FlowDirection));
	}

	public void Dispose()
	{
		TextLine[] textLines = _textLines;
		for (int i = 0; i < textLines.Length; i++)
		{
			textLines[i].Dispose();
		}
	}
}
