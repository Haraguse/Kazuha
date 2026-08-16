using System;
using Avalonia.Metadata;

namespace Avalonia.Threading;

[PrivateApi]
public interface IDispatcherImplWithExplicitBackgroundProcessing : IDispatcherImpl
{
	event Action? ReadyForBackgroundProcessing;

	void RequestBackgroundProcessing();
}
