using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIAccessibilitySettingsProxy : __MicroComIInspectableProxy, IAccessibilitySettings, IInspectable, IUnknown, IDisposable
{
	public unsafe int HighContrast
	{
		get
		{
			int result = 0;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetHighContrast failed", num);
			}
			return result;
		}
	}

	public unsafe nint HighContrastScheme
	{
		get
		{
			nint result = 0;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 1])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetHighContrastScheme failed", num);
			}
			return result;
		}
	}

	protected override int VTableSize => base.VTableSize + 2;

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.Register(typeof(IAccessibilitySettings), new Guid("FE0E8147-C4C0-4562-B962-1327B52AD5B9"), (Func<IntPtr, bool, object>)((nint p, bool owns) => new __MicroComIAccessibilitySettingsProxy(p, owns)));
	}

	protected __MicroComIAccessibilitySettingsProxy(nint nativePointer, bool ownsHandle)
		: base(nativePointer, ownsHandle)
	{
	}
}
