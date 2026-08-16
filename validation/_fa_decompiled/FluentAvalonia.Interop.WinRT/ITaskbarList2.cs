using System;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT;

internal interface ITaskbarList2 : ITaskbarList, IUnknown, IDisposable
{
	void MarkFullscreenWindow(nint hwnd, int fFullscreen);
}
