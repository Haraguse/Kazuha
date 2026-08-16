using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using FluentAvalonia.Interop;
using FluentAvalonia.Interop.Win32;

namespace FluentAvalonia.UI.Windowing;

internal class Win32WindowManager
{
	private static Dictionary<HWND, Win32WindowManager> _appWindowRegistry = new Dictionary<HWND, Win32WindowManager>();

	private readonly FAAppWindow _window;

	private readonly nint _oldWndProc;

	private readonly nint _wndProc;

	public HWND Hwnd { get; }

	public unsafe Win32WindowManager(FAAppWindow window)
	{
		_window = window;
		Hwnd = (HWND)(nint)((TopLevel)_window).TryGetPlatformHandle().Handle;
		_oldWndProc = Win32Interop.GetWindowLongPtrW(Hwnd, -4);
		_appWindowRegistry.Add(Hwnd, this);
		_wndProc = (nint)(delegate* unmanaged<nint, uint, nint, nint, nint>)(&WndProcStatic);
		Win32Interop.SetWindowLongPtrW(Hwnd, -4, _wndProc);
		Application.Current.PlatformSettings.ColorValuesChanged += OnPlatformColorValuesChanged;
		((TopLevel)_window).Closed += WindowOnClosed;
	}

	private LRESULT WndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		switch (msg)
		{
		case 517u:
			HandleRBUTTONUP(lParam);
			break;
		case 274u:
			if (wParam == (ushort)61696)
			{
				return Win32Interop.DefWindowProcW(hWnd, msg, wParam, lParam);
			}
			break;
		case 2u:
			_appWindowRegistry.Remove(hWnd);
			break;
		}
		return Win32Interop.CallWindowProcW(_oldWndProc, hWnd, msg, wParam, lParam);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private double GetScaling()
	{
		return ((TopLevel)_window).RenderScaling;
	}

	private unsafe void HandleRBUTTONUP(LPARAM lParam)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		PixelPoint val = Win32Interop.PointFromLParam(lParam);
		if (_window.HitTestTitleBar(((PixelPoint)(ref val)).ToPoint(GetScaling())))
		{
			HMENU systemMenu = Win32Interop.GetSystemMenu(Hwnd, false);
			bool flag = (int)((Window)_window).WindowState == 2;
			bool showAsDialog = _window.ShowAsDialog;
			MENUITEMINFO mENUITEMINFO = new MENUITEMINFO
			{
				cbSize = (uint)sizeof(MENUITEMINFO),
				fMask = 1u,
				fState = 0u
			};
			Win32Interop.SetMenuItemInfo(systemMenu, 61536u, false, &mENUITEMINFO);
			mENUITEMINFO.fState = (showAsDialog ? 3u : 0u);
			Win32Interop.SetMenuItemInfo(systemMenu, 61472u, false, &mENUITEMINFO);
			mENUITEMINFO.fState = ((!flag || showAsDialog) ? 3u : 0u);
			Win32Interop.SetMenuItemInfo(systemMenu, 61728u, false, &mENUITEMINFO);
			mENUITEMINFO.fState = ((flag | showAsDialog) ? 3u : 0u);
			Win32Interop.SetMenuItemInfo(systemMenu, 61456u, false, &mENUITEMINFO);
			Win32Interop.SetMenuItemInfo(systemMenu, 61440u, false, &mENUITEMINFO);
			Win32Interop.SetMenuItemInfo(systemMenu, 61488u, false, &mENUITEMINFO);
			Win32Interop.SetMenuDefaultItem(systemMenu, uint.MaxValue, 0u);
			PixelPoint val2 = VisualExtensions.PointToScreen((Visual)(object)_window, ((PixelPoint)(ref val)).ToPoint(GetScaling()));
			BOOL bOOL = Win32Interop.TrackPopupMenu(systemMenu, 256u, ((PixelPoint)(ref val2)).X, ((PixelPoint)(ref val2)).Y, 0, Hwnd, null);
			if ((bool)bOOL)
			{
				Win32Interop.PostMessage(Hwnd, 274u, (WPARAM)bOOL, 0);
			}
		}
	}

	private void OnPlatformColorValuesChanged(object sender, PlatformColorValues e)
	{
		Win32Interop.ApplyTheme((nint)Hwnd, useDark: true);
	}

	private void WindowOnClosed(object sender, EventArgs e)
	{
		Application.Current.PlatformSettings.ColorValuesChanged -= OnPlatformColorValuesChanged;
		((TopLevel)_window).Closed -= WindowOnClosed;
	}

	[UnmanagedCallersOnly]
	private static nint WndProcStatic(nint hwnd, uint msg, nint wParam, nint lParam)
	{
		if (_appWindowRegistry.TryGetValue((HWND)hwnd, out var value))
		{
			return (nint)value.WndProc((HWND)hwnd, msg, (WPARAM)wParam, lParam);
		}
		return 0;
	}
}
