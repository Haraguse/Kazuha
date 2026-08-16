using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIActivationFactoryProxy : __MicroComIInspectableProxy, IActivationFactory, IInspectable, IUnknown, IDisposable
{
	protected override int VTableSize => base.VTableSize + 1;

	public unsafe nint ActivateInstance()
	{
		nint result = 0;
		int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize])(((MicroComProxyBase)this).PPV, &result);
		if (num != 0)
		{
			throw new COMException("ActivateInstance failed", num);
		}
		return result;
	}

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.Register(typeof(IActivationFactory), new Guid("00000035-0000-0000-C000-000000000046"), (Func<IntPtr, bool, object>)((nint p, bool owns) => new __MicroComIActivationFactoryProxy(p, owns)));
	}

	protected __MicroComIActivationFactoryProxy(nint nativePointer, bool ownsHandle)
		: base(nativePointer, ownsHandle)
	{
	}
}
