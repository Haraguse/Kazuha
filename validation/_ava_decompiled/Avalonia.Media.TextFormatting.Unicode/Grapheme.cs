namespace Avalonia.Media.TextFormatting.Unicode;

/// <summary>
/// Represents the smallest unit of a writing system of any given language.
/// </summary>
public readonly ref struct Grapheme(Codepoint firstCodepoint, int offset, int length)
{
	/// <summary>
	/// The first <see cref="T:Avalonia.Media.TextFormatting.Unicode.Codepoint" /> of the grapheme cluster.
	/// </summary>
	public Codepoint FirstCodepoint { get; } = firstCodepoint;

	/// <summary>
	/// Gets the starting code unit offset of this grapheme inside its containing text.
	/// </summary>
	public int Offset { get; } = offset;

	/// <summary>
	/// Gets the length of this grapheme, in code units.
	/// </summary>
	public int Length { get; } = length;
}
