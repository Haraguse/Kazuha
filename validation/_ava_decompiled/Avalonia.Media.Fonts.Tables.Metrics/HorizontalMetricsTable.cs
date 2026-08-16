using System;

namespace Avalonia.Media.Fonts.Tables.Metrics;

internal class HorizontalMetricsTable
{
	public const string TagName = "hmtx";

	private readonly ReadOnlyMemory<byte> _data;

	private readonly ushort _numOfHMetrics;

	private readonly int _numGlyphs;

	public static OpenTypeTag Tag { get; } = OpenTypeTag.Parse("hmtx");

	private HorizontalMetricsTable(ReadOnlyMemory<byte> data, ushort numOfHMetrics, int numGlyphs)
	{
		_data = data;
		_numOfHMetrics = numOfHMetrics;
		_numGlyphs = numGlyphs;
	}

	internal static HorizontalMetricsTable? Load(GlyphTypeface glyphTypeface, ushort numberOfHMetrics, int glyphCount)
	{
		if (glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
		{
			return new HorizontalMetricsTable(table, numberOfHMetrics, glyphCount);
		}
		return null;
	}

	/// <summary>
	/// Attempts to retrieve the horizontal glyph metrics for the specified glyph index.
	/// </summary>
	/// <param name="glyphIndex">The index of the glyph for which to retrieve metrics.</param>
	/// <param name="metric">When this method returns, contains the horizontal glyph metric if the glyph index is valid; otherwise, the default value.</param>
	/// <returns><c>true</c> if the glyph index is valid and metrics were retrieved; otherwise, <c>false</c>.</returns>
	public bool TryGetMetrics(ushort glyphIndex, out HorizontalGlyphMetric metric)
	{
		metric = default(HorizontalGlyphMetric);
		if (glyphIndex >= _numGlyphs)
		{
			return false;
		}
		BigEndianBinaryReader bigEndianBinaryReader = new BigEndianBinaryReader(_data.Span);
		if (glyphIndex < _numOfHMetrics)
		{
			bigEndianBinaryReader.Seek(glyphIndex * 4);
			ushort advanceWidth = bigEndianBinaryReader.ReadUInt16();
			short leftSideBearing = bigEndianBinaryReader.ReadInt16();
			metric = new HorizontalGlyphMetric(advanceWidth, leftSideBearing);
		}
		else
		{
			bigEndianBinaryReader.Seek((_numOfHMetrics - 1) * 4);
			ushort advanceWidth2 = bigEndianBinaryReader.ReadUInt16();
			int num = glyphIndex - _numOfHMetrics;
			int offset = _numOfHMetrics * 4 + num * 2;
			bigEndianBinaryReader.Seek(offset);
			short leftSideBearing2 = bigEndianBinaryReader.ReadInt16();
			metric = new HorizontalGlyphMetric(advanceWidth2, leftSideBearing2);
		}
		return true;
	}

	/// <summary>
	/// Attempts to retrieve the advance width for a single glyph.
	/// </summary>
	/// <param name="glyphIndex">Glyph index to query.</param>
	/// <param name="advance">When this method returns, contains the advance width if the glyph index is valid; otherwise, zero.</param>
	/// <returns><c>true</c> if the glyph index is valid and the advance was retrieved; otherwise, <c>false</c>.</returns>
	public bool TryGetAdvance(ushort glyphIndex, out ushort advance)
	{
		advance = 0;
		if (glyphIndex >= _numGlyphs)
		{
			return false;
		}
		BigEndianBinaryReader bigEndianBinaryReader = new BigEndianBinaryReader(_data.Span);
		if (glyphIndex < _numOfHMetrics)
		{
			bigEndianBinaryReader.Seek(glyphIndex * 4);
			advance = bigEndianBinaryReader.ReadUInt16();
		}
		else
		{
			bigEndianBinaryReader.Seek((_numOfHMetrics - 1) * 4);
			advance = bigEndianBinaryReader.ReadUInt16();
		}
		return true;
	}

	/// <summary>
	/// Attempts to retrieve advance widths for multiple glyphs in a single operation.
	/// </summary>
	/// <param name="glyphIndices">Read-only span of glyph indices to query.</param>
	/// <param name="advances">Output span to write the advance widths. Must be at least as long as <paramref name="glyphIndices" />.</param>
	/// <returns><c>true</c> if all glyph indices are valid and advances were retrieved; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// This method is more efficient than calling <see cref="M:Avalonia.Media.Fonts.Tables.Metrics.HorizontalMetricsTable.TryGetAdvance(System.UInt16,System.UInt16@)" /> multiple times as it reuses
	/// the same reader and span reference. If any glyph index is invalid, the method returns <c>false</c>
	/// and the contents of <paramref name="advances" /> are undefined.
	/// </remarks>
	public bool TryGetAdvances(ReadOnlySpan<ushort> glyphIndices, Span<ushort> advances)
	{
		if (advances.Length < glyphIndices.Length)
		{
			return false;
		}
		ReadOnlySpan<byte> span = _data.Span;
		BigEndianBinaryReader bigEndianBinaryReader = new BigEndianBinaryReader(span);
		ushort? num = null;
		for (int i = 0; i < glyphIndices.Length; i++)
		{
			ushort num2 = glyphIndices[i];
			if (num2 >= _numGlyphs)
			{
				return false;
			}
			if (num2 < _numOfHMetrics)
			{
				bigEndianBinaryReader.Seek(num2 * 4);
				advances[i] = bigEndianBinaryReader.ReadUInt16();
				continue;
			}
			if (!num.HasValue)
			{
				bigEndianBinaryReader.Seek((_numOfHMetrics - 1) * 4);
				num = bigEndianBinaryReader.ReadUInt16();
			}
			advances[i] = num.Value;
		}
		return true;
	}

	/// <summary>
	/// Attempts to retrieve horizontal glyph metrics for multiple glyphs in a single operation.
	/// </summary>
	/// <param name="glyphIndices">Read-only span of glyph indices to query.</param>
	/// <param name="metrics">Output span to write the metrics. Must be at least as long as <paramref name="glyphIndices" />.</param>
	/// <returns><c>true</c> if all glyph indices are valid and metrics were retrieved; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// This method is more efficient than calling <see cref="M:Avalonia.Media.Fonts.Tables.Metrics.HorizontalMetricsTable.TryGetMetrics(System.UInt16,Avalonia.Media.Fonts.Tables.Metrics.HorizontalGlyphMetric@)" /> multiple times as it reuses
	/// the same reader and span reference. If any glyph index is invalid, the method returns <c>false</c>
	/// and the contents of <paramref name="metrics" /> are undefined.
	/// </remarks>
	public bool TryGetMetrics(ReadOnlySpan<ushort> glyphIndices, Span<HorizontalGlyphMetric> metrics)
	{
		if (metrics.Length < glyphIndices.Length)
		{
			return false;
		}
		ReadOnlySpan<byte> span = _data.Span;
		BigEndianBinaryReader bigEndianBinaryReader = new BigEndianBinaryReader(span);
		ushort? num = null;
		for (int i = 0; i < glyphIndices.Length; i++)
		{
			ushort num2 = glyphIndices[i];
			if (num2 >= _numGlyphs)
			{
				return false;
			}
			if (num2 < _numOfHMetrics)
			{
				bigEndianBinaryReader.Seek(num2 * 4);
				ushort advanceWidth = bigEndianBinaryReader.ReadUInt16();
				short leftSideBearing = bigEndianBinaryReader.ReadInt16();
				metrics[i] = new HorizontalGlyphMetric(advanceWidth, leftSideBearing);
				continue;
			}
			if (!num.HasValue)
			{
				bigEndianBinaryReader.Seek((_numOfHMetrics - 1) * 4);
				num = bigEndianBinaryReader.ReadUInt16();
			}
			int num3 = num2 - _numOfHMetrics;
			int offset = _numOfHMetrics * 4 + num3 * 2;
			bigEndianBinaryReader.Seek(offset);
			short leftSideBearing2 = bigEndianBinaryReader.ReadInt16();
			metrics[i] = new HorizontalGlyphMetric(num.Value, leftSideBearing2);
		}
		return true;
	}
}
