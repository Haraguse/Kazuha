using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Avalonia.Media.Fonts;

/// <summary>
/// Represents a memory manager for unmanaged font data, providing functionality to access and manage font memory
/// and OpenType table data.
/// </summary>
/// <remarks>This class encapsulates unmanaged memory containing font data and provides methods to
/// retrieve specific OpenType table data. It ensures thread-safe access to the memory and supports pinning for
/// interoperability scenarios. Instances of this class must be properly disposed to release unmanaged
/// resources.</remarks>
internal sealed class UnmanagedFontMemory : MemoryManager<byte>, IFontMemory, IDisposable
{
	private nint _ptr;

	private int _length;

	private int _pinCount;

	private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

	/// <summary>
	/// Represents a cache of font table data, where each entry maps an OpenType tag to its corresponding byte data.
	/// </summary>
	/// <remarks>This dictionary is used to store preloaded font table data for efficient access.  The
	/// keys are OpenType tags, which identify specific font tables, and the values are the corresponding  byte data
	/// stored as read-only memory. This ensures that the data cannot be modified after being loaded into the
	/// cache.</remarks>
	private readonly Dictionary<OpenTypeTag, ReadOnlyMemory<byte>> _tableCache = new Dictionary<OpenTypeTag, ReadOnlyMemory<byte>>();

	private UnmanagedFontMemory(nint ptr, int length)
	{
		_ptr = ptr;
		_length = length;
	}

