using System;
using Avalonia.Metadata;

namespace Avalonia.Threading;

[PrivateApi]
public interface IDispatcherImpl
{
	bool CurrentThreadIsLoopThread { get; }

	long Now { get; }

	event Action? Signaled;

	event Action? Timer;

	void Signal();

	void UpdateTimer(long? dueTimeInMs);
}
