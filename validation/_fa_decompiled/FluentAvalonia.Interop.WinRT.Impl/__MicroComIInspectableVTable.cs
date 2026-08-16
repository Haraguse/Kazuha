using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIInspectableVTable : MicroComVtblBase
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetIidsDelegate(void* @this, ulong* iidCount, Guid** iids);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetRuntimeClassNameDelegate(void* @this, nint* className);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetTrustLevelDelegate(void* @this, TrustLevel* trustLevel);

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetIids(void* @this, ulong* iidCount, Guid** iids)
	{
		IInspectable inspectable = null;
		try
		{
			inspectable = (IInspectable)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			inspectable.GetIids(iidCount, iids);
		}
		catch (COMException ex)
		{
			return ex.ErrorCode;
		}
		catch (Exception ex2)
		{
			MicroComRuntime.UnhandledException((object)inspectable, ex2);
			return -2147467259;
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetRuntimeClassName(void* @this, nint* className)
	{
		IInspectable inspectable = null;
		try
		{
			inspectable = (IInspectable)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			nint runtimeClassName = inspectable.RuntimeClassName;
			*className = runtimeClassName;
		}
		catch (COMException ex)
		{
			return ex.ErrorCode;
		}
		catch (Exception ex2)
		{
			MicroComRuntime.UnhandledException((object)inspectable, ex2);
			return -2147467259;
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetTrustLevel(void* @this, TrustLevel* trustLevel)
	{
		IInspectable inspectable = null;
		try
		{
			inspectable = (IInspectable)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			TrustLevel trustLevel2 = inspectable.TrustLevel;
			*trustLevel = trustLevel2;
		}
		catch (COMException ex)
		{
			return ex.ErrorCode;
		}
		catch (Exception ex2)
		{
			MicroComRuntime.UnhandledException((object)inspectable, ex2);
			return -2147467259;
		}
		return 0;
	}

	protected unsafe __MicroComIInspectableVTable()
	{
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, ulong*, Guid**, int>)(&GetIids));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint*, int>)(&GetRuntimeClassName));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, TrustLevel*, int>)(&GetTrustLevel));
	}

	[ModuleInitializer]
	internal static void __MicroComModuleInit()
	{
		MicroComRuntime.RegisterVTable(typeof(IInspectable), ((MicroComVtblBase)new __MicroComIInspectableVTable()).CreateVTable());
	}
}
