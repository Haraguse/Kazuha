using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIUISettings2Proxy : __MicroComIInspectableProxy, IUISettings2, IInspectable, IUnknown, IDisposable
{
	public unsafe double TextScaleFactor
	{
		get
		{
			double result = 0.0;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetTextScaleFactor failed", num);
			}
			return result;
		}
	}

	protected override int VTableSize => base.VTableSize + 1;

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.Register(typeof(IUISettings2), new Guid("BAD82401-2721-44F9-BB91-2BB228BE442F"), (Func<IntPtr, bool, object>)((nint p, bool owns) => new __MicroComIUISettings2Proxy(p, owns)));
	}

	protected __MicroComIUISettings2Proxy(nint nativePointer, bool ownsHandle)
		: base(nativePointer, ownsHandle)
	{
	}
}
