using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComITaskbarList2VTable : __MicroComITaskbarListVTable
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int MarkFullscreenWindowDelegate(void* @this, nint hwnd, int fFullscreen);

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int MarkFullscreenWindow(void* @this, nint hwnd, int fFullscreen)
	{
		ITaskbarList2 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList2)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.MarkFullscreenWindow(hwnd, fFullscreen);
		}
		catch (COMException ex)
		{
			return ex.ErrorCode;
		}
		catch (Exception ex2)
		{
			MicroComRuntime.UnhandledException((object)taskbarList, ex2);
			return -2147467259;
		}
		return 0;
	}

	protected unsafe __MicroComITaskbarList2VTable()
	{
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, int, int>)(&MarkFullscreenWindow));
	}

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.RegisterVTable(typeof(ITaskbarList2), ((MicroComVtblBase)new __MicroComITaskbarList2VTable()).CreateVTable());
	}
}
