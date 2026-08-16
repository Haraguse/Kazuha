namespace Avalonia.Media.Fonts.Tables;

internal readonly struct VerticalHeaderTable
{
	internal const string TableName = "vhea";

	internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse("vhea");

	public FontVersion Version { get; }

	public short Ascender { get; }

	public short Descender { get; }

	public short LineGap { get; }

	public ushort AdvanceHeightMax { get; }

	public short MinTopSideBearing { get; }

	public short MinBottomSideBearing { get; }

	public short YMaxExtent { get; }

	public short CaretSlopeRise { get; }

	public short CaretSlopeRun { get; }

	public short CaretOffset { get; }

	public ushort NumberOfVMetrics { get; }

	public VerticalHeaderTable(FontVersion version, short ascender, short descender, short lineGap, ushort advanceHeightMax, short minTopSideBearing, short minBottomSideBearing, short yMaxExtent, short caretSlopeRise, short caretSlopeRun, short caretOffset, ushort numberOfVMetrics)
	{
		Version = version;
		Ascender = ascender;
		Descender = descender;
		LineGap = lineGap;
		AdvanceHeightMax = advanceHeightMax;
		MinTopSideBearing = minTopSideBearing;
		MinBottomSideBearing = minBottomSideBearing;
		YMaxExtent = yMaxExtent;
		CaretSlopeRise = caretSlopeRise;
		CaretSlopeRun = caretSlopeRun;
		CaretOffset = caretOffset;
		NumberOfVMetrics = numberOfVMetrics;
	}

	public static bool TryLoad(GlyphTypeface fontFace, out VerticalHeaderTable verticalHeaderTable)
	{
		verticalHeaderTable = default(VerticalHeaderTable);
		if (!fontFace.PlatformTypeface.TryGetTable(Tag, out var table))
		{
			return false;
		}
		BigEndianBinaryReader reader = new BigEndianBinaryReader(table.Span);
		return TryLoad(ref reader, out verticalHeaderTable);
	}

	private static bool TryLoad(ref BigEndianBinaryReader reader, out VerticalHeaderTable verticalHeaderTable)
	{
		verticalHeaderTable = default(VerticalHeaderTable);
		FontVersion version = reader.ReadVersion16Dot16();
		short ascender = reader.ReadFWORD();
		short descender = reader.ReadFWORD();
		short lineGap = reader.ReadFWORD();
		ushort advanceHeightMax = reader.ReadUFWORD();
		short minTopSideBearing = reader.ReadFWORD();
		short minBottomSideBearing = reader.ReadFWORD();
		short yMaxExtent = reader.ReadFWORD();
		short caretSlopeRise = reader.ReadInt16();
		short caretSlopeRun = reader.ReadInt16();
		short caretOffset = reader.ReadInt16();
		reader.ReadInt16();
		reader.ReadInt16();
		reader.ReadInt16();
		reader.ReadInt16();
		if (reader.ReadInt16() != 0)
		{
			return false;
		}
		ushort numberOfVMetrics = reader.ReadUInt16();
		verticalHeaderTable = new VerticalHeaderTable(version, ascender, descender, lineGap, advanceHeightMax, minTopSideBearing, minBottomSideBearing, yMaxExtent, caretSlopeRise, caretSlopeRun, caretOffset, numberOfVMetrics);
		return true;
	}
}
