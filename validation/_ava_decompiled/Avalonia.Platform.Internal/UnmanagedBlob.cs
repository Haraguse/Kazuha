using System;
using System.Runtime.InteropServices;

namespace Avalonia.Platform.Internal;

internal class UnmanagedBlob : IDisposable
{
	private nint _address;

	private readonly object _lock = new object();

	public static bool SuppressFinalizerWarning { get; set; }

	public nint Address
	{
		get
		{
			if (!IsDisposed)
			{
				return _address;
			}
			throw new ObjectDisposedException("UnmanagedBlob");
		}
	}

	public int Size { get; private set; }

	public bool IsDisposed { get; private set; }

	public UnmanagedBlob(int size)
	{
		try
		{
			if (size <= 0)
			{
				throw new ArgumentException("Positive number required", "size");
			}
			_address = Alloc(size);
			GC.AddMemoryPressure(size);
			Size = size;
		}
		catch
		{
			GC.SuppressFinalize(this);
			throw;
		}
	}

	private void DoDispose()
	{
		lock (_lock)
		{
			if (!IsDisposed)
			{
				Free(_address, Size);
				GC.RemoveMemoryPressure(Size);
				IsDisposed = true;
				_address = IntPtr.Zero;
				Size = 0;
			}
		}
	}

	public void Dispose()
	{
		DoDispose();
		GC.SuppressFinalize(this);
	}

	~UnmanagedBlob()
	{
		DoDispose();
	}

	[DllImport("libc")]
	private static extern nint mmap(nint addr, nint length, int prot, int flags, int fd, nint offset);

	[DllImport("libc")]
	private static extern int munmap(nint addr, nint length);

	private nint Alloc(int size)
	{
		if (!OperatingSystem.IsLinux())
		{
			return Marshal.AllocHGlobal(size);
		}
		nint result = mmap(IntPtr.Zero, new IntPtr(size), 3, 34, -1, IntPtr.Zero);
		if (((IntPtr)result).ToInt64() == -1 || ((IntPtr)result).ToInt64() == uint.MaxValue)
		{
			throw new Exception("Unable to allocate memory: " + Marshal.GetLastSystemError());
		}
		return result;
	}

	private void Free(nint ptr, int len)
	{
		if (!OperatingSystem.IsLinux())
		{
			Marshal.FreeHGlobal(ptr);
		}
		else if (munmap(ptr, new IntPtr(len)) == -1)
		{
			throw new Exception("Unable to free memory: " + Marshal.GetLastSystemError());
		}
	}
}
