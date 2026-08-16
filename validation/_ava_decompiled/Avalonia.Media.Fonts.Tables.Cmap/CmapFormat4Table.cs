using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.Fonts.Tables.Cmap;

internal sealed class CmapFormat4Table
{
	private readonly ReadOnlyMemory<byte> _table;

	private readonly int _segCount;

	private readonly ReadOnlyMemory<byte> _endCodes;

	private readonly ReadOnlyMemory<byte> _startCodes;

	private readonly ReadOnlyMemory<byte> _idDeltas;

	private readonly ReadOnlyMemory<byte> _idRangeOffsets;

	private readonly ReadOnlyMemory<byte> _glyphIdArray;

	/// <summary>
	/// Gets the language code for the cmap subtable.
	/// For non-language-specific tables, this value is 0.
	/// </summary>
	public ushort Language { get; }

	public ushort this[int codePoint]
	{
		get
		{
			int num = FindSegmentIndex(codePoint);
			if (num < 0)
			{
				return 0;
			}
			ushort num2 = ReadUInt16BE(_idRangeOffsets.Span, num);
			ushort num3 = ReadUInt16BE(_idDeltas.Span, num);
			if (num2 == 0)
			{
				return (ushort)((codePoint + num3) & 0xFFFF);
			}
			int num4 = ReadUInt16BE(_startCodes.Span, num);
			int num5 = num2 / 2;
			int num6 = codePoint - num4 + num5 - (_segCount - num);
			int num7 = _glyphIdArray.Length / 2;
			if ((uint)num6 < (uint)num7)
			{
				ushort num8 = ReadUInt16BE(_glyphIdArray.Span, num6);
				if (num8 != 0)
				{
					num8 = (ushort)((num8 + num3) & 0xFFFF);
				}
				return num8;
			}
			return 0;
		}
	}

