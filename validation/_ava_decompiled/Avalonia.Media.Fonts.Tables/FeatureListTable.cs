using System;
using System.Collections.Generic;

namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Features provide information about how to use the glyphs in a font to render a script or language.
/// For example, an Arabic font might have a feature for substituting initial glyph forms, and a Kanji font
/// might have a feature for positioning glyphs vertically. All OpenType Layout features define data for
/// glyph substitution, glyph positioning, or both.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/featurelist" />
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-list-table" />
/// </summary>
internal class FeatureListTable
{
	private static OpenTypeTag GSubTag { get; } = OpenTypeTag.Parse("GSUB");

	private static OpenTypeTag GPosTag { get; } = OpenTypeTag.Parse("GPOS");

	public IReadOnlyList<OpenTypeTag> Features { get; }

	private FeatureListTable(IReadOnlyList<OpenTypeTag> features)
	{
		Features = features;
	}

	public static FeatureListTable? LoadGSub(GlyphTypeface glyphTypeface)
	{
		if (!glyphTypeface.PlatformTypeface.TryGetTable(GSubTag, out var table))
		{
			return null;
		}
		BigEndianBinaryReader reader = new BigEndianBinaryReader(table.Span);
		return Load(ref reader);
	}

	public static FeatureListTable? LoadGPos(GlyphTypeface glyphTypeface)
	{
		if (!glyphTypeface.PlatformTypeface.TryGetTable(GPosTag, out var table))
		{
			return null;
		}
		BigEndianBinaryReader reader = new BigEndianBinaryReader(table.Span);
		return Load(ref reader);
	}

	private static FeatureListTable Load(ref BigEndianBinaryReader reader)
	{
		reader.ReadUInt16();
		reader.ReadUInt16();
		reader.ReadOffset16();
		ushort offset = reader.ReadOffset16();
		return Load(ref reader, offset);
	}

	private static FeatureListTable Load(ref BigEndianBinaryReader reader, int offset)
	{
		reader.Seek(offset);
		ushort num = reader.ReadUInt16();
		if (num == 0)
		{
			return new FeatureListTable(Array.Empty<OpenTypeTag>());
		}
		Span<OpenTypeTag> span = ((num > 64) ? ((Span<OpenTypeTag>)new OpenTypeTag[num]) : stackalloc OpenTypeTag[(int)num]);
		Span<OpenTypeTag> span2 = span;
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			uint value = reader.ReadUInt32();
			reader.ReadOffset16();
			OpenTypeTag openTypeTag = new OpenTypeTag(value);
			if (!((ReadOnlySpan<OpenTypeTag>)span2).Contains(openTypeTag))
			{
				span2[num2++] = openTypeTag;
			}
		}
		OpenTypeTag[] array = new OpenTypeTag[num2];
		span2.Slice(0, num2).CopyTo(array);
		return new FeatureListTable(array);
	}
}
