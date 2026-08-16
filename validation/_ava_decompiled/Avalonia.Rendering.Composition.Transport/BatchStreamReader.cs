using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Rendering.Composition.Transport;

internal class BatchStreamReader : IDisposable
{
	private readonly BatchStreamData _input;

	private readonly BatchStreamMemoryPool _memoryPool;

	private readonly BatchStreamObjectPool<object?> _objectPool;

	private BatchStreamSegment<object?[]?> _currentObjectSegment;

	private BatchStreamSegment<nint> _currentDataSegment;

	private int _memoryOffset;

	private int _objectOffset;

	public bool IsObjectEof
	{
		get
		{
			if (_currentObjectSegment.Data == null)
			{
				return _input.Objects.Count == 0;
			}
			return false;
		}
	}

	public bool IsStructEof
	{
		get
		{
			if (_currentDataSegment.Data == IntPtr.Zero)
			{
				return _input.Structs.Count == 0;
			}
			return false;
		}
	}

	public BatchStreamReader(BatchStreamData input, BatchStreamMemoryPool memoryPool, BatchStreamObjectPool<object?> objectPool)
	{
		_input = input;
		_memoryPool = memoryPool;
		_objectPool = objectPool;
	}

	public unsafe T Read<T>() where T : unmanaged
	{
		int num = Unsafe.SizeOf<T>();
		if (_currentDataSegment.Data == IntPtr.Zero)
		{
			if (_input.Structs.Count == 0)
			{
				throw new EndOfStreamException();
			}
			_currentDataSegment = _input.Structs.Dequeue();
			_memoryOffset = 0;
		}
		if (_memoryOffset + num > _currentDataSegment.ElementCount)
		{
			throw new InvalidOperationException("Attempted to read more memory then left in the current segment");
		}
		byte* ptr = (byte*)(_currentDataSegment.Data + _memoryOffset);
		T result = ((RuntimeInformation.ProcessArchitecture != Architecture.Arm) ? Unsafe.ReadUnaligned<T>(ptr) : UnalignedMemoryHelper.ReadUnaligned<T>(ptr));
		_memoryOffset += num;
		if (_memoryOffset == _currentDataSegment.ElementCount)
		{
			_memoryPool.Return(_currentDataSegment.Data);
			_currentDataSegment = default(BatchStreamSegment<nint>);
		}
		return result;
	}

	public unsafe void Read(Span<byte> destination)
	{
		while (destination.Length > 0)
		{
			if (_currentDataSegment.Data == IntPtr.Zero)
			{
				if (_input.Structs.Count == 0)
				{
					throw new EndOfStreamException();
				}
				_currentDataSegment = _input.Structs.Dequeue();
				_memoryOffset = 0;
			}
			int num = Math.Min(_currentDataSegment.ElementCount - _memoryOffset, destination.Length);
			new ReadOnlySpan<byte>((void*)(_currentDataSegment.Data + _memoryOffset), num).CopyTo(destination);
			_memoryOffset += num;
			destination = destination.Slice(num);
			if (_memoryOffset == _currentDataSegment.ElementCount)
			{
				_memoryPool.Return(_currentDataSegment.Data);
				_currentDataSegment = default(BatchStreamSegment<nint>);
			}
		}
	}

	public T ReadObject<T>() where T : class?
	{
		return (T)ReadObject();
	}

	public object? ReadObject()
	{
		if (_currentObjectSegment.Data == null)
		{
			if (_input.Objects.Count == 0)
			{
				throw new EndOfStreamException();
			}
			_currentObjectSegment = _input.Objects.Dequeue();
			_objectOffset = 0;
		}
		object? result = _currentObjectSegment.Data[_objectOffset];
		_objectOffset++;
		if (_objectOffset == _currentObjectSegment.ElementCount)
		{
			_objectPool.Return(_currentObjectSegment.Data);
			_currentObjectSegment = default(BatchStreamSegment<object[]>);
		}
		return result;
	}

	public void Dispose()
	{
		if (_currentDataSegment.Data != IntPtr.Zero)
		{
			_memoryPool.Return(_currentDataSegment.Data);
			_currentDataSegment = default(BatchStreamSegment<nint>);
		}
		while (_input.Structs.Count > 0)
		{
			_memoryPool.Return(_input.Structs.Dequeue().Data);
		}
		if (_currentObjectSegment.Data != null)
		{
			_objectPool.Return(_currentObjectSegment.Data);
			_currentObjectSegment = default(BatchStreamSegment<object[]>);
		}
		while (_input.Objects.Count > 0)
		{
			_objectPool.Return(_input.Objects.Dequeue().Data);
		}
	}
}
