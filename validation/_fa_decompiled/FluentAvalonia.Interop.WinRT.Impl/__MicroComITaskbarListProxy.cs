using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComITaskbarListProxy : MicroComProxyBase, ITaskbarList, IUnknown, IDisposable
{
	protected override int VTableSize => ((MicroComProxyBase)this).VTableSize + 5;

	public unsafe void HrInit()
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, int>)(*((MicroComProxyBase)this).PPV)[((MicroComProxyBase)this).VTableSize])(((MicroComProxyBase)this).PPV);
		if (num != 0)
		{
			throw new COMException("HrInit failed", num);
		}
	}

	public unsafe void AddTab(nint hwnd)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, int>)(*((MicroComProxyBase)this).PPV)[((MicroComProxyBase)this).VTableSize + 1])(((MicroComProxyBase)this).PPV, hwnd);
		if (num != 0)
		{
			throw new COMException("AddTab failed", num);
		}
	}

	public unsafe void DeleteTab(nint hwnd)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, int>)(*((MicroComProxyBase)this).PPV)[((MicroComProxyBase)this).VTableSize + 2])(((MicroComProxyBase)this).PPV, hwnd);
		if (num != 0)
		{
			throw new COMException("DeleteTab failed", num);
		}
	}

	public unsafe void ActivateTab(nint hwnd)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, int>)(*((MicroComProxyBase)this).PPV)[((MicroComProxyBase)this).VTableSize + 3])(((MicroComProxyBase)this).PPV, hwnd);
		if (num != 0)
		{
			throw new COMException("ActivateTab failed", num);
		}
	}

	public unsafe void SetActiveAlt(nint hwnd)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, int>)(*((MicroComProxyBase)this).PPV)[((MicroComProxyBase)this).VTableSize + 4])(((MicroComProxyBase)this).PPV, hwnd);
		if (num != 0)
		{
			throw new COMException("SetActiveAlt failed", num);
		}
	}

	[ModuleInitializer]
	internal static void __MicroComModuleInit()
	{
		MicroComRuntime.Register(typeof(ITaskbarList), new Guid("56FDF342-FD6D-11d0-958A-006097C9A090"), (Func<IntPtr, bool, object>)((nint p, bool owns) => new __MicroComITaskbarListProxy(p, owns)));
	}

	protected __MicroComITaskbarListProxy(nint nativePointer, bool ownsHandle)
		: base((IntPtr)nativePointer, ownsHandle)
	{
	}
}
