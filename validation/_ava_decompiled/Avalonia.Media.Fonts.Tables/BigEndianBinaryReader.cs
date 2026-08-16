using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// BinaryReader using big-endian encoding for ReadOnlySpan&lt;byte&gt;.
/// </summary>
[DebuggerDisplay("Start: {StartOfSpan}, Position: {Position}")]
internal ref struct BigEndianBinaryReader
{
	private readonly ReadOnlySpan<byte> _span;

	private int _position;

	private readonly int _startOfSpan;

	private readonly int StartOfSpan => _startOfSpan;

	/// <summary>
	/// Gets the current position in the span.
	/// </summary>
	public readonly int Position => _position;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Fonts.Tables.BigEndianBinaryReader" /> class.
	/// </summary>
	/// <param name="span">Span to read data from</param>
	public BigEndianBinaryReader(ReadOnlySpan<byte> span)
	{
		_span = span;
		_position = 0;
		_startOfSpan = 0;
	}

	/// <summary>
	/// Seeks within the span.
	/// </summary>
	/// <param name="offset">Offset to seek to.</param>
	public void Seek(int offset)
	{
		int num = _startOfSpan + offset;
		if (offset < 0 || num > _span.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		_position = num;
	}

	public byte ReadByte()
	{
		EnsureAvailable(1);
		return _span[_position++];
	}

	public sbyte ReadSByte()
	{
		EnsureAvailable(1);
		return (sbyte)_span[_position++];
	}

	public float ReadF2dot14()
	{
		return (float)ReadInt16() / 16384f;
	}

	public short ReadInt16()
	{
		EnsureAvailable(2);
		short result = BinaryPrimitives.ReadInt16BigEndian(_span.Slice(_position, 2));
		_position += 2;
		return result;
	}

	public TEnum ReadInt16<TEnum>() where TEnum : struct, Enum
	{
		TryConvert<ushort, TEnum>(ReadUInt16(), out var value);
		return value;
	}

	public short ReadFWORD()
	{
		return ReadInt16();
	}

	public short[] ReadFWORDArray(int length)
	{
		return ReadInt16Array(length);
	}

	public ushort ReadUFWORD()
	{
		return ReadUInt16();
	}

	public float ReadFixed()
	{
		EnsureAvailable(4);
		float result = (float)BinaryPrimitives.ReadInt32BigEndian(_span.Slice(_position, 4)) / 65536f;
		_position += 4;
		return result;
	}

	public FontVersion ReadVersion16Dot16()
	{
		EnsureAvailable(4);
		uint value = BinaryPrimitives.ReadUInt32BigEndian(_span.Slice(_position, 4));
		_position += 4;
		return new FontVersion(value);
	}

	public int ReadInt32()
	{
		EnsureAvailable(4);
		int result = BinaryPrimitives.ReadInt32BigEndian(_span.Slice(_position, 4));
		_position += 4;
		return result;
	}

	public long ReadInt64()
	{
		EnsureAvailable(8);
		long result = BinaryPrimitives.ReadInt64BigEndian(_span.Slice(_position, 8));
		_position += 8;
		return result;
	}

	public ushort ReadUInt16()
	{
		EnsureAvailable(2);
		ushort result = BinaryPrimitives.ReadUInt16BigEndian(_span.Slice(_position, 2));
		_position += 2;
		return result;
	}

	public ushort ReadOffset16()
	{
		return ReadUInt16();
	}

	public TEnum ReadUInt16<TEnum>() where TEnum : struct, Enum
	{
		TryConvert<ushort, TEnum>(ReadUInt16(), out var value);
		return value;
	}

	public ushort[] ReadUInt16Array(int length)
	{
		ushort[] array = new ushort[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = ReadUInt16();
		}
		return array;
	}

	public void ReadUInt16Array(Span<ushort> buffer)
	{
		for (int i = 0; i < buffer.Length; i++)
		{
			buffer[i] = ReadUInt16();
		}
	}

	public uint[] ReadUInt32Array(int length)
	{
		uint[] array = new uint[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = ReadUInt32();
		}
		return array;
	}

	public byte[] ReadUInt8Array(int length)
	{
		byte[] array = new byte[length];
		ReadBytesInternal(array, length);
		return array;
	}

	public short[] ReadInt16Array(int length)
	{
		short[] array = new short[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = ReadInt16();
		}
		return array;
	}

	public void ReadInt16Array(Span<short> buffer)
	{
		for (int i = 0; i < buffer.Length; i++)
		{
			buffer[i] = ReadInt16();
		}
	}

	public byte ReadUInt8()
	{
		EnsureAvailable(1);
		return _span[_position++];
	}

	public int ReadUInt24()
	{
		return (ReadByte() << 16) | ReadUInt16();
	}

	public uint ReadUInt32()
	{
		EnsureAvailable(4);
		uint result = BinaryPrimitives.ReadUInt32BigEndian(_span.Slice(_position, 4));
		_position += 4;
		return result;
	}

	public uint ReadOffset32()
	{
		return ReadUInt32();
	}

	public byte[] ReadBytes(int count)
	{
		int num = Math.Min(count, _span.Length - _position);
		byte[] array = new byte[num];
		ReadBytesInternal(array, num);
		return array;
	}

	public string ReadString(int bytesToRead, Encoding encoding)
	{
		EnsureAvailable(bytesToRead);
		string result = encoding.GetString(_span.Slice(_position, bytesToRead));
		_position += bytesToRead;
		return result;
	}

	public string ReadTag()
	{
		EnsureAvailable(4);
		string result = Encoding.UTF8.GetString(_span.Slice(_position, 4));
		_position += 4;
		return result;
	}

	public int ReadOffset(int size)
	{
		return size switch
		{
			1 => ReadByte(), 
			2 => (ReadByte() << 8) | ReadByte(), 
			3 => (ReadByte() << 16) | (ReadByte() << 8) | ReadByte(), 
			4 => (ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte(), 
			_ => throw new InvalidOperationException(), 
		};
	}

	private void ReadBytesInternal(byte[] data, int size)
	{
		EnsureAvailable(size);
		_span.Slice(_position, size).CopyTo(data);
		_position += size;
	}

	private readonly void EnsureAvailable(int size)
	{
		if (_position + size > _span.Length)
		{
			throw new InvalidOperationException($"End of span reached with {size - (_span.Length - _position)} byte{((size - (_span.Length - _position) == 1) ? "s" : string.Empty)} left to read.");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvert<T, TEnum>(T input, out TEnum value) where T : struct, IConvertible, IFormattable, IComparable where TEnum : struct, Enum
	{
		if (Unsafe.SizeOf<T>() == Unsafe.SizeOf<TEnum>())
		{
			value = Unsafe.As<T, TEnum>(ref input);
			return true;
		}
		value = default(TEnum);
		return false;
	}
}
