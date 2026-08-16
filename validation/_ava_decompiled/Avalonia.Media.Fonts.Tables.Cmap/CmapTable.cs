using System;

namespace Avalonia.Media.Fonts.Tables.Cmap;

/// <summary>
/// Represents the 'cmap' table in an OpenType font, which maps character codes to glyph indices.
/// </summary>
/// <remarks>The 'cmap' table is a critical component of an OpenType font, enabling the mapping of
/// character codes (e.g., Unicode) to glyph indices used for rendering text. This class provides functionality to
/// load and parse the 'cmap' table from a font's platform-specific typeface.</remarks>
internal sealed class CmapTable
{
	internal const string TableName = "cmap";

	internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse("cmap");

	public static CharacterToGlyphMap Load(GlyphTypeface glyphTypeface)
	{
		if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
		{
			throw new InvalidOperationException("No cmap table found.");
		}
		BigEndianBinaryReader bigEndianBinaryReader = new BigEndianBinaryReader(table.Span);
		bigEndianBinaryReader.ReadUInt16();
		ushort num = bigEndianBinaryReader.ReadUInt16();
		CmapSubtableEntry[] array = new CmapSubtableEntry[num];
		for (int i = 0; i < num; i++)
		{
			PlatformID platform = (PlatformID)bigEndianBinaryReader.ReadUInt16();
			CmapEncoding encoding = (CmapEncoding)bigEndianBinaryReader.ReadUInt16();
			int offset = (int)bigEndianBinaryReader.ReadUInt32();
			int position = bigEndianBinaryReader.Position;
			bigEndianBinaryReader.Seek(offset);
			CmapFormat format = (CmapFormat)bigEndianBinaryReader.ReadUInt16();
			bigEndianBinaryReader.Seek(position);
			CmapSubtableEntry cmapSubtableEntry = new CmapSubtableEntry(platform, encoding, offset, format);
			array[i] = cmapSubtableEntry;
		}
		if (TryFindFormat12Or13Entry(array, CmapFormat.Format12, out var result))
		{
			return new CharacterToGlyphMap(new CmapFormat12Or13Table(result.GetSubtableMemory(table)));
		}
		if (TryFindFormat4Entry(array, out var result2))
		{
			return new CharacterToGlyphMap(new CmapFormat4Table(result2.GetSubtableMemory(table)));
		}
		if (TryFindFormat12Or13Entry(array, CmapFormat.Format13, out var result3))
		{
			return new CharacterToGlyphMap(new CmapFormat12Or13Table(result3.GetSubtableMemory(table)));
		}
		throw new InvalidOperationException("No suitable cmap subtable found.");
		static bool TryFindFormat12Or13Entry(CmapSubtableEntry[] entries, CmapFormat expectedFormat, out CmapSubtableEntry reference)
		{
			reference = default(CmapSubtableEntry);
			int num2 = int.MaxValue;
			int num3 = int.MaxValue;
			for (int j = 0; j < entries.Length; j++)
			{
				CmapSubtableEntry cmapSubtableEntry2 = entries[j];
				if (cmapSubtableEntry2.Format == expectedFormat)
				{
					int num4 = cmapSubtableEntry2.Platform switch
					{
						PlatformID.Unicode => 0, 
						PlatformID.Windows => 1, 
						_ => 2, 
					};
					int num5 = 2;
					switch (cmapSubtableEntry2.Platform)
					{
					case PlatformID.Unicode:
						if (cmapSubtableEntry2.Encoding == CmapEncoding.Unicode_2_0_full)
						{
							num5 = 0;
						}
						else if (cmapSubtableEntry2.Encoding == CmapEncoding.Unicode_2_0_BMP)
						{
							num5 = 1;
						}
						break;
					case PlatformID.Windows:
						if (cmapSubtableEntry2.Encoding == CmapEncoding.Microsoft_UCS4 && num4 != 0)
						{
							num5 = 0;
						}
						else if (cmapSubtableEntry2.Encoding == CmapEncoding.Unicode_1_1 && num4 != 0)
						{
							num5 = 1;
						}
						break;
					}
					if (num5 < num3 || (num5 == num3 && num4 < num2))
					{
						reference = cmapSubtableEntry2;
						num3 = num5;
						num2 = num4;
					}
					else if (num4 < num2)
					{
						reference = cmapSubtableEntry2;
						num3 = num5;
						num2 = num4;
					}
					if (num2 == 0 && num3 == 0)
					{
						break;
					}
				}
			}
			return reference.Format != CmapFormat.Format0;
		}
		static bool TryFindFormat4Entry(CmapSubtableEntry[] entries, out CmapSubtableEntry reference)
		{
			reference = default(CmapSubtableEntry);
			int num2 = int.MaxValue;
			for (int j = 0; j < entries.Length; j++)
			{
				CmapSubtableEntry cmapSubtableEntry2 = entries[j];
				if (cmapSubtableEntry2.Format == CmapFormat.Format4)
				{
					int num3 = cmapSubtableEntry2.Platform switch
					{
						PlatformID.Unicode => 0, 
						PlatformID.Windows => 1, 
						_ => 2, 
					};
					if (num3 < num2)
					{
						reference = cmapSubtableEntry2;
						num2 = num3;
					}
					if (num2 == 0)
					{
						break;
					}
				}
			}
			return reference.Format != CmapFormat.Format0;
		}
	}
}
