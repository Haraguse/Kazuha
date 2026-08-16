using System.Globalization;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Represents a base class for text formatting.
/// </summary>
public abstract class TextFormatter
{
	/// <summary>
	/// Gets the current <see cref="T:Avalonia.Media.TextFormatting.TextFormatter" /> that is used for non complex text formatting.
	/// </summary>
	public static TextFormatter Current
	{
		get
		{
			TextFormatter service = AvaloniaLocator.Current.GetService<TextFormatter>();
			if (service != null)
			{
				return service;
			}
			service = new TextFormatterImpl();
			AvaloniaLocator.CurrentMutable.Bind<TextFormatter>().ToConstant(service);
			return service;
		}
	}

	/// <summary>
	/// Formats a text line.
	/// </summary>
	/// <param name="textSource">The text source.</param>
	/// <param name="firstTextSourceIndex">The first character index to start the text line from.</param>
	/// <param name="paragraphWidth">A <see cref="T:System.Double" /> value that specifies the width of the paragraph that the line fills.</param>
	/// <param name="paragraphProperties">A <see cref="T:Avalonia.Media.TextFormatting.TextParagraphProperties" /> value that represents paragraph properties,
	/// such as TextWrapping, TextAlignment, or TextStyle.</param>
	/// <param name="previousLineBreak">A <see cref="T:Avalonia.Media.TextFormatting.TextLineBreak" /> value that specifies the text formatter state,
	/// in terms of where the previous line in the paragraph was broken by the text formatting process.</param>
	/// <returns>The formatted line.</returns>
	public abstract TextLine? FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth, TextParagraphProperties paragraphProperties, TextLineBreak? previousLineBreak = null);

	/// <summary>
	/// Formats a text line with an optional <see cref="T:Avalonia.Media.TextFormatting.TextRunCache" /> to avoid redundant shaping
	/// when only the paragraph width changes.
	/// </summary>
	/// <param name="textSource">The text source.</param>
	/// <param name="firstTextSourceIndex">The first character index to start the text line from.</param>
	/// <param name="paragraphWidth">A <see cref="T:System.Double" /> value that specifies the width of the paragraph that the line fills.</param>
	/// <param name="paragraphProperties">A <see cref="T:Avalonia.Media.TextFormatting.TextParagraphProperties" /> value that represents paragraph properties,
	/// such as TextWrapping, TextAlignment, or TextStyle.</param>
	/// <param name="previousLineBreak">A <see cref="T:Avalonia.Media.TextFormatting.TextLineBreak" /> value that specifies the text formatter state,
	/// in terms of where the previous line in the paragraph was broken by the text formatting process.</param>
	/// <param name="textRunCache">A <see cref="T:Avalonia.Media.TextFormatting.TextRunCache" /> that caches shaped text runs.</param>
	/// <returns>The formatted line.</returns>
	public virtual TextLine? FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth, TextParagraphProperties paragraphProperties, TextLineBreak? previousLineBreak, TextRunCache? textRunCache)
	{
		return FormatLine(textSource, firstTextSourceIndex, paragraphWidth, paragraphProperties, previousLineBreak);
	}

	/// <summary>
	/// Creates a shaped symbol.
	/// </summary>
	/// <param name="textRun">The symbol run to shape.</param>
	/// <param name="flowDirection">The flow direction.</param>
	/// <returns>
	/// The shaped symbol.
	/// </returns>
	public static ShapedTextRun CreateSymbol(TextRun textRun, FlowDirection flowDirection)
	{
		TextShaper current = TextShaper.Current;
		GlyphTypeface cachedGlyphTypeface = textRun.Properties.CachedGlyphTypeface;
		double fontRenderingEmSize = textRun.Properties.FontRenderingEmSize;
		CultureInfo cultureInfo = textRun.Properties.CultureInfo;
		return new ShapedTextRun(current.ShapeText(options: new TextShaperOptions(cachedGlyphTypeface, fontRenderingEmSize, (sbyte)flowDirection, cultureInfo, 0.0, 0.0, textRun.Properties.FontFeatures), text: textRun.Text), textRun.Properties);
	}
}
