using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FluentAvalonia.Interop.Win32;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComITaskbarList3VTable : __MicroComITaskbarList2VTable
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int SetProgressValueDelegate(void* @this, nint hwnd, ulong ullCompleted, ulong ullTotal);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int SetProgressStateDelegate(void* @this, nint hwnd, int tbpFlags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int RegisterTabDelegate(void* @this, nint hwndTab, nint hwndMDI);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int UnregisterTabDelegate(void* @this, nint hwndTab);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int SetTabOrderDelegate(void* @this, nint hwndTab, nint hwndInsertBefore);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int SetTabActiveDelegate(void* @this, nint hwndTab, nint hwndMDI, int dwReserved);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int ThumbBarAddButtonsDelegate(void* @this, nint hwnd, uint cButtons, int pButton);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int ThumbBarUpdateButtonsDelegate(void* @this, nint hwnd, uint cButtons, int pButton);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int ThumbBarSetImageListDelegate(void* @this, nint hwnd, nint himl);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int SetOverlayIconDelegate(void* @this, nint hwnd, void* hIcon, ushort* pszDescription);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int SetThumbnailTooltipDelegate(void* @this, nint hwnd, ushort* pszTip);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int SetThumbnailClipDelegate(void* @this, nint hwnd, RECT* prcClip);

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int SetProgressValue(void* @this, nint hwnd, ulong ullCompleted, ulong ullTotal)
	{
		ITaskbarList3 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.SetProgressValue(hwnd, ullCompleted, ullTotal);
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
	private unsafe static int SetProgressState(void* @this, nint hwnd, int tbpFlags)
	{
		ITaskbarList3 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.SetProgressState(hwnd, tbpFlags);
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
	private unsafe static int RegisterTab(void* @this, nint hwndTab, nint hwndMDI)
	{
		ITaskbarList3 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.RegisterTab(hwndTab, hwndMDI);
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
	private unsafe static int UnregisterTab(void* @this, nint hwndTab)
	{
		ITaskbarList3 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.UnregisterTab(hwndTab);
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
	private unsafe static int SetTabOrder(void* @this, nint hwndTab, nint hwndInsertBefore)
	{
		ITaskbarList3 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.SetTabOrder(hwndTab, hwndInsertBefore);
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
	private unsafe static int SetTabActive(void* @this, nint hwndTab, nint hwndMDI, int dwReserved)
	{
		ITaskbarList3 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.SetTabActive(hwndTab, hwndMDI, dwReserved);
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
	private unsafe static int ThumbBarAddButtons(void* @this, nint hwnd, uint cButtons, int pButton)
	{
		ITaskbarList3 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.ThumbBarAddButtons(hwnd, cButtons, pButton);
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
	private unsafe static int ThumbBarUpdateButtons(void* @this, nint hwnd, uint cButtons, int pButton)
	{
		ITaskbarList3 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.ThumbBarUpdateButtons(hwnd, cButtons, pButton);
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
	private unsafe static int ThumbBarSetImageList(void* @this, nint hwnd, nint himl)
	{
		ITaskbarList3 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.ThumbBarSetImageList(hwnd, himl);
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
	private unsafe static int SetOverlayIcon(void* @this, nint hwnd, void* hIcon, ushort* pszDescription)
	{
		ITaskbarList3 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.SetOverlayIcon(hwnd, hIcon, pszDescription);
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
	private unsafe static int SetThumbnailTooltip(void* @this, nint hwnd, ushort* pszTip)
	{
		ITaskbarList3 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.SetThumbnailTooltip(hwnd, pszTip);
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
	private unsafe static int SetThumbnailClip(void* @this, nint hwnd, RECT* prcClip)
	{
		ITaskbarList3 taskbarList = null;
		try
		{
			taskbarList = (ITaskbarList3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			taskbarList.SetThumbnailClip(hwnd, prcClip);
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

	protected unsafe __MicroComITaskbarList3VTable()
	{
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, ulong, ulong, int>)(&SetProgressValue));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, int, int>)(&SetProgressState));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, nint, int>)(&RegisterTab));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, int>)(&UnregisterTab));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, nint, int>)(&SetTabOrder));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, nint, int, int>)(&SetTabActive));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, uint, int, int>)(&ThumbBarAddButtons));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, uint, int, int>)(&ThumbBarUpdateButtons));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, nint, int>)(&ThumbBarSetImageList));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, void*, ushort*, int>)(&SetOverlayIcon));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, ushort*, int>)(&SetThumbnailTooltip));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint, RECT*, int>)(&SetThumbnailClip));
	}

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.RegisterVTable(typeof(ITaskbarList3), ((MicroComVtblBase)new __MicroComITaskbarList3VTable()).CreateVTable());
	}
}
