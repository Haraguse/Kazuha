using System;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT;

internal interface IUISettings4 : IInspectable, IUnknown, IDisposable
{
	int AdvancedEffectsEnabled { get; }
}
