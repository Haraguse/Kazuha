using System;
using FluentAvalonia.Interop.Win32;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT;

internal interface ITaskbarList3 : ITaskbarList2, ITaskbarList, IUnknown, IDisposable
{
	void SetProgressValue(nint hwnd, ulong ullCompleted, ulong ullTotal);

	void SetProgressState(nint hwnd, int tbpFlags);

	void RegisterTab(nint hwndTab, nint hwndMDI);

	void UnregisterTab(nint hwndTab);

	void SetTabOrder(nint hwndTab, nint hwndInsertBefore);

	void SetTabActive(nint hwndTab, nint hwndMDI, int dwReserved);

	void ThumbBarAddButtons(nint hwnd, uint cButtons, int pButton);

	void ThumbBarUpdateButtons(nint hwnd, uint cButtons, int pButton);

	void ThumbBarSetImageList(nint hwnd, nint himl);

	unsafe void SetOverlayIcon(nint hwnd, void* hIcon, ushort* pszDescription);

	unsafe void SetThumbnailTooltip(nint hwnd, ushort* pszTip);

	unsafe void SetThumbnailClip(nint hwnd, RECT* prcClip);
}
