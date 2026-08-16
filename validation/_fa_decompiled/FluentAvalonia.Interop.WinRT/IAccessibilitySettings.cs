using System;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT;

internal interface IAccessibilitySettings : IInspectable, IUnknown, IDisposable
{
	int HighContrast { get; }

	nint HighContrastScheme { get; }
}
