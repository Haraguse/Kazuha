using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIUISettingsProxy : __MicroComIInspectableProxy, IUISettings, IInspectable, IUnknown, IDisposable
{
	public unsafe HandPreference HandPreference
	{
		get
		{
			HandPreference result = HandPreference.LeftHanded;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetHandPreference failed", num);
			}
			return result;
		}
	}

	public unsafe WinRTSize CursorSize
	{
		get
		{
			WinRTSize result = default(WinRTSize);
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 1])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetCursorSize failed", num);
			}
			return result;
		}
	}

	public unsafe WinRTSize ScrollBarSize
	{
		get
		{
			WinRTSize result = default(WinRTSize);
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 2])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetScrollBarSize failed", num);
			}
			return result;
		}
	}

	public unsafe WinRTSize ScrollBarArrowSize
	{
		get
		{
			WinRTSize result = default(WinRTSize);
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 3])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetScrollBarArrowSize failed", num);
			}
			return result;
		}
	}

	public unsafe WinRTSize ScrollBarThumbBoxSize
	{
		get
		{
			WinRTSize result = default(WinRTSize);
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 4])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetScrollBarThumbBoxSize failed", num);
			}
			return result;
		}
	}

	public unsafe uint MessageDuration
	{
		get
		{
			uint result = 0u;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 5])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetMessageDuration failed", num);
			}
			return result;
		}
	}

	public unsafe int AnimationsEnabled
	{
		get
		{
			int result = 0;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 6])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetAnimationsEnabled failed", num);
			}
			return result;
		}
	}

	public unsafe int CaretBrowsingEnabled
	{
		get
		{
			int result = 0;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 7])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetCaretBrowsingEnabled failed", num);
			}
			return result;
		}
	}

	public unsafe uint CaretBlinkRate
	{
		get
		{
			uint result = 0u;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 8])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetCaretBlinkRate failed", num);
			}
			return result;
		}
	}

	public unsafe uint CaretWidth
	{
		get
		{
			uint result = 0u;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 9])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetCaretWidth failed", num);
			}
			return result;
		}
	}

	public unsafe uint DoubleClickTime
	{
		get
		{
			uint result = 0u;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 10])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetDoubleClickTime failed", num);
			}
			return result;
		}
	}

	public unsafe uint MouseHoverTime
	{
		get
		{
			uint result = 0u;
			int num = ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 11])(((MicroComProxyBase)this).PPV, &result);
			if (num != 0)
			{
				throw new COMException("GetMouseHoverTime failed", num);
			}
			return result;
		}
	}

	protected override int VTableSize => base.VTableSize + 13;

	public unsafe WinRTColor UIElementColor(UIElementType desiredElement)
	{
		WinRTColor result = default(WinRTColor);
		int num = ((delegate* unmanaged[Stdcall]<void*, UIElementType, void*, int>)(*((MicroComProxyBase)this).PPV)[base.VTableSize + 12])(((MicroComProxyBase)this).PPV, desiredElement, &result);
		if (num != 0)
		{
			throw new COMException("UIElementColor failed", num);
		}
		return result;
	}

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.Register(typeof(IUISettings), new Guid("85361600-1C63-4627-BCB1-3A89E0BC9C55"), (Func<IntPtr, bool, object>)((nint p, bool owns) => new __MicroComIUISettingsProxy(p, owns)));
	}

	protected __MicroComIUISettingsProxy(nint nativePointer, bool ownsHandle)
		: base(nativePointer, ownsHandle)
	{
	}
}
