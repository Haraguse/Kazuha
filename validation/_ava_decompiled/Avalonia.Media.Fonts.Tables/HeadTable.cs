using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables;

internal sealed class HeadTable
{
	internal const string TableName = "head";

	private static readonly DateTime s_fontEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse("head");

	public FontVersion Version { get; }

	public FontVersion FontRevision { get; }

	public uint CheckSumAdjustment { get; }

	public uint MagicNumber { get; }

	public HeadFlags Flags { get; }

	public ushort UnitsPerEm { get; }

	public DateTime Created { get; }

	public DateTime Modified { get; }

	public short XMin { get; }

	public short YMin { get; }

	public short XMax { get; }

	public short YMax { get; }

	public MacStyleFlags MacStyle { get; }

	public ushort LowestRecPPEM { get; }

	public FontDirectionHint FontDirectionHint { get; }

	public IndexToLocFormat IndexToLocFormat { get; }

	public GlyphDataFormat GlyphDataFormat { get; }

	private HeadTable(FontVersion version, FontVersion fontRevision, uint checkSumAdjustment, uint magicNumber, HeadFlags flags, ushort unitsPerEm, DateTime created, DateTime modified, short xMin, short yMin, short xMax, short yMax, MacStyleFlags macStyle, ushort lowestRecPPEM, FontDirectionHint fontDirectionHint, IndexToLocFormat indexToLocFormat, GlyphDataFormat glyphDataFormat)
	{
		Version = version;
		FontRevision = fontRevision;
		CheckSumAdjustment = checkSumAdjustment;
		MagicNumber = magicNumber;
		Flags = flags;
		UnitsPerEm = unitsPerEm;
		Created = created;
		Modified = modified;
		XMin = xMin;
		YMin = yMin;
		XMax = xMax;
		YMax = yMax;
		MacStyle = macStyle;
		LowestRecPPEM = lowestRecPPEM;
		FontDirectionHint = fontDirectionHint;
		IndexToLocFormat = indexToLocFormat;
		GlyphDataFormat = glyphDataFormat;
	}

	public static bool TryLoad(GlyphTypeface glyphTypeface, [NotNullWhen(true)] out HeadTable? headTable)
	{
		headTable = null;
		if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
		{
			return false;
		}
		BigEndianBinaryReader reader = new BigEndianBinaryReader(table.Span);
		headTable = Load(ref reader);
		return true;
	}

	private static HeadTable Load(ref BigEndianBinaryReader reader)
	{
		FontVersion version = reader.ReadVersion16Dot16();
		FontVersion fontRevision = reader.ReadVersion16Dot16();
		uint checkSumAdjustment = reader.ReadUInt32();
		uint magicNumber = reader.ReadUInt32();
		HeadFlags flags = (HeadFlags)reader.ReadUInt16();
		ushort unitsPerEm = reader.ReadUInt16();
		long seconds = reader.ReadInt64();
		long seconds2 = reader.ReadInt64();
		short xMin = reader.ReadInt16();
		short yMin = reader.ReadInt16();
		short xMax = reader.ReadInt16();
		short yMax = reader.ReadInt16();
		MacStyleFlags macStyle = (MacStyleFlags)reader.ReadUInt16();
		ushort lowestRecPPEM = reader.ReadUInt16();
		FontDirectionHint fontDirectionHint = (FontDirectionHint)reader.ReadInt16();
		IndexToLocFormat indexToLocFormat = (IndexToLocFormat)reader.ReadInt16();
		GlyphDataFormat glyphDataFormat = (GlyphDataFormat)reader.ReadInt16();
		DateTime created = SafeAddSeconds(s_fontEpoch, seconds);
		DateTime modified = SafeAddSeconds(s_fontEpoch, seconds2);
		return new HeadTable(version, fontRevision, checkSumAdjustment, magicNumber, flags, unitsPerEm, created, modified, xMin, yMin, xMax, yMax, macStyle, lowestRecPPEM, fontDirectionHint, indexToLocFormat, glyphDataFormat);
	}

	private static DateTime SafeAddSeconds(DateTime epoch, long seconds)
	{
		try
		{
			if (seconds < 0)
			{
				long num = (long)(DateTime.MinValue - epoch).TotalSeconds;
				if (seconds < num)
				{
					return DateTime.MinValue;
				}
			}
			else
			{
				long num2 = (long)(DateTime.MaxValue - epoch).TotalSeconds;
				if (seconds > num2)
				{
					return DateTime.MaxValue;
				}
			}
			return epoch.AddSeconds(seconds);
		}
		catch (ArgumentOutOfRangeException)
		{
			return (seconds < 0) ? DateTime.MinValue : DateTime.MaxValue;
		}
	}
}