	/// <summary>
	/// Attempts to retrieve the memory region corresponding to the specified OpenType table tag.
	/// </summary>
	/// <remarks>This method searches for the specified OpenType table in the font data and retrieves
	/// its memory region if found. The method performs bounds checks to ensure the requested table is valid and
	/// safely accessible. If the table is not found or the font data is invalid, the method returns <see langword="false" />.</remarks>
	/// <param name="tag">The <see cref="T:Avalonia.Media.Fonts.OpenTypeTag" /> identifying the table to retrieve. Must not be <see cref="F:Avalonia.Media.Fonts.OpenTypeTag.None" />.</param>
	/// <param name="table">When this method returns, contains the memory region of the requested table if the operation succeeds;
	/// otherwise, contains the default value.</param>
	/// <returns><see langword="true" /> if the table memory was successfully retrieved; otherwise, <see langword="false" />.</returns>
	/// <exception cref="T:System.ObjectDisposedException">Thrown if the font memory has been disposed.</exception>
	public bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table)
	{
		table = default(ReadOnlyMemory<byte>);
		if (tag == OpenTypeTag.None)
		{
			return false;
		}
		_lock.EnterUpgradeableReadLock();
		try
		{
			if (_ptr == IntPtr.Zero || _length < 12)
			{
				return false;
			}
			Memory<byte> memory = Memory;
			Span<byte> span = memory.Span;
			if (span.Length < 12)
			{
				return false;
			}
			if (_tableCache.TryGetValue(tag, out var value))
			{
				table = value;
				return true;
			}
			ushort num = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4, 2));
			int num2 = 12;
			int num3 = checked(num2 + num * 16);
			if (span.Length < num3)
			{
				return false;
			}
			for (int i = 0; i < num; i++)
			{
				int start = num2 + i * 16;
				Span<byte> span2 = span.Slice(start, 16);
				if (BinaryPrimitives.ReadUInt32BigEndian(span2.Slice(0, 4)) != tag)
				{
					continue;
				}
				uint num4 = BinaryPrimitives.ReadUInt32BigEndian(span2.Slice(8, 4));
				uint num5 = BinaryPrimitives.ReadUInt32BigEndian(span2.Slice(12, 4));
				if (num4 > (uint)span.Length || num5 > (uint)span.Length)
				{
					return false;
				}
				if (num4 + num5 > (uint)span.Length)
				{
					return false;
				}
				memory = Memory;
				table = memory.Slice((int)num4, (int)num5);
				_lock.EnterWriteLock();
				try
				{
					_tableCache[tag] = table;
					return true;
				}
				finally
				{
					_lock.ExitWriteLock();
				}
			}
			return false;
		}
		finally
		{
			_lock.ExitUpgradeableReadLock();
		}
	}

	/// <summary>
	/// Loads font data from the specified stream into unmanaged memory.
	/// </summary>
	public static UnmanagedFontMemory LoadFromStream(Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (!stream.CanRead)
		{
			throw new ArgumentException("Stream is not readable", "stream");
		}
		if (stream.CanSeek)
		{
			int num = checked((int)stream.Length);
			nint num2 = Marshal.AllocHGlobal(num);
			byte[] array = ArrayPool<byte>.Shared.Rent(8192);
			try
			{
				int num3 = num;
				int num4 = 0;
				while (num3 > 0)
				{
					int count = Math.Min(array.Length, num3);
					int num5 = stream.Read(array, 0, count);
					if (num5 == 0)
					{
						break;
					}
					Marshal.Copy(array, 0, num2 + num4, num5);
					num4 += num5;
					num3 -= num5;
				}
				return new UnmanagedFontMemory(num2, num4);
			}
			catch
			{
				Marshal.FreeHGlobal(num2);
				throw;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
		using MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		int length = checked((int)memoryStream.Length);
		return CreateFromBytes(new ReadOnlySpan<byte>(memoryStream.GetBuffer(), 0, length));
	}

	/// <summary>
	/// Creates an instance of <see cref="T:Avalonia.Media.Fonts.UnmanagedFontMemory" /> from the specified byte data.
	/// </summary>
	/// <remarks>The method allocates unmanaged memory to store the provided byte data. The caller is
	/// responsible for ensuring that the returned <see cref="T:Avalonia.Media.Fonts.UnmanagedFontMemory" /> instance is properly disposed
	/// to release the allocated memory.</remarks>
	/// <param name="data">A read-only span of bytes representing the font data. The span must not be empty.</param>
	/// <returns>An instance of <see cref="T:Avalonia.Media.Fonts.UnmanagedFontMemory" /> that encapsulates the unmanaged memory containing the font
	/// data.</returns>
	private unsafe static UnmanagedFontMemory CreateFromBytes(ReadOnlySpan<byte> data)
	{
		int length = data.Length;
		nint num = Marshal.AllocHGlobal(length);
		try
		{
			if (length > 0)
			{
				data.CopyTo(new Span<byte>((void*)num, length));
			}
			return new UnmanagedFontMemory(num, length);
		}
		catch
		{
			Marshal.FreeHGlobal(num);
			throw;
		}
	}

	public unsafe override Span<byte> GetSpan()
	{
		_lock.EnterReadLock();
		try
		{
			if (_ptr == IntPtr.Zero || _length <= 0)
			{
				return Span<byte>.Empty;
			}
			return new Span<byte>(((IntPtr)_ptr).ToPointer(), _length);
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}

	public unsafe override MemoryHandle Pin(int elementIndex = 0)
	{
		if (elementIndex < 0)
		{
			throw new ArgumentOutOfRangeException("elementIndex");
		}
		Interlocked.Increment(ref _pinCount);
		_lock.EnterReadLock();
		try
		{
			if (_ptr == IntPtr.Zero || _length == 0)
			{
				return default(MemoryHandle);
			}
			if (elementIndex > _length)
			{
				throw new ArgumentOutOfRangeException("elementIndex");
			}
			return new MemoryHandle((byte*)((IntPtr)_ptr).ToPointer() + elementIndex);
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}

	public override void Unpin()
	{
		Interlocked.Decrement(ref _pinCount);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected override void Dispose(bool disposing)
	{
		_lock.EnterWriteLock();
		try
		{
			if (Volatile.Read(in _pinCount) > 0)
			{
				throw new InvalidOperationException("Cannot dispose while memory is pinned.");
			}
			if (_ptr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_ptr);
				_ptr = IntPtr.Zero;
			}
			_length = 0;
		}
		finally
		{
			_lock.ExitWriteLock();
			_lock.Dispose();
		}
	}
}
