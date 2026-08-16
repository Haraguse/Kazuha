using System;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT;

internal interface IUISettings3 : IInspectable, IUnknown, IDisposable
{
	WinRTColor GetColorValue(UIColorType desiredColor);
}
