using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Rendering.Composition.Drawing;

internal struct RenderDataWriter : IDisposable
{
	private byte[]? _buffer;

	private int _length;

	public int Length => _length;

	public ReadOnlySpan<byte> Written => (_buffer == null) ? default(Span<byte>) : _buffer.AsSpan(0, _length);

	private Span<byte> Advance(int size)
	{
		int num = _length + size;
		if (_buffer == null)
		{
			_buffer = ArrayPool<byte>.Shared.Rent(Math.Max(num, 256));
		}
		else if (_buffer.Length < num)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent(Math.Max(num, _buffer.Length * 2));
			Array.Copy(_buffer, array, _length);
			ArrayPool<byte>.Shared.Return(_buffer);
			_buffer = array;
		}
		Span<byte> result = _buffer.AsSpan(_length, size);
		_length += size;
		return result;
	}

	public Span<byte> Reserve(int count)
	{
		return Advance(count);
	}

	public void Rewind(int length)
	{
		_length = length;
	}

	public void Write<T>(T value) where T : unmanaged
	{
		MemoryMarshal.Write(Advance(Unsafe.SizeOf<T>()), in value);
	}

	public void WriteOpcode(RenderDataOpcode opcode)
	{
		Write(opcode);
	}

	public void WritePayload<T>(T payload) where T : unmanaged, IRenderDataPayload<T>
	{
		Write(T.Opcode);
		Write(payload);
	}

	public void Dispose()
	{
		if (_buffer != null)
		{
			ArrayPool<byte>.Shared.Return(_buffer);
		}
		_buffer = null;
		_length = 0;
	}
}
