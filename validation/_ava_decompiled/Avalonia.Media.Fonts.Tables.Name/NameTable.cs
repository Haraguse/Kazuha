using System.Collections;
using System.Collections.Generic;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Media.Fonts.Tables.Name;

internal class NameTable : IEnumerable<NameRecord>, IEnumerable
{
	internal const string TableName = "name";

	internal static readonly OpenTypeTag Tag = OpenTypeTag.Parse("name");

	private const ushort USEnglishLanguageId = 1033;

	private readonly NameRecord[] _names;

	private string? _cachedFamilyName;

	private string? _cachedTypographicFamilyName;

	internal NameTable(NameRecord[] names)
	{
		_names = names;
	}

	/// <summary>
	/// Gets the name of the font.
	/// </summary>
	/// <value>
	/// The name of the font.
	/// </value>
	public string Id(ushort culture)
	{
		return GetNameById(culture, KnownNameIds.UniqueFontID);
	}

	/// <summary>
	/// Gets the name of the font.
	/// </summary>
	/// <value>
	/// The name of the font.
	/// </value>
	public string FontName(ushort culture)
	{
		return GetNameById(culture, KnownNameIds.FullFontName);
	}

	/// <summary>
	/// Gets the name of the font.
	/// </summary>
	/// <value>
	/// The name of the font.
	/// </value>
	public string FontFamilyName(ushort culture)
	{
		if (culture == 1033 && _cachedFamilyName != null)
		{
			return _cachedFamilyName;
		}
		string nameById = GetNameById(culture, KnownNameIds.FontFamilyName);
		if (culture == 1033)
		{
			_cachedFamilyName = nameById;
		}
		return nameById;
	}

	/// <summary>
	/// Gets the name of the font.
	/// </summary>
	/// <value>
	/// The name of the font.
	/// </value>
	public string FontSubFamilyName(ushort culture)
	{
		return GetNameById(culture, KnownNameIds.FontSubfamilyName);
	}

	public string GetNameById(ushort culture, KnownNameIds nameId)
	{
		if (nameId == KnownNameIds.TypographicFamilyName && culture == 1033 && _cachedTypographicFamilyName != null)
		{
			return _cachedTypographicFamilyName;
		}
		ushort num = culture;
		NameRecord? nameRecord = null;
		NameRecord? nameRecord2 = null;
		NameRecord? nameRecord3 = null;
		NameRecord[] names = _names;
		for (int i = 0; i < names.Length; i++)
		{
			NameRecord nameRecord4 = names[i];
			if (nameRecord4.NameID != nameId)
			{
				continue;
			}
			NameRecord valueOrDefault = nameRecord3.GetValueOrDefault();
			if (!nameRecord3.HasValue)
			{
				valueOrDefault = nameRecord4;
				nameRecord3 = valueOrDefault;
			}
			if (nameRecord4.Platform != PlatformID.Windows)
			{
				continue;
			}
			valueOrDefault = nameRecord2.GetValueOrDefault();
			if (!nameRecord2.HasValue)
			{
				valueOrDefault = nameRecord4;
				nameRecord2 = valueOrDefault;
			}
			if (nameRecord4.LanguageID == 1033)
			{
				valueOrDefault = nameRecord.GetValueOrDefault();
				if (!nameRecord.HasValue)
				{
					valueOrDefault = nameRecord4;
					nameRecord = valueOrDefault;
				}
			}
			if (nameRecord4.LanguageID == num)
			{
				return nameRecord4.GetValue();
			}
		}
		string text = nameRecord?.GetValue() ?? nameRecord2?.GetValue() ?? nameRecord3?.GetValue() ?? string.Empty;
		if (nameId == KnownNameIds.TypographicFamilyName && culture == 1033)
		{
			_cachedTypographicFamilyName = text;
		}
		return text;
	}

	public string GetNameById(ushort culture, ushort nameId)
	{
		return GetNameById(culture, (KnownNameIds)nameId);
	}

	public static NameTable? Load(GlyphTypeface glyphTypeface)
	{
		if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
		{
			return null;
		}
		BigEndianBinaryReader bigEndianBinaryReader = new BigEndianBinaryReader(table.Span);
		bigEndianBinaryReader.ReadUInt16();
		ushort num = bigEndianBinaryReader.ReadUInt16();
		ushort start = bigEndianBinaryReader.ReadUInt16();
		NameRecord[] array = new NameRecord[num];
		for (int i = 0; i < num; i++)
		{
			PlatformID platform = bigEndianBinaryReader.ReadUInt16<PlatformID>();
			Encoding encoding = bigEndianBinaryReader.ReadUInt16<EncodingIDs>().AsEncoding();
			ushort languageId = bigEndianBinaryReader.ReadUInt16();
			KnownNameIds nameId = bigEndianBinaryReader.ReadUInt16<KnownNameIds>();
			ushort length = bigEndianBinaryReader.ReadUInt16();
			ushort offset = bigEndianBinaryReader.ReadUInt16();
			array[i] = new NameRecord(table.Slice(start), platform, languageId, nameId, offset, length, encoding);
		}
		return new NameTable(array);
	}

	public IEnumerator<NameRecord> GetEnumerator()
	{
		return new ImmutableReadOnlyListStructEnumerator<NameRecord>(_names);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
