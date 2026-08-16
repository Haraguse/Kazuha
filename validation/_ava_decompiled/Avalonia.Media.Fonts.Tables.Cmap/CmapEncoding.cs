namespace Avalonia.Media.Fonts.Tables.Cmap;

internal enum CmapEncoding : ushort
{
	Unicode_1_0 = 0,
	Unicode_1_1 = 1,
	Unicode_ISO_10646 = 2,
	Unicode_2_0_BMP = 3,
	Unicode_2_0_full = 4,
	Macintosh_Roman = Unicode_1_0,
	Macintosh_Japanese = Unicode_1_1,
	Macintosh_ChineseTraditional = Unicode_ISO_10646,
	Macintosh_Korean = Unicode_2_0_BMP,
	Macintosh_Arabic = Unicode_2_0_full,
	Macintosh_Hebrew = 5,
	Macintosh_Greek = 6,
	Macintosh_Russian = 7,
	Macintosh_RSymbol = 8,
	Microsoft_Symbol = Unicode_1_0,
	Microsoft_UnicodeBMP = Unicode_1_1,
	Microsoft_ShiftJIS = Unicode_ISO_10646,
	Microsoft_PRChina = Unicode_2_0_BMP,
	Microsoft_Big5 = Unicode_2_0_full,
	Microsoft_Wansung = Macintosh_Hebrew,
	Microsoft_Johab = Macintosh_Greek,
	Microsoft_UCS4 = 10
}
