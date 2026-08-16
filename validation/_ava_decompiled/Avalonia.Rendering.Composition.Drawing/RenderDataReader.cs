using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Rendering.Composition.Drawing;

internal ref struct RenderDataReader(ReadOnlySpan<byte> buffer)
{
	private readonly ReadOnlySpan<byte> _buffer = buffer;

	private int _position = 0;

	public int Position => _position;

	public bool IsAtEnd => _position >= _buffer.Length;

	public ReadOnlySpan<byte> Take(int count)
	{
		ReadOnlySpan<byte> result = _buffer.Slice(_position, count);
		_position += count;
		return result;
	}

	public T Read<T>() where T : unmanaged
	{
		return MemoryMarshal.Read<T>(Take(Unsafe.SizeOf<T>()));
	}

	public T Peek<T>() where T : unmanaged
	{
		return MemoryMarshal.Read<T>(_buffer.Slice(_position, Unsafe.SizeOf<T>()));
	}

	public T ReadPayload<T>() where T : unmanaged, IRenderDataPayload<T>
	{
		Read<RenderDataOpcode>();
		return Read<T>();
	}
}
