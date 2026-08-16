using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIUISettings2VTable : __MicroComIInspectableVTable
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetTextScaleFactorDelegate(void* @this, double* value);

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetTextScaleFactor(void* @this, double* value)
	{
		IUISettings2 iUISettings = null;
		try
		{
			iUISettings = (IUISettings2)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			double textScaleFactor = iUISettings.TextScaleFactor;
			*value = textScaleFactor;
		}
		catch (COMException ex)
		{
			return ex.ErrorCode;
		}
		catch (Exception ex2)
		{
			MicroComRuntime.UnhandledException((object)iUISettings, ex2);
			return -2147467259;
		}
		return 0;
	}

	protected unsafe __MicroComIUISettings2VTable()
	{
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, double*, int>)(&GetTextScaleFactor));
	}

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.RegisterVTable(typeof(IUISettings2), ((MicroComVtblBase)new __MicroComIUISettings2VTable()).CreateVTable());
	}
}
