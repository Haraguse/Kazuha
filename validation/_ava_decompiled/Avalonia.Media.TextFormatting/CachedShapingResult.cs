namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Stores the result of text shaping for a paragraph segment starting at a given text source index.
/// </summary>
internal readonly struct CachedShapingResult(TextRun[] shapedRuns, FlowDirection resolvedFlowDirection, TextEndOfLine? textEndOfLine, int textSourceLength)
{
	/// <summary>
	/// The shaped text runs (output of ShapeTextRuns).
	/// </summary>
	public readonly TextRun[] ShapedRuns = shapedRuns;

	/// <summary>
	/// The resolved flow direction for the paragraph.
	/// </summary>
	public readonly FlowDirection ResolvedFlowDirection = resolvedFlowDirection;

	/// <summary>
	/// The end of line marker, if any.
	/// </summary>
	public readonly TextEndOfLine? TextEndOfLine = textEndOfLine;

	/// <summary>
	/// The total text source length consumed.
	/// </summary>
	public readonly int TextSourceLength = textSourceLength;
}
