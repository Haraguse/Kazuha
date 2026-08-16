using System;
using System.Collections.Generic;

namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Reads the script tags declared in a font's OpenType Layout (<c>GSUB</c>/<c>GPOS</c>)
/// ScriptList. A declared script tag signals that the font carries shaping rules for that
/// script — the signal used to decide whether a font can actually <em>shape</em> a complex
/// script rather than merely map its codepoints through cmap.
/// <see href="https://learn.microsoft.com/typography/opentype/spec/chapter2#script-list-table-and-script-record" />
/// </summary>
internal static class ScriptListTable
{
	private static readonly OpenTypeTag s_gsub = OpenTypeTag.Parse("GSUB");

	private static readonly OpenTypeTag s_gpos = OpenTypeTag.Parse("GPOS");

	/// <summary>
	/// Adds the GSUB and GPOS script tags declared by <paramref name="glyphTypeface" /> to
	/// <paramref name="scriptTags" />.
	/// </summary>
	/// <returns>
	/// <c>false</c> if a layout table is present but could not be parsed — the caller should
	/// then treat shaping capability as unknown rather than unsupported (don't reject the font
	/// on the strength of an empty set). An absent table is not a failure.
	/// </returns>
	public static bool TryReadScriptTags(GlyphTypeface glyphTypeface, HashSet<OpenTypeTag> scriptTags)
	{
		return TryReadScriptList(glyphTypeface, s_gsub, scriptTags) & TryReadScriptList(glyphTypeface, s_gpos, scriptTags);
	}

	private static bool TryReadScriptList(GlyphTypeface glyphTypeface, OpenTypeTag tableTag, HashSet<OpenTypeTag> scriptTags)
	{
		if (!glyphTypeface.PlatformTypeface.TryGetTable(tableTag, out var table))
		{
			return true;
		}
		try
		{
			BigEndianBinaryReader bigEndianBinaryReader = new BigEndianBinaryReader(table.Span);
			bigEndianBinaryReader.ReadUInt16();
			bigEndianBinaryReader.ReadUInt16();
			ushort offset = bigEndianBinaryReader.ReadOffset16();
			bigEndianBinaryReader.Seek(offset);
			ushort num = bigEndianBinaryReader.ReadUInt16();
			for (int i = 0; i < num; i++)
			{
				scriptTags.Add(new OpenTypeTag(bigEndianBinaryReader.ReadUInt32()));
				bigEndianBinaryReader.ReadOffset16();
			}
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
