namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Produces <see cref="T:Avalonia.Media.TextFormatting.TextRun" /> objects that are used by the <see cref="T:Avalonia.Media.TextFormatting.TextFormatter" />.
/// </summary>
public interface ITextSource
{
	/// <summary>
	/// Gets a <see cref="T:Avalonia.Media.TextFormatting.TextRun" /> for specified text source index.
	/// </summary>
	/// <param name="textSourceIndex">The text source index.</param>
	/// <returns>The text run.</returns>
	TextRun? GetTextRun(int textSourceIndex);
}
