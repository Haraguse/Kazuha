using System;

namespace Avalonia.Media.Fonts.Tables.Cmap;

internal readonly record struct CmapSubtableEntry
{
	/// <summary>
	/// Gets the platform identifier for the current environment.
	/// </summary>
	public PlatformID Platform { get; }

	/// <summary>
	/// Gets the character map (CMap) encoding associated with this instance.
	/// </summary>
	public CmapEncoding Encoding { get; }

	/// <summary>
	/// Gets the offset of the sub table.
	/// </summary>
	public int Offset { get; }

	/// <summary>
	/// Gets the format of the character-to-glyph mapping (cmap) table.
	/// </summary>
	public CmapFormat Format { get; }

	public CmapSubtableEntry(PlatformID platform, CmapEncoding encoding, int offset, CmapFormat format)
	{
		Platform = platform;
		Encoding = encoding;
		Offset = offset;
		Format = format;
	}

	public ReadOnlyMemory<byte> GetSubtableMemory(ReadOnlyMemory<byte> table)
	{
		return table.Slice(Offset);
	}
}
