using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIActivationFactoryVTable : __MicroComIInspectableVTable
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int ActivateInstanceDelegate(void* @this, nint* instance);

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int ActivateInstance(void* @this, nint* instance)
	{
		IActivationFactory activationFactory = null;
		try
		{
			activationFactory = (IActivationFactory)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			nint num = activationFactory.ActivateInstance();
			*instance = num;
		}
		catch (COMException ex)
		{
			return ex.ErrorCode;
		}
		catch (Exception ex2)
		{
			MicroComRuntime.UnhandledException((object)activationFactory, ex2);
			return -2147467259;
		}
		return 0;
	}

	protected unsafe __MicroComIActivationFactoryVTable()
	{
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, nint*, int>)(&ActivateInstance));
	}

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.RegisterVTable(typeof(IActivationFactory), ((MicroComVtblBase)new __MicroComIActivationFactoryVTable()).CreateVTable());
	}
}
