namespace Avalonia.Media.Fonts.Tables;

internal readonly struct PostTable
{
	internal const string TableName = "post";

	internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse("post");

	public FontVersion Version { get; }

	public float ItalicAngle { get; }

	public short UnderlinePosition { get; }

	public short UnderlineThickness { get; }

	public bool IsFixedPitch { get; }

	private PostTable(FontVersion version, float italicAngle, short underlinePosition, short underlineThickness, bool isFixedPitch)
	{
		Version = version;
		ItalicAngle = italicAngle;
		UnderlinePosition = underlinePosition;
		UnderlineThickness = underlineThickness;
		IsFixedPitch = isFixedPitch;
	}

	public static PostTable Load(GlyphTypeface glyphTypeface)
	{
		if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
		{
			return default(PostTable);
		}
		BigEndianBinaryReader reader = new BigEndianBinaryReader(table.Span);
		return Load(ref reader);
	}

	private static PostTable Load(ref BigEndianBinaryReader reader)
	{
		FontVersion version = reader.ReadVersion16Dot16();
		float italicAngle = reader.ReadFixed();
		short underlinePosition = reader.ReadFWORD();
		short underlineThickness = reader.ReadFWORD();
		uint num = reader.ReadUInt32();
		return new PostTable(version, italicAngle, underlinePosition, underlineThickness, num != 0);
	}
}
