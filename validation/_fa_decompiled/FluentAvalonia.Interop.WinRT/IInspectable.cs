using System;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT;

internal interface IInspectable : IUnknown, IDisposable
{
	nint RuntimeClassName { get; }

	TrustLevel TrustLevel { get; }

	unsafe void GetIids(ulong* iidCount, Guid** iids);
}
