using System;

namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Mac style flags for font styling (used by macOS).
/// </summary>
[Flags]
internal enum MacStyleFlags : ushort
{
	/// <summary>
	/// Bit 0: Bold (if set to 1).
	/// </summary>
	Bold = 1,
	/// <summary>
	/// Bit 1: Italic (if set to 1).
	/// </summary>
	Italic = 2,
	/// <summary>
	/// Bit 2: Underline (if set to 1).
	/// </summary>
	Underline = 4,
	/// <summary>
	/// Bit 3: Outline (if set to 1).
	/// </summary>
	Outline = 8,
	/// <summary>
	/// Bit 4: Shadow (if set to 1).
	/// </summary>
	Shadow = 0x10,
	/// <summary>
	/// Bit 5: Condensed (if set to 1).
	/// </summary>
	Condensed = 0x20,
	/// <summary>
	/// Bit 6: Extended (if set to 1).
	/// </summary>
	Extended = 0x40
}
