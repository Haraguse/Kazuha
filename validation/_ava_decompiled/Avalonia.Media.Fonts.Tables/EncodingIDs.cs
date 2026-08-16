namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Encoding IDS
/// </summary>
internal enum EncodingIDs : ushort
{
	/// <summary>
	/// Unicode 1.0 semantics
	/// </summary>
	Unicode1,
	/// <summary>
	/// Unicode 1.1 semantics
	/// </summary>
	Unicode11,
	/// <summary>
	/// ISO/IEC 10646 semantics
	/// </summary>
	ISO10646,
	/// <summary>
	/// Unicode 2.0 and onwards semantics, Unicode BMP only (cmap subtable formats 0, 4, 6).
	/// </summary>
	Unicode2,
	/// <summary>
	/// Unicode 2.0 and onwards semantics, Unicode full repertoire (cmap subtable formats 0, 4, 6, 10, 12).
	/// </summary>
	Unicode2Plus,
	/// <summary>
	/// Unicode Variation Sequences (cmap subtable format 14).
	/// </summary>
	UnicodeVariationSequences,
	/// <summary>
	/// Unicode full repertoire (cmap subtable formats 0, 4, 6, 10, 12, 13)
	/// </summary>
	UnicodeFull
}
