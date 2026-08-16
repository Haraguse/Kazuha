using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.Fonts.Tables.Cmap;

internal sealed class CmapFormat12Or13Table
{
	private readonly ReadOnlyMemory<byte> _table;

	private readonly int _groupCount;

	private readonly ReadOnlyMemory<byte> _groups;

	public CmapFormat Format { get; }

	/// <summary>
	/// Gets the language code for the cmap subtable.
	/// For non-language-specific tables, this value is 0.
	/// </summary>
	public uint Language { get; }

	public ushort this[int codePoint]
	{
		get
		{
			int num = FindGroupIndex(codePoint);
			if (num < 0)
			{
				return 0;
			}
			ReadOnlySpan<byte> span = _groups.Span;
			uint start = ReadUInt32BE(span, num, 0);
			uint startGlyph = ReadUInt32BE(span, num, 8);
			return CalcEffectiveGlyph(codePoint, start, startGlyph);
		}
	}

	public CmapFormat12Or13Table(ReadOnlyMemory<byte> table)
	{
		BigEndianBinaryReader bigEndianBinaryReader = new BigEndianBinaryReader(table.Span);
		ushort num = bigEndianBinaryReader.ReadUInt16();
		Format = (CmapFormat)num;
		bigEndianBinaryReader.ReadUInt16();
		_table = table[..(int)bigEndianBinaryReader.ReadUInt32()];
		Language = bigEndianBinaryReader.ReadUInt32();
		_groupCount = (int)bigEndianBinaryReader.ReadUInt32();
		int position = bigEndianBinaryReader.Position;
		int length = _groupCount * 12;
		_groups = _table.Slice(position, length);
	}

	/// <summary>
	/// Retrieves the glyph index corresponding to the specified Unicode code point.
	/// </summary>
	/// <param name="codePoint">The Unicode code point for which to obtain the glyph index. Must be a valid code point supported by the
	/// font.</param>
	/// <returns>The glyph index as an unsigned 16-bit integer for the specified code point.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ushort GetGlyph(int codePoint)
	{
		return this[codePoint];
	}

	/// <summary>
	/// Determines whether the specified Unicode code point is present in the glyph set.
	/// </summary>
	/// <param name="codePoint">The Unicode code point to check for presence in the glyph set. Must be a valid integer representing a
	/// Unicode character.</param>
	/// <returns>true if the glyph set contains the specified code point; otherwise, false.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ContainsGlyph(int codePoint)
	{
		return FindGroupIndex(codePoint) >= 0;
	}

	/// <summary>
	/// Maps multiple Unicode code points to glyph indices in a single operation.
	/// </summary>
	/// <param name="codePoints">Read-only span of code points to map.</param>
	/// <param name="glyphIds">Output span to write glyph IDs. Must be at least as long as <paramref name="codePoints" />.</param>
	/// <remarks>
	/// This method is significantly more efficient than calling the indexer multiple times as it:
	/// - Reuses span references (no repeated memory access)
	/// - Caches group data for sequential lookups
	/// - Optimizes for locality of code points (common in text runs)
	/// Format 12 is commonly used for fonts with large character sets (CJK, emoji, etc.)
	/// This is the preferred method for batch character-to-glyph mapping in text shaping.
	/// </remarks>
	public void GetGlyphs(ReadOnlySpan<int> codePoints, Span<ushort> glyphIds)
	{
		if (glyphIds.Length < codePoints.Length)
		{
			throw new ArgumentException("Output span must be at least as long as input span", "glyphIds");
		}
		ReadOnlySpan<byte> span = _groups.Span;
		int num = -1;
		uint num2 = 0u;
		uint num3 = 0u;
		uint startGlyph = 0u;
		for (int i = 0; i < codePoints.Length; i++)
		{
			int num4 = codePoints[i];
			if (num >= 0 && num4 >= num2 && num4 <= num3)
			{
				glyphIds[i] = CalcEffectiveGlyph(num4, num2, startGlyph);
				continue;
			}
			int num5 = FindGroupIndexOptimized(num4, span);
			if (num5 < 0)
			{
				glyphIds[i] = 0;
				num = -1;
				continue;
			}
			num = num5;
			num2 = ReadUInt32BE(span, num5, 0);
			num3 = ReadUInt32BE(span, num5, 4);
			startGlyph = ReadUInt32BE(span, num5, 8);
			glyphIds[i] = CalcEffectiveGlyph(num4, num2, startGlyph);
		}
	}

	public bool TryGetGlyph(int codePoint, out ushort glyphId)
	{
		glyphId = this[codePoint];
		return glyphId != 0;
	}

	internal bool TryGetRange(int index, out CodepointRange range)
	{
		if ((uint)index >= (uint)_groupCount)
		{
			range = default(CodepointRange);
			return false;
		}
		ReadOnlySpan<byte> span = _groups.Span;
		int start = (int)ReadUInt32BE(span, index, 0);
		int end = (int)ReadUInt32BE(span, index, 4);
		range = new CodepointRange(start, end);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ushort CalcEffectiveGlyph(int codePoint, uint start, uint startGlyph)
	{
		if (Format == CmapFormat.Format13)
		{
			return (ushort)startGlyph;
		}
		return (ushort)(startGlyph + (codePoint - start));
	}

	private int FindGroupIndexOptimized(int codePoint, ReadOnlySpan<byte> groups)
	{
		int num = 0;
		int num2 = _groupCount - 1;
		while (num <= num2)
		{
			int num3 = num + num2 >> 1;
			uint num4 = ReadUInt32BE(groups, num3, 0);
			uint num5 = ReadUInt32BE(groups, num3, 4);
			if (codePoint < num4)
			{
				num2 = num3 - 1;
				continue;
			}
			if (codePoint > num5)
			{
				num = num3 + 1;
				continue;
			}
			return num3;
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint ReadUInt32BE(ReadOnlySpan<byte> span, int groupIndex, int fieldOffset)
	{
		int start = groupIndex * 12 + fieldOffset;
		return BinaryPrimitives.ReadUInt32BigEndian(span.Slice(start, 4));
	}

	private int FindGroupIndex(int codePoint)
	{
		int num = 0;
		int num2 = _groupCount - 1;
		ReadOnlySpan<byte> span = _groups.Span;
		while (num <= num2)
		{
			int num3 = num + num2 >> 1;
			uint num4 = ReadUInt32BE(span, num3, 0);
			uint num5 = ReadUInt32BE(span, num3, 4);
			if (codePoint < num4)
			{
				num2 = num3 - 1;
				continue;
			}
			if (codePoint > num5)
			{
				num = num3 + 1;
				continue;
			}
			return num3;
		}
		return -1;
	}
}
