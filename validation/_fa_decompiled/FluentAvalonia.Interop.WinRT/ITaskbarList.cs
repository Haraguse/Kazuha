using System;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT;

internal interface ITaskbarList : IUnknown, IDisposable
{
	void HrInit();

	void AddTab(nint hwnd);

	void DeleteTab(nint hwnd);

	void ActivateTab(nint hwnd);

	void SetActiveAlt(nint hwnd);
}
