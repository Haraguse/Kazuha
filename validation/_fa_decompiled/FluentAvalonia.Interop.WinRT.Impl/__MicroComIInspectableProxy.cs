using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIInspectableProxy : MicroComProxyBase, IInspectable, IUnknown, IDisposable
{
	public unsafe nint RuntimeClassName
	{
		get
		{
			nint result = 0;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[((MicroComProxyBase)this).VTableSize + 1])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetRuntimeClassName failed", num);
			}
			return result;
		}
	}

	public unsafe TrustLevel TrustLevel
	{
		get
		{
			TrustLevel result = TrustLevel.BaseTrust;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[((MicroComProxyBase)this).VTableSize + 2])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetTrustLevel failed", num);
			}
			return result;
		}
	}

	protected override int VTableSize => ((MicroComProxyBase)this).VTableSize + 3;

	public unsafe void GetIids(ulong* iidCount, Guid** iids)
	{
		int num = ((delegate* unmanaged[Stdcall]<void*, void*, void*, int>)(*((MicroComProxyBase)this).PPV)[((MicroComProxyBase)this).VTableSize])(((MicroComProxyBase)this).PPV, iidCount, iids);
		if (num != 0)
		{
			throw new COMException("GetIids failed", num);
		}
	}

	[ModuleInitializer]
	internal static void __MicroComModuleInit()
	{
		MicroComRuntime.Register(typeof(IInspectable), new Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90"), (Func<IntPtr, bool, object>)((nint p, bool owns) => new __MicroComIInspectableProxy(p, owns)));
	}

	protected __MicroComIInspectableProxy(nint nativePointer, bool ownsHandle)
		: base((IntPtr)nativePointer, ownsHandle)
	{
	}
}
