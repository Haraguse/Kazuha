using System;

namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Flags for the 'head' table.
/// </summary>
[Flags]
internal enum HeadFlags : ushort
{
	/// <summary>
	/// Bit 0: Baseline for font at y=0.
	/// </summary>
	BaselineAtY0 = 1,
	/// <summary>
	/// Bit 1: Left sidebearing point at x=0 (relevant only for TrueType rasterizers).
	/// </summary>
	LeftSidebearingAtX0 = 2,
	/// <summary>
	/// Bit 2: Instructions may depend on point size.
	/// </summary>
	InstructionsDependOnPointSize = 4,
	/// <summary>
	/// Bit 3: Force ppem to integer values for all internal scaler math; may use fractional ppem sizes if this bit is clear.
	/// </summary>
	ForcePpemToInteger = 8,
	/// <summary>
	/// Bit 4: Instructions may alter advance width (the advance widths might not scale linearly).
	/// </summary>
	InstructionsMayAlterAdvanceWidth = 0x10,
	/// <summary>
	/// Bit 5: This bit should be set in fonts that are intended to be laid out vertically, and in which the glyphs have been drawn such that an x-coordinate of 0 corresponds to the desired vertical baseline.
	/// </summary>
	VerticalBaseline = 0x20,
	/// <summary>
	/// Bit 7: Font data is 'lossless' as a result of having been subjected to optimizing transformation and/or compression.
	/// </summary>
	Lossless = 0x80,
	/// <summary>
	/// Bit 8: Font converted (produce compatible metrics).
	/// </summary>
	FontConverted = 0x100,
	/// <summary>
	/// Bit 9: Font optimized for ClearType. Note that this implies that instructions may alter advance widths (bit 4 should also be set).
	/// </summary>
	ClearTypeOptimized = 0x200,
	/// <summary>
	/// Bit 10: Last Resort font. If set, indicates that the glyphs encoded in the 'cmap' subtables are simply generic symbolic representations of code point ranges and don't truly represent support for those code points.
	/// </summary>
	LastResortFont = 0x400
}
