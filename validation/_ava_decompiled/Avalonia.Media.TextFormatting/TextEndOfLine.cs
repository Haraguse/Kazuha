namespace Avalonia.Media.TextFormatting;

/// <summary>
/// A text run that indicates the end of a line.
/// </summary>
public class TextEndOfLine : TextRun
{
	public override int Length { get; }

	public TextEndOfLine(int textSourceLength = 1)
	{
		Length = textSourceLength;
	}
}
