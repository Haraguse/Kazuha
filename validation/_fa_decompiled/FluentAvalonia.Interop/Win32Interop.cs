using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using Avalonia;
using FluentAvalonia.Interop.Win32;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop;

internal static class Win32Interop
{
	public const int GWLP_WNDPROC = -4;

	public const int GWL_STYLE = -16;

	public const uint WS_MAXIMIZE = 16777216u;

	public const int WM_CREATE = 1;

	public const int WM_SIZE = 5;

	public const int WM_NCMOUSEMOVE = 160;

	public const int WM_NCLBUTTONDOWN = 161;

	public const int WM_NCLBUTTONUP = 162;

	public const int WM_NCHITTEST = 132;

	public const int WM_NCCALCSIZE = 131;

	public const int WM_ACTIVATE = 6;

	public const int WM_NCRBUTTONDOWN = 164;

	public const int WM_NCRBUTTONDBLCLK = 166;

	public const int WM_NCRBUTTONUP = 165;

	public const int WM_SYSCOMMAND = 274;

	public const int WM_RBUTTONUP = 517;

	public const int WM_SETTINGCHANGE = 26;

	public const int WM_SYSCOLORCHANGE = 21;

	public const int WM_DESTROY = 2;

	public const int SC_CLOSE = 61536;

	public const int SC_KEYMENU = 61696;

	public const int SC_MAXIMIZE = 61488;

	public const int SC_MINIMIZE = 61472;

	public const int SC_MOVE = 61456;

	public const int SC_RESTORE = 61728;

	public const int SC_SIZE = 61440;

	public const int MIIM_STATE = 1;

	public const int SWP_NOSIZE = 1;

	public const int SWP_NOMOVE = 2;

	public const int SWP_NOZORDER = 4;

	public const int SWP_NOREDRAW = 8;

	public const int SWP_NOACTIVATE = 16;

	public const int SWP_FRAMECHANGED = 32;

	public const int SWP_SHOWWINDOW = 64;

	public const int SWP_NOOWNERZORDER = 512;

	public const int SWP_DRAWFRAME = 32;

	public const int SWP_NOREPOSITION = 512;

	public const int SM_CXPADDEDBORDER = 92;

	public const int SM_CYSIZEFRAME = 33;

	public const int HTCLIENT = 1;

	public const int HTCAPTION = 2;

	public const int HTMAXBUTTON = 9;

	public const int HTMINBUTTON = 8;

	public const int HTCLOSE = 20;

	public const int HTTOP = 12;

	public const int MFS_DISABLED = 3;

	public const int MFS_ENABLED = 0;

	public const int TPM_RETURNCMD = 256;

	public static readonly Guid ITaskBarList3CLSID = Guid.Parse("56FDF344-FD6D-11D0-958A-006097C9A090");

	public static readonly Guid ITaskBarList3IID = Guid.Parse("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf");

	private const string s_ole32 = "ole32.dll";

	private const string s_dwmapi = "dwmapi";

	private const string s_user32 = "user32.dll";

	private const string s_ntdll = "ntdll.dll";

