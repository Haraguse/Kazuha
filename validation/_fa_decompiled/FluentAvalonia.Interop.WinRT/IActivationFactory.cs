using System;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT;

internal interface IActivationFactory : IInspectable, IUnknown, IDisposable
{
	nint ActivateInstance();
}
