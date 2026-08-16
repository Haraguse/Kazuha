using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIAccessibilitySettingsVTable : __MicroComIInspectableVTable
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetHighContrastDelegate(void* @this, int* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetHighContrastSchemeDelegate(void* @this, nint* value);

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetHighContrast(void* @this, int* value)
	{
		IAccessibilitySettings accessibilitySettings = null;
		try
		{
			accessibilitySettings = (IAccessibilitySettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			int highContrast = accessibilitySettings.HighContrast;
			*value = highContrast;
		}
		catch (COMException ex)
		{
			return ex.ErrorCode;
		}
		catch (Exception ex2)
		{
			MicroComRuntime.UnhandledException((object)accessibilitySettings, ex2);
			return -2147467259;
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetHighContrastScheme(void* @this, nint* value)
	{
		IAccessibilitySettings accessibilitySettings = null;
		try
		{
			accessibilitySettings = (IAccessibilitySettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			nint highContrastScheme = accessibilitySettings.HighContrastScheme;
			*value = highContrastScheme;
		}
		catch (COMException ex)
		{
			return ex.ErrorCode;
		}
		catch (Exception ex2)
		{
			MicroComRuntime.UnhandledException((object)accessibilitySettings, ex2);
			return -2147467259;
		}
		return 0;
	}

	protected unsafe __MicroComIAccessibilitySettingsVTable()
	{
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, int*, int>)(&GetHighContrast));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint*, int>)(&GetHighContrastScheme));
	}

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.RegisterVTable(typeof(IAccessibilitySettings), ((MicroComVtblBase)new __MicroComIAccessibilitySettingsVTable()).CreateVTable());
	}
}