	public CmapFormat4Table(ReadOnlyMemory<byte> table)
	{
		BigEndianBinaryReader bigEndianBinaryReader = new BigEndianBinaryReader(table.Span);
		bigEndianBinaryReader.ReadUInt16();
		ushort num = bigEndianBinaryReader.ReadUInt16();
		_table = table.Slice(0, num);
		Language = bigEndianBinaryReader.ReadUInt16();
		ushort num2 = bigEndianBinaryReader.ReadUInt16();
		_segCount = num2 / 2;
		bigEndianBinaryReader.ReadUInt16();
		bigEndianBinaryReader.ReadUInt16();
		bigEndianBinaryReader.ReadUInt16();
		int position = bigEndianBinaryReader.Position;
		int num3 = position + _segCount * 2 + 2;
		int num4 = num3 + _segCount * 2;
		int num5 = num4 + _segCount * 2;
		int num6 = num5 + _segCount * 2;
		_endCodes = _table.Slice(position, _segCount * 2);
		_startCodes = _table.Slice(num3, _segCount * 2);
		_idDeltas = _table.Slice(num4, _segCount * 2);
		_idRangeOffsets = _table.Slice(num5, _segCount * 2);
		int num7 = (num - num6) / 2;
		_glyphIdArray = _table.Slice(num6, num7 * 2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ushort GetGlyph(int codePoint)
	{
		return this[codePoint];
	}

	public bool ContainsGlyph(int codePoint)
	{
		int num = FindSegmentIndex(codePoint);
		if ((uint)num >= (uint)_segCount)
		{
			return false;
		}
		ushort num2 = ReadUInt16BE(_idRangeOffsets.Span, num);
		ushort num3 = ReadUInt16BE(_idDeltas.Span, num);
		if (num2 == 0)
		{
			return ((codePoint + num3) & 0xFFFF) != 0;
		}
		int num4 = ReadUInt16BE(_startCodes.Span, num);
		int num5 = num2 >> 1;
		int num6 = codePoint - num4 + num5 - (_segCount - num);
		if ((uint)num6 >= (uint)(_glyphIdArray.Length >> 1))
		{
			return false;
		}
		return ReadUInt16BE(_glyphIdArray.Span, num6) != 0;
	}

	/// <summary>
	/// Maps multiple Unicode code points to glyph indices in a single operation.
	/// </summary>
	/// <param name="codePoints">Read-only span of code points to map.</param>
	/// <param name="glyphIds">Output span to write glyph IDs. Must be at least as long as <paramref name="codePoints" />.</param>
	/// <remarks>
	/// This method is significantly more efficient than calling the indexer multiple times as it:
	/// - Reuses span references (no repeated .Span property access)
	/// - Caches segment data for sequential lookups
	/// - Optimizes for locality of code points (common in text runs)
	/// This is the preferred method for batch character-to-glyph mapping in text shaping.
	/// </remarks>
	public void GetGlyphs(ReadOnlySpan<int> codePoints, Span<ushort> glyphIds)
	{
		if (glyphIds.Length < codePoints.Length)
		{
			throw new ArgumentException("Output span must be at least as long as input span", "glyphIds");
		}
		ReadOnlySpan<byte> span = _startCodes.Span;
		ReadOnlySpan<byte> span2 = _endCodes.Span;
		ReadOnlySpan<byte> span3 = _idDeltas.Span;
		ReadOnlySpan<byte> span4 = _idRangeOffsets.Span;
		ReadOnlySpan<byte> span5 = _glyphIdArray.Span;
		int num = span5.Length / 2;
		int num2 = -1;
		for (int i = 0; i < codePoints.Length; i++)
		{
			int num3 = codePoints[i];
			int num6;
			if (num2 >= 0 && num2 < _segCount)
			{
				int num4 = ReadUInt16BE(span, num2);
				int num5 = ReadUInt16BE(span2, num2);
				if (num3 >= num4 && num3 <= num5)
				{
					num6 = num2;
					goto IL_00d9;
				}
			}
			num6 = FindSegmentIndexOptimized(num3, span, span2);
			if (num6 < 0)
			{
				glyphIds[i] = 0;
				continue;
			}
			num2 = num6;
			goto IL_00d9;
			IL_00d9:
			ushort num7 = ReadUInt16BE(span4, num6);
			ushort num8 = ReadUInt16BE(span3, num6);
			if (num7 == 0)
			{
				glyphIds[i] = (ushort)((num3 + num8) & 0xFFFF);
				continue;
			}
			int num9 = ReadUInt16BE(span, num6);
			int num10 = num7 / 2;
			int num11 = num3 - num9 + num10 - (_segCount - num6);
			if ((uint)num11 < (uint)num)
			{
				ushort num12 = ReadUInt16BE(span5, num11);
				if (num12 != 0)
				{
					num12 = (ushort)((num12 + num8) & 0xFFFF);
				}
				glyphIds[i] = num12;
			}
			else
			{
				glyphIds[i] = 0;
			}
		}
	}

	public bool TryGetGlyph(int codePoint, out ushort glyphId)
	{
		int num = FindSegmentIndex(codePoint);
		if ((uint)num >= (uint)_segCount)
		{
			glyphId = 0;
			return false;
		}
		ushort num2 = ReadUInt16BE(_idRangeOffsets.Span, num);
		ushort num3 = ReadUInt16BE(_idDeltas.Span, num);
		if (num2 == 0)
		{
			glyphId = (ushort)((codePoint + num3) & 0xFFFF);
			return glyphId != 0;
		}
		int num4 = ReadUInt16BE(_startCodes.Span, num);
		int num5 = num2 >> 1;
		int num6 = codePoint - num4 + num5 - (_segCount - num);
		if ((uint)num6 >= (uint)(_glyphIdArray.Length >> 1))
		{
			glyphId = 0;
			return false;
		}
		glyphId = ReadUInt16BE(_glyphIdArray.Span, num6);
		if (glyphId != 0)
		{
			glyphId = (ushort)((glyphId + num3) & 0xFFFF);
		}
		return glyphId != 0;
	}

	internal bool TryGetRange(int index, out CodepointRange range)
	{
		if ((uint)index >= (uint)_segCount)
		{
			range = default(CodepointRange);
			return false;
		}
		int num = ReadUInt16BE(_startCodes.Span, index);
		int num2 = ReadUInt16BE(_endCodes.Span, index);
		if (num == 65535 && num2 == 65535)
		{
			range = default(CodepointRange);
			return false;
		}
		range = new CodepointRange(num, num2);
		return true;
	}

	private ushort ResolveGlyph(int segmentIndex, int codePoint)
	{
		ushort num = ReadUInt16BE(_idRangeOffsets.Span, segmentIndex);
		ushort num2 = ReadUInt16BE(_idDeltas.Span, segmentIndex);
		if (num == 0)
		{
			return (ushort)((codePoint + num2) & 0xFFFF);
		}
		int num3 = ReadUInt16BE(_startCodes.Span, segmentIndex);
		int num4 = num / 2;
		int num5 = codePoint - num3 + num4 - (_segCount - segmentIndex);
		int num6 = _glyphIdArray.Length / 2;
		if ((uint)num5 < (uint)num6)
		{
			ushort num7 = ReadUInt16BE(_glyphIdArray.Span, num5);
			if (num7 != 0)
			{
				num7 = (ushort)((num7 + num2) & 0xFFFF);
			}
			return num7;
		}
		return 0;
	}

	private int FindSegmentIndex(int codePoint)
	{
		int num = 0;
		int num2 = _segCount - 1;
		ReadOnlySpan<byte> span = _startCodes.Span;
		ReadOnlySpan<byte> span2 = _endCodes.Span;
		while (num <= num2)
		{
			int num3 = num + num2 >> 1;
			int num4 = ReadUInt16BE(span2, num3);
			if (codePoint > num4)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		if (num < _segCount)
		{
			int num5 = ReadUInt16BE(span, num);
			if (codePoint >= num5)
			{
				return num;
			}
		}
		return -1;
	}

	private int FindSegmentIndexOptimized(int codePoint, ReadOnlySpan<byte> startCodes, ReadOnlySpan<byte> endCodes)
	{
		int num = 0;
		int num2 = _segCount - 1;
		while (num <= num2)
		{
			int num3 = num + num2 >> 1;
			int num4 = ReadUInt16BE(endCodes, num3);
			if (codePoint > num4)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		if (num < _segCount)
		{
			int num5 = ReadUInt16BE(startCodes, num);
			if (codePoint >= num5)
			{
				return num;
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ushort ReadUInt16BE(ReadOnlySpan<byte> span, int wordIndex)
	{
		int start = wordIndex * 2;
		return BinaryPrimitives.ReadUInt16BigEndian(span.Slice(start, 2));
	}
}
