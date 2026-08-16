using System;
using System.Text;

namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Parses the OpenType <c>meta</c> table (script and design language tags).
/// See <see href="https://learn.microsoft.com/typography/opentype/spec/meta" />.
/// </summary>
internal readonly struct MetaTable
{
	private const int HeaderSize = 16;

	private const int DataMapRecordSize = 12;

	internal const string TableName = "meta";

	private static readonly OpenTypeTag s_dlngTag = OpenTypeTag.Parse("dlng");

	private static readonly OpenTypeTag s_slngTag = OpenTypeTag.Parse("slng");

	internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse("meta");

	/// <summary>
	/// The BCP-47 language tags declared by the font under the <c>dlng</c> (design languages) data tag.
	/// </summary>
	public string[] DesignLanguages { get; }

	/// <summary>
	/// The BCP-47 language tags declared by the font under the <c>slng</c> (supported languages) data tag.
	/// </summary>
	public string[] SupportedLanguages { get; }

	private MetaTable(string[] designLanguages, string[] supportedLanguages)
	{
		DesignLanguages = designLanguages;
		SupportedLanguages = supportedLanguages;
	}

	public static bool TryLoad(GlyphTypeface glyphTypeface, out MetaTable metaTable)
	{
		metaTable = default(MetaTable);
		if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
		{
			return false;
		}
		return TryParse(table.Span, out metaTable);
	}

	internal static bool TryParse(ReadOnlySpan<byte> span, out MetaTable metaTable)
	{
		metaTable = default(MetaTable);
		if (span.Length < 16)
		{
			return false;
		}
		BigEndianBinaryReader bigEndianBinaryReader = new BigEndianBinaryReader(span);
		if (bigEndianBinaryReader.ReadUInt32() != 1)
		{
			return false;
		}
		bigEndianBinaryReader.ReadUInt32();
		bigEndianBinaryReader.ReadUInt32();
		uint num = bigEndianBinaryReader.ReadUInt32();
		if (num == 0)
		{
			metaTable = new MetaTable(Array.Empty<string>(), Array.Empty<string>());
			return true;
		}
		uint num2 = (uint)((span.Length - 16) / 12);
		if (num > num2)
		{
			return false;
		}
		string[] array = null;
		string[] array2 = null;
		for (int i = 0; i < num; i++)
		{
			OpenTypeTag openTypeTag = new OpenTypeTag(bigEndianBinaryReader.ReadUInt32());
			uint num3 = bigEndianBinaryReader.ReadUInt32();
			uint num4 = bigEndianBinaryReader.ReadUInt32();
			if ((openTypeTag != s_dlngTag && openTypeTag != s_slngTag) || num3 > (uint)span.Length || num4 > (uint)(span.Length - (int)num3))
			{
				continue;
			}
			string[] array3 = ParseLanguageTags(span.Slice((int)num3, (int)num4));
			if (openTypeTag == s_dlngTag)
			{
				if (array == null)
				{
					array = array3;
				}
			}
			else if (array2 == null)
			{
				array2 = array3;
			}
		}
		metaTable = new MetaTable(array ?? Array.Empty<string>(), array2 ?? Array.Empty<string>());
		return true;
	}

	private static string[] ParseLanguageTags(ReadOnlySpan<byte> data)
	{
		return Encoding.UTF8.GetString(data).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}
}
