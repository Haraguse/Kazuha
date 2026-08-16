using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComITaskbarList2Proxy : __MicroComITaskbarListProxy, ITaskbarList2, ITaskbarList, IUnknown, IDisposable
{
	protected override int VTableSize => base.VTableSize + 1;

	public unsafe void MarkFullscreenWindow(nint hwnd, int fFullscreen)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, nint, int, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize])(((MicroComProxyBase)this).PPV, hwnd, fFullscreen);
		if (num != 0)
		{
			throw new COMException("MarkFullscreenWindow failed", num);
		}
	}

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.Register(typeof(ITaskbarList2), new Guid("602D4995-B13A-429b-A66E-1935E44F4317"), (Func<IntPtr, bool, object>)((nint p, bool owns) => new __MicroComITaskbarList2Proxy(p, owns)));
	}

	protected __MicroComITaskbarList2Proxy(nint nativePointer, bool ownsHandle)
		: base(nativePointer, ownsHandle)
	{
	}
}
