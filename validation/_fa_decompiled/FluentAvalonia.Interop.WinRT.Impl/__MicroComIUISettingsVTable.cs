using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT.Impl;

internal class __MicroComIUISettingsVTable : __MicroComIInspectableVTable
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetHandPreferenceDelegate(void* @this, HandPreference* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetCursorSizeDelegate(void* @this, WinRTSize* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetScrollBarSizeDelegate(void* @this, WinRTSize* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetScrollBarArrowSizeDelegate(void* @this, WinRTSize* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetScrollBarThumbBoxSizeDelegate(void* @this, WinRTSize* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetMessageDurationDelegate(void* @this, uint* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetAnimationsEnabledDelegate(void* @this, int* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetCaretBrowsingEnabledDelegate(void* @this, int* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetCaretBlinkRateDelegate(void* @this, uint* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetCaretWidthDelegate(void* @this, uint* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetDoubleClickTimeDelegate(void* @this, uint* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int GetMouseHoverTimeDelegate(void* @this, uint* value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private unsafe delegate int UIElementColorDelegate(void* @this, UIElementType desiredElement, WinRTColor* value);

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetHandPreference(void* @this, HandPreference* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			HandPreference handPreference = iUISettings.HandPreference;
			*value = handPreference;
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetCursorSize(void* @this, WinRTSize* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			WinRTSize cursorSize = iUISettings.CursorSize;
			*value = cursorSize;
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetScrollBarSize(void* @this, WinRTSize* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			WinRTSize scrollBarSize = iUISettings.ScrollBarSize;
			*value = scrollBarSize;
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetScrollBarArrowSize(void* @this, WinRTSize* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			WinRTSize scrollBarArrowSize = iUISettings.ScrollBarArrowSize;
			*value = scrollBarArrowSize;
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetScrollBarThumbBoxSize(void* @this, WinRTSize* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			WinRTSize scrollBarThumbBoxSize = iUISettings.ScrollBarThumbBoxSize;
			*value = scrollBarThumbBoxSize;
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetMessageDuration(void* @this, uint* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			uint messageDuration = iUISettings.MessageDuration;
			*value = messageDuration;
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetAnimationsEnabled(void* @this, int* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			int animationsEnabled = iUISettings.AnimationsEnabled;
			*value = animationsEnabled;
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetCaretBrowsingEnabled(void* @this, int* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			int caretBrowsingEnabled = iUISettings.CaretBrowsingEnabled;
			*value = caretBrowsingEnabled;
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetCaretBlinkRate(void* @this, uint* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			uint caretBlinkRate = iUISettings.CaretBlinkRate;
			*value = caretBlinkRate;
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetCaretWidth(void* @this, uint* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			uint caretWidth = iUISettings.CaretWidth;
			*value = caretWidth;
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetDoubleClickTime(void* @this, uint* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			uint doubleClickTime = iUISettings.DoubleClickTime;
			*value = doubleClickTime;
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int GetMouseHoverTime(void* @this, uint* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			uint mouseHoverTime = iUISettings.MouseHoverTime;
			*value = mouseHoverTime;
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

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int UIElementColor(void* @this, UIElementType desiredElement, WinRTColor* value)
	{
		IUISettings iUISettings = null;
		try
		{
			iUISettings = (IUISettings)MicroComRuntime.GetObjectFromCcw((IntPtr)new IntPtr(@this));
			WinRTColor winRTColor = iUISettings.UIElementColor(desiredElement);
			*value = winRTColor;
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

	protected unsafe __MicroComIUISettingsVTable()
	{
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, HandPreference*, int>)(&GetHandPreference));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, WinRTSize*, int>)(&GetCursorSize));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, WinRTSize*, int>)(&GetScrollBarSize));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, WinRTSize*, int>)(&GetScrollBarArrowSize));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, WinRTSize*, int>)(&GetScrollBarThumbBoxSize));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, uint*, int>)(&GetMessageDuration));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, int*, int>)(&GetAnimationsEnabled));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, int*, int>)(&GetCaretBrowsingEnabled));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, uint*, int>)(&GetCaretBlinkRate));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, uint*, int>)(&GetCaretWidth));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, uint*, int>)(&GetDoubleClickTime));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, uint*, int>)(&GetMouseHoverTime));
		((MicroComVtblBase)this).AddMethod((void*)(delegate* unmanaged[Stdcall]<void*, UIElementType, WinRTColor*, int>)(&UIElementColor));
	}

	[ModuleInitializer]
	internal new static void __MicroComModuleInit()
	{
		MicroComRuntime.RegisterVTable(typeof(IUISettings), ((MicroComVtblBase)new __MicroComIUISettingsVTable()).CreateVTable());
	}
}
