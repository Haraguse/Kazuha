using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting;

/// <summary>Represents a line break that occurred due to wrapping.</summary>
internal sealed class WrappingTextLineBreak : TextLineBreak
{
	private List<TextRun>? _remainingRuns;

	public WrappingTextLineBreak(TextEndOfLine? textEndOfLine, FlowDirection flowDirection, List<TextRun> remainingRuns)
		: base(textEndOfLine, flowDirection, isSplit: true)
	{
		_remainingRuns = remainingRuns;
	}

	/// <summary>
	/// Gets the remaining runs from this line break, and clears them from this line break.
	/// </summary>
	/// <returns>A list of text runs.</returns>
	public List<TextRun>? AcquireRemainingRuns()
	{
		List<TextRun>? remainingRuns = _remainingRuns;
		_remainingRuns = null;
		return remainingRuns;
	}
}