	private const string s_uxtheme = "uxtheme.dll";

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public unsafe static BOOL GetWindowRect(HWND hWnd, RECT* lpRect)
	{
		Marshal.SetLastSystemError(0);
		BOOL result = __PInvoke(hWnd, lpRect);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "GetWindowRect", ExactSpelling = true)]
		unsafe static extern BOOL __PInvoke(HWND __hWnd_native, RECT* __lpRect_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public unsafe static BOOL GetClientRect(HWND hWnd, RECT* lpRect)
	{
		Marshal.SetLastSystemError(0);
		BOOL result = __PInvoke(hWnd, lpRect);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "GetClientRect", ExactSpelling = true)]
		unsafe static extern BOOL __PInvoke(HWND __hWnd_native, RECT* __lpRect_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public unsafe static BOOL AdjustWindowRectExForDpi(RECT* lpRect, int dwStyle, BOOL bMenu, int dwExStyle, int dpi)
	{
		Marshal.SetLastSystemError(0);
		BOOL result = __PInvoke(lpRect, dwStyle, bMenu, dwExStyle, dpi);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "AdjustWindowRectExForDpi", ExactSpelling = true)]
		unsafe static extern BOOL __PInvoke(RECT* __lpRect_native, int __dwStyle_native, BOOL __bMenu_native, int __dwExStyle_native, int __dpi_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	private static int GetSystemMetrics(int smIndex)
	{
		Marshal.SetLastSystemError(0);
		int result = __PInvoke(smIndex);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "GetSystemMetrics", ExactSpelling = true)]
		static extern int __PInvoke(int __smIndex_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	private static int GetSystemMetricsForDpi(int nIndex, uint dpi)
	{
		Marshal.SetLastSystemError(0);
		int result = __PInvoke(nIndex, dpi);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "GetSystemMetricsForDpi", ExactSpelling = true)]
		static extern int __PInvoke(int __nIndex_native, uint __dpi_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public static BOOL SetWindowPos(HWND hWnd, HWND hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags)
	{
		Marshal.SetLastSystemError(0);
		BOOL result = __PInvoke(hWnd, hWndInsertAfter, x, y, cx, cy, uFlags);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "SetWindowPos", ExactSpelling = true)]
		static extern BOOL __PInvoke(HWND __hWnd_native, HWND __hWndInsertAfter_native, int __x_native, int __y_native, int __cx_native, int __cy_native, uint __uFlags_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	private unsafe static BOOL AdjustWindowRectEx(RECT* lpRect, int dwStyle, BOOL bMenu, int dwExStyle)
	{
		Marshal.SetLastSystemError(0);
		BOOL result = __PInvoke(lpRect, dwStyle, bMenu, dwExStyle);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "AdjustWindowRectEx", ExactSpelling = true)]
		unsafe static extern BOOL __PInvoke(RECT* __lpRect_native, int __dwStyle_native, BOOL __bMenu_native, int __dwExStyle_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public static LRESULT DefWindowProcW(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		Marshal.SetLastSystemError(0);
		LRESULT result = __PInvoke(hWnd, msg, wParam, lParam);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "DefWindowProcW", ExactSpelling = true)]
		static extern LRESULT __PInvoke(HWND __hWnd_native, uint __msg_native, WPARAM __wParam_native, LPARAM __lParam_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public static LRESULT CallWindowProcW(nint lpPrevWndProc, HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		Marshal.SetLastSystemError(0);
		LRESULT result = __PInvoke(lpPrevWndProc, hWnd, msg, wParam, lParam);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "CallWindowProcW", ExactSpelling = true)]
		static extern LRESULT __PInvoke(nint __lpPrevWndProc_native, HWND __hWnd_native, uint __msg_native, WPARAM __wParam_native, LPARAM __lParam_native);
	}

	[LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public static BOOL PostMessage(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		Marshal.SetLastSystemError(0);
		BOOL result = __PInvoke(hWnd, msg, wParam, lParam);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "PostMessageW", ExactSpelling = true)]
		static extern BOOL __PInvoke(HWND __hWnd_native, uint __msg_native, WPARAM __wParam_native, LPARAM __lParam_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public static HMENU GetSystemMenu(HWND hWnd, BOOL bRevert)
	{
		Marshal.SetLastSystemError(0);
		HMENU result = __PInvoke(hWnd, bRevert);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "GetSystemMenu", ExactSpelling = true)]
		static extern HMENU __PInvoke(HWND __hWnd_native, BOOL __bRevert_native);
	}

	[LibraryImport("user32.dll", EntryPoint = "SetMenuItemInfoW", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public unsafe static BOOL SetMenuItemInfo(HMENU hMenu, uint item, BOOL fByPosition, MENUITEMINFO* lpmii)
	{
		Marshal.SetLastSystemError(0);
		BOOL result = __PInvoke(hMenu, item, fByPosition, lpmii);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "SetMenuItemInfoW", ExactSpelling = true)]
		unsafe static extern BOOL __PInvoke(HMENU __hMenu_native, uint __item_native, BOOL __fByPosition_native, MENUITEMINFO* __lpmii_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public static BOOL SetMenuDefaultItem(HMENU hMenu, uint uItem, uint fByPos)
	{
		Marshal.SetLastSystemError(0);
		BOOL result = __PInvoke(hMenu, uItem, fByPos);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "SetMenuDefaultItem", ExactSpelling = true)]
		static extern BOOL __PInvoke(HMENU __hMenu_native, uint __uItem_native, uint __fByPos_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public unsafe static BOOL TrackPopupMenu(HMENU hMenu, uint uFlags, int x, int y, int nReserved, HWND hWnd, RECT* prcRect)
	{
		Marshal.SetLastSystemError(0);
		BOOL result = __PInvoke(hMenu, uFlags, x, y, nReserved, hWnd, prcRect);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "TrackPopupMenu", ExactSpelling = true)]
		unsafe static extern BOOL __PInvoke(HMENU __hMenu_native, uint __uFlags_native, int __x_native, int __y_native, int __nReserved_native, HWND __hWnd_native, RECT* __prcRect_native);
	}

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	public static extern int GetWindowLongW(HWND hWnd, int nIndex);

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	public static extern int SetWindowLongW(HWND hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", ExactSpelling = true)]
	[LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	private static extern nint GetWindowLongPtrW_native(HWND hWnd, int nIndex);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", ExactSpelling = true)]
	[LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	private static extern nint SetWindowLongPtrW_native(HWND hWnd, int nIndex, nint dwNewLong);

	public static nint GetWindowLongPtr(HWND hWnd, int nIndex)
	{
		return GetWindowLongPtrW(hWnd, nIndex);
	}

	public unsafe static nint GetWindowLongPtrW(HWND hWnd, int nIndex)
	{
		if (sizeof(nint) == 4)
		{
			return GetWindowLongW(hWnd, nIndex);
		}
		return GetWindowLongPtrW_native(hWnd, nIndex);
	}

	public static nint SetWindowLongPtr(HWND hWnd, int nIndex, nint dwNewLong)
	{
		return SetWindowLongPtrW(hWnd, nIndex, dwNewLong);
	}

	public unsafe static nint SetWindowLongPtrW(HWND hWnd, int nIndex, nint dwNewLong)
	{
		if (sizeof(nint) == 4)
		{
			return SetWindowLongW(hWnd, nIndex, (int)dwNewLong);
		}
		return SetWindowLongPtrW_native(hWnd, nIndex, dwNewLong);
	}

	[LibraryImport("dwmapi", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public unsafe static HRESULT DwmExtendFrameIntoClientArea(HWND hWnd, MARGINS* margins)
	{
		Marshal.SetLastSystemError(0);
		HRESULT result = __PInvoke(hWnd, margins);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("dwmapi", EntryPoint = "DwmExtendFrameIntoClientArea", ExactSpelling = true)]
		unsafe static extern HRESULT __PInvoke(HWND __hWnd_native, MARGINS* __margins_native);
	}

	[DllImport("ole32.dll", ExactSpelling = true)]
	[LibraryImport("ole32.dll")]
	public unsafe static extern HRESULT CoCreateInstance(Guid* rclsid, void* pUnkOuter, int dwClsContext, Guid* riid, void** ppv);

	internal unsafe static T CreateInstance<T>(Guid clsid, Guid iid) where T : IUnknown
	{
		void* ptr = default(void*);
		HRESULT hRESULT = CoCreateInstance(&clsid, null, 1, &iid, &ptr);
		if (hRESULT != 0)
		{
			throw new COMException("CreateInstance", hRESULT);
		}
		IUnknown val = MicroComRuntime.CreateProxyFor<IUnknown>(ptr, true);
		try
		{
			return MicroComRuntime.QueryInterface<T>(val);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	[LibraryImport("dwmapi", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public unsafe static int DwmSetWindowAttribute(nint hWnd, DWMWINDOWATTRIBUTE attr, void* value, int attrSize)
	{
		Marshal.SetLastSystemError(0);
		int result = __PInvoke(hWnd, attr, value, attrSize);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("dwmapi", EntryPoint = "DwmSetWindowAttribute", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __hWnd_native, DWMWINDOWATTRIBUTE __attr_native, void* __value_native, int __attrSize_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public unsafe static int SetWindowCompositionAttribute(nint hwnd, WINDOWCOMPOSITIONATTRIBDATA* data)
	{
		Marshal.SetLastSystemError(0);
		int result = __PInvoke(hwnd, data);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "SetWindowCompositionAttribute", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __hwnd_native, WINDOWCOMPOSITIONATTRIBDATA* __data_native);
	}

	[LibraryImport("uxtheme.dll", EntryPoint = "#104", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public static void fnRefreshImmersiveColorPolicyState()
	{
		Marshal.SetLastSystemError(0);
		__PInvoke();
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		[DllImport("uxtheme.dll", EntryPoint = "#104", ExactSpelling = true)]
		static extern void __PInvoke();
	}

	[LibraryImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	public static PreferredAppMode fnSetPreferredAppMode(nint hwnd, PreferredAppMode appMode)
	{
		Marshal.SetLastSystemError(0);
		PreferredAppMode result = __PInvoke(hwnd, appMode);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("uxtheme.dll", EntryPoint = "#135", ExactSpelling = true)]
		static extern PreferredAppMode __PInvoke(nint __hwnd_native, PreferredAppMode __appMode_native);
	}

	[DllImport("uxtheme.dll", EntryPoint = "#135", ExactSpelling = true)]
	[LibraryImport("uxtheme.dll", EntryPoint = "#135")]
	public static extern BOOL fnAllowDarkModeForApp(nint hwnd, BOOL allow);

	[SecurityCritical]
	[LibraryImport("ntdll.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	internal unsafe static int RtlGetVersion(OSVERSIONINFOEX* versionInfo)
	{
		Marshal.SetLastSystemError(0);
		int result = __PInvoke(versionInfo);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("ntdll.dll", EntryPoint = "RtlGetVersion", ExactSpelling = true)]
		unsafe static extern int __PInvoke(OSVERSIONINFOEX* __versionInfo_native);
	}

	public static int GetSystemMetricsWithFallback(int nIndex, uint dpi)
	{
		if (OSVersionHelper.IsAtLeastWindows10_1607())
		{
			return GetSystemMetricsForDpi(nIndex, dpi);
		}
		return GetSystemMetrics(nIndex);
	}

	public unsafe static void AdjustWindowRectExWithFallback(RECT* lpRect, int dwStyle, BOOL bMenu, int dwExStyle, int dpi)
	{
		if (OSVersionHelper.IsAtLeastWindows10_1607())
		{
			AdjustWindowRectExForDpi(lpRect, dwStyle, bMenu, dwExStyle, dpi);
		}
		else
		{
			AdjustWindowRectEx(lpRect, dwStyle, bMenu, dwExStyle);
		}
	}

	public unsafe static bool ApplyTheme(nint hwnd, bool useDark)
	{
		if (!OSVersionHelper.IsAtLeastWindows10_1809())
		{
			return false;
		}
		if (!OSVersionHelper.IsAtLeastWindows10_1903())
		{
			BOOL bOOL = fnAllowDarkModeForApp(hwnd, useDark);
			if (bOOL == false)
			{
				return bOOL;
			}
			int num = (useDark ? 1 : 0);
			DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &num, 4);
		}
		else
		{
			fnSetPreferredAppMode(hwnd, useDark ? PreferredAppMode.AllowDark : PreferredAppMode.Default);
			fnRefreshImmersiveColorPolicyState();
			WINDOWCOMPOSITIONATTRIBDATA wINDOWCOMPOSITIONATTRIBDATA = new WINDOWCOMPOSITIONATTRIBDATA
			{
				attrib = WINDOWCOMPOSITIONATTRIB.WCA_USEDARKMODECOLORS,
				data = &useDark,
				sizeOfData = 4
			};
			if (SetWindowCompositionAttribute(hwnd, &wINDOWCOMPOSITIONATTRIBDATA) == 0)
			{
				return false;
			}
		}
		SetWindowPos((HWND)hwnd, HWND.NULL, 0, 0, 0, 0, 55u);
		return true;
	}

	public static PixelPoint PointFromLParam(LPARAM lParam)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		int num = (int)lParam;
		return new PixelPoint((int)(short)(num & 0xFFFF), (int)(short)(num >> 16));
	}
}
