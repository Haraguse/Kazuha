using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIUISettings4VTable : __MicroComIInspectableVTable
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetAdvancedEffectsEnabledDelegate(void* @this, int* value);

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetAdvancedEffectsEnabled(void* @this, int* value)
	{
		IUISettings4 iUISettings = null;
		try
		{
			iUISettings = (IUISettings4)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			int advancedEffectsEnabled = iUISettings.AdvancedEffectsEnabled;
			*value = advancedEffectsEnabled;
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

	protected unsafe __MicroComIUISettings4VTable()
	{
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, int*, int>)(&GetAdvancedEffectsEnabled));
	}

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.RegisterVTable(typeof(IUISettings4), ((MicroComVtblBase)new __MicroComIUISettings4VTable()).CreateVTable());
	}
}
