namespace Avalonia.Media.TextFormatting;

internal struct OrderedBidiRun(int runIndex, TextRun run, sbyte level)
{
	public int RunIndex { get; } = runIndex;

	public sbyte Level { get; } = level;

	public TextRun Run { get; } = run;

	public int NextRunIndex { get; set; } = -1;
}
