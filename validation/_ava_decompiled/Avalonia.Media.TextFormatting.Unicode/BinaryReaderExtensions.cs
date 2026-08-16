using System;
using System.IO;

namespace Avalonia.Media.TextFormatting.Unicode;

internal static class BinaryReaderExtensions
{
	public static int ReadInt32BE(this BinaryReader reader)
	{
		byte[] array = reader.ReadBytes(4);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(array);
		}
		return BitConverter.ToInt32(array, 0);
	}

	public static uint ReadUInt32BE(this BinaryReader reader)
	{
		byte[] array = reader.ReadBytes(4);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(array);
		}
		return BitConverter.ToUInt32(array, 0);
	}

	public static void WriteBE(this BinaryWriter writer, int value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		writer.Write(bytes);
	}

	public static void WriteBE(this BinaryWriter writer, uint value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		writer.Write(bytes);
	}
}
