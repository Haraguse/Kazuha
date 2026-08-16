namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Represents the PANOSE classification for a font.
/// PANOSE is a font classification system that describes the visual characteristics of a typeface.
/// </summary>
/// <remarks>
/// The interpretation of bytes 1-9 depends on the FamilyKind (byte 0).
/// This struct represents the Latin Text interpretation (FamilyKind = 2), which is the most common.
/// For other family kinds, access the raw bytes via the indexer.
/// </remarks>
internal readonly struct Panose(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9)
{
	private readonly byte[] _data = new byte[10] { b0, b1, b2, b3, b4, b5, b6, b7, b8, b9 };

	/// <summary>
	/// Gets the family kind classification (byte 0).
	/// </summary>
	public PanoseFamilyKind FamilyKind => (PanoseFamilyKind)_data[0];

	/// <summary>
	/// Gets the serif style (byte 1) for Latin Text fonts.
	/// </summary>
	public PanoseSerifStyle SerifStyle => (PanoseSerifStyle)_data[1];

	/// <summary>
	/// Gets the weight (byte 2) for Latin Text fonts.
	/// </summary>
	public PanoseWeight Weight => (PanoseWeight)_data[2];

	/// <summary>
	/// Gets the proportion (byte 3) for Latin Text fonts.
	/// </summary>
	public PanoseProportion Proportion => (PanoseProportion)_data[3];

	/// <summary>
	/// Gets the contrast (byte 4) for Latin Text fonts.
	/// </summary>
	public PanoseContrast Contrast => (PanoseContrast)_data[4];

	/// <summary>
	/// Gets the stroke variation (byte 5) for Latin Text fonts.
	/// </summary>
	public PanoseStrokeVariation StrokeVariation => (PanoseStrokeVariation)_data[5];

	/// <summary>
	/// Gets the arm style (byte 6) for Latin Text fonts.
	/// </summary>
	public PanoseArmStyle ArmStyle => (PanoseArmStyle)_data[6];

	/// <summary>
	/// Gets the letterform (byte 7) for Latin Text fonts.
	/// </summary>
	public PanoseLetterform Letterform => (PanoseLetterform)_data[7];

	/// <summary>
	/// Gets the midline (byte 8) for Latin Text fonts.
	/// </summary>
	public PanoseMidline Midline => (PanoseMidline)_data[8];

	/// <summary>
	/// Gets the x-height (byte 9) for Latin Text fonts.
	/// </summary>
	public PanoseXHeight XHeight => (PanoseXHeight)_data[9];

	public static Panose Load(ref BigEndianBinaryReader reader)
	{
		return new Panose(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
	}
}
