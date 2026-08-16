using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Avalonia.Utilities;

public static class AvaloniaResourcesIndexReaderWriter
{
	private const int XmlLegacyVersion = 1;

	private const int BinaryCurrentVersion = 2;

	public static List<AvaloniaResourcesIndexEntry> ReadIndex(Stream stream)
	{
		using BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
		int num = binaryReader.ReadInt32();
		return num switch
		{
			1 => ReadXmlIndex(), 
			2 => ReadBinaryIndex(binaryReader), 
			_ => throw new Exception($"Unknown resources index format version {num}"), 
		};
	}

	private static List<AvaloniaResourcesIndexEntry> ReadXmlIndex()
	{
		throw new NotSupportedException("Found legacy resources index format: please recompile your XAML files");
	}

	private static List<AvaloniaResourcesIndexEntry> ReadBinaryIndex(BinaryReader reader)
	{
		int num = reader.ReadInt32();
		List<AvaloniaResourcesIndexEntry> list = new List<AvaloniaResourcesIndexEntry>(num);
		for (int i = 0; i < num; i++)
		{
			list.Add(new AvaloniaResourcesIndexEntry
			{
				Path = reader.ReadString(),
				Offset = reader.ReadInt32(),
				Size = reader.ReadInt32()
			});
		}
		return list;
	}

	public static void WriteIndex(Stream output, List<AvaloniaResourcesIndexEntry> entries)
	{
		using BinaryWriter writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
		WriteIndex(writer, entries);
	}

	private static void WriteIndex(BinaryWriter writer, List<AvaloniaResourcesIndexEntry> entries)
	{
		writer.Write(2);
		writer.Write(entries.Count);
		foreach (AvaloniaResourcesIndexEntry entry in entries)
		{
			writer.Write(entry.Path ?? string.Empty);
			writer.Write(entry.Offset);
			writer.Write(entry.Size);
		}
	}

	public static void WriteResources(Stream output, IReadOnlyList<AvaloniaResourcesEntry> resources)
	{
		List<AvaloniaResourcesIndexEntry> list = new List<AvaloniaResourcesIndexEntry>();
		Dictionary<string, (AvaloniaResourcesIndexEntry, Func<Stream>)> dictionary = new Dictionary<string, (AvaloniaResourcesIndexEntry, Func<Stream>)>();
		int num = 0;
		foreach (AvaloniaResourcesEntry resource in resources)
		{
			if (!string.IsNullOrEmpty(resource.SystemPath) && dictionary.TryGetValue(resource.SystemPath, out var value))
			{
				list.Add(new AvaloniaResourcesIndexEntry
				{
					Path = resource.Path,
					Offset = value.Item1.Offset,
					Size = value.Item1.Size
				});
			}
			else
			{
				AvaloniaResourcesIndexEntry avaloniaResourcesIndexEntry = new AvaloniaResourcesIndexEntry
				{
					Path = resource.Path,
					Offset = num,
					Size = resource.Size
				};
				dictionary[resource.SystemPath ?? num.ToString()] = (avaloniaResourcesIndexEntry, resource.Open);
				list.Add(avaloniaResourcesIndexEntry);
				num += resource.Size;
			}
		}
		using BinaryWriter binaryWriter = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
		binaryWriter.Write(0);
		long position = output.Position;
		WriteIndex(binaryWriter, list);
		long position2 = output.Position;
		int value2 = (int)(position2 - position);
		output.Position = 0L;
		binaryWriter.Write(value2);
		output.Position = position2;
		foreach (KeyValuePair<string, (AvaloniaResourcesIndexEntry, Func<Stream>)> item in dictionary)
		{
			using Stream stream = item.Value.Item2();
			stream.CopyTo(output);
		}
	}
}
