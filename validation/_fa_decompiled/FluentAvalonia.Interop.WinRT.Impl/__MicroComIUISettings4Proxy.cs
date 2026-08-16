using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIUISettings4Proxy : __MicroComIInspectableProxy, IUISettings4, IInspectable, IUnknown, IDisposable
{
	public unsafe int AdvancedEffectsEnabled
	{
		get
		{
			int result = 0;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetAdvancedEffectsEnabled failed", num);
			}
			return result;
		}
	}

	protected override int VTableSize => base.VTableSize + 1;

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.Register(typeof(IUISettings4), new Guid("52BB3002-919B-4D6B-9B78-8DD66FF4B93B"), (Func<IntPtr, bool, object>)((nint p, bool owns) => new __MicroComIUISettings4Proxy(p, owns)));
	}

	protected __MicroComIUISettings4Proxy(nint nativePointer, bool ownsHandle)
		: base(nativePointer, ownsHandle)
	{
	}
}
