using System;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT;

internal interface IUISettings2 : IInspectable, IUnknown, IDisposable
{
	double TextScaleFactor { get; }
}
