using System;

namespace Avalonia.Media.Fonts;

/// <summary>
/// Codepage coverage flags advertised by a font through the OpenType
/// <c>OS/2.ulCodePageRange1</c> and <c>ulCodePageRange2</c> bitfields.
/// </summary>
/// <remarks>
/// Bit positions match the OpenType specification verbatim. Bits 0..31 are
/// sourced from <c>ulCodePageRange1</c> and bits 32..63 from
/// <c>ulCodePageRange2</c>, so the full 64-bit value can be tested with a
/// single mask. See
/// <see href="https://learn.microsoft.com/typography/opentype/spec/os2#ulcodepagerange1-bits-031">
/// the OpenType OS/2 spec
/// </see>.
/// </remarks>
[Flags]
public enum FontCodePageCoverage : ulong
{
	/// <summary>No codepage coverage information is advertised.</summary>
	None = 0uL,
	/// <summary>1252 Latin 1.</summary>
	Latin1 = 1uL,
	/// <summary>1250 Latin 2: Eastern Europe.</summary>
	Latin2EasternEurope = 2uL,
	/// <summary>1251 Cyrillic.</summary>
	Cyrillic = 4uL,
	/// <summary>1253 Greek.</summary>
	Greek = 8uL,
	/// <summary>1254 Turkish.</summary>
	Turkish = 0x10uL,
	/// <summary>1255 Hebrew.</summary>
	Hebrew = 0x20uL,
	/// <summary>1256 Arabic.</summary>
	Arabic = 0x40uL,
	/// <summary>1257 Windows Baltic.</summary>
	WindowsBaltic = 0x80uL,
	/// <summary>1258 Vietnamese.</summary>
	Vietnamese = 0x100uL,
	/// <summary>874 Thai.</summary>
	Thai = 0x10000uL,
	/// <summary>932 JIS/Japan.</summary>
	JapaneseJis = 0x20000uL,
	/// <summary>936 Chinese: Simplified (PRC, Singapore).</summary>
	ChineseSimplified = 0x40000uL,
	/// <summary>949 Korean Wansung.</summary>
	KoreanWansung = 0x80000uL,
	/// <summary>950 Chinese: Traditional (Taiwan, Hong Kong SAR).</summary>
	ChineseTraditional = 0x100000uL,
	/// <summary>1361 Korean Johab.</summary>
	KoreanJohab = 0x200000uL,
	/// <summary>Macintosh Character Set (US Roman).</summary>
	MacRoman = 0x20000000uL,
	/// <summary>OEM Character Set.</summary>
	Oem = 0x40000000uL,
	/// <summary>Symbol Character Set.</summary>
	Symbol = 0x80000000uL,
	/// <summary>869 IBM Greek.</summary>
	Ibm869 = 0x1000000000000uL,
	/// <summary>866 MS-DOS Russian.</summary>
	Msdos866 = 0x2000000000000uL,
	/// <summary>865 MS-DOS Nordic.</summary>
	Msdos865 = 0x4000000000000uL,
	/// <summary>864 Arabic.</summary>
	Arabic864 = 0x8000000000000uL,
	/// <summary>863 MS-DOS Canadian French.</summary>
	Msdos863 = 0x10000000000000uL,
	/// <summary>862 Hebrew.</summary>
	Hebrew862 = 0x20000000000000uL,
	/// <summary>861 MS-DOS Icelandic.</summary>
	Msdos861 = 0x40000000000000uL,
	/// <summary>860 MS-DOS Portuguese.</summary>
	Msdos860 = 0x80000000000000uL,
	/// <summary>857 IBM Turkish.</summary>
	IbmTurkish857 = 0x100000000000000uL,
	/// <summary>855 IBM Cyrillic; primarily Russian.</summary>
	IbmCyrillic855 = 0x200000000000000uL,
	/// <summary>852 Latin 2.</summary>
	Latin2_852 = 0x400000000000000uL,
	/// <summary>775 MS-DOS Baltic.</summary>
	Msdos775 = 0x800000000000000uL,
	/// <summary>737 Greek (formerly 437G).</summary>
	Greek737 = 0x1000000000000000uL,
	/// <summary>708 Arabic ASMO 708.</summary>
	Arabic708 = 0x2000000000000000uL,
	/// <summary>850 WE/Latin 1.</summary>
	WeLatin1_850 = 0x4000000000000000uL,
	/// <summary>437 US.</summary>
	Us437 = 9223372036854775808uL
}
