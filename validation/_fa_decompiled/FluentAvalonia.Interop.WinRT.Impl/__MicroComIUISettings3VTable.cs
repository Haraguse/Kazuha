using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIUISettings3VTable : __MicroComIInspectableVTable
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetColorValueDelegate(void* @this, UIColorType desiredColor, WinRTColor* value);

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetColorValue(void* @this, UIColorType desiredColor, WinRTColor* value)
	{
		IUISettings3 iUISettings = null;
		try
		{
			iUISettings = (IUISettings3)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			WinRTColor colorValue = iUISettings.GetColorValue(desiredColor);
			*value = colorValue;
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

	protected unsafe __MicroComIUISettings3VTable()
	{
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, UIColorType, WinRTColor*, int>)(&GetColorValue));
	}

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.RegisterVTable(typeof(IUISettings3), ((MicroComVtblBase)new __MicroComIUISettings3VTable()).CreateVTable());
	}
}
