using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FluentAvalonia.Interop.Win32;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComITaskbarList3Proxy : __MicroComITaskbarList2Proxy, ITaskbarList3, ITaskbarList2, ITaskbarList, IUnknown, IDisposable
{
	protected override int VTableSize => base.VTableSize + 12;

	public unsafe void SetProgressValue(nint hwnd, ulong ullCompleted, ulong ullTotal)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, ulong, ulong, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize])(((MicroComProxyBase)this).PPV, hwnd, ullCompleted, ullTotal);
		if (num != 0)
		{
			throw new COMException("SetProgressValue failed", num);
		}
	}

	public unsafe void SetProgressState(nint hwnd, int tbpFlags)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, int, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 1])(((MicroComProxyBase)this).PPV, hwnd, tbpFlags);
		if (num != 0)
		{
			throw new COMException("SetProgressState failed", num);
		}
	}

	public unsafe void RegisterTab(nint hwndTab, nint hwndMDI)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, nint, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 2])(((MicroComProxyBase)this).PPV, hwndTab, hwndMDI);
		if (num != 0)
		{
			throw new COMException("RegisterTab failed", num);
		}
	}

	public unsafe void UnregisterTab(nint hwndTab)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 3])(((MicroComProxyBase)this).PPV, hwndTab);
		if (num != 0)
		{
			throw new COMException("UnregisterTab failed", num);
		}
	}

	public unsafe void SetTabOrder(nint hwndTab, nint hwndInsertBefore)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, nint, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 4])(((MicroComProxyBase)this).PPV, hwndTab, hwndInsertBefore);
		if (num != 0)
		{
			throw new COMException("SetTabOrder failed", num);
		}
	}

	public unsafe void SetTabActive(nint hwndTab, nint hwndMDI, int dwReserved)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, nint, int, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 5])(((MicroComProxyBase)this).PPV, hwndTab, hwndMDI, dwReserved);
		if (num != 0)
		{
			throw new COMException("SetTabActive failed", num);
		}
	}

	public unsafe void ThumbBarAddButtons(nint hwnd, uint cButtons, int pButton)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, uint, int, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 6])(((MicroComProxyBase)this).PPV, hwnd, cButtons, pButton);
		if (num != 0)
		{
			throw new COMException("ThumbBarAddButtons failed", num);
		}
	}

	public unsafe void ThumbBarUpdateButtons(nint hwnd, uint cButtons, int pButton)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, uint, int, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 7])(((MicroComProxyBase)this).PPV, hwnd, cButtons, pButton);
		if (num != 0)
		{
			throw new COMException("ThumbBarUpdateButtons failed", num);
		}
	}

	public unsafe void ThumbBarSetImageList(nint hwnd, nint himl)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, nint, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 8])(((MicroComProxyBase)this).PPV, hwnd, himl);
		if (num != 0)
		{
			throw new COMException("ThumbBarSetImageList failed", num);
		}
	}

	public unsafe void SetOverlayIcon(nint hwnd, void* hIcon, ushort* pszDescription)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 9])(((MicroComProxyBase)this).PPV, hwnd, hIcon, pszDescription);
		if (num != 0)
		{
			throw new COMException("SetOverlayIcon failed", num);
		}
	}

	public unsafe void SetThumbnailTooltip(nint hwnd, ushort* pszTip)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 10])(((MicroComProxyBase)this).PPV, hwnd, pszTip);
		if (num != 0)
		{
			throw new COMException("SetThumbnailTooltip failed", num);
		}
	}

	public unsafe void SetThumbnailClip(nint hwnd, RECT* prcClip)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 11])(((MicroComProxyBase)this).PPV, hwnd, prcClip);
		if (num != 0)
		{
			throw new COMException("SetThumbnailClip failed", num);
		}
	}

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.Register(typeof(ITaskbarList3), new Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf"), (Func<IntPtr, bool, object>)((nint p, bool owns) => new __MicroComITaskbarList3Proxy(p, owns)));
	}

	protected __MicroComITaskbarList3Proxy(nint nativePointer, bool ownsHandle)
		: base(nativePointer, ownsHandle)
	{
	}
}
