namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Font direction hint for mixed directional text.
/// </summary>
internal enum FontDirectionHint : short
{
	/// <summary>
	/// Fully mixed directional glyphs.
	/// </summary>
	FullyMixed = 0,
	/// <summary>
	/// Only strongly left to right glyphs.
	/// </summary>
	OnlyLeftToRight = 1,
	/// <summary>
	/// Like 1 but also contains neutrals.
	/// </summary>
	LeftToRightWithNeutrals = 2,
	/// <summary>
	/// Only strongly right to left glyphs.
	/// </summary>
	OnlyRightToLeft = -1,
	/// <summary>
	/// Like -1 but also contains neutrals.
	/// </summary>
	RightToLeftWithNeutrals = -2
}
