using System;

namespace Avalonia.Media.Fonts.Tables;

internal readonly struct MaxpTable
{
	internal const string TableName = "maxp";

	internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse("maxp");

	public FontVersion Version { get; }

	public ushort NumGlyphs { get; }

	public ushort MaxPoints { get; }

	public ushort MaxContours { get; }

	public ushort MaxCompositePoints { get; }

	public ushort MaxCompositeContours { get; }

	public ushort MaxZones { get; }

	public ushort MaxTwilightPoints { get; }

	public ushort MaxStorage { get; }

	public ushort MaxFunctionDefs { get; }

	public ushort MaxInstructionDefs { get; }

	public ushort MaxStackElements { get; }

	public ushort MaxSizeOfInstructions { get; }

	public ushort MaxComponentElements { get; }

	public ushort MaxComponentDepth { get; }

	private MaxpTable(FontVersion version, ushort numGlyphs, ushort maxPoints, ushort maxContours, ushort maxCompositePoints, ushort maxCompositeContours, ushort maxZones, ushort maxTwilightPoints, ushort maxStorage, ushort maxFunctionDefs, ushort maxInstructionDefs, ushort maxStackElements, ushort maxSizeOfInstructions, ushort maxComponentElements, ushort maxComponentDepth)
	{
		Version = version;
		NumGlyphs = numGlyphs;
		MaxPoints = maxPoints;
		MaxContours = maxContours;
		MaxCompositePoints = maxCompositePoints;
		MaxCompositeContours = maxCompositeContours;
		MaxZones = maxZones;
		MaxTwilightPoints = maxTwilightPoints;
		MaxStorage = maxStorage;
		MaxFunctionDefs = maxFunctionDefs;
		MaxInstructionDefs = maxInstructionDefs;
		MaxStackElements = maxStackElements;
		MaxSizeOfInstructions = maxSizeOfInstructions;
		MaxComponentElements = maxComponentElements;
		MaxComponentDepth = maxComponentDepth;
	}

	public static MaxpTable Load(GlyphTypeface fontFace)
	{
		if (!fontFace.PlatformTypeface.TryGetTable(Tag, out var table))
		{
			throw new InvalidOperationException("Could not load the 'maxp' table.");
		}
		BigEndianBinaryReader reader = new BigEndianBinaryReader(table.Span);
		return Load(ref reader);
	}

	private static MaxpTable Load(ref BigEndianBinaryReader reader)
	{
		FontVersion version = reader.ReadVersion16Dot16();
		ushort numGlyphs = reader.ReadUInt16();
		if (version.Major < 1)
		{
			return new MaxpTable(version, numGlyphs, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		}
		ushort maxPoints = reader.ReadUInt16();
		ushort maxContours = reader.ReadUInt16();
		ushort maxCompositePoints = reader.ReadUInt16();
		ushort maxCompositeContours = reader.ReadUInt16();
		ushort maxZones = reader.ReadUInt16();
		ushort maxTwilightPoints = reader.ReadUInt16();
		ushort maxStorage = reader.ReadUInt16();
		ushort maxFunctionDefs = reader.ReadUInt16();
		ushort maxInstructionDefs = reader.ReadUInt16();
		ushort maxStackElements = reader.ReadUInt16();
		ushort maxSizeOfInstructions = reader.ReadUInt16();
		ushort maxComponentElements = reader.ReadUInt16();
		ushort maxComponentDepth = reader.ReadUInt16();
		return new MaxpTable(version, numGlyphs, maxPoints, maxContours, maxCompositePoints, maxCompositeContours, maxZones, maxTwilightPoints, maxStorage, maxFunctionDefs, maxInstructionDefs, maxStackElements, maxSizeOfInstructions, maxComponentElements, maxComponentDepth);
	}
}
