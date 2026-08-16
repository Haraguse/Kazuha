using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComITaskbarListVTable : MicroComVtblBase
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int HrInitDelegate(void* @this);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int AddTabDelegate(void* @this, nint hwnd);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int DeleteTabDelegate(void* @this, nint hwnd);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int ActivateTabDelegate(void* @this, nint hwnd);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int SetActiveAltDelegate(void* @this, nint hwnd);

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int HrInit(void* @this)
	{
		ITaskbarList taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.HrInit();
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int AddTab(void* @this, nint hwnd)
	{
		ITaskbarList taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.AddTab(hwnd);
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int DeleteTab(void* @this, nint hwnd)
	{
		ITaskbarList taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.DeleteTab(hwnd);
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int ActivateTab(void* @this, nint hwnd)
	{
		ITaskbarList taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.ActivateTab(hwnd);
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int SetActiveAlt(void* @this, nint hwnd)
	{
		ITaskbarList taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.SetActiveAlt(hwnd);
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

	protected unsafe __MicroComITaskbarListVTable()
	{
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, int>)(&HrInit));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, int>)(&AddTab));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, int>)(&DeleteTab));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, int>)(&ActivateTab));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, int>)(&SetActiveAlt));
	}

	[ModuleInitializer]
	internal static void __MicroComModuleInit()
	{
		MicroComRuntime.RegisterVTable(typeof(ITaskbarList), ((MicroComVtblBase)new __MicroComITaskbarListVTable()).CreateVTable());
	}
}
